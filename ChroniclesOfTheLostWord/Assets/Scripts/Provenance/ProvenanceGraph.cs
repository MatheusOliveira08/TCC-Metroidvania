using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TerraSilente.Provenance
{
    public class ProvenanceGraph
    {
        private readonly List<ProvenanceEvent> events = new();

        public ProvenanceSession Session { get; private set; } = ProvenanceSession.CreateUnstarted();

        public IReadOnlyList<ProvenanceEvent> Events => events;

        public void StartSession(string sessionId, float timestamp)
        {
            Session = new ProvenanceSession
            {
                SessionId = string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString() : sessionId,
                Result = "unfinished",
                StartTimestamp = timestamp
            };
        }

        public void EndSession(
            string result,
            float timestamp,
            float playerRemainingHealth = 0f,
            float bossRemainingHealth = 0f,
            float totalDamageDealtByPlayer = 0f,
            float totalDamageTakenByPlayer = 0f)
        {
            if (string.IsNullOrWhiteSpace(Session.SessionId))
            {
                StartSession(null, 0f);
            }

            Session.Result = string.IsNullOrWhiteSpace(result) ? "unfinished" : result;
            Session.EndTimestamp = timestamp;
            Session.Duration = Mathf.Max(0f, timestamp - Session.StartTimestamp);
            Session.PlayerRemainingHealth = playerRemainingHealth;
            Session.BossRemainingHealth = bossRemainingHealth;
            Session.TotalDamageDealtByPlayer = totalDamageDealtByPlayer;
            Session.TotalDamageTakenByPlayer = totalDamageTakenByPlayer;
        }

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

            var snapshot = new ProvenanceGraphSnapshot(Session, events);
            var json = JsonUtility.ToJson(snapshot, true);

            File.WriteAllText(filePath, json);
        }

        [Serializable]
        private sealed class ProvenanceGraphSnapshot
        {
            public string sessionId;
            public string result;
            public float startTimestamp;
            public float endTimestamp;
            public float duration;
            public float playerRemainingHealth;
            public float bossRemainingHealth;
            public float totalDamageDealtByPlayer;
            public float totalDamageTakenByPlayer;
            public List<ProvenanceEventSnapshot> events = new();

            public ProvenanceGraphSnapshot(ProvenanceSession session, IEnumerable<ProvenanceEvent> sourceEvents)
            {
                sessionId = session.SessionId;
                result = session.Result;
                startTimestamp = session.StartTimestamp;
                endTimestamp = session.EndTimestamp;
                duration = session.Duration;
                playerRemainingHealth = session.PlayerRemainingHealth;
                bossRemainingHealth = session.BossRemainingHealth;
                totalDamageDealtByPlayer = session.TotalDamageDealtByPlayer;
                totalDamageTakenByPlayer = session.TotalDamageTakenByPlayer;

                foreach (var sourceEvent in sourceEvents)
                {
                    events.Add(new ProvenanceEventSnapshot(sourceEvent));
                }
            }
        }

        [Serializable]
        private sealed class ProvenanceEventSnapshot
        {
            public string eventId;
            public float timestamp;
            public string actorId;
            public string actionType;
            public Vector2 position;
            public float value;
            public string parentEventId;

            public ProvenanceEventSnapshot(ProvenanceEvent sourceEvent)
            {
                eventId = sourceEvent.EventId;
                timestamp = sourceEvent.Timestamp;
                actorId = sourceEvent.ActorId;
                actionType = sourceEvent.ActionType;
                position = sourceEvent.Position;
                value = sourceEvent.Value;
                parentEventId = sourceEvent.ParentEventId;
            }
        }
    }

    public sealed class ProvenanceSession
    {
        public string SessionId { get; set; }
        public string Result { get; set; }
        public float StartTimestamp { get; set; }
        public float EndTimestamp { get; set; }
        public float Duration { get; set; }
        public float PlayerRemainingHealth { get; set; }
        public float BossRemainingHealth { get; set; }
        public float TotalDamageDealtByPlayer { get; set; }
        public float TotalDamageTakenByPlayer { get; set; }

        public static ProvenanceSession CreateUnstarted()
        {
            return new ProvenanceSession
            {
                Result = "unfinished"
            };
        }
    }
}
