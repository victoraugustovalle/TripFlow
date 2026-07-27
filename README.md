# TripFlow

API para organizar viagem em grupo: participantes, gastos divididos, checklist, roteiro, documentos, reservas e orçamento — tudo dentro de uma "viagem", com autenticação e controle de acesso por papel (dono/editor/visualizador) em cada uma.

Projeto de portfólio focado em backend: o objetivo não é só ter endpoints, é mostrar autenticação e autorização feitas a sério (JWT com rotação e detecção de reuso de refresh token, login com Google, senha com Argon2id, autorização por recurso, rate limiting) num domínio grande o suficiente pra parecer produto de verdade.

## Stack

- **.NET 10** — ASP.NET Core Web API (controllers)
- **PostgreSQL** + EF Core
- **Argon2id** para hash de senha, **JWT (RS256)** + refresh token rotativo com detecção de reuso
- **Google OAuth** como método alternativo de login
- **Serilog** (log estruturado em JSON)
- **FluentValidation** nas entradas
- xUnit (unidade + integração com `WebApplicationFactory` + SQLite em memória)

## Estrutura

```
TripFlow.Domain/          entidades e regras de negócio puras (inclui o SettlementCalculator - divisão de gastos)
TripFlow.Application/     casos de uso, DTOs, validators, interfaces (IPasswordHasher, ITokenService, IEmailSender...)
TripFlow.Infrastructure/  EF Core (Postgres), Argon2id, JWT, SMTP, Google auth
TripFlow.Api/             controllers, autenticação/autorização, rate limiting, Swagger, Program.cs
TripFlow.Tests/           testes de unidade e integração
```

## Segurança - o que tem implementado

- Senha com **Argon2id** (parâmetros de custo guardados junto do hash, dá pra endurecer depois sem invalidar senha antiga)
- **Access token JWT assinado RS256**, curto (15 min por padrão), enviado via header `Authorization: Bearer`
- **Refresh token** em cookie `HttpOnly` + `Secure` + `SameSite=Strict`, com **rotação a cada uso** e **detecção de reuso**: se um token já usado for apresentado de novo, a família inteira de tokens daquela sessão é revogada
- **Denylist por `jti`** para revogar um access token antes do vencimento natural (logout, reuso de refresh token detectado)
- **Login com Google** (valida o `id_token` via `Google.Apis.Auth`) como alternativa ao login por senha
- **Autorização por recurso**: além do papel global (Admin/User), cada participante tem um papel por viagem (Owner/Editor/Viewer), verificado via policy-based authorization
- **Rate limiting** diferenciado: login/refresh (alvo natural de brute force) em um limite mais apertado que registro/confirmação/reset
- Bloqueio de conta após tentativas de login falhas, verificação de e-mail, reset de senha com token de expiração curta
- Mitigação de user enumeration no login (tempo de resposta parecido exista ou não o e-mail)
- CORS por allow-list, HSTS, handler de exceção genérico em produção (não vaza stack trace)
- `X-Forwarded-For` processado corretamente atrás de proxy (Fly.io) — sem isso, rate limiting e o IP registrado no refresh token ficam errados
- Upload de documento valida o arquivo pelos **bytes de verdade** (magic number), não só pelo Content-Type declarado — um `.html` disfarçado de `.pdf` é rejeitado
- Download de documento sempre passa pela API (nunca expõe URL direta do bucket), então a autorização por papel na viagem é checada antes de qualquer byte sair
- Rate limit próprio pro geocoding, com chave **global** (não por usuário) — respeita o limite de uso do Nominatim (1 req/s pro serviço inteiro)

## Rodando localmente

Pré-requisitos: .NET 10 SDK, um Postgres (local via Docker ou uma instância gratuita no [Neon](https://neon.tech)).

```bash
cd TripFlow.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=tripflow;Username=postgres;Password=..."
```

O JWT usa um par de chaves RSA (não commitadas). Gerar um par novo:

```bash
openssl genrsa -out private.pem 2048
openssl rsa -in private.pem -pubout -out public.pem

cd TripFlow.Api
dotnet user-secrets set "Jwt:PrivateKeyPem" "$(cat ../private.pem)"
dotnet user-secrets set "Jwt:PublicKeyPem" "$(cat ../public.pem)"
rm ../private.pem ../public.pem
```

Sem SMTP configurado, e-mails (confirmação, convite, reset de senha) só são logados em vez de enviados — dá pra rodar e testar o fluxo sem precisar de credencial de e-mail:

```bash
dotnet user-secrets set "Smtp:Host" "smtp.exemplo.com"
dotnet user-secrets set "Smtp:User" "..."
dotnet user-secrets set "Smtp:Password" "..."
```

Login com Google (opcional):

```bash
dotnet user-secrets set "GoogleAuth:ClientId" "seu-client-id.apps.googleusercontent.com"
```

Storage de documentos (opcional): sem credencial de R2 configurada, os arquivos vão pra `TripFlow.Api/App_Data/uploads` (disco local, só pra dev — nunca use isso em produção, o volume some se o container for recriado):

```bash
dotnet user-secrets set "FileStorage:R2AccountId" "..."
dotnet user-secrets set "FileStorage:R2AccessKeyId" "..."
dotnet user-secrets set "FileStorage:R2SecretAccessKey" "..."
dotnet user-secrets set "FileStorage:R2BucketName" "tripflow-documents"
```

```bash
dotnet run
```

A API sobe com Swagger em `/swagger` (ambiente Development) e aplica as migrations pendentes automaticamente no startup.

### Testes

```bash
dotnet test
```

Os testes de integração usam SQLite em memória (não precisam do Postgres rodando).

## Roadmap

- [x] **Fase 1** — autenticação completa (JWT + refresh + Google), Viagem, Participantes, Gastos + Orçamento, Checklist
- [x] **Fase 2** — Roteiro, Mapa (geocoding via Nominatim), Documentos (upload validado + storage externo), Reservas vinculadas ao roteiro
- [ ] **Fase 3** — tempo real (SignalR), 2FA, notificações, frontend

## Deploy

Pensado para hospedagem gratuita:

- **Banco**: Postgres no [Neon](https://neon.tech)
- **API**: [Fly.io](https://fly.io) (`Dockerfile` + `fly.toml` já prontos)
- **Storage de arquivo** (documentos): Cloudflare R2 — crie um bucket em [dash.cloudflare.com](https://dash.cloudflare.com) → R2, gere um API token com permissão de leitura/escrita nesse bucket

```bash
fly launch --no-deploy   # usa o fly.toml existente, não sobrescreve
fly secrets set ConnectionStrings__Default="Host=...;Database=...;Username=...;Password=..."
fly secrets set Jwt__PrivateKeyPem="$(cat private.pem)"
fly secrets set Jwt__PublicKeyPem="$(cat public.pem)"
fly secrets set GoogleAuth__ClientId="..."
fly secrets set Smtp__Host="..." Smtp__User="..." Smtp__Password="..."
fly secrets set FileStorage__R2AccountId="..." FileStorage__R2AccessKeyId="..." FileStorage__R2SecretAccessKey="..." FileStorage__R2BucketName="tripflow-documents"
fly deploy
```

CORS: configurar `Cors__AllowedOrigins__0`, `__1`, etc. via secret com a URL do frontend quando ele existir.
