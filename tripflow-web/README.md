# TripFlow Web

Frontend do [TripFlow](../README.md) — React + TypeScript + Vite + Tailwind, consumindo a API .NET no mesmo repositório.

## Stack

- **React 19** + **TypeScript** + **Vite**
- **Tailwind CSS 4** (via `@tailwindcss/vite`, sem arquivo de config separado)
- **TanStack Query** para estado de servidor (cache, refetch, invalidação)
- **React Router** para rotas
- **react-hook-form + zod** nos formulários
- **zustand** para a sessão de autenticação (token em memória, não em `localStorage`)
- **@microsoft/signalr** para as atualizações em tempo real

## Como a autenticação funciona aqui

O access token JWT só vive em memória (nunca em `localStorage`/`sessionStorage`, para reduzir a superfície de um XSS). Isso significa que ele some a cada F5 — por isso, no boot do app (`AuthBootstrap`), a primeira coisa que acontece é tentar trocar o cookie `httpOnly` de refresh (que o backend já configura) por um access token novo, via `POST /api/auth/refresh` com `credentials: "include"`.

O cliente de API (`src/api/client.ts`) também intercepta qualquer `401` de uma chamada autenticada, tenta um refresh e repete a chamada original uma vez — com um lock (`refreshInFlight`) para nunca disparar dois refreshes ao mesmo tempo (o refresh token *rotaciona* a cada uso no backend; duas chamadas concorrentes fariam a segunda ser tratada como reuso e derrubaria a sessão inteira).

## Rodando localmente

Pré-requisitos: Node 20+, a API do TripFlow rodando (veja o [README principal](../README.md)).

```bash
npm install
npm run dev
```

Sobe em `http://localhost:5173`. Confira `.env.development` — `VITE_API_BASE_URL` precisa apontar para onde a API está rodando (`http://localhost:5299` por padrão). No lado da API, `Cors:AllowedOrigins` precisa incluir a origem do frontend (já configurado em `TripFlow.Api/appsettings.Development.json`).

### Build

```bash
npm run build   # tsc -b && vite build
npm run lint    # oxlint
```

## Escopo atual (v1)

Login/registro/2FA, viagens, participantes (convidar/aceitar), gastos com cálculo de quem-deve-pra-quem, e checklist com atualização em tempo real via SignalR quando outra pessoa mexe na mesma viagem.

Ainda não tem: roteiro, mapa, upload de documentos, reservas, login com Google (a API já suporta tudo isso — é questão de construir a tela).
