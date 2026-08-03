# Política de Segurança

## Escopo

O TripFlow lida com dados sensíveis de usuários e de suas viagens — senhas
(hash com Argon2id), tokens JWT e refresh tokens, tokens de OAuth do Google,
e informações pessoais de viagem (documentos, reservas, orçamento e gastos
divididos entre participantes). Autenticação e autorização são o foco
central do projeto, então falhas nessas áreas são tratadas como prioridade
máxima, mesmo sendo um projeto mantido por uma única pessoa.

## Versões suportadas

Este é um projeto com um único ambiente de produção. Apenas o código na
branch `main` recebe correções de segurança — não há versões antigas
mantidas em paralelo.

| Versão                 | Suportada          |
| ---------------------- | ------------------ |
| `main` (mais recente)  | :white_check_mark: |
| Commits anteriores     | :x:                 |

## Reportando uma vulnerabilidade

**Não abra uma issue pública para vulnerabilidades de segurança.**

Em vez disso, reporte diretamente para **victoraugusto3215@gmail.com** com:

- Descrição do problema e impacto potencial (ex.: bypass de autorização por
  papel em uma viagem, reuso de refresh token não detectado, exposição de
  documentos/reservas de outro usuário).
- Passos para reproduzir, se possível.
- Versão/commit afetado.

Você deve receber uma resposta inicial em até 72 horas. Se a vulnerabilidade
for confirmada, o objetivo é publicar uma correção antes de qualquer
divulgação pública, e você será creditado na descrição do fix (a menos que
prefira anonimato).

## Fora de escopo

- Ataques que dependam de acesso físico ao dispositivo do usuário ou de
  engenharia social contra participantes de uma viagem estão fora do escopo
  deste repositório.
- Vulnerabilidades em dependências de terceiros devem ser reportadas
  diretamente ao mantenedor daquele pacote; o Dependabot deste repositório
  cobre a atualização, não a triagem do upstream.
