# Plano Mestre V3 (Definitivo) — TCC "Terra Silente" (Vertical Slice)

> **Prazo:** 17/06 → 03/07 (16 dias)  
> **Objetivo:** Arena de chefe greybox + Proveniência + Treinamento PPO com Reward Shaping por Proveniência + Comparação Quantitativa FSM vs IA  
> **Versão:** 3.0 — Definitivo (17/06)

---

## Changelog

| Versão | Alteração |
|--------|-----------|
| V1 → V2 | Removido GAIL com `.demo`. Treinamento agora é PPO + Reward Shaping por Proveniência. Código entregue como Blueprints. |
| V2 → V3 | Removidos playtesters externos e questionários. Avaliação agora é 100% quantitativa com métricas coletadas pelo próprio dev. |

---

## Visão Geral da Arquitetura

```mermaid
graph TD
    A["🎮 Dev joga como Elian<br/>(Player vs FSM Boss)"] -->|inputs| B["🕹️ PlayerController<br/>(Move/Jump/Dash/Attack)"]
    B -->|ações + contexto| C["📊 ProvenanceLogger<br/>(Grafo Causa-Efeito)"]
    C -->|exporta JSON| D["📁 provenance_data/<br/>(sessões com resultado)"]

    D -->|filtra vitórias| E["🔬 ProvenanceFilter<br/>(Python script)"]
    E -->|extrai sequências<br/>de 3 ações vitoriosas| F["📁 winning_sequences.json"]

    F -->|lido em runtime| G["🎯 ProvenanceRewardShaper<br/>(C# - Reward Shaping)"]
    
    H["🤖 BossAgent<br/>(ML-Agents PPO)"] -->|executa ações| I["🏟️ Arena"]
    I -->|observações| H
    G -->|AddReward() quando<br/>sequência dá match| H

    H -->|treina via| J["🧠 mlagents-learn<br/>(PPO config)"]
    J -->|exporta| K["📦 BossAgent.onnx"]
    K -->|Inference| L["👹 Chefe Adaptativo"]

    M["👹 Chefe FSM<br/>(Baseline)"] -.->|comparar| L
    
    L -->|métricas quantitativas| N["📈 Resultados TCC"]
    M -->|métricas quantitativas| N
```

### Fluxo em Texto Claro

```
1. COLETA:    Dev joga como Elian contra BossFSM → ProvenanceLogger grava JSON
2. FILTRO:    Script Python lê JSONs → filtra sessões com result:"victory"
              → extrai sequências de 3 ações consecutivas que precederam dano ao boss
              → salva em winning_sequences.json
3. REWARD:    ProvenanceRewardShaper.cs carrega winning_sequences.json
              → monitora as últimas 3 ações do BossAgent em runtime
              → se a sequência bater com alguma sequência vitoriosa → AddReward(+alto)
4. TREINO:    mlagents-learn (PPO) treina BossAgent na arena
              → recompensa extrínseca (acertar player, vencer) + recompensa de proveniência
5. DEPLOY:    Exporta .onnx → BossAgent usa modelo treinado → Inference Only
6. AVALIAÇÃO: Dev joga contra FSM e IA → GameMetrics coleta dados → Análise quantitativa
```

---

## Cronograma Detalhado (16 dias)

### 🟢 FASE 1 — Fundação (Dias 1-3 | 17-19 Jun)

#### Dia 1 (17/06) — Setup do Projeto
| # | Tarefa | Detalhe |
|---|--------|---------|
| 1.1 | Adicionar ML-Agents ao projeto | `"com.unity.ml-agents": "4.0.3"` no manifest.json |
| 1.2 | Criar estrutura de pastas | Ver seção "Estrutura de Pastas" abaixo |
| 1.3 | Configurar Miniconda + Python 3.10 | Instalar Miniconda, criar env `mlagents`, instalar `mlagents 1.1.0` (release_23) |
| 1.4 | Criar cena "BossArena" | Cena básica com câmera 2D, chão, paredes, limites |

#### Dia 2 (18/06) — Player Controller (Blueprints + Implementação)
| # | Tarefa | Entrega |
|---|--------|---------|
| 2.1 | `PlayerInputHandler.cs` | Blueprint → Implementação isolada |
| 2.2 | `PlayerMotor.cs` | Blueprint → Implementação isolada |
| 2.3 | `PlayerCombat.cs` | Blueprint → Implementação isolada |
| 2.4 | `PlayerHealth.cs` | Blueprint → Implementação isolada |
| 2.5 | Montar prefab Player | Quadrado com Rigidbody2D, BoxCollider2D, scripts, cor azul |

