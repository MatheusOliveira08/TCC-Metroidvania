using TerraSilente.Boss;
using TerraSilente.Player;
using UnityEngine;

namespace TerraSilente.Provenance
{
    [DisallowMultipleComponent]
    public class ProvenanceLogger : MonoBehaviour
    {
        [SerializeField] private global::PlayerController playerController;
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private BossHealth bossHealth;
        [SerializeField] private Transform bossTransform;
        [SerializeField] private string playerActorId = "Player";
        [SerializeField] private string bossActorId = "Boss";
        [SerializeField] private string systemActorId = "System";
        [SerializeField] private bool exportOnSessionEnd = true;
        [SerializeField] private string outputDirectory;

        private ProvenanceGraph graph = new();
        private string lastPlayerEventId;
        private string lastPlayerActionType;
        private string lastBossEventId;
        private string sessionStartEventId;
        private bool isSubscribedToSources;

        public ProvenanceGraph Graph => graph;

        public bool ExportOnSessionEnd
        {
            get => exportOnSessionEnd;
            set => exportOnSessionEnd = value;
        }

        public string OutputDirectory
        {
            get => outputDirectory;
            set => outputDirectory = value;
        }

        public string LastExportedFilePath { get; private set; }

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

        public void BindCombatSources(PlayerCombat newPlayerCombat, BossHealth newBossHealth)
        {
            UnsubscribeFromSources();

            playerCombat = newPlayerCombat;
            bossHealth = newBossHealth;

            if (bossHealth != null)
            {
                bossTransform = bossHealth.transform;
            }

            SubscribeToSources();
        }

        public void ResetSession()
        {
            graph = new ProvenanceGraph();
            lastPlayerEventId = null;
            lastPlayerActionType = null;
            lastBossEventId = null;
            sessionStartEventId = null;
            LastExportedFilePath = null;
        }

        public void StartSession(string sessionId = null)
        {
            graph.StartSession(sessionId, Time.time);
            var sessionStartEvent = AddEvent(systemActorId, "SessionStart", transform.position, null);
            sessionStartEventId = sessionStartEvent.EventId;
        }

        public void EndSession(
            string result,
            float playerRemainingHealth = 0f,
            float bossRemainingHealth = 0f,
            float totalDamageDealtByPlayer = 0f,
            float totalDamageTakenByPlayer = 0f)
        {
            if (string.IsNullOrWhiteSpace(graph.Session.SessionId))
            {
                StartSession();
            }

            var parentEventId = GetLastEventId();

            graph.EndSession(
                result,
                Time.time,
                playerRemainingHealth,
                bossRemainingHealth,
                totalDamageDealtByPlayer,
                totalDamageTakenByPlayer);

            AddEvent(systemActorId, "SessionEnd", transform.position, parentEventId);

            if (exportOnSessionEnd)
            {
                LastExportedFilePath = ProvenanceExporter.Export(graph, outputDirectory, graph.Session.SessionId);
            }
        }

        public void LogPlayerJump()
        {
            LogPlayerAction("PlayerJump");
        }

        public void LogPlayerAttack()
        {
            LogPlayerAction("PlayerAttack");
        }

        public void LogPlayerDash()
        {
            LogPlayerAction("PlayerDash");
        }

        public void LogPlayerDamageDealt(float damageAmount = 0f)
        {
            LogPlayerAction("PlayerDamageDealt", damageAmount);
        }

        public void LogPlayerDamageTaken(float damageAmount = 0f)
        {
            var provenanceEvent = AddEvent(
                playerActorId,
                "PlayerDamageTaken",
                GetPlayerPosition(),
                ResolveParentEventId(lastBossEventId),
                damageAmount);
            lastPlayerEventId = provenanceEvent.EventId;
        }

        public void LogBossAttack()
        {
            var provenanceEvent = AddEvent(
                bossActorId,
                "BossAttack",
                GetBossPosition(),
                ResolveParentEventId(lastBossEventId));
            lastBossEventId = provenanceEvent.EventId;
        }

