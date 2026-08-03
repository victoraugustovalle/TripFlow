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
src/auth/           login, registro, confirmação de e-mail, login Google, verificação/setup de 2FA, layout de autenticação
src/trips/          lista de viagens, criar viagem, detalhe da viagem (abas, capa, editar nome, apagar, presença online)
src/overview/       visão geral: timeline de atividades, prontidão da viagem, orçamento, saldo, checklist, próximo do roteiro
src/activity/       timeline de atividades da viagem (componente + hook)
src/participants/   painel de participantes
src/expenses/       painel de gastos, settlement ("quem deve pra quem") com fechamento de dívida, rascunho de gasto vindo de reserva
src/budgets/        seção de orçamento por categoria (embutida no painel de gastos)
src/checklist/      painel de checklist, com responsável e prazo
src/itinerary/      painel de roteiro, mapas (item e do dia), geocoding, cálculo de rota, resumo do dia imprimível
src/reservations/   formulário e card de reserva
src/documents/      painel de documentos (upload, download, paginado)
src/notifications/  sino de notificações (paginado) e modal de preferências por tipo
src/retrospective/  retrospectiva pós-viagem: números agregados + memórias por participante
src/components/     componentes de UI compartilhados (Button, Card, Modal, Badge, Avatar, Toaster, Skeleton...)
src/toast/          store do sistema de toast (zustand)
src/layouts/        layout autenticado (header) e guarda de rota protegida
src/realtime/       hook do SignalR (invalidação de queries, toast de atividade, presença online)
src/utils/          formatação (moeda, data) e labels dos enums vindos da API
```

## O que já dá pra fazer

- **Login, registro, confirmação de e-mail, login com Google e 2FA completo** (setup, ligar/desligar e verificação no login, tudo pela interface)
- **Viagens**: criar, editar o nome direto no lugar (clique no lápis ao lado do título), apagar (com confirmação por nome digitado, não é só um `confirm()` do navegador), e **definir uma capa** — enviando uma foto do próprio dispositivo (redimensionada e comprimida no navegador antes de enviar) ou buscando uma foto livre de direitos autorais direto de dentro do app, via [Openverse](https://openverse.org)
- **Overview como página inicial de verdade**: timeline cronológica de tudo que aconteceu na viagem, indicador de "prontidão" (% da viagem pronta, com lista de pendências que linkam pra aba certa), orçamento, saldo e próximos itens do roteiro
- **Participantes**: convidar por e-mail, definir papel (visualizador/editor/dono), remover, com avatar de iniciais e status colorido (convidado/aceito/recusado), e ver quem mais está com a viagem aberta agora
- **Gastos**: lançar, divisão automática igualitária ou customizada, "quem deve pra quem" com **fechamento de dívida** (marcar como pago → a outra ponta confirma), lançar gasto direto a partir de uma reserva com um clique
- **Orçamento**: planejado vs. gasto real por categoria, com barra de progresso
- **Checklist**: colaborativo, com responsável e prazo (destaque quando atrasado), atualização em tempo real quando outra pessoa marca ou adiciona um item
- **Roteiro**: itens por tipo (atividade/transporte/hospedagem/refeição/outro), busca de endereço com autocomplete (geocoding via Nominatim, com debounce por causa do rate limit da API), mapa por item e um mapa do dia inteiro com a rota calculada entre os pontos (OSRM) — inclusive foto do local puxada da Wikipedia quando abre o marcador no mapa — e um resumo do dia pronto pra imprimir
- **Reservas** (voo/hotel/carro/outro), vinculadas opcionalmente a um item do roteiro
- **Documentos**: upload validado, download, paginado
- **Notificações**: sino com contagem de não lidas, paginado, e preferências por tipo (silenciar categorias específicas por viagem)
- **Retrospectiva**: quando a viagem é marcada como concluída, a Overview vira retrospectiva — total gasto, quem gastou mais, progresso do checklist, e memórias que cada participante registra (melhor momento, nota de 1 a 5, foto)
- **Tempo real via SignalR** em praticamente tudo, com toast discreto quando é ação de outra pessoa e indicador de quem mais está online na viagem agora
- **Feedback de toda ação** (toast de sucesso, confirmação por nome pra apagar, skeleton de carregamento no lugar de spinner central pra não pular o layout)

## Identidade visual

O app se chama "Voo" por dentro — mascote ilustrado à mão (`components/Mascot.tsx`), motivo gráfico próprio (a trilha pontilhada com aviãozinho de papel, `components/FlightTrail.tsx`), paleta quente e proposital (teal + coral + creme + navy, com âmbar pra estado de atenção) definida em `src/index.css`. Não é template — cada escolha de cor tem um motivo documentado ali no próprio `@theme`.

Componentes de estado semântico (`Badge`) e feedback (`Toaster`) seguem o mesmo sistema de tokens em todo canto, incluindo dentro dos mapas do Leaflet, onde cor teria que ser feita via hex direto no JS em vez de classe Tailwind — é fácil esquecer esse canto e acabar com uma cor genérica solta ali.
