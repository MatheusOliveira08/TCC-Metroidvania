using TerraSilente.Boss;
using TerraSilente.Player;
using UnityEngine;

namespace TerraSilente.Arena
{
    [DisallowMultipleComponent]
    public class GameMetrics : MonoBehaviour
    {
        [SerializeField] private string bossType = "PPO";
        [SerializeField] private string outputDirectory;
        [SerializeField] private string outputFileName = GameMetricsExporter.DefaultFileName;
        [SerializeField] private global::PlayerController playerController;
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private BossHealth bossHealth;
        [SerializeField] private BossFsmController bossFsmController;
        [SerializeField] private BossAgent bossAgent;

        private GameMetricsSession currentSession;
        private bool isSessionActive;
        private bool isSubscribedToSources;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToSources();
        }

        private void OnDisable()
        {
            UnsubscribeFromSources();
        }

        public void Configure(string newBossType, string newOutputDirectory, string newOutputFileName = GameMetricsExporter.DefaultFileName)
        {
            bossType = string.IsNullOrWhiteSpace(newBossType) ? bossType : newBossType;
            outputDirectory = newOutputDirectory;
            outputFileName = string.IsNullOrWhiteSpace(newOutputFileName) ? GameMetricsExporter.DefaultFileName : newOutputFileName;
        }

        public void BindSources(
            global::PlayerController newPlayerController,
            PlayerCombat newPlayerCombat,
            BossHealth newBossHealth,
            BossFsmController newBossFsmController,
            BossAgent newBossAgent = null,
            PlayerHealth newPlayerHealth = null)
        {
            UnsubscribeFromSources();

            playerController = newPlayerController;
            playerCombat = newPlayerCombat;
            playerHealth = newPlayerHealth;
            bossHealth = newBossHealth;
            bossFsmController = newBossFsmController;
            bossAgent = newBossAgent;

            SubscribeToSources();
        }

        public void BeginSession(string sessionId)
        {
            BeginSession(sessionId, Time.time);
        }

        public void BeginSession(string sessionId, float startTimeSeconds)
        {
            currentSession = new GameMetricsSession
            {
                SessionId = string.IsNullOrWhiteSpace(sessionId) ? "evaluation-session" : sessionId,
                BossType = string.IsNullOrWhiteSpace(bossType) ? "PPO" : bossType.Trim(),
                Result = "unfinished",
                StartTimeSeconds = startTimeSeconds,
                EndTimeSeconds = startTimeSeconds
            };
            isSessionActive = true;
        }

        public string EndSession(string result)
        {
            return EndSession(result, Time.time, currentSession?.EpisodeSteps ?? 0);
        }

        public string EndSession(string result, float endTimeSeconds, int episodeSteps)
        {
            if (!isSessionActive || currentSession == null)
            {
                return null;
            }

            currentSession.Result = string.IsNullOrWhiteSpace(result) ? "unfinished" : result;
            currentSession.EndTimeSeconds = endTimeSeconds;
            currentSession.EpisodeSteps = episodeSteps;
            isSessionActive = false;

            return GameMetricsExporter.AppendSession(currentSession, outputDirectory, outputFileName);
        }

        public void RecordEpisodeStep()
        {
            if (isSessionActive && currentSession != null)
            {
                currentSession.EpisodeSteps++;
            }
        }

        public void RecordBossDamageTaken(float damageAmount)
        {
            if (isSessionActive && currentSession != null)
            {
                currentSession.BossDamageTaken += Mathf.Max(0f, damageAmount);
            }
        }

        public void RecordPlayerDamageTaken(float damageAmount)
        {
            if (isSessionActive && currentSession != null)
            {
                currentSession.PlayerDamageTaken += Mathf.Max(0f, damageAmount);
            }
        }

        public void RecordPlayerJump()
        {
            if (isSessionActive && currentSession != null)
            {
                currentSession.PlayerJumpCount++;
            }
        }

        public void RecordPlayerAttack()
        {
            if (isSessionActive && currentSession != null)
            {
                currentSession.PlayerAttackCount++;
            }
        }

        public void RecordPlayerDash()
        {
            if (isSessionActive && currentSession != null)
            {
                currentSession.PlayerDashCount++;
            }
        }

        public void RecordBossAttack()
        {
            if (isSessionActive && currentSession != null)
            {
                currentSession.BossAttackCount++;
            }
        }

