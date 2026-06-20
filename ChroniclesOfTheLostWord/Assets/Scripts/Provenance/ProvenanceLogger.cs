using UnityEngine;

namespace TerraSilente.Provenance
{
    [DisallowMultipleComponent]
    public class ProvenanceLogger : MonoBehaviour
    {
        [SerializeField] private global::PlayerController playerController;
        [SerializeField] private Transform bossTransform;
        [SerializeField] private string playerActorId = "Player";
        [SerializeField] private string bossActorId = "Boss";
        [SerializeField] private string systemActorId = "System";
        [SerializeField] private bool exportOnSessionEnd = true;
        [SerializeField] private string outputDirectory;

        private ProvenanceGraph graph = new();
        private string lastPlayerEventId;
        private string lastBossEventId;
        private string sessionStartEventId;

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
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<global::PlayerController>();
            }
        }

        private void OnEnable()
        {
            if (playerController == null)
            {
                return;
            }

            playerController.OnPlayerJump += LogPlayerJump;
            playerController.OnPlayerAttack += LogPlayerAttack;
        }

        private void OnDisable()
        {
            if (playerController == null)
            {
                return;
            }

            playerController.OnPlayerJump -= LogPlayerJump;
            playerController.OnPlayerAttack -= LogPlayerAttack;
        }

        public void ResetSession()
        {
            graph = new ProvenanceGraph();
            lastPlayerEventId = null;
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

        private void LogPlayerAction(string actionType, float value = 0f)
        {
            var provenanceEvent = AddEvent(
                playerActorId,
                actionType,
                GetPlayerPosition(),
                ResolveParentEventId(lastPlayerEventId),
                value);
            lastPlayerEventId = provenanceEvent.EventId;
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
