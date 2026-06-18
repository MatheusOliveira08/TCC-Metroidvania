using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TerraSilente.Provenance
{
    public class ProvenanceGraph
    {
        private readonly List<ProvenanceEvent> events = new();

        public IReadOnlyList<ProvenanceEvent> Events => events;

        public void AddEvent(ProvenanceEvent provenanceEvent)
        {
            if (provenanceEvent == null)
            {
                throw new ArgumentNullException(nameof(provenanceEvent));
            }

            if (string.IsNullOrWhiteSpace(provenanceEvent.EventId))
            {
                provenanceEvent.EventId = Guid.NewGuid().ToString();
            }

            events.Add(provenanceEvent);
        }

        public void ExportToJson(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Export path cannot be empty.", nameof(filePath));
            }

            var directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var snapshot = new ProvenanceGraphSnapshot(events);
            var json = JsonUtility.ToJson(snapshot, true);

            File.WriteAllText(filePath, json);
        }

        [Serializable]
        private sealed class ProvenanceGraphSnapshot
        {
            public List<ProvenanceEventSnapshot> Events = new();

            public ProvenanceGraphSnapshot(IEnumerable<ProvenanceEvent> sourceEvents)
            {
                foreach (var sourceEvent in sourceEvents)
                {
                    Events.Add(new ProvenanceEventSnapshot(sourceEvent));
                }
            }
        }

        [Serializable]
        private sealed class ProvenanceEventSnapshot
        {
            public string EventId;
            public float Timestamp;
            public string ActorId;
            public string ActionType;
            public Vector2 Position;
            public string ParentEventId;

            public ProvenanceEventSnapshot(ProvenanceEvent sourceEvent)
            {
                EventId = sourceEvent.EventId;
                Timestamp = sourceEvent.Timestamp;
                ActorId = sourceEvent.ActorId;
                ActionType = sourceEvent.ActionType;
                Position = sourceEvent.Position;
                ParentEventId = sourceEvent.ParentEventId;
            }
        }
    }
}
