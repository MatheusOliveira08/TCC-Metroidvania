using System;
using TerraSilente.Provenance;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TerraSilente.Arena
{
    [DisallowMultipleComponent]
    public class ArenaManager : MonoBehaviour
    {
        [SerializeField] private ProvenanceLogger provenanceLogger;
        [SerializeField] private bool startOnSceneStart = true;
        [SerializeField] private string sessionIdPrefix = "arena";
        [SerializeField] private bool enableDebugVictoryHotkey = true;

        public bool IsFightActive { get; private set; }

        public string CurrentSessionId { get; private set; }

        private void Awake()
        {
            if (provenanceLogger == null)
            {
                provenanceLogger = FindFirstObjectByType<ProvenanceLogger>();
            }
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
            // TODO: remover quando BossHealth/BossFSM encerrarem a luta de verdade.
            if (enableDebugVictoryHotkey && Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
            {
                EndFightAsVictory();
            }
        }

        public void StartFight()
        {
            StartFight(BuildSessionId());
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
            float totalDamageDealtByPlayer = 0f,
            float totalDamageTakenByPlayer = 0f)
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

            IsFightActive = false;
        }

        private string BuildSessionId()
        {
            var prefix = string.IsNullOrWhiteSpace(sessionIdPrefix) ? "arena" : sessionIdPrefix;
            return prefix + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
        }
    }
}
