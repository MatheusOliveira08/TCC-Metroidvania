using System;
using UnityEngine;

namespace TerraSilente.Provenance
{
    [Serializable]
    public class ProvenanceEvent
    {
        public string EventId { get; set; }

        public float Timestamp { get; set; }

        public string ActorId { get; set; }

        public string ActionType { get; set; }

        public Vector2 Position { get; set; }

        public string ParentEventId { get; set; }
    }
}
