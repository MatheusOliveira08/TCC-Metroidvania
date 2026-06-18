using System.IO;
using NUnit.Framework;
using TerraSilente.Provenance;
using UnityEngine;

namespace TerraSilente.Tests.Provenance
{
    public class ProvenanceExporterTests
    {
        [Test]
        public void Export_WithOutputDirectoryAndSessionId_ShouldWriteGraphJsonFile()
        {
            var graph = new ProvenanceGraph();
            graph.AddEvent(new ProvenanceEvent
            {
                EventId = "event-exporter",
                Timestamp = 4f,
                ActorId = "Player",
                ActionType = "PlayerAttack",
                Position = new Vector2(3f, 2f),
                ParentEventId = "event-parent"
            });

            var outputDirectory = Path.Combine(Application.temporaryCachePath, "provenance-exporter-tests");
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }

            try
            {
                var filePath = ProvenanceExporter.Export(graph, outputDirectory, "session-test");

                Assert.That(filePath, Is.EqualTo(Path.Combine(outputDirectory, "session-test.json")));
                Assert.That(File.Exists(filePath), Is.True);

                var json = File.ReadAllText(filePath);
                Assert.That(json, Does.Contain("event-exporter"));
                Assert.That(json, Does.Contain("PlayerAttack"));
                Assert.That(json, Does.Contain("event-parent"));
            }
            finally
            {
                if (Directory.Exists(outputDirectory))
                {
                    Directory.Delete(outputDirectory, true);
                }
            }
        }
    }
}
