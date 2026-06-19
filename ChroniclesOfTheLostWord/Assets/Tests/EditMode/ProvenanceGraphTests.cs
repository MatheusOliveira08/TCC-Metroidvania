using System.IO;
using NUnit.Framework;
using TerraSilente.Provenance;
using UnityEngine;

namespace TerraSilente.Tests.Provenance
{
    public class ProvenanceGraphTests
    {
        [Test]
        public void AddEvent_WhenCalled_ShouldStoreEventInGraph()
        {
            var graph = new ProvenanceGraph();
            var provenanceEvent = new ProvenanceEvent
            {
                EventId = "event-001",
                Timestamp = 1.25f,
                ActorId = "Player",
                ActionType = "PlayerAttack",
                Position = new Vector2(2f, 3f)
            };

            graph.AddEvent(provenanceEvent);

            Assert.That(graph.Events, Has.Count.EqualTo(1));
            Assert.That(graph.Events[0].EventId, Is.EqualTo("event-001"));
            Assert.That(graph.Events[0].ActorId, Is.EqualTo("Player"));
            Assert.That(graph.Events[0].ActionType, Is.EqualTo("PlayerAttack"));
        }

        [Test]
        public void AddEvent_WithParentEventId_ShouldStoreCausalRelationship()
        {
            var graph = new ProvenanceGraph();
            var parentEvent = new ProvenanceEvent
            {
                EventId = "event-dash",
                Timestamp = 2f,
                ActorId = "Player",
                ActionType = "PlayerDash",
                Position = new Vector2(1f, 0f)
            };
            var childEvent = new ProvenanceEvent
            {
                EventId = "event-attack",
                Timestamp = 2.2f,
                ActorId = "Player",
                ActionType = "PlayerAttack",
                Position = new Vector2(2f, 0f),
                ParentEventId = parentEvent.EventId
            };

            graph.AddEvent(parentEvent);
            graph.AddEvent(childEvent);

            Assert.That(graph.Events, Has.Count.EqualTo(2));
            Assert.That(graph.Events[1].ParentEventId, Is.EqualTo("event-dash"));
        }

        [Test]
        public void StartSession_WhenCalled_ShouldStoreSessionMetadata()
        {
            var graph = new ProvenanceGraph();

            graph.StartSession("session-001", 10f);

            Assert.That(graph.Session.SessionId, Is.EqualTo("session-001"));
            Assert.That(graph.Session.Result, Is.EqualTo("unfinished"));
            Assert.That(graph.Session.StartTimestamp, Is.EqualTo(10f));
        }

        [Test]
        public void EndSession_WhenCalled_ShouldStoreResultAndMetrics()
        {
            var graph = new ProvenanceGraph();
            graph.StartSession("session-002", 10f);

            graph.EndSession("victory", 22f, 80f, 0f, 120f, 15f);

            Assert.That(graph.Session.Result, Is.EqualTo("victory"));
            Assert.That(graph.Session.EndTimestamp, Is.EqualTo(22f));
            Assert.That(graph.Session.Duration, Is.EqualTo(12f).Within(0.001f));
            Assert.That(graph.Session.PlayerRemainingHealth, Is.EqualTo(80f));
            Assert.That(graph.Session.BossRemainingHealth, Is.EqualTo(0f));
            Assert.That(graph.Session.TotalDamageDealtByPlayer, Is.EqualTo(120f));
            Assert.That(graph.Session.TotalDamageTakenByPlayer, Is.EqualTo(15f));
        }

        [Test]
        public void ExportToJson_WhenCalled_ShouldCreateJsonFileWithGraphData()
        {
            var graph = new ProvenanceGraph();
            graph.StartSession("session-export", 1f);
            graph.EndSession("victory", 4f, 90f, 0f, 40f, 10f);
            graph.AddEvent(new ProvenanceEvent
            {
                EventId = "event-export",
                Timestamp = 3.5f,
                ActorId = "BossFSM",
                ActionType = "BossDamageTaken",
                Position = new Vector2(5f, 1f),
                ParentEventId = "event-player-attack"
            });

            var filePath = Path.Combine(Application.temporaryCachePath, "provenance-graph-test.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            try
            {
                graph.ExportToJson(filePath);

                Assert.That(File.Exists(filePath), Is.True);

                var json = File.ReadAllText(filePath);
                Assert.That(json, Does.Contain("event-export"));
                Assert.That(json, Does.Contain("BossFSM"));
                Assert.That(json, Does.Contain("BossDamageTaken"));
                Assert.That(json, Does.Contain("event-player-attack"));
                Assert.That(json, Does.Contain("\"sessionId\": \"session-export\""));
                Assert.That(json, Does.Contain("\"result\": \"victory\""));
                Assert.That(json, Does.Contain("\"parentEventId\""));
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }
    }
}
