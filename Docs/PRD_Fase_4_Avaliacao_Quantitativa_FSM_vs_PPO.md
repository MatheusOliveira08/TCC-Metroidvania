# PRD: Fase 4 - Avaliacao Quantitativa FSM vs PPO

## Problem Statement

A Fase 3D fechou o pipeline principal do TCC: o boss PPO foi treinado com Reward Shaping por Proveniencia, exportado como `BossAgent.onnx` e integrado ao Unity em modo inference. Ainda falta transformar esse resultado em evidencia quantitativa para a banca.

Hoje ja existe comparacao conceitual entre o Chefe FSM baseline e o Chefe PPO, mas ainda nao existe um fluxo padronizado para coletar metricas de partidas, exportar CSVs e produzir uma tabela comparativa objetiva.

Sem essa coleta, a conclusao do TCC ficaria dependente de percepcao visual ou relato manual. A Fase 4 precisa gerar dados reproduziveis, mesmo com uma amostra pequena, para sustentar a comparacao FSM vs IA.

## Solution

Implementar uma coleta quantitativa simples para sessoes de boss, exportando CSVs em `TreinamentoML/evaluation_data/`.

O desenvolvedor vai jogar ou executar 10 sessoes contra o Chefe FSM e 10 sessoes contra o Chefe PPO. Cada sessao deve registrar metricas essenciais: tipo do chefe, resultado, duracao da luta, dano recebido pelo boss, dano recebido pelo player quando disponivel, contagem de acoes relevantes e duracao do episodio.

Depois da coleta, os CSVs serao usados para montar uma tabela comparativa com medias e desvio padrao, alem de apoiar screenshots do TensorBoard, exemplos de grafo de proveniencia e videos curtos para apresentacao.

## User Stories

1. Como desenvolvedor do TCC, quero coletar metricas automaticamente por sessao, para nao depender de anotacoes manuais durante as partidas.

2. Como desenvolvedor do TCC, quero diferenciar sessoes `FSM` e `PPO`, para comparar o baseline com o agente treinado.

3. Como desenvolvedor do TCC, quero exportar CSVs em `TreinamentoML/evaluation_data/`, para manter os dados de avaliacao separados dos dados de treino.

4. Como desenvolvedor do TCC, quero registrar o resultado da sessao, para separar vitorias, derrotas e sessoes incompletas.

5. Como desenvolvedor do TCC, quero registrar a duracao da luta, para medir tempo medio de sobrevivencia e ritmo do combate.

6. Como desenvolvedor do TCC, quero registrar o dano total recebido pelo boss, para medir eficiencia ofensiva do jogador contra cada tipo de chefe.

7. Como desenvolvedor do TCC, quero registrar o dano total recebido pelo player quando o jogo emitir esse dado, para medir agressividade efetiva do chefe.

8. Como desenvolvedor do TCC, quero registrar contagens de `PlayerJump`, `PlayerAttack` e `PlayerDash`, para relacionar a avaliacao com as acoes usadas no filtro de proveniencia.

9. Como desenvolvedor do TCC, quero registrar contagens de `BossAttack`, para comparar agressividade entre FSM e PPO.

10. Como desenvolvedor do TCC, quero registrar contagens de acoes discretas do PPO quando estiver disponivel, para avaliar variedade de comportamento do agente.

11. Como desenvolvedor do TCC, quero que a coleta funcione sem alterar o pipeline de proveniencia existente, para evitar reabrir fases ja fechadas.

12. Como desenvolvedor do TCC, quero que a coleta use eventos ja existentes sempre que possivel, para reduzir risco de bugs novos.

13. Como desenvolvedor do TCC, quero que o CSV tenha cabecalho estavel, para poder importar os dados facilmente em planilha.

14. Como desenvolvedor do TCC, quero que cada linha represente uma sessao, para simplificar calculo de medias e desvios.

15. Como desenvolvedor do TCC, quero rodar 10 sessoes FSM e 10 sessoes PPO, para cumprir o marco da Fase 4 sem aumentar escopo.

16. Como desenvolvedor do TCC, quero poder apagar e regenerar os CSVs, para tratar esses arquivos como artefatos locais.

