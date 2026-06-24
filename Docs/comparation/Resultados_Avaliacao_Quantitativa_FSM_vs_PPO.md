# Resultados da Avaliação Quantitativa: FSM vs PPO

## Resumo da Coleta

A avaliação quantitativa foi conduzida a partir de sessões de combate registradas no arquivo local `TreinamentoML/evaluation_data/boss_evaluation_metrics.csv`. A amostra final é composta por 20 sessões oficiais, sendo 10 contra o chefe baseado em máquina de estados finitos (FSM) e 10 contra o chefe controlado pelo agente PPO.

As duas condições utilizaram o mesmo personagem jogável, Elian, nas cenas oficiais de avaliação. O agente PPO foi avaliado a partir do modelo `BossAgent.onnx` já treinado e congelado em modo de inferência, sem retreinamento ou ajuste de política durante esta etapa. Os valores apresentados nas tabelas correspondem à média acompanhada do desvio-padrão amostral.

## Resultado Geral

| Condição | Sessões | Vitórias do jogador | Derrotas do jogador | Taxa de vitória do jogador |
| --- | ---: | ---: | ---: | ---: |
| FSM | 10 | 10 | 0 | 100% |
| PPO | 10 | 9 | 1 | 90% |

## Comparação Quantitativa

| Métrica | FSM | PPO | Interpretação |
| --- | ---: | ---: | --- |
| Duração da luta (s) | 5,97 ± 2,79 | 15,18 ± 7,74 | As lutas contra o PPO apresentaram maior duração média. |
| Dano recebido pelo chefe | 100,00 ± 0,00 | 95,00 ± 15,81 | O chefe FSM foi derrotado em todas as sessões; no PPO, uma sessão terminou com derrota do jogador antes da eliminação do chefe. |
| Dano recebido pelo jogador | 50,00 ± 14,14 | 56,00 ± 22,21 | O PPO causou maior dano médio ao jogador, embora com maior variabilidade. |
| Pulos do jogador | 0,30 ± 0,48 | 3,50 ± 3,44 | A condição PPO exigiu maior movimentação vertical do jogador. |
| Ataques do jogador | 10,70 ± 0,95 | 15,80 ± 2,57 | O jogador realizou mais ataques, em média, nas lutas contra o PPO. |
| Dashes do jogador | 0,20 ± 0,63 | 0,50 ± 1,08 | O uso de dash permaneceu baixo em ambas as condições. |
| Ataques do chefe | 5,60 ± 1,58 | 296,60 ± 186,86 | O PPO apresentou frequência de ataque substancialmente superior à do FSM. |

## Ações Discretas do Agente PPO

As métricas `ppo_*` são aplicáveis apenas às sessões do chefe PPO, pois o chefe FSM não utiliza o componente `BossAgent`. Nas sessões FSM, essas colunas permanecem zeradas por definição.

| Ação PPO | Soma nas 10 sessões | Média por sessão |
| --- | ---: | ---: |
| Idle | 463 | 46,30 ± 25,38 |
| Move Left | 550 | 55,00 ± 31,67 |
| Move Right | 698 | 69,80 ± 31,94 |
| Jump | 2123 | 212,30 ± 149,10 |
| Attack | 2966 | 296,60 ± 186,86 |
| Dash | 796 | 79,60 ± 40,77 |

A distribuição aproximada das ações discretas do PPO, considerando o total das 10 sessões, foi a seguinte:

| Ação PPO | Percentual aproximado |
| --- | ---: |
| Attack | 39,05% |
| Jump | 27,95% |
| Dash | 10,48% |
| Move Right | 9,19% |
| Move Left | 7,24% |
| Idle | 6,10% |

## Análise dos Resultados

Os resultados indicam que o chefe controlado pelo PPO gerou combates mais longos do que o chefe FSM. A duração média das lutas contra o PPO foi de 15,18 segundos, enquanto a média observada contra o FSM foi de 5,97 segundos. Além disso, o PPO foi a única condição experimental na qual ocorreu uma derrota do jogador durante a amostra coletada.

O dano médio recebido pelo jogador também foi maior na condição PPO, com média de 56,00 pontos, em comparação aos 50,00 pontos observados na condição FSM. Essa diferença deve ser interpretada com cautela, pois a amostra é reduzida e o desvio-padrão na condição PPO é mais elevado. Ainda assim, a ocorrência de uma derrota do jogador sugere que o comportamento aprendido pelo agente foi capaz de produzir ameaça real dentro das regras atuais do protótipo.

A diferença mais expressiva entre as duas condições está na quantidade de ataques realizados pelo chefe. O FSM apresentou média de 5,60 ataques por sessão, enquanto o PPO apresentou média de 296,60. Esse resultado confirma quantitativamente a observação feita durante os testes manuais: o modelo PPO tende a repetir ações de ataque com alta frequência. Esse comportamento foi mantido na avaliação por se tratar do modelo final congelado, evitando alterações posteriores que pudessem descaracterizar o resultado experimental.

Também se observa alta frequência da ação `Jump` no PPO, com média de 212,30 ocorrências por sessão. Em conjunto, `Attack` e `Jump` representam a maior parte das decisões tomadas pelo agente. Esse padrão sugere uma política agressiva e repetitiva, porém funcional o suficiente para aumentar a duração dos combates e gerar uma derrota do jogador em uma das sessões.

## Considerações Metodológicas

- A amostra foi composta por 20 sessões locais, divididas igualmente entre FSM e PPO.
- Não foram utilizados playtesters externos, questionários ou instrumentos subjetivos de avaliação.
- As duas condições foram avaliadas com o mesmo personagem jogável e com arenas equivalentes.
- O agente PPO foi avaliado como artefato final congelado, sem treinamento adicional.
- A comparação descreve o comportamento observado no protótipo atual, sem pretensão de generalizar a superioridade do algoritmo PPO.
- O arquivo CSV bruto permanece como artefato local e regenerável, não sendo versionado por padrão.

## Conclusão Inicial

Na amostra analisada, o chefe PPO apresentou comportamento mais persistente e mais agressivo do que o chefe FSM utilizado como baseline. A principal diferença quantitativa foi observada na duração média das lutas e na frequência de ações ofensivas executadas pelo chefe.

Ao mesmo tempo, a elevada repetição de ataques evidencia uma limitação do modelo treinado: embora o agente tenha aprendido um padrão funcional de pressão sobre o jogador, esse padrão ainda se mostra pouco refinado e fortemente concentrado em poucas ações. Essa limitação é relevante para a discussão dos resultados, pois indica que o treinamento com Reward Shaping por Proveniência gerou comportamento mensuravelmente distinto do baseline, mas não necessariamente um comportamento ideal ou variado.

Assim, os resultados sustentam a contribuição central do trabalho: a integração entre proveniência e aprendizado por reforço produziu um agente comparável ao baseline FSM e passível de análise quantitativa. A avaliação não depende de afirmar que o PPO é superior em todos os aspectos, mas demonstra que a abordagem proposta resultou em diferenças observáveis, mensuráveis e discutíveis no contexto do protótipo desenvolvido.
