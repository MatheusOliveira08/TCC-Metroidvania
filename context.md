# Contexto do Projeto: Chronicles of The Lost Word (TCC)

## Visão Geral

O projeto é um protótipo (Vertical Slice) de um jogo Metroidvania 2D focado em comprovar a viabilidade do uso de Aprendizado por Reforço (algoritmo PPO) guiado por Grafos de Proveniência para treinar inimigos/chefes. O jogo é desenvolvido na Unity 6.3 LTS (C#). A avaliação do trabalho será quantitativa, comparando o desempenho da IA contra um chefe tradicional (FSM).

## O Jogo

Em "Terra Silente", o protagonista Elian busca restaurar a Palavra Primordial derrotando as 7 Potestades (pecados). O combate envolve movimentação rápida, pulo, dash e ataques. Em vez de matar cegamente, o jogador pode "libertar" almas corrompidas, gerando um dilema moral.

## O Foco Atual (Protótipo)

Não estamos desenvolvendo o jogo completo agora. O foco absoluto é criar uma "Arena do Chefe" em ambiente de Greyboxing para a coleta de dados de proveniência do jogador.

1. O jogador (Elian, que por enquanto é um quadrado com física e controle) se move, pula e ataca.
2. Cada ação é gravada com sua relação de causa e efeito no Grafo de Proveniência.
3. Filtramos as sequências vitoriosas desse grafo.
4. O Unity ML-Agents treinará a IA do chefe usando PPO. Se a IA executar uma sequência que dê "match" com a proveniência vitoriosa, ela ganha um Reward massivo (Reward Shaping).

## Nomenclaturas Oficiais

Para evitar confusões de código legado:

- O chefe tradicional se chama `BossFSM`.
- O chefe com Machine Learning se chama `BossPPO` ou `BossAgent`.