#### Dia 3 (19/06) — Arena Greybox
| # | Tarefa | Entrega |
|---|--------|---------|
| 3.1 | Construir arena | Plataforma, paredes, teto. BoxCollider2D em tudo. |
| 3.2 | `ArenaManager.cs` | Blueprint → Implementação |
| 3.3 | `HUDManager.cs` | Blueprint → Implementação |
| 3.4 | Teste de jogabilidade | Player funcional na arena. Validar controles. |

**✅ Marco Fase 1:** Player se move, pula, dá dash e ataca dentro da arena com HUD funcional.

---

### 🟡 FASE 2 — Chefe FSM + Proveniência (Dias 4-7 | 20-23 Jun)

#### Dia 4 (20/06) — Chefe FSM (Baseline)
| # | Tarefa | Entrega |
|---|--------|---------|
| 4.1 | `BossMotor.cs` | Blueprint → Implementação |
| 4.2 | `BossCombat.cs` | Blueprint → Implementação (2-3 ataques fixos) |
| 4.3 | `BossHealth.cs` | Blueprint → Implementação |
| 4.4 | `BossFSM.cs` | Blueprint → Implementação (Idle→Chase→Attack→Retreat) |

**✅ Marco Dia 4:** Jogador pode lutar contra o chefe FSM. Comportamento previsível e repetitivo (isso é intencional — é o baseline).

#### Dia 5 (21/06) — Sistema de Proveniência (CORE ACADÊMICO)
| # | Tarefa | Entrega |
|---|--------|---------|
| 5.1 | `ProvenanceEvent.cs` | Blueprint → Implementação |
| 5.2 | `ProvenanceGraph.cs` | Blueprint → Implementação |
| 5.3 | `ProvenanceLogger.cs` | Blueprint → Implementação |
| 5.4 | `ProvenanceExporter.cs` | Blueprint → Implementação |

> **IMPORTANTE:** Este é o **diferencial acadêmico** do TCC. O grafo rastreia **causa e efeito** (não apenas logs). Cada evento tem um `parentEventId` que liga a cadeia causal. A inovação está em usar essa cadeia para moldar a recompensa do agente de IA.

#### Dia 6 (22/06) — Integrar Proveniência ao Gameplay
| # | Tarefa | Detalhe |
|---|--------|---------|
| 6.1 | Hooks de eventos | Player emite: `OnPlayerAttack`, `OnPlayerJump`, `OnPlayerDash`, `OnPlayerDamageDealt`, `OnPlayerDamageTaken`. Boss emite: `OnBossAttack`, `OnBossDamageTaken`, `OnBossDeath`. |
| 6.2 | Cadeias causais | Ex: `PlayerDash(id:5) → PlayerAttack(id:6, parent:5) → BossDamageTaken(id:7, parent:6)`. Logger liga IDs via janela temporal + contexto. |
| 6.3 | Sessões | `SessionStart`, `SessionEnd(result, metrics)`. Marcar vitória/derrota, HP restante, tempo, dano total. |
| 6.4 | Teste manual | Jogar 3-5 partidas contra FSM. Verificar JSONs gerados. |

#### Dia 7 (23/06) — Testes + Filtro de Proveniência
| # | Tarefa | Detalhe |
|---|--------|---------|
| 7.1 | Testes unitários (**permanentes**) | `ProvenanceGraph`: adicionar nós, verificar ligações causais, serializar JSON. **Estes testes ficam no repo.** |
| 7.2 | `filter_winning_sequences.py` | Script Python: lê JSONs de proveniência → filtra `result: "victory"` → extrai sequências de 3 ações consecutivas que precedem `BossDamageTaken` → salva `winning_sequences.json` |
| 7.3 | **COLETA DE DADOS** | Dev joga 15-20 partidas contra a FSM com perfis variados (agressivo, defensivo, misto). Gerar JSONs de proveniência em quantidade. |

**✅ Marco Fase 2:** Chefe FSM funcional + JSONs de proveniência com cadeias causais + `winning_sequences.json` gerado com padrões de vitória.

---

### 🔴 FASE 3 — ML-Agents + PPO com Reward Shaping (Dias 8-12 | 24-28 Jun)

#### Estado ao Entrar na Fase 3

