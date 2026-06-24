# Resumo Completo do Projeto Chronicles Of The Lost Word

## Finalidade Deste Arquivo

Este documento foi criado para contextualizar outras IAs ou colaboradores que venham ajudar na escrita do TCC, na organização dos resultados ou na preparação da apresentação. Ele reúne o que foi feito, por que foi feito, quais decisões mudaram ao longo do caminho, quais arquivos são importantes, quais evidências foram geradas e quais cuidados metodológicos devem ser preservados.

O objetivo não é substituir o TCC final. Este arquivo funciona como uma memória técnica e acadêmica do desenvolvimento do vertical slice, para que uma IA externa não precise redescobrir o projeto do zero nem faça inferências erradas sobre o pipeline.

## Resumo Executivo

O projeto é um vertical slice 2D em Unity chamado, no contexto do TCC, de Chronicles Of The Lost Word. A proposta central foi construir uma arena de chefe com dois comportamentos comparáveis: um chefe baseline baseado em máquina de estados finitos (FSM) e um chefe treinado com aprendizado por reforço via PPO, usando Unity ML-Agents.

A contribuição acadêmica principal não é apenas ter treinado um chefe com PPO. A contribuição está na integração entre proveniência e aprendizado por reforço: sessões jogadas contra o chefe FSM geraram grafos de proveniência com cadeias causais de eventos; desses grafos foram extraídas sequências de ações associadas a vitórias; essas sequências foram utilizadas como Reward Shaping durante o treinamento PPO. Assim, o agente não imitou diretamente um arquivo `.demo` nem usou GAIL. Ele explorou o ambiente via PPO, mas recebeu recompensa adicional quando executava padrões extraídos de ações vitoriosas registradas por proveniência.

O pipeline final seguiu esta lógica:

1. O desenvolvedor joga como Elian contra o chefe FSM.
2. O sistema registra eventos de jogo em grafos de proveniência.
3. Um script Python filtra sessões vitoriosas e extrai sequências de 3 ações associadas a dano no chefe.
4. O `ProvenanceRewardShaper` carrega essas sequências e calcula recompensa adicional.
5. O `BossAgent` é treinado com PPO usando ML-Agents e Reward Shaping por Proveniência.
6. O modelo final é exportado como `BossAgent.onnx` e integrado ao Unity em modo `InferenceOnly`.
7. O desenvolvedor joga 10 sessões contra FSM e 10 contra PPO.
8. O `GameMetrics` exporta um CSV quantitativo.
9. A comparação FSM vs PPO é analisada em relatório e complementada por evidências visuais.

## Estado Atual do Projeto

O projeto chegou ao ponto em que a comparação quantitativa principal já foi executada e documentada. A amostra oficial usada no relatório possui 20 sessões: 10 contra FSM e 10 contra PPO. O relatório acadêmico da comparação está em `Docs/evidencias/comparation/Resultados_Avaliacao_Quantitativa_FSM_vs_PPO.md`.

Também foram selecionadas evidências complementares em `Docs/evidencias/`, incluindo vídeos curtos, imagens do TensorBoard e um JSON de grafo de proveniência representativo. O arquivo `Docs/evidencias/Evidencias_Selecionadas.md` documenta como essas evidências devem ser usadas no texto e nos slides.

Há um detalhe importante: depois da coleta oficial, foram feitas gravações e/ou execuções adicionais para gerar evidências. Por isso o CSV local `TreinamentoML/evaluation_data/boss_evaluation_metrics.csv` pode conter mais de 20 linhas. No momento em que este resumo foi escrito, ele continha 22 linhas. A análise oficial do relatório considera as primeiras 20 linhas, sendo 10 FSM e 10 PPO. As linhas 21 e 22 vieram depois e não fazem parte da amostra oficial.

## Estado do Git e Artefatos Locais

Últimos commits relevantes no histórico:

| Commit    | Mensagem                                               | Papel                                                                                                                   |
| --------- | ------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------- |
| `a5b117f` | `docs: registrando comparacao quantitativa fsm vs ppo` | Registrou o relatório acadêmico inicial da comparação. Depois o arquivo foi movido para `Docs/evidencias/comparation/`. |
| `7df0486` | `feat: adicionando derrota do player na avaliacao`     | Adicionou vida real do Elian, dano do boss e condição de derrota na avaliação.                                          |
| `11a0c8f` | `feat: preparando coleta oficial da fase 4`            | Preparou as cenas para coleta oficial jogável FSM vs PPO.                                                               |
| `24a0845` | `fix: resolvendo caminho relativo das metricas`        | Corrigiu caminho relativo do CSV de métricas.                                                                           |
| `353470f` | `feat: integrando avaliacao nas arenas do boss`        | Ligou o coletor de métricas nas arenas.                                                                                 |
| `6e071f8` | `feat: coletando metricas de avaliacao do boss`        | Criou o sistema de métricas e exportação CSV.                                                                           |
| `8680758` | `feat: integrando modelo PPO final do boss`            | Integrou o `BossAgent.onnx` final.                                                                                      |
| `89ea54b` | `feat: configurando treino PPO do boss`                | Configurou treinamento PPO.                                                                                             |
| `c57b580` | `feat: adicionando boss agent com ml-agents`           | Criou o `BossAgent`.                                                                                                    |
| `fde0ffe` | `feat: adicionando reward shaping por proveniencia`    | Implementou Reward Shaping por Proveniência.                                                                            |
| `12a28c5` | `feat: adicionando dash e sequencias vitoriosas`       | Adicionou dash e extração de sequências.                                                                                |

