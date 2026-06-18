using UnityEngine;

namespace TerraSilente.Provenance
{
    [DisallowMultipleComponent]
    public class ProvenanceLogger : MonoBehaviour
    {
        [SerializeField] private global::PlayerController playerController;
        [SerializeField] private string playerActorId = "Player";

        private ProvenanceGraph graph = new();
        private string lastPlayerEventId;

        public ProvenanceGraph Graph => graph;

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
        }

        public void LogPlayerJump()
        {
            LogPlayerAction("PlayerJump");
        }

        public void LogPlayerAttack()
        {
            LogPlayerAction("PlayerAttack");
        }

        private void LogPlayerAction(string actionType)
        {
            var provenanceEvent = new ProvenanceEvent
            {
                Timestamp = Time.time,
                ActorId = playerActorId,
                ActionType = actionType,
                Position = GetPlayerPosition(),
                ParentEventId = lastPlayerEventId
            };

            graph.AddEvent(provenanceEvent);
            lastPlayerEventId = provenanceEvent.EventId;
        }

        private Vector2 GetPlayerPosition()
        {
            if (playerController != null)
            {
                return playerController.transform.position;
            }

            return transform.position;
        }
    }
}