- A Fase 2 gerou JSONs de proveniência com `PlayerJump`, `PlayerDash`, `PlayerAttack`, `BossDamageTaken`, `BossDeath` e `SessionEnd` causalmente encadeados.
- `TreinamentoML/scripts/filter_winning_sequences.py` extrai sequências de 3 ações permitidas antes de cada `BossDamageTaken` em sessões `result: "victory"`.
- `TreinamentoML/winning_sequences.json` é o artefato de entrada da Fase 3. Ele pode ser regenerado localmente e não precisa ser commitado se a coleta bruta estiver sendo guardada fora do projeto.
- `TreinamentoML/provenance_data` continua sendo dado bruto de runtime e não deve ser commitado por padrão.

#### Fase 3A — Reward Shaping por Proveniência Isolado
| # | Tarefa | Entrega |
|---|--------|---------|
| 3A.1 | `ProvenanceRewardShaper.cs` | Implementação testável sem depender ainda de ML-Agents. Carrega o JSON de sequências vitoriosas. |
| 3A.2 | Buffer de ações | Mantém as últimas 3 ações recebidas via API pública e compara com as sequências carregadas. |
| 3A.3 | Reward function isolada | Match exato → retorna recompensa configurável, padrão `+2.0`. Sem chamar `AddReward()` ainda. |
| 3A.4 | Testes EditMode | Validar carga do JSON, match de sequência e ausência de reward quando a sequência não bate. |

#### Fase 3B — Boss Agent Setup + Integração com Reward Shaper
| # | Tarefa | Entrega |
|---|--------|---------|
| 3B.1 | Adicionar ML-Agents | Incluir `com.unity.ml-agents` e validar compilação do pacote antes de criar o agente. |
| 3B.2 | `BossAgent.cs` | Implementação inicial herdando de `Agent`, com observações e ações discretas. |
| 3B.3 | Observations | Posição boss (2), velocidade (2), posição relativa ao player (2), HP normalizado ambos (2), grounded (1), dash cooldown (1) = **~10 floats**. |
| 3B.4 | Actions (Discrete) | Branch 0: {0=idle, 1=left, 2=right, 3=jump, 4=attack_melee, 5=dash}. |
| 3B.5 | Integração de reward | `BossAgent` registra ações no `ProvenanceRewardShaper` e chama `AddReward()` com a recompensa retornada. |

#### Fase 3C — Ambiente de Treinamento e Smoke Test PPO
| # | Tarefa | Detalhe |
|---|--------|---------|
| 3C.1 | Player controlado por script simples na cena de treino | Durante o treinamento, o Player precisa de um comportamento automático (andar, atacar aleatoriamente) para o Boss ter contra quem treinar. Criar `PlayerDummyAI.cs`. |
| 3C.2 | Reset de episódio | `OnEpisodeBegin()`: resetar posições, HPs, timers. |
| 3C.3 | Criar config YAML | `TreinamentoML/config/boss_ppo.yaml` — PPO puro (sem GAIL reward signal). |
| 3C.4 | Teste local rápido | `mlagents-learn` com 5k steps. Verificar se treina sem erros antes de treinos longos. |

#### Fase 3D — Treinamento Completo + Exportação ONNX
| # | Tarefa | Detalhe |
|---|--------|---------|
| 3D.1 | Treino intermediário de validação | Rodar `boss_v1_100k` com `100k` steps para validar convergência inicial, geração de resultados e exportação `.onnx` sem gastar o ciclo completo. |
| 3D.2 | Monitorar TensorBoard | Usar `tensorboard --logdir results` ou diretório temporário equivalente. Observar `Cumulative Reward`, `Episode Length` e perdas da policy. |
| 3D.3 | Exportar modelo inicial | Copiar `BossAgent.onnx` gerado pelo run `boss_v1_100k` para `ChroniclesOfTheLostWord/Assets/Models/BossAgent.onnx` para smoke de inference. |
| 3D.4 | Validar inference no Editor | Configurar `BehaviorParameters` com o modelo `.onnx`, `BehaviorType = Inference Only`, e validar que a cena roda sem Python conectado. |
| 3D.5 | Treino final de TCC | Depois do smoke com `100k`, rodar o treino final com `TreinamentoML/config/boss_ppo.yaml` em `500k` steps (`run-id=boss_v1_500k`) para produzir o modelo usado na Fase 4. |