No estado atual, podem existir arquivos locais não commitados, especialmente em `Docs/evidencias/`, `TreinamentoML/evaluation_data/` e artefatos gerados automaticamente pela Unity/ML-Agents. O CSV bruto e os JSONs de proveniência são dados locais/regeneráveis. Não devem ser commitados por padrão, a menos que o orientando decida explicitamente versionar evidências finais.

Artefatos que normalmente devem ficar fora do commit:

| Caminho                                     | Motivo                                                             |
| ------------------------------------------- | ------------------------------------------------------------------ |
| `TreinamentoML/evaluation_data/`            | CSV bruto local da avaliação. Fonte dos cálculos, mas regenerável. |
| `TreinamentoML/provenance_data/`            | JSONs brutos de proveniência gerados em runtime.                   |
| `ChroniclesOfTheLostWord/Assets/ML-Agents/` | Pasta gerada por Play/Test Runner/ML-Agents, incluindo timers.     |
| `ProjectSettings/TimeManager.asset`         | Frequentemente reserializado pela Unity sem mudança intencional.   |
| `ProjectSettings/ProjectSettings.asset`     | Pode receber flags de analytics/Sentis sem relação com a tarefa.   |
| `BossAgent.onnx.meta`                       | Pode ser reserializado com whitespace pela Unity.                  |

## Escopo e Mudanças de Direção

O plano inicial do TCC foi refinado durante o desenvolvimento. As decisões mais importantes foram:

| Decisão                                                                | Motivo                                                                                                                           |
| ---------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------- |
| Remover GAIL e `.demo`                                                 | O foco passou a ser PPO com Reward Shaping por Proveniência, evitando imitação direta e mantendo a contribuição causal do grafo. |
| Remover playtesters e questionários                                    | O escopo do TCC precisava ser fechado em uma avaliação quantitativa local, controlada pelo desenvolvedor.                        |
| Usar FSM como baseline                                                 | A FSM fornece comportamento previsível e interpretável para comparação com o agente treinado.                                    |
| Usar proveniência como fonte de reward shaping                         | Permite transformar ações humanas vitoriosas em sinal de recompensa sem usar demonstração direta.                                |
| Não retreinar o PPO na Fase 4                                          | A avaliação deveria medir o modelo congelado final, não ajustar o comportamento após observar resultados.                        |
| Não corrigir o spam de ataque do PPO antes da análise                  | O spam é um resultado experimental do modelo congelado. Corrigi-lo descaracterizaria a avaliação.                                |
| Não habilitar JSON de proveniência no PPO durante a comparação oficial | A métrica principal era o CSV; o JSON FSM já servia como evidência do mecanismo de proveniência.                                 |
| Adicionar vida real ao Elian antes da coleta                           | Sem `PlayerHealth`, o resultado tenderia a ser sempre vitória e `player_damage_taken` ficaria inconsistente.                     |

## Arquitetura Geral

A arquitetura foi estruturada em módulos com responsabilidades claras:

| Área          | Responsabilidade                                                           |
| ------------- | -------------------------------------------------------------------------- |
| Player        | Entrada, movimento, ataque e vida do Elian.                                |
| Boss FSM      | Baseline determinístico com estados `Idle`, `Chase`, `Attack` e `Retreat`. |
| Proveniência  | Registro causal de eventos, exportação JSON e Reward Shaping.              |
| Boss PPO      | Agente ML-Agents treinado com ações discretas e reward shaping.            |
| Arena         | Início/fim de sessão, vitória/derrota, coleta CSV e dano real do boss.     |
| TreinamentoML | Configuração PPO, script de filtragem e artefatos de treinamento/coleta.   |
| Docs          | Plano, PRDs, relatório de comparação, evidências e este resumo.            |

Arquivos centrais:

| Arquivo                       | Função                                                                       |
| ----------------------------- | ---------------------------------------------------------------------------- |
| `PlayerController.cs`         | Captura input e executa movimento, pulo, ataque e dash.                      |
| `PlayerCombat.cs`             | Aplica dano no alvo via `IDamageable` usando `OverlapBoxAll`.                |
| `PlayerHealth.cs`             | Vida do Elian, dano recebido, morte e eventos.                               |
| `BossHealth.cs`               | Vida do chefe, dano recebido, morte e eventos.                               |
| `BossFsmController.cs`        | FSM baseline do chefe.                                                       |
| `BossAgent.cs`                | Agente PPO com observações, ações discretas e integração com reward shaping. |
| `ProvenanceGraph.cs`          | Estrutura em memória da sessão e dos eventos causais.                        |
| `ProvenanceLogger.cs`         | Escuta eventos do gameplay e popula o grafo de proveniência.                 |
| `ProvenanceExporter.cs`       | Exporta grafos de proveniência para JSON.                                    |
| `ProvenanceRewardShaper.cs`   | Carrega `winning_sequences.json` e recompensa sequências compatíveis.        |
| `ArenaManager.cs`             | Controla sessão, vitória, derrota e integração com métricas/proveniência.    |
| `GameMetrics.cs`              | Coleta métricas de sessão e ações FSM/PPO.                                   |
| `GameMetricsExporter.cs`      | Escreve CSV com cabeçalho estável e formato invariante.                      |
| `BossAttackDamage.cs`         | Aplica dano real do boss no Elian para FSM e PPO.                            |
| `filter_winning_sequences.py` | Extrai sequências de ações vitoriosas dos JSONs de proveniência.             |
| `boss_ppo.yaml`               | Configuração PPO do ML-Agents.                                               |

