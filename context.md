# Contexto do Projeto: Chronicles of The Lost Word (TCC)

## Visão Geral

O projeto é um protótipo (Vertical Slice) de um jogo Metroidvania 2D focado em comprovar a viabilidade do uso de Aprendizado por Imitação (Imitation Learning via framework GAIL) utilizando Grafos de Proveniência para treinar inimigos/chefes. O jogo é desenvolvido na Unity 6.3 LTS (C#).

## O Jogo

Em "Terra Silente", o protagonista Elian busca restaurar a Palavra Primordial derrotando as 7 Potestades (pecados). O combate envolve movimentação rápida, pulo, dash e ataques. Em vez de matar cegamente, o jogador pode "libertar" almas corrompidas, gerando um dilema moral.

## O Foco Atual (Protótipo)

Não estamos desenvolvendo o jogo completo agora. O foco absoluto é criar uma "Arena do Chefe" em ambiente de Greyboxing para a coleta de dados de proveniência do jogador.

1. O jogador (um quadrado com física e controle) se move, pula e ataca.
2. Cada ação é gravada com sua relação de causa e efeito (Proveniência).
3. O Unity ML-Agents lerá esses dados e treinará a IA do chefe para imitar o comportamento de vitória do jogador humano.