**Status em 23/06:** Fase 3D concluida. O run intermediario `boss_v1_100k` validou o pipeline de treino/exportacao e o run final `boss_v1_500k` chegou a `500000` steps. O treino final exportou `BossAgent.onnx`, integrado em `Assets/Models/BossAgent.onnx`. O prefab `Boss_PPO.prefab` esta configurado com `BehaviorType = InferenceOnly` e referencia o modelo final importado.

**Resultado do run final:** `boss_v1_500k` terminou com `Mean Reward: 1766.400`, `Std of Reward: 12.800` e `Time Elapsed: 2007.533 s`. O artefato exportado foi copiado de `C:\Users\MATHEU~1\AppData\Local\Temp\opencode\mlagents-results\boss_v1_500k\BossAgent.onnx` para o projeto Unity.

**Notas operacionais:** nesta maquina, o treino estavel pelo Editor usou `python -m mlagents.trainers.learn`, `torch==2.8.0`, `onnx==1.15.0` e `setuptools==65.5.1`. O `.exe` standalone e algumas DLLs JIT continuam sujeitos ao bloqueio local do Windows App Control/Device Guard.

> **IMPORTANTE:** Esta é a **contribuição original** do TCC. O agente não imita um `.demo` diretamente — ele **descobre por conta própria** (via exploração PPO) que certas sequências de ações geram alta recompensa, e essas sequências foram extraídas do grafo de proveniência de jogadores humanos vitoriosos. A proveniência está guiando o aprendizado.

#### Dia 11-12 (27-28/Jun) — Treinamento e Iteração
| # | Tarefa | Detalhe |
|---|--------|---------|
| 11.1 | Treinamento completo | `python -m mlagents.trainers.learn TreinamentoML/config/boss_ppo.yaml --run-id=boss_v1_500k`. Target final: 500k steps. |
| 11.2 | Monitorar TensorBoard | `tensorboard --logdir results`. Observar curva de reward. Capturar screenshots para o TCC. |
| 11.3 | Iterar se necessário | Ajustar rewards, `hidden_units`, `batch_size`, `learning_rate`. Re-treinar. |
| 11.4 | Exportar .onnx | `results/boss_v1/BossAgent.onnx` → `Assets/Models/` |
| 11.5 | Testar em Unity | `BehaviorParameters` → Model = .onnx, BehaviorType = Inference Only. Observar comportamento. |

**✅ Marco Fase 3:** BossAgent treinado com .onnx funcional. Chefe se movimenta e ataca de forma diferente da FSM, influenciado pelos padrões de proveniência.

---

### 🟣 FASE 4 — Avaliação Quantitativa (Dias 13-14 | 29-30 Jun)

> **Nota V3:** Avaliação feita 100% pelo dev. Zero playtesters. Zero questionários. Foco em métricas objetivas.

#### Dia 13 (29/06) — Coleta de Métricas
| # | Tarefa | Detalhe |
|---|--------|---------|
| 13.1 | `GameMetrics.cs` | Coletar automaticamente por sessão: tempo de luta, dano dado pelo chefe, dano recebido pelo chefe, ações por tipo (contagem), duração do episódio, resultado (vitória/derrota). Exportar CSV. |
| 13.2 | Sessões vs FSM | Dev joga **10 partidas** contra o Chefe FSM. `GameMetrics` salva os dados. |
| 13.3 | Sessões vs IA | Dev joga **10 partidas** contra o Chefe IA (PPO). `GameMetrics` salva os dados. |
| 13.4 | Gravar vídeos | OBS Studio: gravar pelo menos 2 partidas limpas contra cada chefe. Esses vídeos vão para a apresentação. |

#### Dia 14 (30/06) — Análise dos Dados
| # | Tarefa | Detalhe |
|---|--------|---------|
| 14.1 | Compilar métricas | Tabela comparativa FSM vs IA com médias e desvio padrão. |
| 14.2 | Métricas-chave para a banca | **Tempo médio de sobrevivência** (FSM vs IA): chefe IA sobrevive mais? **Dano médio no jogador** (FSM vs IA): chefe IA acerta mais? **Variância de ações** (FSM vs IA): chefe IA é menos previsível? **Episode length** durante o treino (convergência). |
| 14.3 | Screenshots TensorBoard | Salvar gráficos: Cumulative Reward, Episode Length, Policy Loss. Anotar momentos de convergência. |
| 14.4 | Grafo de proveniência exemplo | Exportar 1-2 grafos de proveniência visuais (JSON → diagrama) para mostrar na banca como evidência do rastreamento causal. |

**✅ Marco Fase 4:** CSVs com métricas de 10 partidas FSM + 10 partidas IA. Tabela comparativa pronta. Vídeos e screenshots do TensorBoard salvos.