## Fluxo de Proveniência

O sistema de proveniência registra eventos de jogo como nós de um grafo causal. Cada evento tem um `eventId`, `timestamp`, `actorId`, `actionType`, `position`, `value` e `parentEventId`. O `parentEventId` é a ligação causal principal.

Exemplo conceitual de cadeia:

```text
SessionStart
-> BossAttack
-> PlayerDamageTaken
-> PlayerAttack
-> PlayerDamageDealt
-> BossDamageTaken
-> BossDeath
-> SessionEnd
```

Essa estrutura é importante para o TCC porque ela mostra que o jogo não apenas gera logs planos. Ele registra uma cadeia de eventos com relações de causa e efeito. A proveniência é usada em dois momentos:

1. Como dado bruto exportado em JSON para análise e evidência.
2. Como fonte indireta de recompensa para o treinamento PPO.

O `ProvenanceLogger` escuta eventos reais emitidos por `PlayerController`, `PlayerCombat`, `PlayerHealth`, `BossHealth` e `BossFsmController`. Ele registra ações do player, ataques do boss, dano recebido, dano causado, morte do boss e encerramento da sessão. Quando a sessão termina, o `ProvenanceExporter` grava um JSON em `TreinamentoML/provenance_data/`.

Durante a avaliação final, a cena FSM exportou JSONs de proveniência. A cena PPO ficou com `exportOnSessionEnd` desligado, porque o foco da comparação quantitativa era o CSV. Isso foi aceito como decisão de escopo.

## Extração de Sequências Vitoriosas

O script `TreinamentoML/scripts/filter_winning_sequences.py` lê os JSONs de `TreinamentoML/provenance_data/`, filtra apenas sessões com `result == "victory"` e extrai sequências de ações do jogador antes de cada evento `BossDamageTaken`.

Configuração do script:

| Item                 | Valor                                      |
| -------------------- | ------------------------------------------ |
| Ações permitidas     | `PlayerJump`, `PlayerAttack`, `PlayerDash` |
| Tamanho da sequência | 3                                          |
| Evento-alvo          | `BossDamageTaken`                          |
| Entrada padrão       | `TreinamentoML/provenance_data`            |
| Saída padrão         | `TreinamentoML/winning_sequences.json`     |

O objetivo foi transformar padrões de ações humanas bem-sucedidas em um artefato legível pelo Unity. Esse artefato não é uma demonstração direta. Ele é uma lista de sequências que podem gerar recompensa adicional quando o agente PPO executa ações equivalentes.

## Reward Shaping por Proveniência

O `ProvenanceRewardShaper` carrega `TreinamentoML/winning_sequences.json`. Ele mantém um buffer das últimas ações e compara esse buffer com as sequências vitoriosas carregadas. Quando há match exato, retorna uma recompensa configurável, com padrão `+2.0`.

Esse design foi escolhido porque:

1. Isola a lógica de recompensa do `BossAgent`.
2. Permite testar o reward shaping sem depender do ML-Agents.
3. Mantém o PPO como algoritmo principal, sem introduzir GAIL.
4. Preserva a contribuição acadêmica de usar proveniência como guia causal.

Um detalhe importante: em `BossAgent`, as ações discretas de pulo, ataque e dash registram strings `PlayerJump`, `PlayerAttack` e `PlayerDash` no reward shaper. Isso permite comparar as ações do agente com as sequências extraídas das ações do jogador humano, mantendo a mesma linguagem de ação.

## Boss FSM Baseline

O chefe FSM foi implementado como baseline determinístico. Ele usa `Rigidbody2D` e estados simples:

| Estado    | Comportamento                                                                                    |
| --------- | ------------------------------------------------------------------------------------------------ |
| `Idle`    | Fica parado, geralmente quando o player está em alcance mas o cooldown ainda não permite ataque. |
| `Chase`   | Move-se horizontalmente em direção ao player.                                                    |
| `Attack`  | Para, emite `OnBossAttackPerformed`, mostra feedback visual e entra em cooldown.                 |
| `Retreat` | Move-se para longe do player por um curto período.                                               |

Parâmetros principais do FSM:

| Parâmetro         | Valor padrão |
| ----------------- | -----------: |
| `chaseSpeed`      |            2 |
| `attackRange`     |         1.25 |
| `attackDuration`  |          0.2 |
| `retreatDuration` |         0.45 |
| `retreatSpeed`    |          1.5 |
| `attackCooldown`  |          0.8 |

O FSM é propositalmente previsível. Isso não é uma limitação acidental; é uma característica desejável para baseline.

## Boss PPO