17. Como orientando, quero uma tabela comparativa FSM vs PPO, para apresentar a contribuicao do agente treinado com dados objetivos.

18. Como orientando, quero preservar screenshots do TensorBoard, para demonstrar convergencia do treinamento PPO.

19. Como orientando, quero gravar partidas curtas FSM e PPO, para mostrar diferenca comportamental na apresentacao.

20. Como orientando, quero selecionar 1 ou 2 grafos de proveniencia representativos, para conectar a avaliacao ao rastreamento causal.

21. Como avaliador do TCC, quero ver metricas quantitativas claras, para entender a diferenca entre o baseline FSM e a IA treinada.

22. Como avaliador do TCC, quero ver que a comparacao nao usa questionarios ou playtesters externos, para manter o escopo consistente com o Plano Mestre V4.

23. Como futuro mantenedor, quero que a coleta seja pequena e modular, para poder evoluir depois sem impactar o boss ou a proveniencia.

24. Como futuro mantenedor, quero que os nomes das metricas sejam explicitos, para evitar ambiguidade na analise.

25. Como futuro mantenedor, quero que a coleta tenha testes de contrato, para garantir que o CSV nao quebre silenciosamente.

## Implementation Decisions

- A Fase 4 e uma avaliacao quantitativa local conduzida pelo desenvolvedor. Nao havera playtesters externos, entrevistas ou questionarios.

- A coleta deve comparar dois tipos de chefe: `FSM` para o baseline e `PPO` para o agente treinado com `BossAgent.onnx`.

- A saida padrao dos dados de avaliacao sera `TreinamentoML/evaluation_data/`.

- Os CSVs de avaliacao sao artefatos locais/regeneraveis. Eles podem ser usados para analise e apresentacao, mas nao precisam ser commitados por padrao.

- A unidade principal de registro sera uma sessao de luta. Cada linha do CSV representa uma sessao finalizada.

- O coletor de metricas deve se integrar ao ciclo atual de inicio/fim de luta, usando o estado de sessao ja controlado pela arena.

- A coleta deve reaproveitar eventos existentes de combate e proveniencia quando possivel: dano no boss, morte do boss, acoes do player e ataques do boss.

- O CSV deve ter cabecalho fixo. Versao inicial sugerida:

```csv
session_id,boss_type,result,start_time_seconds,end_time_seconds,duration_seconds,episode_steps,boss_damage_taken,player_damage_taken,player_jump_count,player_attack_count,player_dash_count,boss_attack_count,ppo_idle_count,ppo_move_left_count,ppo_move_right_count,ppo_jump_count,ppo_attack_count,ppo_dash_count
```

- Antes da coleta oficial, `player_damage_taken` deve vir de um fluxo real de vida/dano no Elian. Ataques do boss devem poder causar derrota para que `result` diferencie `victory` e `defeat` sem depender de anotacao manual.

- Contagens `ppo_*` so devem ser preenchidas para o chefe PPO. Para sessoes FSM, elas podem ficar `0`.

- O modo de chefe pode ser configurado manualmente no Inspector para cada cena/sessao, evitando deteccao automatica complexa nesta fase.

- A coleta nao deve modificar o comportamento do boss. Ela apenas observa eventos e escreve dados ao final da sessao.

- O export deve criar o diretorio de saida se ele nao existir.

- O export deve escrever cabecalho quando o CSV ainda nao existir e apenas anexar novas linhas nas sessoes seguintes.

- A amostra oficial da Fase 4 sera de 20 sessoes: 10 contra FSM e 10 contra PPO.

- A analise final deve calcular pelo menos media e desvio padrao para duracao da luta, dano recebido pelo boss, dano recebido pelo player e contagens de acoes.

- TensorBoard deve ser usado apenas como evidencia complementar do treino PPO, nao como substituto dos CSVs de avaliacao.

- Videos curtos gravados via OBS entram como material de apresentacao, nao como fonte primaria de metricas.

## Proposed Delivery Slices

### Etapa 1: Coletor e CSV de Sessoes

Objetivo: criar o coletor de metricas de sessao e exportar uma linha CSV por luta finalizada.

Entregaveis:

- Componente de metricas acoplavel a cena de arena.
- Configuracao manual de `boss_type`.
- Contagem de duracao, dano no boss e acoes basicas.
- Escrita em `TreinamentoML/evaluation_data/`.
- Testes de contrato do formato CSV.

Criterios de aceite:

- Uma sessao finalizada gera exatamente uma linha de CSV.
- O cabecalho e escrito uma unica vez.
- O diretorio de saida e criado automaticamente.
- Campos numericos usam formato consistente para planilha.

Commit sugerido:

```bash
feat: coletando metricas de avaliacao do boss
```

### Etapa 2: Integracao com FSM e PPO

Objetivo: garantir que as cenas/prefabs permitam coletar sessoes FSM e PPO sem alterar a logica do boss.

Entregaveis:

- Coletor ligado na arena FSM.
- Coletor ligado na arena PPO.
- Configuracao clara de `boss_type` para cada contexto.
- Smoke manual de uma sessao FSM e uma sessao PPO gerando CSV.

Criterios de aceite:

- CSV diferencia `FSM` e `PPO`.
- `Boss_PPO.prefab` permanece em `InferenceOnly`.
- A coleta nao exige Python rodando.
- Nenhum artefato temporario de ML-Agents entra no commit.

Commit sugerido:

```bash
feat: integrando avaliacao nas arenas do boss
```

### Etapa 2.5: Preflight da Coleta Oficial

Objetivo: garantir que a coleta oficial da Fase 4 seja jogavel, comparavel e metodologicamente valida antes de registrar as 20 sessoes finais.

Entregaveis:

- Cena PPO de avaliacao jogavel pelo desenvolvedor, sem depender de `PlayerDummy_Training`.
- FSM e PPO avaliados com player controlavel e condicoes equivalentes de arena.
- `GameMetrics` registrando acoes discretas reais do `BossAgent` PPO.
- Smoke manual de uma sessao FSM e uma sessao PPO antes da coleta oficial.
- Polimento visual minimo e passivo, apenas quando nao alterar fisica, IA, camera, hitbox, posicoes de spawn ou metricas.

Criterios de aceite:

- A cena PPO oficial contem `PlayerController` e `PlayerCombat` ativos.
- A cena PPO oficial nao usa `PlayerDummyAI` como fonte da coleta oficial.
- As colunas `ppo_*` do CSV sao preenchidas a partir das acoes reais do `BossAgent`.
- `boss_attack_count` tambem registra ataques tentados pelo PPO.
- `Boss_PPO.prefab` permanece em `InferenceOnly`.
- Qualquer melhoria visual adicionada nao possui collider novo nem altera comportamento de jogo.
- MSBuild e EditMode passam antes da coleta oficial.

Commit sugerido, se aprovado:

```bash
feat: preparando coleta oficial da fase 4
```

### Etapa 2.6: Condicao de Derrota do Player

Objetivo: garantir que a coleta oficial tenha condicao real de derrota, dano recebido pelo player e resultados comparaveis entre FSM e PPO.

Entregaveis:

- `PlayerHealth` no Elian das cenas oficiais FSM e PPO.
- Ataques do boss causando dano real no Elian quando estiverem dentro do alcance.
- Mesmo dano, alcance e cooldown efetivo de dano para FSM e PPO.
- `ArenaManager` finalizando a sessao como `defeat` quando o Elian morrer.
- `GameMetrics` registrando `player_damage_taken` a partir do dano real recebido pelo Elian.
- `ProvenanceLogger` registrando eventos `PlayerDamageTaken` quando o Elian receber dano.
- Smoke manual confirmando uma derrota possivel sem alterar IA, reward, modelo PPO ou logica decisoria do FSM.

Criterios de aceite:

- O Elian possui vida configurada nas duas cenas oficiais.
- Ataques do FSM e acoes `Attack` do PPO aplicam dano apenas quando o Elian esta dentro do alcance configurado.
- O dano do boss respeita cooldown para evitar morte instantanea por spam de ataque do PPO.
- A morte do Elian encerra a sessao com `result=defeat`.
- O CSV preenche `player_damage_taken` com valor maior que zero quando o Elian recebe dano.
- A implementacao nao altera politica PPO, reward shaping, observacoes, velocidades, camera, spawns ou comportamento decisorio do FSM.
- MSBuild e EditMode passam antes da coleta oficial.