        public void LogBossDamageTaken(float damageAmount = 0f)
        {
            var provenanceEvent = AddEvent(
                bossActorId,
                "BossDamageTaken",
                GetBossPosition(),
                ResolveParentEventId(lastPlayerEventId),
                damageAmount);
            lastBossEventId = provenanceEvent.EventId;
        }

        public void LogBossDeath()
        {
            var provenanceEvent = AddEvent(
                bossActorId,
                "BossDeath",
                GetBossPosition(),
                ResolveParentEventId(lastBossEventId));
            lastBossEventId = provenanceEvent.EventId;
        }

        private void LogBossDamageTakenFromCombat(float damageAmount)
        {
            if (lastPlayerActionType == "PlayerAttack")
            {
                LogPlayerDamageDealt(damageAmount);
            }

            LogBossDamageTaken(damageAmount);
        }

        private void LogPlayerAction(string actionType, float value = 0f)
        {
            var provenanceEvent = AddEvent(
                playerActorId,
                actionType,
                GetPlayerPosition(),
                ResolveParentEventId(lastPlayerEventId),
                value);
            lastPlayerEventId = provenanceEvent.EventId;
            lastPlayerActionType = actionType;
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

            if (bossHealth == null)
            {
                bossHealth = FindFirstObjectByType<BossHealth>();
            }

            if (bossTransform == null && bossHealth != null)
            {
                bossTransform = bossHealth.transform;
            }
        }

        private void SubscribeToSources()
        {
            if (isSubscribedToSources)
            {
                return;
            }

            var hasSubscription = false;

            if (playerController != null)
            {
                playerController.OnPlayerJump += LogPlayerJump;
                hasSubscription = true;
            }

            if (playerCombat != null)
            {
                playerCombat.OnPlayerAttackPerformed += LogPlayerAttack;
                hasSubscription = true;
            }
            else if (playerController != null)
            {
                playerController.OnPlayerAttack += LogPlayerAttack;
            }

            if (bossHealth != null)
            {
                bossHealth.OnBossDamageTaken += LogBossDamageTakenFromCombat;
                bossHealth.OnBossDeath += LogBossDeath;
                hasSubscription = true;
            }

            isSubscribedToSources = hasSubscription;
        }

        private void UnsubscribeFromSources()
        {
            if (!isSubscribedToSources)
            {
                return;
            }

            if (playerController != null)
            {
                playerController.OnPlayerJump -= LogPlayerJump;
                playerController.OnPlayerAttack -= LogPlayerAttack;
            }

            if (playerCombat != null)
            {
                playerCombat.OnPlayerAttackPerformed -= LogPlayerAttack;
            }

            if (bossHealth != null)
            {
                bossHealth.OnBossDamageTaken -= LogBossDamageTakenFromCombat;
                bossHealth.OnBossDeath -= LogBossDeath;
            }

            isSubscribedToSources = false;
        }

        private string ResolveParentEventId(string preferredParentEventId)
        {
            return string.IsNullOrWhiteSpace(preferredParentEventId)
                ? sessionStartEventId
                : preferredParentEventId;
        }

        private ProvenanceEvent AddEvent(string actorId, string actionType, Vector2 position, string parentEventId, float value = 0f)
        {
            var provenanceEvent = new ProvenanceEvent
            {
                Timestamp = Time.time,
                ActorId = actorId,
                ActionType = actionType,
                Position = position,
                Value = value,
                ParentEventId = parentEventId
            };

            graph.AddEvent(provenanceEvent);
            return provenanceEvent;
        }

        private string GetLastEventId()
        {
            return graph.Events.Count == 0 ? null : graph.Events[graph.Events.Count - 1].EventId;
        }

        private Vector2 GetPlayerPosition()
        {
            if (playerController != null)
            {
                return playerController.transform.position;
            }

            return transform.position;
        }

        private Vector2 GetBossPosition()
        {
            if (bossTransform != null)
            {
                return bossTransform.position;
            }

            return transform.position;
        }
    }
}
