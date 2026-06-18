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
        public void ExportToJson_WhenCalled_ShouldCreateJsonFileWithGraphData()
        {
            var graph = new ProvenanceGraph();
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
