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
- Hub do SignalR exige o mesmo JWT dos endpoints REST; entrar no grupo de uma viagem checa participação aceita (não dá pra escutar atualização de viagem que você não faz parte)
- **2FA (TOTP)**: segredo criptografado em repouso com AES-256-GCM (não hash — precisa voltar em claro pra calcular o código), login com Google também respeita o 2FA (não é um jeito de pular o segundo fator), desligar exige senha + código, códigos de recuperação de uso único pra quem perde o celular

## 2FA (TOTP)

Segunda camada de login via app autenticador (Google Authenticator, Authy, 1Password, etc).

```
POST /api/auth/2fa/setup            [autenticado] gera o segredo (2FA ainda desligado) + QR code (PNG base64) + otpauth:// URI
POST /api/auth/2fa/enable           [autenticado] { code } - confirma com um código do app e liga o 2FA; devolve 8 códigos de recuperação (só aparecem essa vez)
POST /api/auth/2fa/disable          [autenticado] { password, code } - desliga
POST /api/auth/2fa/verify           [público, rate limit apertado] { email, challengeToken, code | recoveryCode } - segundo passo do login
```

Fluxo de login com 2FA ligado:

1. `POST /api/auth/login` com e-mail+senha → em vez do access token, devolve `{ requiresTwoFactor: true, twoFactorChallengeToken: "..." }`
2. `POST /api/auth/2fa/verify` com esse token + o código de 6 dígitos do app (ou um código de recuperação) → devolve o access token normalmente

O mesmo desafio vale pro login com Google (`/api/auth/google`) — se a conta tem 2FA, entrar pelo Google também para no passo 2, não é uma forma de contornar.

## Tempo real (SignalR)

Quem estiver com uma viagem aberta recebe atualização ao vivo quando alguém adiciona um gasto ou marca um item do checklist, sem precisar dar F5.

- **Hub**: `/hubs/trip`
- **Autenticação**: o token JWT vai via `accessTokenFactory` do cliente SignalR (WebSocket não permite header customizado no handshake do navegador, então o token vai por query string só nesse path)
- **Uso**: depois de conectar, chama `JoinTrip(tripId)` — o servidor confere se você é participante aceito daquela viagem antes de te colocar no grupo; se não for, a chamada lança `HubException`
- **Eventos recebidos**: `ExpenseCreated`, `ExpenseDeleted`, `ChecklistItemCreated`, `ChecklistItemUpdated`, `ChecklistItemDeleted`

Exemplo (cliente JS):

```js
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/trip", { accessTokenFactory: () => accessToken })
  .build();

connection.on("ExpenseCreated", (expense) => { /* atualiza a lista na tela */ });
connection.on("ChecklistItemUpdated", (item) => { /* atualiza o item marcado */ });

await connection.start();
await connection.invoke("JoinTrip", tripId);
```

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

2FA: precisa de uma chave AES-256 (32 bytes) pra criptografar o segredo TOTP em repouso:

```bash
dotnet user-secrets set "TwoFactor:EncryptionKeyBase64" "$(openssl rand -base64 32)"
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

## Frontend

[`tripflow-web/`](tripflow-web/) — React + TypeScript + Vite + Tailwind consumindo essa API. Cobre login/registro/2FA, viagens, participantes, gastos com settlement e checklist com atualização em tempo real. Detalhes de como rodar em [tripflow-web/README.md](tripflow-web/README.md).

## Roadmap

- [x] **Fase 1** — autenticação completa (JWT + refresh + Google), Viagem, Participantes, Gastos + Orçamento, Checklist
- [x] **Fase 2** — Roteiro, Mapa (geocoding via Nominatim), Documentos (upload validado + storage externo), Reservas vinculadas ao roteiro
- [ ] **Fase 3** — [x] tempo real (SignalR — gastos e checklist), [x] 2FA (TOTP), [x] frontend (MVP), notificações, resto das telas (roteiro/mapa/documentos/reservas)

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
fly secrets set TwoFactor__EncryptionKeyBase64="$(openssl rand -base64 32)"
fly deploy
```

CORS: configurar `Cors__AllowedOrigins__0`, `__1`, etc. via secret com a URL do frontend quando ele existir.
