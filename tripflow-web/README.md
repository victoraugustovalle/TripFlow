# TripFlow Web

Frontend do [TripFlow](../README.md) — React + TypeScript + Vite + Tailwind, consumindo a API .NET no mesmo repositório.

## Stack

- **React 19** + **TypeScript** + **Vite**
- **Tailwind CSS 4** (via `@tailwindcss/vite`, sem arquivo de config separado — os tokens de tema ficam direto em `src/index.css`)
- **TanStack Query** para estado de servidor (cache, refetch, invalidação)
- **React Router** para rotas
- **react-hook-form + zod** nos formulários
- **zustand** para a sessão de autenticação (token em memória, não em `localStorage`) e pro sistema de toast
- **@microsoft/signalr** para as atualizações em tempo real
- **Leaflet + react-leaflet** para os mapas do roteiro (tiles do OpenStreetMap)

Sem biblioteca de ícones, sem framer-motion, sem UI kit pronto — os componentes (`Button`, `Card`, `Modal`, `Badge`, `Avatar`, `Toaster`, `Skeleton`...) são todos escritos à mão em `src/components/`. Foi opção, não falta de tempo: dá mais controle sobre a identidade visual do que compor em cima de um kit genérico, e o app é pequeno o suficiente pra isso não virar manutenção.

## Como a autenticação funciona aqui

O access token JWT só vive em memória (nunca em `localStorage`/`sessionStorage`, para reduzir a superfície de um XSS). Isso significa que ele some a cada F5 — por isso, no boot do app (`AuthBootstrap`), a primeira coisa que acontece é tentar trocar o cookie `httpOnly` de refresh (que o backend já configura) por um access token novo, via `POST /api/auth/refresh` com `credentials: "include"`.

O cliente de API (`src/api/client.ts`) também intercepta qualquer `401` de uma chamada autenticada, tenta um refresh e repete a chamada original uma vez — com um lock (`refreshInFlight`) para nunca disparar dois refreshes ao mesmo tempo (o refresh token *rotaciona* a cada uso no backend; duas chamadas concorrentes fariam a segunda ser tratada como reuso e derrubaria a sessão inteira).

## Rodando localmente

Pré-requisitos: Node 20+, a API do TripFlow rodando (veja o [README principal](../README.md) — o jeito mais direto é `dotnet run --launch-profile https` lá em `TripFlow.Api`).

```bash
npm install
npm run dev
```

Sobe em `http://localhost:5173`. Confira `.env.development` — `VITE_API_BASE_URL` precisa apontar pra onde a API está rodando (`https://localhost:7194` por padrão). Do lado da API, `Cors:AllowedOrigins` precisa incluir a origem do frontend (já vem configurado em `TripFlow.Api/appsettings.Development.json`).

### Build

```bash
npm run build   # tsc -b && vite build
npm run lint    # oxlint
```

## Estrutura

```
src/api/            chamadas HTTP pra API (uma por domínio) + client.ts (fetch com refresh automático)
src/auth/           login, registro, confirmação de e-mail, verificação de 2FA, layout de autenticação
src/trips/          lista de viagens, criar viagem, detalhe da viagem (abas, capa, editar nome, apagar)
src/participants/   painel de participantes
src/expenses/       painel de gastos + settlement ("quem deve pra quem")
src/checklist/      painel de checklist
src/itinerary/      painel de roteiro, mapas (item e do dia), geocoding, cálculo de rota
src/components/     componentes de UI compartilhados (Button, Card, Modal, Badge, Avatar, Toaster, Skeleton...)
src/toast/          store do sistema de toast (zustand)
src/layouts/        layout autenticado (header) e guarda de rota protegida
src/realtime/       hook do SignalR
src/utils/          formatação (moeda, data) e labels dos enums vindos da API
```

## O que já dá pra fazer

- **Login, registro, confirmação de e-mail e verificação de 2FA** (o *setup* do 2FA — ligar/desligar — ainda não tem tela; a API já suporta, ver README principal)
- **Viagens**: criar, editar o nome direto no lugar (clique no lápis ao lado do título), apagar (com confirmação por nome digitado, não é só um `confirm()` do navegador), e **definir uma capa** — enviando uma foto do próprio dispositivo (redimensionada e comprimida no navegador antes de enviar) ou buscando uma foto livre de direitos autorais direto de dentro do app, via [Openverse](https://openverse.org)
- **Participantes**: convidar por e-mail, definir papel (visualizador/editor/dono), remover, com avatar de iniciais e status colorido (convidado/aceito/recusado)
- **Gastos**: lançar, divisão automática igualitária entre os participantes aceitos, e a tela de "quem deve pra quem" destaca em cor o que diz respeito a você
- **Checklist**: colaborativo, com atualização em tempo real quando outra pessoa marca ou adiciona um item
- **Roteiro**: itens por tipo (atividade/transporte/hospedagem/refeição/outro), busca de endereço com autocomplete (geocoding via Nominatim, com debounce por causa do rate limit da API), mapa por item e um mapa do dia inteiro com a rota calculada entre os pontos (OSRM) — inclusive foto do local puxada da Wikipedia quando abre o marcador no mapa
- **Tempo real via SignalR** pra gastos e checklist — quem estiver com a mesma viagem aberta vê a atualização sem dar F5
- **Feedback de toda ação** (toast de sucesso, confirmação por nome pra apagar, skeleton de carregamento no lugar de spinner central pra não pular o layout)

Ainda não tem: login com Google, ligar/desligar 2FA pela interface (só a verificação no login), e telas de documentos, reservas e orçamento (a API já suporta as três — é questão de construir a tela).

## Identidade visual

O app se chama "Voo" por dentro — mascote ilustrado à mão (`components/Mascot.tsx`), motivo gráfico próprio (a trilha pontilhada com aviãozinho de papel, `components/FlightTrail.tsx`), paleta quente e proposital (teal + coral + creme + navy, com âmbar pra estado de atenção) definida em `src/index.css`. Não é template — cada escolha de cor tem um motivo documentado ali no próprio `@theme`.

Componentes de estado semântico (`Badge`) e feedback (`Toaster`) seguem o mesmo sistema de tokens em todo canto, incluindo dentro dos mapas do Leaflet, onde cor teria que ser feita via hex direto no JS em vez de classe Tailwind — é fácil esquecer esse canto e acabar com uma cor genérica solta ali.