---

### ⚪ FASE 5 — Documentação e Apresentação (Dias 15-16 | 01-02 Jul)

#### Dia 15 (01/07) — Documentação Técnica + Resultados
| # | Tarefa | Detalhe |
|---|--------|---------|
| 15.1 | Documentar arquitetura | Diagrama de classes, fluxo de dados, grafo de proveniência exemplo (com IDs reais). |
| 15.2 | Resultados quantitativos | Tabelas: FSM vs IA (todas as métricas). Screenshots TensorBoard (reward, loss, episode length). |
| 15.3 | Análise da proveniência | Mostrar como o `winning_sequences.json` influenciou o comportamento aprendido. Exemplo: "A sequência [dash, attack, jump] apareceu em 70% das vitórias e o chefe IA executou variações dela 40% mais que a FSM." |
| 15.4 | Editar vídeos | Cortar, legendar ("Chefe FSM" vs "Chefe IA"), lado a lado se possível. |

#### Dia 16 (02/07) — Apresentação
| # | Tarefa | Detalhe |
|---|--------|---------|
| 16.1 | Montar slides | Problema → Referencial (Proveniência + RL) → Arquitetura → Demo (vídeos) → Resultados Quantitativos → Conclusão |
| 16.2 | Embutir vídeos nos slides | Gameplay FSM vs IA. TensorBoard. Grafo de proveniência visual. |
| 16.3 | Ensaiar | 15-20 min de apresentação + 10 min para perguntas da banca. |

---

## Estrutura de Pastas (V3)

```
ChroniclesOfTheLostWord/
└── Assets/
    ├── Scripts/
    │   ├── Player/
    │   │   ├── PlayerInputHandler.cs     ← Captura de inputs (New Input System)
    │   │   ├── PlayerMotor.cs            ← Movimentação + física (FixedUpdate)
    │   │   ├── PlayerCombat.cs           ← Ataque + hitbox
    │   │   ├── PlayerHealth.cs           ← HP + dano + morte + eventos
    │   │   └── PlayerDummyAI.cs          ← IA simples do player para cena de treino
    │   ├── Boss/
    │   │   ├── BossMotor.cs              ← Movimentação do chefe
    │   │   ├── BossCombat.cs             ← Ataques do chefe
    │   │   ├── BossHealth.cs             ← HP do chefe
    │   │   ├── BossFSM.cs               ← Chefe FSM (baseline manual)
    │   │   └── BossAgent.cs              ← Chefe ML-Agents (PPO + Provenance Reward)
    │   ├── Provenance/
    │   │   ├── ProvenanceEvent.cs        ← Struct do evento
    │   │   ├── ProvenanceGraph.cs        ← Grafo em memória (nós + arestas causais)
    │   │   ├── ProvenanceLogger.cs       ← Escuta eventos do jogo, popula grafo
    │   │   ├── ProvenanceExporter.cs     ← Serializa grafo → JSON
    │   │   └── ProvenanceRewardShaper.cs ← Carrega winning_sequences.json, calcula reward por sequência
    │   ├── Arena/
    │   │   ├── ArenaManager.cs           ← Gerencia início/fim da luta
    │   │   └── GameMetrics.cs            ← Coleta métricas automáticas por sessão (exporta CSV)
    │   └── UI/
    │       └── HUDManager.cs             ← Barras de HP, timer
    ├── Prefabs/
    │   ├── Player.prefab
    │   ├── Boss_FSM.prefab
    │   └── Boss_PPO.prefab
    ├── Scenes/
    │   ├── BossArena_FSM.unity           ← Cena jogável: Player vs FSM
    │   ├── BossArena_PPO.unity           ← Cena jogável: Player vs IA treinada
    │   └── TrainingArena.unity           ← Cena de treino: BossAgent vs PlayerDummyAI
    └── Models/
        └── BossAgent.onnx               ← Modelo treinado

TreinamentoML/
├── config/
│   └── boss_ppo.yaml                    ← Config PPO (sem GAIL .demo)
├── provenance_data/                     ← JSONs de proveniência exportados
├── winning_sequences.json               ← Sequências vitoriosas filtradas
├── scripts/
│   └── filter_winning_sequences.py      ← Script de filtragem
├── results/                             ← Outputs do treinamento + TensorBoard logs
└── evaluation/                          ← CSVs de métricas (10 FSM + 10 IA)
```

---

