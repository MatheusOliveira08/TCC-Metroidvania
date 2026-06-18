using NUnit.Framework;
using TerraSilente.Provenance;
using UnityEngine;

namespace TerraSilente.Tests.Provenance
{
    public class ProvenanceLoggerTests
    {
        private GameObject loggerObject;
        private ProvenanceLogger logger;

        [SetUp]
        public void SetUp()
        {
            loggerObject = new GameObject("Provenance Logger Test");
            logger = loggerObject.AddComponent<ProvenanceLogger>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(loggerObject);
        }

        [Test]
        public void LogPlayerJump_WhenCalled_ShouldAddPlayerJumpEvent()
        {
            logger.LogPlayerJump();

            Assert.That(logger.Graph.Events, Has.Count.EqualTo(1));
            Assert.That(logger.Graph.Events[0].ActorId, Is.EqualTo("Player"));
            Assert.That(logger.Graph.Events[0].ActionType, Is.EqualTo("PlayerJump"));
            Assert.That(logger.Graph.Events[0].ParentEventId, Is.Null.Or.Empty);
        }

        [Test]
        public void LogPlayerAttack_AfterPlayerJump_ShouldLinkAttackToJump()
        {
            logger.LogPlayerJump();
            var jumpEventId = logger.Graph.Events[0].EventId;

            logger.LogPlayerAttack();

            Assert.That(logger.Graph.Events, Has.Count.EqualTo(2));
            Assert.That(logger.Graph.Events[1].ActionType, Is.EqualTo("PlayerAttack"));
            Assert.That(logger.Graph.Events[1].ParentEventId, Is.EqualTo(jumpEventId));
        }

        [Test]
        public void ResetSession_WhenCalled_ShouldClearGraphAndCausalState()
        {
            logger.LogPlayerJump();

            logger.ResetSession();
            logger.LogPlayerAttack();

            Assert.That(logger.Graph.Events, Has.Count.EqualTo(1));
            Assert.That(logger.Graph.Events[0].ActionType, Is.EqualTo("PlayerAttack"));
            Assert.That(logger.Graph.Events[0].ParentEventId, Is.Null.Or.Empty);
        }
    }
}
