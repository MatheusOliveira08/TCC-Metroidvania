using System;
using TerraSilente.Boss;
using TerraSilente.Provenance;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TerraSilente.Arena
{
    [DisallowMultipleComponent]
    public class ArenaManager : MonoBehaviour
    {
        [SerializeField] private ProvenanceLogger provenanceLogger;
        [SerializeField] private BossHealth bossHealth;
        [SerializeField] private GameMetrics gameMetrics;
        [SerializeField] private bool startOnSceneStart = true;
        [SerializeField] private string sessionIdPrefix = "arena";
        [SerializeField] private bool enableDebugVictoryHotkey = true;

        private bool isSubscribedToBossDeath;

        public bool IsFightActive { get; private set; }

        public string CurrentSessionId { get; private set; }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToBossDeath();
        }

        private void OnDisable()
        {
            UnsubscribeFromBossDeath();
        }

        private void Start()
        {
            if (startOnSceneStart)
            {
                StartFight();
            }
        }

        private void Update()
        {
            // Fallback de debug enquanto os fluxos completos de vitória/derrota evoluem.
            if (enableDebugVictoryHotkey && Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
            {
                EndFightAsVictory();
            }
        }

        public void StartFight()
        {
            StartFight(BuildSessionId());
        }

        public void BindDependencies(ProvenanceLogger newProvenanceLogger, BossHealth newBossHealth)
        {
            UnsubscribeFromBossDeath();

            provenanceLogger = newProvenanceLogger;
            bossHealth = newBossHealth;

            SubscribeToBossDeath();
        }

        public void BindMetrics(GameMetrics newGameMetrics)
        {
            gameMetrics = newGameMetrics;
        }

        public void StartFight(string sessionId)
        {
            if (IsFightActive || provenanceLogger == null)
            {
                return;
            }

            CurrentSessionId = string.IsNullOrWhiteSpace(sessionId) ? BuildSessionId() : sessionId;
            provenanceLogger.ResetSession();
            provenanceLogger.StartSession(CurrentSessionId);
            gameMetrics?.BeginSession(CurrentSessionId);
            IsFightActive = true;
        }

        public void EndFightAsVictory()
        {
            EndFight("victory");
        }

        public void EndFightAsDefeat()
        {
            EndFight("defeat");
        }

        public void EndFightAsUnfinished()
        {
            EndFight("unfinished");
        }

        public void EndFight(
            string result,
            float playerRemainingHealth = 0f,
            float bossRemainingHealth = 0f,
            float? totalDamageDealtByPlayer = null,
            float? totalDamageTakenByPlayer = null)
        {
            if (!IsFightActive || provenanceLogger == null)
            {
                return;
            }

            provenanceLogger.EndSession(
                result,
                playerRemainingHealth,
                bossRemainingHealth,
                totalDamageDealtByPlayer,
                totalDamageTakenByPlayer);
            gameMetrics?.EndSession(result);

            IsFightActive = false;
        }

        private void HandleBossDeath()
        {
            if (!IsFightActive)
            {
                return;
            }

            provenanceLogger?.LogBossDeath();
            EndFight("victory", bossRemainingHealth: 0f);
        }

        private void ResolveReferences()
        {
            if (provenanceLogger == null)
            {
                provenanceLogger = FindFirstObjectByType<ProvenanceLogger>();
            }

            if (bossHealth == null)
            {
                bossHealth = FindFirstObjectByType<BossHealth>();
            }

            if (gameMetrics == null)
            {
                gameMetrics = FindFirstObjectByType<GameMetrics>();
            }
        }

        private void SubscribeToBossDeath()
        {
            if (isSubscribedToBossDeath || bossHealth == null)
            {
                return;
            }

            bossHealth.OnBossDeath += HandleBossDeath;
            isSubscribedToBossDeath = true;
        }

        private void UnsubscribeFromBossDeath()
        {
            if (!isSubscribedToBossDeath || bossHealth == null)
            {
                return;
            }

            bossHealth.OnBossDeath -= HandleBossDeath;
            isSubscribedToBossDeath = false;
        }

        private string BuildSessionId()
        {
            var prefix = string.IsNullOrWhiteSpace(sessionIdPrefix) ? "arena" : sessionIdPrefix;
            return prefix + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
        }
    }
}
