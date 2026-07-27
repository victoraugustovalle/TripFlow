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
- xUnit para testes

## Estrutura

```
TripFlow.Domain/          entidades e regras de negócio puras, sem dependência externa
TripFlow.Application/     casos de uso, DTOs, validators, interfaces (IFileStorageService, IEmailSender...)
TripFlow.Infrastructure/  EF Core (Postgres), hashing, JWT, storage, e-mail, Google auth
TripFlow.Api/             controllers, middlewares, Program.cs, Swagger
TripFlow.Tests/           testes de unidade e integração (xUnit)
```

## Rodando localmente

Pré-requisitos: .NET 10 SDK, um Postgres (local via Docker ou uma instância gratuita no Neon).

```bash
cd TripFlow.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=tripflow;Username=postgres;Password=..."
dotnet user-secrets set "Jwt:PrivateKey" "..."
dotnet run
```

Detalhes de configuração (chaves esperadas, como gerar o par RSA do JWT, credenciais do Google) ficam documentados aqui conforme cada peça for implementada.

## Roadmap

- [ ] **Fase 1** — autenticação completa, Viagem, Participantes, Gastos + Orçamento, Checklist
- [ ] **Fase 2** — Roteiro, Mapa (geocoding), Documentos (upload), Reservas
- [ ] **Fase 3** — tempo real (SignalR), 2FA, notificações, frontend

## Deploy

Pensado para hospedagem gratuita: Postgres no Neon, storage de arquivo no Cloudflare R2, API no Fly.io.