        public void RecordPpoAction(GameMetricsPpoAction action)
        {
            if (!isSessionActive || currentSession == null)
            {
                return;
            }

            switch (action)
            {
                case GameMetricsPpoAction.Idle:
                    currentSession.PpoIdleCount++;
                    break;
                case GameMetricsPpoAction.MoveLeft:
                    currentSession.PpoMoveLeftCount++;
                    break;
                case GameMetricsPpoAction.MoveRight:
                    currentSession.PpoMoveRightCount++;
                    break;
                case GameMetricsPpoAction.Jump:
                    currentSession.PpoJumpCount++;
                    break;
                case GameMetricsPpoAction.Attack:
                    currentSession.PpoAttackCount++;
                    break;
                case GameMetricsPpoAction.Dash:
                    currentSession.PpoDashCount++;
                    break;
            }
        }

        private void ResolveReferences()
        {
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<global::PlayerController>();
            }

            if (playerCombat == null)
            {
                playerCombat = FindFirstObjectByType<PlayerCombat>();
            }

            if (playerHealth == null)
            {
                playerHealth = FindFirstObjectByType<PlayerHealth>();
            }

            if (bossHealth == null)
            {
                bossHealth = FindFirstObjectByType<BossHealth>();
            }

            if (bossFsmController == null)
            {
                bossFsmController = FindFirstObjectByType<BossFsmController>();
            }

            if (bossAgent == null)
            {
                bossAgent = FindFirstObjectByType<BossAgent>();
            }
        }

        private void SubscribeToSources()
        {
            if (isSubscribedToSources)
            {
                return;
            }

            if (playerController != null)
            {
                playerController.OnPlayerJump += RecordPlayerJump;
                playerController.OnPlayerDash += RecordPlayerDash;
            }

            if (playerCombat != null)
            {
                playerCombat.OnPlayerAttackPerformed += RecordPlayerAttack;
            }

            if (playerHealth != null)
            {
                playerHealth.OnPlayerDamageTaken += RecordPlayerDamageTaken;
            }

            if (bossHealth != null)
            {
                bossHealth.OnBossDamageTaken += RecordBossDamageTaken;
            }

            if (bossFsmController != null)
            {
                bossFsmController.OnBossAttackPerformed += RecordBossAttack;
            }

            if (bossAgent != null)
            {
                bossAgent.OnDiscreteActionApplied += RecordBossAgentAction;
            }

            isSubscribedToSources = true;
        }

        private void UnsubscribeFromSources()
        {
            if (!isSubscribedToSources)
            {
                return;
            }

            if (playerController != null)
            {
                playerController.OnPlayerJump -= RecordPlayerJump;
                playerController.OnPlayerDash -= RecordPlayerDash;
            }

            if (playerCombat != null)
            {
                playerCombat.OnPlayerAttackPerformed -= RecordPlayerAttack;
            }

            if (playerHealth != null)
            {
                playerHealth.OnPlayerDamageTaken -= RecordPlayerDamageTaken;
            }

            if (bossHealth != null)
            {
                bossHealth.OnBossDamageTaken -= RecordBossDamageTaken;
            }

            if (bossFsmController != null)
            {
                bossFsmController.OnBossAttackPerformed -= RecordBossAttack;
            }

            if (bossAgent != null)
            {
                bossAgent.OnDiscreteActionApplied -= RecordBossAgentAction;
            }

            isSubscribedToSources = false;
        }

        private void RecordBossAgentAction(int actionIndex)
        {
            switch (actionIndex)
            {
                case BossAgent.MoveLeftAction:
                    RecordPpoAction(GameMetricsPpoAction.MoveLeft);
                    break;
                case BossAgent.MoveRightAction:
                    RecordPpoAction(GameMetricsPpoAction.MoveRight);
                    break;
                case BossAgent.JumpAction:
                    RecordPpoAction(GameMetricsPpoAction.Jump);
                    break;
                case BossAgent.AttackMeleeAction:
                    RecordPpoAction(GameMetricsPpoAction.Attack);
                    RecordBossAttack();
                    break;
                case BossAgent.DashAction:
                    RecordPpoAction(GameMetricsPpoAction.Dash);
                    break;
                default:
                    RecordPpoAction(GameMetricsPpoAction.Idle);
                    break;
            }
        }
    }
}