O chefe PPO é implementado em `BossAgent.cs`, herdando de `Agent` do Unity ML-Agents. Ele usa ações discretas e observações vetoriais.

Ações discretas:

| Índice | Ação         |
| -----: | ------------ |
|      0 | Idle         |
|      1 | Move Left    |
|      2 | Move Right   |
|      3 | Jump         |
|      4 | Attack Melee |
|      5 | Dash         |

Observações atuais do agente:

| Observação                 | Descrição                                                   |
| -------------------------- | ----------------------------------------------------------- |
| `bossPosition.x`           | Posição X do boss.                                          |
| `bossPosition.y`           | Posição Y do boss.                                          |
| `bossVelocity.x`           | Velocidade X do boss.                                       |
| `bossVelocity.y`           | Velocidade Y do boss.                                       |
| `relativePlayerPosition.x` | Distância relativa X até o player.                          |
| `relativePlayerPosition.y` | Distância relativa Y até o player.                          |
| `normalizedBossHealth`     | Vida normalizada do boss.                                   |
| `1f`                       | Placeholder de HP do player usado no recorte de treino.     |
| `IsGrounded()`             | Indicador se o boss está no chão.                           |
| `0f`                       | Placeholder de cooldown de dash usado no recorte de treino. |

Ponto crítico para outras IAs: `PlayerHealth` foi adicionado depois para a avaliação quantitativa, mas o PPO final não foi retreinado com essa nova vida real do player como observação. Portanto, não se deve afirmar que o modelo treinado aprendeu observando a vida real do Elian. O modelo avaliado é congelado e usa a arquitetura de observação da época do treino.

## Treinamento PPO

O treinamento foi feito com Unity ML-Agents, sem GAIL. A configuração está em `TreinamentoML/config/boss_ppo.yaml`.

Configuração principal:

| Campo           | Valor       |
| --------------- | ----------- |
| `trainer_type`  | `ppo`       |
| `batch_size`    | 1024        |
| `buffer_size`   | 10240       |
| `learning_rate` | `3.0e-4`    |
| `beta`          | `5.0e-3`    |
| `epsilon`       | 0.2         |
| `lambd`         | 0.95        |
| `num_epoch`     | 3           |
| `hidden_units`  | 128         |
| `num_layers`    | 2           |
| `max_steps`     | 500000      |
| `time_horizon`  | 64          |
| `summary_freq`  | 5000        |
| Reward signal   | `extrinsic` |

O run final foi `boss_v1_500k`. Ele chegou a 500k steps e gerou o `BossAgent.onnx` usado na avaliação final.

Resultado do run final registrado no plano mestre:

| Métrica       |      Valor |
| ------------- | ---------: |
| Mean Reward   |   1766.400 |
| Std of Reward |     12.800 |
| Time Elapsed  | 2007.533 s |

O artefato final foi copiado de:

```text
C:\Users\MATHEU~1\AppData\Local\Temp\opencode\mlagents-results\boss_v1_500k\BossAgent.onnx
```

para:

```text
ChroniclesOfTheLostWord/Assets/Models/BossAgent.onnx
```

Hash registrado do modelo final:

```text
1E823FD2CA4C3C2D5E260BCF1F74B4EE6539B27ADA84552B89EBD5B8A907B858
```

## Integração do Modelo ONNX

O modelo final `BossAgent.onnx` foi integrado ao projeto Unity em `ChroniclesOfTheLostWord/Assets/Models/BossAgent.onnx`. O prefab `Boss_PPO.prefab` foi configurado para usar o modelo em modo `InferenceOnly`.

Durante o Play Mode é esperado aparecer aviso de que não há conexão com o trainer, algo como:

```text
Couldn't connect to trainer... Will perform inference instead
```

Esse aviso é normal na avaliação, porque o PPO está em inferência local e não depende de Python rodando.

## Cenas Oficiais

As duas cenas oficiais de avaliação são:

| Cena                                                        | Função                                                                                   |
| ----------------------------------------------------------- | ---------------------------------------------------------------------------------------- |
| `ChroniclesOfTheLostWord/Assets/Scenes/ArenaChefe.unity`    | Cena oficial FSM. Gera JSON de proveniência e CSV de métricas.                           |
| `ChroniclesOfTheLostWord/Assets/Scenes/BossArena_PPO.unity` | Cena oficial PPO. Gera CSV de métricas, mas não exporta JSON de proveniência por padrão. |

Ambas foram preparadas para serem jogáveis pelo desenvolvedor, com Elian controlável. A cena PPO inicialmente era mais voltada ao treino e precisou ser convertida para avaliação jogável, removendo a dependência do `PlayerDummyAI` como fonte da coleta oficial.

Também foi adicionado `HorizontalCameraFollow` para a câmera seguir o Elian apenas no eixo X. Isso melhorou a jogabilidade sem alterar IA, reward, modelo, hitboxes ou métricas.

## Condição Real de Derrota do Player

Antes da etapa 2.6 da avaliação, foi descoberto que o Elian não tinha vida real no fluxo oficial de coleta. Isso gerava uma limitação metodológica séria: o campo `player_damage_taken` tenderia a zero ou ficaria artificial, e o resultado da sessão tenderia sempre a `victory`.

