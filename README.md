# TripFlow

API para organizar viagem em grupo: participantes, gastos divididos, checklist, roteiro, documentos, reservas e orçamento — tudo dentro de uma "viagem", com autenticação e controle de acesso por papel (dono/editor/visualizador) em cada uma.

Projeto de portfólio focado em backend: o objetivo não é só ter endpoints, é mostrar autenticação e autorização feitas a sério (JWT com rotação e detecção de reuso de refresh token, login com Google, senha com Argon2id, autorização por recurso, rate limiting) num domínio grande o suficiente pra parecer produto de verdade.

Além do CRUD básico, o TripFlow tem uma camada de produto que conecta os módulos entre si: uma timeline de atividades, fechamento social de dívidas ("marcar como pago" → confirmação), um indicador de prontidão da viagem que cruza roteiro/reservas/documentos/checklist, notificações configuráveis por tipo e presença em tempo real (quem mais está com a viagem aberta agora). Ver a seção [Produto](#produto) mais abaixo.

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
- **Eventos recebidos**: `ExpenseCreated`, `ExpenseDeleted`, `ChecklistItemCreated`, `ChecklistItemUpdated`, `ChecklistItemDeleted`, `ItineraryItemCreated`, `ItineraryItemUpdated`, `ItineraryItemDeleted`, `ReservationCreated`, `ReservationUpdated`, `ReservationDeleted`, `ParticipantsChanged`, `NotificationCreated`, `ActivityCreated`, `SettlementChanged`, `PresenceChanged`

Exemplo (cliente JS):

```js
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/trip", { accessTokenFactory: () => accessToken })
  .build();

connection.on("ExpenseCreated", (expense) => { /* atualiza a lista na tela */ });
connection.on("ChecklistItemUpdated", (item) => { /* atualiza o item marcado */ });
connection.on("PresenceChanged", (users) => { /* lista completa de quem esta na viagem agora */ });

await connection.start();
await connection.invoke("JoinTrip", tripId);
```

`PresenceChanged` não é persistido — `TripPresenceTracker` (singleton em memória na Api) rastreia quem entrou/saiu de cada grupo de viagem via `JoinTrip`/`LeaveTrip`/`OnDisconnectedAsync` e manda a lista completa toda vez que ela muda (mais simples pro cliente do que reconciliar um delta).

## Produto

Cada peça abaixo reaproveita a mesma infraestrutura (SignalR, `NotificationService`, o padrão de composição do `OverviewService`) em vez de introduzir camadas novas - a ideia foi ligar os módulos que já existiam, não empilhar mais um.

- **Timeline da viagem** (`ActivityLogEntry`) — cada ação relevante (gasto lançado, item do checklist atribuído, reserva criada, quitação confirmada...) vira uma entrada cronológica, gravada no mesmo ponto onde o service já chama `ITripNotifier`. `GET /api/trips/{tripId}/activity`, paginado, com o autor resolvido no momento (sobrevive à remoção do participante depois).
- **Fechar dívida — "Quitar"** (`SettlementRecord`) — o `SettlementCalculator` já dizia quem devia quanto; agora o devedor pode marcar uma transferência como paga (`POST .../settlement/mark-paid`) e o credor confirma (`POST .../settlement/{id}/confirm`). Uma quitação confirmada entra no cálculo de saldo como se fosse um gasto reverso — mesma lógica testada em `SettlementCalculatorTests`, sem reescrever nada. Sem gateway de pagamento real: é honestidade mútua, com notificação pros dois lados.
- **Prontidão da viagem** (`TripReadinessService`) — cruza `ItineraryItemType`, `ReservationType`, `DocumentCategory` e o progresso de `ChecklistItem`/`Budget` em regras explícitas (nada de IA/heurística vaga) pra responder "essa viagem está pronta?": tem roteiro? orçamento definido? checklist concluído? reserva de voo internacional com passaporte anexado? Cada pendência aponta pra aba certa do frontend.
- **Notificações expandidas + preferências por tipo** — além dos gatilhos originais (convite aceito, item de checklist atribuído), agora notifica gasto lançado, orçamento estourado, reserva criada/atualizada, item do roteiro atualizado, documento removido e os dois eventos de quitação — sempre pra quem não foi o autor da ação. Cada notificação carrega um `NotificationType`, e cada participante pode silenciar tipos específicos por viagem (`NotificationMute`, `GET`/`PUT /api/trips/{tripId}/notification-preferences`) sem afetar os outros participantes.
- **Reserva → Despesa em um clique** — o frontend pré-popula o formulário de gasto a partir de `Reservation.Price`/`Currency`, sem endpoint novo.
- **Retrospectiva enriquecida** (`TripMemory`) — além dos números agregados (total gasto, quem gastou mais, progresso do checklist), cada participante pode registrar seu próprio "melhor momento", uma nota de 1 a 5 e uma foto (`PUT /api/trips/{tripId}/retrospective/memory`, upsert). A retrospectiva mostra a média das notas do grupo.
- **Presença em tempo real** — ver quem mais está com a viagem aberta agora, via `TripPresenceTracker` (ver seção de SignalR acima).

## Swagger

Com a API rodando em ambiente `Development`, o Swagger UI fica em `/swagger` — é o jeito mais rápido de explorar e testar os endpoints sem precisar montar um Postman do zero. A documentação vem dos comentários `///` nos controllers (dá pra ver isso nos exemplos de 2FA e SignalR mais abaixo), então o Swagger tende a ficar atualizado junto do código em vez de ser uma coisa separada que alguém esquece de manter.

Os endpoints que exigem token (a maioria) pedem o header `Authorization: Bearer <token>` — pega o `accessToken` da resposta de `POST /api/auth/login` e usa o botão "Authorize" no topo da página do Swagger.

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
dotnet run --launch-profile https
```

Sobe em `https://localhost:7194` (e também `http://localhost:5299`) — é o perfil que o frontend espera por padrão (confira `VITE_API_BASE_URL` em `tripflow-web/.env.development`). Se preferir só HTTP, `dotnet run --launch-profile http` sobe só na porta 5299. As migrations pendentes são aplicadas automaticamente no startup, não precisa rodar `dotnet ef database update` à parte.

### Testes

```bash
dotnet test
```

Os testes de integração usam SQLite em memória (não precisam do Postgres rodando).

## Frontend

[`tripflow-web/`](tripflow-web/) — React + TypeScript + Vite + Tailwind consumindo essa API. Cobre login/registro/confirmação de e-mail/Google/2FA (setup e desligar pela interface), viagens (com capa — upload do dispositivo ou busca de foto na web), participantes, gastos com settlement e fechamento de dívida, orçamento, checklist com responsável/prazo, roteiro com geocoding/mapas/resumo do dia imprimível, reservas, documentos, notificações com preferências por tipo, retrospectiva enriquecida (melhor momento/nota/foto) e uma Overview com timeline de atividades + indicador de prontidão da viagem — praticamente tudo com atualização em tempo real via SignalR, incluindo toast discreto pra ação de outra pessoa e indicador de quem mais está na viagem agora. Tem identidade visual e sistema de design próprios (não é um template genérico de admin). Detalhes de como rodar e decisões de UI em [tripflow-web/README.md](tripflow-web/README.md).

## Roadmap

- [x] **Fase 1** — autenticação completa (JWT + refresh + Google), Viagem, Participantes, Gastos + Orçamento, Checklist
- [x] **Fase 2** — Roteiro, Mapa (geocoding via Nominatim), Documentos (upload validado + storage externo), Reservas vinculadas ao roteiro
- [x] **Fase 3** — tempo real (SignalR — gastos e checklist), 2FA (TOTP), capa de viagem (upload ou busca de foto), frontend cobrindo auth/viagens/participantes/gastos/checklist/roteiro+mapa
- [x] **Fase 4** — telas de Documentos, Reservas e Orçamento no frontend, login com Google e setup/desligar 2FA pela interface, notificações in-app
- [x] **Fase 5** — timeline de atividades, fechamento de dívida ("Quitar"), prontidão da viagem, notificações expandidas + preferências por tipo, reserva→despesa em um clique, retrospectiva enriquecida (melhor momento/nota/foto por participante), presença em tempo real, paginação em Documentos/Notificações, toast de tempo real, responsável/prazo do checklist na UI, resumo do dia imprimível

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

CORS: configurar `Cors__AllowedOrigins__0`, `__1`, etc. via secret com a URL de onde o frontend estiver hospedado (ex: a URL do Vercel/Netlify do `tripflow-web`).