Commit sugerido, se aprovado:

```bash
feat: adicionando derrota do player na avaliacao
```

### Etapa 3: Coleta Oficial da Amostra

Objetivo: coletar os dados oficiais da Fase 4.

Entregaveis:

- 10 sessoes FSM registradas.
- 10 sessoes PPO registradas.
- CSV final em `TreinamentoML/evaluation_data/`.
- Observacoes breves sobre qualquer sessao descartada ou repetida.

Criterios de aceite:

- Existem 20 linhas validas de sessao.
- Cada linha tem `session_id`, `boss_type`, `result` e `duration_seconds` preenchidos.
- A amostra contem exatamente 10 sessoes `FSM` e 10 sessoes `PPO`.

Commit sugerido:

```bash
chore: coletando amostra quantitativa da fase 4
```

### Etapa 4: Analise e Evidencias

Objetivo: transformar os CSVs em material de resultado para o TCC.

Entregaveis:

- Tabela comparativa com media e desvio padrao.
- Screenshots do TensorBoard do run `boss_v1_500k`.
- 1 ou 2 grafos de proveniencia representativos.
- Videos curtos FSM e PPO para apresentacao.

Criterios de aceite:

- A tabela permite comparar FSM vs PPO sem abrir o Unity.
- As metricas principais estao calculadas para os dois grupos.
- Evidencias visuais ficam separadas dos dados brutos.

Commit sugerido:

```bash
docs: registrando resultados quantitativos da fase 4
```

## Testing Decisions

- Os testes devem validar comportamento externo do coletor, nao detalhes internos.

- O principal contrato testavel e: dado um resumo de sessao, o exportador gera CSV com cabecalho correto e linha de dados correta.

- O teste deve cobrir append em arquivo existente, garantindo que o cabecalho nao duplique.

- O teste deve cobrir criacao automatica do diretorio de saida.

- O teste deve cobrir formatacao de valores numericos para evitar CSV instavel entre culturas de sistema.

- Testes permanentes fazem sentido para o exportador CSV, porque o formato vira insumo direto da analise do TCC.

- Testes manuais continuam necessarios para confirmar que as cenas FSM e PPO disparam fim de sessao e que as linhas aparecem no CSV.

- MSBuild deve passar depois da implementacao.

- EditMode deve ser rodado quando a Unity nao estiver aberta no mesmo projeto.

- `git diff --check` deve passar antes de cada commit.

## Out of Scope

- Treinar novamente o PPO.

- Alterar hiperparametros do PPO.

- Usar GAIL ou qualquer novo algoritmo de aprendizado.

- Criar dashboard visual dentro da Unity.

- Criar UI completa para escolher FSM/PPO.

- Automatizar 20 partidas sem intervencao humana.

- Implementar questionarios, entrevistas ou playtests externos.

- Fazer analise estatistica avancada alem de media e desvio padrao.

- Melhorar visual do boss com animacoes, VFX, camera dinamica ou elementos com collider novo. Polimento visual passivo e minimo e permitido apenas na Etapa 2.5, desde que nao altere fisica, IA, camera, hitbox, posicoes de spawn ou metricas.

- Reabrir o PRD de dash/filtro de sequencias vitoriosas.

## Further Notes

- Esta fase e a ponte entre a contribuicao tecnica e a apresentacao do TCC.

- O dado mais importante nao e provar que o PPO e "melhor" em todos os sentidos, mas demonstrar uma diferenca mensuravel entre FSM baseline e IA treinada com recompensa informada por proveniencia.

- Se alguma metrica ficar zerada por falta de evento real no jogo, ela deve ser registrada como limitacao da implementacao, nao mascarada manualmente.

- A comparacao deve ser honesta: se o PPO se mover pouco, repetir padroes ou tiver performance parecida com FSM, isso ainda e um resultado valido para discutir convergencia, escopo e proximos passos.

- Antes da coleta oficial, executar pelo menos uma sessao smoke FSM e uma sessao smoke PPO para validar o CSV.