Para resolver isso, foram adicionados:

| Componente                       | Papel                                                        |
| -------------------------------- | ------------------------------------------------------------ |
| `PlayerHealth.cs`                | Vida do Elian, dano recebido, morte e eventos.               |
| `BossAttackDamage.cs`            | Aplica dano real quando FSM ou PPO atacam dentro do alcance. |
| Integração em `ArenaManager`     | Morte do Elian encerra sessão como `defeat`.                 |
| Integração em `GameMetrics`      | `player_damage_taken` passa a vir de dano real.              |
| Integração em `ProvenanceLogger` | Dano real no Elian gera `PlayerDamageTaken`.                 |

Parâmetros usados para dano do boss:

| Parâmetro                | Valor |
| ------------------------ | ----: |
| `PlayerHealth.maxHealth` |   100 |
| `damageAmount`           |    10 |
| `attackRange`            |  1.25 |
| `damageCooldown`         |   0.8 |

Esses valores foram mantidos iguais para FSM e PPO para não enviesar a comparação. O `damageCooldown` existe especialmente para impedir que o spam de ações de ataque do PPO mate o player instantaneamente.

## Sistema de Métricas Quantitativas

O sistema de métricas foi criado para a Fase 4. Ele observa eventos já existentes e exporta uma linha CSV por sessão encerrada.

Arquivo de saída padrão:

```text
TreinamentoML/evaluation_data/boss_evaluation_metrics.csv
```

Cabeçalho do CSV:

```csv
session_id,boss_type,result,start_time_seconds,end_time_seconds,duration_seconds,episode_steps,boss_damage_taken,player_damage_taken,player_jump_count,player_attack_count,player_dash_count,boss_attack_count,ppo_idle_count,ppo_move_left_count,ppo_move_right_count,ppo_jump_count,ppo_attack_count,ppo_dash_count
```

Campos principais:

| Campo                 | Significado                                                       |
| --------------------- | ----------------------------------------------------------------- |
| `session_id`          | Identificador único da sessão.                                    |
| `boss_type`           | `FSM` ou `PPO`.                                                   |
| `result`              | `victory`, `defeat` ou `unfinished`.                              |
| `duration_seconds`    | Duração da luta.                                                  |
| `boss_damage_taken`   | Dano total recebido pelo chefe.                                   |
| `player_damage_taken` | Dano total recebido pelo Elian.                                   |
| `player_jump_count`   | Quantidade de pulos do jogador.                                   |
| `player_attack_count` | Quantidade de ataques do jogador.                                 |
| `player_dash_count`   | Quantidade de dashes do jogador.                                  |
| `boss_attack_count`   | Ataques do boss; no PPO inclui tentativas de `AttackMeleeAction`. |
| `ppo_*`               | Contagens das ações discretas do `BossAgent`.                     |

O `GameMetricsExporter` resolve caminhos relativos a partir da raiz do repositório, cria o diretório se necessário, escreve cabeçalho uma única vez e usa `CultureInfo.InvariantCulture` para manter decimal com ponto no CSV.

## Coleta Oficial

A coleta oficial da Fase 4 foi feita manualmente pelo desenvolvedor:

| Condição | Sessões oficiais |
| -------- | ---------------: |
| FSM      |               10 |
| PPO      |               10 |

Durante a validação inicial da coleta oficial, o CSV tinha exatamente 20 linhas:

| Condição | Vitórias do jogador | Derrotas do jogador |
| -------- | ------------------: | ------------------: |
| FSM      |                  10 |                   0 |
| PPO      |                   9 |                   1 |

Sessões oficiais FSM:

| Ordem | `session_id`                | Resultado | Duração | Dano no boss | Dano no player |
| ----: | --------------------------- | --------- | ------: | -----------: | -------------: |
|     1 | `arena-20260624-025552-108` | victory   |  11.198 |          100 |             50 |
|     2 | `arena-20260624-025623-474` | victory   |   7.237 |          100 |             40 |
|     3 | `arena-20260624-025645-147` | victory   |   4.529 |          100 |             50 |
|     4 | `arena-20260624-025706-440` | victory   |   9.518 |          100 |             70 |
|     5 | `arena-20260624-025750-182` | victory   |   4.067 |          100 |             40 |
|     6 | `arena-20260624-025810-062` | victory   |   7.879 |          100 |             80 |
|     7 | `arena-20260624-025840-002` | victory   |   3.872 |          100 |             40 |
|     8 | `arena-20260624-025901-428` | victory   |   3.586 |          100 |             40 |
|     9 | `arena-20260624-025920-799` | victory   |   3.482 |          100 |             40 |
|    10 | `arena-20260624-025943-543` | victory   |   4.318 |          100 |             50 |

Sessões oficiais PPO:

| Ordem | `session_id`                         | Resultado | Duração | Dano no boss | Dano no player |
| ----: | ------------------------------------ | --------- | ------: | -----------: | -------------: |
|    11 | `ppo-evaluation-20260624-030317-259` | victory   |  12.801 |          100 |             40 |
|    12 | `ppo-evaluation-20260624-030402-045` | victory   |  14.700 |          100 |             70 |
|    13 | `ppo-evaluation-20260624-030442-202` | defeat    |  36.120 |           50 |            100 |
|    14 | `ppo-evaluation-20260624-030537-435` | victory   |   9.522 |          100 |             30 |
|    15 | `ppo-evaluation-20260624-030608-721` | victory   |   8.806 |          100 |             30 |
|    16 | `ppo-evaluation-20260624-030635-957` | victory   |  16.752 |          100 |             80 |
|    17 | `ppo-evaluation-20260624-030709-888` | victory   |  14.114 |          100 |             50 |
|    18 | `ppo-evaluation-20260624-030742-868` | victory   |  11.991 |          100 |             50 |
|    19 | `ppo-evaluation-20260624-030827-743` | victory   |  12.262 |          100 |             50 |
|    20 | `ppo-evaluation-20260624-030903-020` | victory   |  14.715 |          100 |             60 |

As linhas extras posteriores, fora da amostra oficial, são:

| Ordem | `session_id`                         | Condição | Observação                                                                     |
| ----: | ------------------------------------ | -------- | ------------------------------------------------------------------------------ |
|    21 | `arena-20260624-035214-868`          | FSM      | Sessão gerada após a coleta oficial, provavelmente durante gravação/evidência. |
|    22 | `ppo-evaluation-20260624-035406-496` | PPO      | Sessão gerada após a coleta oficial, provavelmente durante gravação/evidência. |

Outras IAs devem usar o relatório de resultados como fonte da amostra oficial, não recalcular diretamente o CSV atual sem filtrar as linhas extras.

## Resultados Quantitativos Oficiais

Resultados gerais oficiais:

| Condição | Sessões | Vitórias do jogador | Derrotas do jogador | Taxa de vitória do jogador |
| -------- | ------: | ------------------: | ------------------: | -------------------------: |
| FSM      |      10 |                  10 |                   0 |                       100% |
| PPO      |      10 |                   9 |                   1 |                        90% |

Tabela de médias e desvios oficiais:

| Métrica                    |           FSM |             PPO |
| -------------------------- | ------------: | --------------: |
| Duração da luta (s)        |   5,97 ± 2,79 |    15,18 ± 7,74 |
| Dano recebido pelo chefe   | 100,00 ± 0,00 |   95,00 ± 15,81 |
| Dano recebido pelo jogador | 50,00 ± 14,14 |   56,00 ± 22,21 |
| Pulos do jogador           |   0,30 ± 0,48 |     3,50 ± 3,44 |
| Ataques do jogador         |  10,70 ± 0,95 |    15,80 ± 2,57 |
| Dashes do jogador          |   0,20 ± 0,63 |     0,50 ± 1,08 |
| Ataques do chefe           |   5,60 ± 1,58 | 296,60 ± 186,86 |

Ações PPO oficiais:

| Ação PPO   | Soma nas 10 sessões | Média por sessão |
| ---------- | ------------------: | ---------------: |
| Idle       |                 463 |    46,30 ± 25,38 |
| Move Left  |                 550 |    55,00 ± 31,67 |
| Move Right |                 698 |    69,80 ± 31,94 |
| Jump       |                2123 |  212,30 ± 149,10 |
| Attack     |                2966 |  296,60 ± 186,86 |
| Dash       |                 796 |    79,60 ± 40,77 |

Distribuição aproximada das ações PPO oficiais:

| Ação PPO   | Percentual aproximado |
| ---------- | --------------------: |
| Attack     |                39,05% |
| Jump       |                27,95% |
| Dash       |                10,48% |
| Move Right |                 9,19% |
| Move Left  |                 7,24% |
| Idle       |                 6,10% |

Interpretação acadêmica sugerida:

O PPO gerou lutas mais longas, causou ligeiramente mais dano médio ao jogador e foi a única condição a produzir uma derrota do jogador. Ao mesmo tempo, exibiu alta repetição de ataques e pulos. Isso demonstra que o agente treinado gerou comportamento mensuravelmente distinto do FSM, mas ainda pouco refinado e concentrado em poucas ações.

## Evidências Selecionadas

As evidências foram reunidas em `Docs/evidencias/`.

| Evidência               | Arquivo                                                                       | Uso                                           |
| ----------------------- | ----------------------------------------------------------------------------- | --------------------------------------------- |
| Relatório quantitativo  | `Docs/evidencias/comparation/Resultados_Avaliacao_Quantitativa_FSM_vs_PPO.md` | Base textual dos resultados.                  |
| Índice de evidências    | `Docs/evidencias/Evidencias_Selecionadas.md`                                  | Explica por que cada evidência foi escolhida. |
| Vídeo FSM               | `Docs/evidencias/boss FSM.mp4`                                                | Demonstra comportamento baseline.             |
| Vídeo PPO               | `Docs/evidencias/boss PPO.mp4`                                                | Demonstra comportamento do agente treinado.   |
| Grafo representativo    | `Docs/evidencias/grafo-representativo.json`                                   | Exemplo de cadeia causal de proveniência.     |
| TensorBoard reward      | `Docs/evidencias/graphs/Environment-Cumulative Reward.png`                    | Evolução da recompensa acumulada.             |
| TensorBoard reward hist | `Docs/evidencias/graphs/Environment-Cumulative Reward_hist.png`               | Complemento visual da recompensa.             |
| TensorBoard policy loss | `Docs/evidencias/graphs/Losses-Policy Loss.png`                               | Dinâmica da política durante treino.          |
| TensorBoard value loss  | `Docs/evidencias/graphs/Losses-Value Loss.png`                                | Estimativa de valor durante treino.           |

