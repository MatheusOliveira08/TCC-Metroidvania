# Evidências Selecionadas da Comparação FSM vs PPO

## Objetivo

Este documento registra as evidências selecionadas para complementar a avaliação quantitativa entre o chefe FSM e o chefe PPO. As evidências foram escolhidas para sustentar a discussão do TCC com três tipos de material: dados quantitativos, comportamento visual do jogo e rastros de proveniência.

## Evidências Selecionadas

| Evidência | Arquivo | Uso recomendado |
| --- | --- | --- |
| Relatório quantitativo | `Docs/evidencias/comparation/Resultados_Avaliacao_Quantitativa_FSM_vs_PPO.md` | Base textual para a seção de resultados do TCC. |
| CSV bruto da coleta | `TreinamentoML/evaluation_data/boss_evaluation_metrics.csv` | Fonte local dos cálculos; não deve ser versionado por padrão. |
| Vídeo do chefe FSM | `Docs/evidencias/boss FSM.mp4` | Demonstração visual do comportamento baseline. |
| Vídeo do chefe PPO | `Docs/evidencias/boss PPO.mp4` | Demonstração visual do comportamento aprendido pelo agente. |
| Grafo de proveniência representativo | `Docs/evidencias/grafo-representativo.json` | Exemplo de rastreabilidade causal dos eventos da luta. |
| TensorBoard: recompensa acumulada | `Docs/evidencias/graphs/Environment-Cumulative Reward.png` | Evidência da evolução do treinamento PPO. |
| TensorBoard: recompensa acumulada com histograma | `Docs/evidencias/graphs/Environment-Cumulative Reward_hist.png` | Complemento visual da distribuição da recompensa durante o treino. |
| TensorBoard: policy loss | `Docs/evidencias/graphs/Losses-Policy Loss.png` | Evidência complementar da dinâmica de otimização da política. |
| TensorBoard: value loss | `Docs/evidencias/graphs/Losses-Value Loss.png` | Evidência complementar da estimativa de valor durante o treino. |

## Justificativa da Seleção

O relatório quantitativo consolida a amostra oficial de 20 sessões, dividida em 10 partidas contra o chefe FSM e 10 partidas contra o chefe PPO. Ele deve ser utilizado como principal evidência textual da comparação, pois apresenta médias, desvios-padrão e uma interpretação inicial dos resultados obtidos.

Os vídeos curtos servem como evidência qualitativa complementar. O vídeo do FSM representa o comportamento determinístico do baseline, com ações mais previsíveis e menor frequência de ataques. O vídeo do PPO, por sua vez, evidencia o comportamento mais ativo e agressivo observado durante a coleta, incluindo a repetição frequente de ataques identificada também nos dados quantitativos.

O grafo de proveniência representativo documenta a cadeia causal de uma sessão de combate, relacionando eventos como ataques, dano recebido, dano causado e encerramento da luta. Essa evidência conecta a avaliação quantitativa ao mecanismo de proveniência utilizado no projeto, mostrando que os dados não foram apenas anotados manualmente, mas derivados de eventos registrados pelo sistema.

As imagens do TensorBoard demonstram o processo de treinamento do agente PPO. Elas não substituem a avaliação no jogo, mas complementam a análise ao mostrar que o modelo utilizado na comparação foi resultado de um processo de treinamento monitorado, com métricas como recompensa acumulada e perdas da política.

## Uso no Texto do TCC

No texto do TCC, recomenda-se utilizar o relatório quantitativo como base da seção de resultados e inserir apenas os principais números na narrativa: duração média das lutas, dano recebido pelo jogador, taxa de vitória e frequência de ataques do chefe. O CSV bruto deve ser tratado como fonte dos cálculos, mas não precisa aparecer integralmente no corpo do texto.

O grafo de proveniência pode ser citado na seção metodológica ou na discussão dos resultados, com foco em demonstrar a rastreabilidade dos eventos de jogo. As imagens do TensorBoard podem ser usadas na seção de treinamento do agente PPO, enquanto os vídeos são mais adequados para apresentação oral ou banca.

## Uso nos Slides

Para os slides, recomenda-se uma seleção reduzida:

| Slide | Evidência sugerida | Finalidade |
| --- | --- | --- |
| Treinamento PPO | `Environment-Cumulative Reward.png` | Mostrar evolução da recompensa durante o treinamento. |
| Comparação quantitativa | Tabela do relatório em `Resultados_Avaliacao_Quantitativa_FSM_vs_PPO.md` | Apresentar diferenças objetivas entre FSM e PPO. |
| Gameplay FSM | `boss FSM.mp4` | Mostrar o comportamento baseline. |
| Gameplay PPO | `boss PPO.mp4` | Mostrar o comportamento do agente treinado. |
| Proveniência | `grafo-representativo.json` | Demonstrar rastreabilidade causal dos eventos. |

## Observações Metodológicas

- A amostra oficial da comparação permanece definida como 20 sessões: 10 FSM e 10 PPO.
- O agente PPO foi avaliado com o modelo final congelado, sem retreinamento durante a coleta.
- O CSV em `TreinamentoML/evaluation_data/` é um artefato bruto local e regenerável.
- As evidências visuais complementam os resultados, mas a conclusão quantitativa deve se apoiar principalmente no relatório e no CSV da coleta.
- A repetição de ataques do PPO deve ser apresentada como resultado experimental observado, não como erro a ser corrigido nesta fase.