## Setup Técnico

### Unity
```json
// Adicionar ao manifest.json:
"com.unity.ml-agents": "4.0.3"
```

### Miniconda + Python 3.10
```bash
# Instalar Miniconda (se não tiver): https://docs.conda.io/en/latest/miniconda.html

# Criar ambiente
conda create -n mlagents python=3.10
conda activate mlagents

# Instalar ML-Agents (release_23 — compatível com Unity 6)
git clone --branch release_23 https://github.com/Unity-Technologies/ml-agents.git
cd ml-agents
pip install ./ml-agents-envs
pip install ./ml-agents

# Verificar
mlagents-learn --help
```

### Config PPO (TreinamentoML/config/boss_ppo.yaml)
```yaml
behaviors:
  BossAgent:
    trainer_type: ppo
    hyperparameters:
      batch_size: 1024
      buffer_size: 10240
      learning_rate: 3.0e-4
      beta: 5.0e-3
      epsilon: 0.2
      lambd: 0.95
      num_epoch: 3
    network_settings:
      normalize: false
      hidden_units: 128
      num_layers: 2
    reward_signals:
      extrinsic:
        # Toda a recompensa vem do código C# (ProvenanceRewardShaper + rewards básicos)
        strength: 1.0
        gamma: 0.99
    max_steps: 500000
    time_horizon: 64
    summary_freq: 5000
```

> **Nota:** Sem `gail` reward signal. Toda recompensa é extrínseca, vinda do `AddReward()` no C# — que inclui a lógica de match com sequências de proveniência.

---

## Contrato de Entrega de Código (Blueprint)

Cada script será entregue como **Blueprint Arquitetural**:

```csharp
// EXEMPLO DE FORMATO BLUEPRINT
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerMotor : MonoBehaviour
{
    // === ESTADO ===
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float dashForce;
    [SerializeField] private float dashCooldown;
    
    private Rigidbody2D rb;
    private bool isGrounded;
    private bool isDashing;
    
    // === EVENTOS ===
    public event System.Action OnJump;
    public event System.Action OnDash;
    
    // === MÉTODOS PÚBLICOS ===
    
    /// Move o player horizontalmente. Chamar de FixedUpdate().
    public void Move(float direction) { /* implementar */ }
    
    /// Executa pulo se grounded. Aplica jumpForce via rb.
    public void Jump() { /* implementar */ }
    
    /// Executa dash na direção atual se cooldown permitir.
    public void Dash(float direction) { /* implementar */ }
    
    /// Retorna true se o player está no chão (raycast para baixo).
    public bool CheckGrounded() { /* implementar */ }
}
```

Outra instância receberá esse Blueprint e implementará a lógica interna de cada método isoladamente.

---

## Plano de Verificação

### Testes Automatizados
- Testes unitários permanentes para `ProvenanceGraph` (adicionar nó, ligar causa-efeito, exportar JSON)
- Validação do formato do `winning_sequences.json`

### Verificação Manual
- Dia 3: Player se move, pula, dá dash, ataca corretamente na arena
- Dia 4: Player luta contra chefe FSM com comportamento previsível
- Dia 6: JSONs de proveniência são gerados e contêm cadeias causais corretas
- Dia 7: `winning_sequences.json` contém sequências filtradas de vitórias
- Dia 10: Treinamento roda sem erros por 5k steps
- Dia 12: Modelo .onnx carrega e o chefe se comporta de forma diferente da FSM
- Dia 13: `GameMetrics.cs` exporta CSVs corretamente após cada partida
- Dia 14: Tabela comparativa FSM vs IA preenchida com dados de 20 partidas

---

## Resumo Executivo

| Fase | Dias | Entregáveis |
|------|------|-------------|
| 🟢 Fundação | 1-3 (17-19/Jun) | Player funcional + Arena greybox + Setup ML-Agents/Python |
| 🟡 FSM + Proveniência | 4-7 (20-23/Jun) | Chefe FSM jogável + Sistema de proveniência completo + winning_sequences.json |
| 🔴 ML-Agents + PPO | 8-12 (24-28/Jun) | BossAgent treinado + .onnx funcional + RewardShaper por proveniência |
| 🟣 Avaliação Quantitativa | 13-14 (29-30/Jun) | 10 partidas FSM + 10 partidas IA + CSVs + TensorBoard + Vídeos gravados |
| ⚪ Apresentação | 15-16 (01-02/Jul) | Documentação + Slides + Vídeos editados + Ensaio |