Tamanho aproximado dos arquivos de evidência:

| Arquivo                     |               Tamanho |
| --------------------------- | --------------------: |
| `boss FSM.mp4`              |               0,60 MB |
| `boss PPO.mp4`              |               0,63 MB |
| `grafo-representativo.json` |               0,02 MB |
| Gráficos TensorBoard        | Menos de 0,10 MB cada |

## Testes e Verificação

O desenvolvimento seguiu TDD prático, com testes permanentes para partes importantes do projeto. Os principais testes cobrem:

| Área           | Testes                                                                                   |
| -------------- | ---------------------------------------------------------------------------------------- |
| Proveniência   | `ProvenanceGraphTests`, `ProvenanceExporterTests`, `ProvenanceLoggerTests`.              |
| Reward Shaping | `ProvenanceRewardShaperTests`.                                                           |
| Player         | `PlayerControllerHookTests`, `PlayerDummyAITests`, `PlayerCombatTests`.                  |
| Boss           | `BossHealthTests`, `BossFsmControllerTests`, `BossAgentTests`, `BossTrainingAssetTests`. |
| Arena/Métricas | `ArenaManagerTests`.                                                                     |
| Python         | `test_filter_winning_sequences.py`.                                                      |

Verificações já executadas em momentos relevantes:

| Verificação                          | Resultado                                           |
| ------------------------------------ | --------------------------------------------------- |
| MSBuild após Etapa 2.6               | Passou.                                             |
| Unity EditMode após Etapa 2.6        | 77/77 passou.                                       |
| `git diff --check` antes dos commits | Passou, ignorando avisos LF/CRLF conhecidos.        |
| Validação CSV após 10 FSM            | 10 linhas FSM, todas válidas.                       |
| Validação CSV após 10 PPO            | 20 linhas oficiais, 10 FSM e 10 PPO, todas válidas. |

Comandos úteis:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "C:\Users\Matheus Oliveira\Documents\TCC-Metroidvania\ChroniclesOfTheLostWord\ChroniclesOfTheLostWord.sln" /p:Platform="Any CPU" /p:Configuration=Debug /verbosity:minimal
```

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.17f1\Editor\Unity.exe" -batchmode -projectPath "C:\Users\Matheus Oliveira\Documents\TCC-Metroidvania\ChroniclesOfTheLostWord" -runTests -testPlatform EditMode -testResults "C:\Users\MATHEU~1\AppData\Local\Temp\opencode\editmode_results.xml" -logFile "C:\Users\MATHEU~1\AppData\Local\Temp\opencode\editmode.log"
```

Observação operacional: nesta máquina, não usar `-quit` no Test Runner batchmode. O Unity batchmode também falha se o Editor já estiver aberto no mesmo projeto.

## Ambiente Técnico

| Item                   | Valor                                                     |
| ---------------------- | --------------------------------------------------------- |
| Unity                  | `6000.3.17f1`                                             |
| Projeto Unity          | `ChroniclesOfTheLostWord`                                 |
| Pacote ML-Agents Unity | `com.unity.ml-agents: 4.0.3`                              |
| Input System           | `com.unity.inputsystem: 1.19.0`                           |
| Test Framework         | `com.unity.test-framework: 1.6.0`                         |
| Python ML-Agents       | Ambiente `C:\Users\Matheus Oliveira\.conda\envs\mlagents` |
| `mlagents` Python      | 1.1.0                                                     |
| Python                 | 3.10.12                                                   |
| `torch`                | 2.8.0+cpu                                                 |
| `onnx`                 | 1.15.0                                                    |

TensorBoard do run final:

```powershell
& "C:\Users\Matheus Oliveira\.conda\envs\mlagents\Scripts\tensorboard.exe" --logdir "C:\Users\MATHEU~1\AppData\Local\Temp\opencode\mlagents-results\boss_v1_500k" --port 6006
```

URL:

```text
http://localhost:6006
```

Tags relevantes encontradas no TensorBoard:

| Tag                               |
| --------------------------------- |
| `Policy/Entropy`                  |
| `Policy/Extrinsic Value Estimate` |
| `Environment/Episode Length`      |
| `Environment/Cumulative Reward`   |
| `Policy/Extrinsic Reward`         |
| `Losses/Policy Loss`              |
| `Losses/Value Loss`               |
| `Policy/Learning Rate`            |
| `Policy/Epsilon`                  |
| `Policy/Beta`                     |

## Narrativa Acadêmica Recomendada

A narrativa mais sólida para o TCC é:

1. Jogos digitais podem se beneficiar de agentes adaptativos, mas é difícil orientar aprendizado de forma interpretável.
2. Proveniência permite rastrear cadeias causais de eventos em uma sessão de jogo.
3. Em vez de usar demonstrações diretas ou GAIL, o projeto usa grafos de proveniência para extrair padrões de ações vitoriosas.
4. Esses padrões são convertidos em Reward Shaping para PPO.
5. O chefe PPO é treinado e comparado com um chefe FSM baseline.
6. A comparação é quantitativa, local e objetiva, sem playtesters externos.
7. O PPO gerou comportamento mensuravelmente diferente: lutas mais longas, maior agressividade e uma derrota do jogador na amostra.
8. O comportamento do PPO não é ideal; ele é agressivo e repetitivo, especialmente em ataques e pulos.
9. Essa limitação é parte do resultado e deve ser discutida honestamente.
10. A contribuição está em demonstrar a viabilidade de integrar proveniência e aprendizado por reforço em um protótipo jogável, não em provar superioridade absoluta do PPO.

Frase central sugerida:

```text
O trabalho demonstrou que sequências extraídas de grafos de proveniência podem ser utilizadas como sinal auxiliar de recompensa em um agente PPO, produzindo um chefe com comportamento mensuravelmente distinto do baseline FSM em um ambiente de jogo 2D.
```

## O Que Não Dizer

Evitar afirmações como:

| Afirmação inadequada                                  | Por que evitar                                                                                 |
| ----------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| "O PPO é melhor que o FSM."                           | A amostra é pequena; o PPO é mais agressivo e persistente, mas também repetitivo.              |
| "O agente aprendeu a observar a vida real do player." | A observação de HP do player era placeholder no treino final.                                  |
| "Foi usado GAIL."                                     | GAIL foi removido do escopo. O projeto usa PPO com Reward Shaping por Proveniência.            |
| "O PPO imitou o jogador."                             | Ele não imitou diretamente; recebeu recompensa por sequências extraídas de sessões vitoriosas. |
| "A avaliação teve playtesters."                       | A avaliação final foi feita localmente pelo desenvolvedor.                                     |
| "O CSV atual inteiro é a amostra oficial."            | O CSV local atual pode ter linhas extras após gravações. A amostra oficial é de 20 linhas.     |

## Limitações Assumidas

As limitações devem aparecer no TCC como parte da discussão:

1. A amostra quantitativa é pequena: 10 sessões FSM e 10 PPO.
2. A avaliação foi conduzida pelo próprio desenvolvedor.
3. O PPO apresentou spam de ataques e alta repetição de pulos.
4. O modelo final não foi retreinado após adicionar `PlayerHealth` real à avaliação.
5. A cena PPO não exporta JSON de proveniência por padrão.
6. As métricas provam diferença comportamental observada, não superioridade universal.
7. O boss PPO pode parecer visualmente preso na cor de ataque porque o feedback visual representa a última ação aplicada e o agente ataca com alta frequência.

## Próximos Passos Para Escrita do TCC

Uma IA que for ajudar na escrita pode usar esta estrutura:

1. Introdução: problema de IA em jogos e necessidade de comportamento adaptativo/interpretável.
2. Referencial teórico: jogos digitais, FSM, aprendizado por reforço, PPO, proveniência e reward shaping.
3. Metodologia: vertical slice 2D, Unity, ML-Agents, coleta de proveniência, extração de sequências e treino PPO.
4. Implementação: arquitetura dos scripts, fluxo de dados e integração do modelo ONNX.
5. Avaliação: 10 sessões FSM, 10 sessões PPO, métricas CSV e evidências.
6. Resultados: usar tabela de `Docs/evidencias/comparation/Resultados_Avaliacao_Quantitativa_FSM_vs_PPO.md`.
7. Discussão: PPO mais persistente/agressivo, mas repetitivo; proveniência como sinal auxiliar viável.
8. Limitações: amostra pequena, avaliação local, sem retreino, modelo congelado.
9. Conclusão: integração entre proveniência e PPO gerou comportamento mensurável e comparável ao baseline.

## Arquivos Que Devem Ser Consultados Primeiro Por Outras IAs

Ordem recomendada:

1. `Docs/resumo.md`
2. `Docs/Plano_Mestre_V4_TCC_Terra_Silente.md`
3. `Docs/evidencias/comparation/Resultados_Avaliacao_Quantitativa_FSM_vs_PPO.md`
4. `Docs/evidencias/Evidencias_Selecionadas.md`
5. `Docs/PRD_Fase_4_Avaliacao_Quantitativa_FSM_vs_PPO.md`
6. `ChroniclesOfTheLostWord/Assets/Scripts/Provenance/ProvenanceLogger.cs`
7. `ChroniclesOfTheLostWord/Assets/Scripts/Provenance/ProvenanceRewardShaper.cs`
8. `ChroniclesOfTheLostWord/Assets/Scripts/Boss/Agent/BossAgent.cs`
9. `ChroniclesOfTheLostWord/Assets/Scripts/Arena/GameMetrics.cs`
10. `TreinamentoML/scripts/filter_winning_sequences.py`

## Estado Final em Uma Frase

O projeto implementou um vertical slice de metroidvania com uma arena de chefe, registrou proveniência causal de partidas, extraiu padrões vitoriosos, usou esses padrões como Reward Shaping em um agente PPO treinado por ML-Agents, integrou o modelo final em Unity e realizou uma comparação quantitativa contra um chefe FSM baseline, concluindo que o PPO gerou comportamento mais persistente e agressivo, embora repetitivo, em relação ao baseline.
