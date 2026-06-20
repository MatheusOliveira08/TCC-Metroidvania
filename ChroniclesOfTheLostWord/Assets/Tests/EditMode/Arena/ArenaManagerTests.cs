using System.Linq;
using NUnit.Framework;
using TerraSilente.Arena;
using TerraSilente.Boss;
using TerraSilente.Provenance;
using UnityEngine;

namespace TerraSilente.Tests.Arena
{
    public class ArenaManagerTests
    {
        private GameObject arenaObject;
        private GameObject loggerObject;
        private GameObject bossObject;
        private ArenaManager arenaManager;
        private ProvenanceLogger provenanceLogger;
        private BossHealth bossHealth;

        [SetUp]
        public void SetUp()
        {
            bossObject = new GameObject("Boss Arena Test");
            bossHealth = bossObject.AddComponent<BossHealth>();
            bossHealth.ResetHealth();

            loggerObject = new GameObject("Provenance Logger Arena Test");
            provenanceLogger = loggerObject.AddComponent<ProvenanceLogger>();
            provenanceLogger.ExportOnSessionEnd = false;

            arenaObject = new GameObject("Arena Manager Test");
            arenaManager = arenaObject.AddComponent<ArenaManager>();

            arenaManager.BindDependencies(provenanceLogger, bossHealth);
            provenanceLogger.BindCombatSources(null, bossHealth);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(arenaObject);
            Object.DestroyImmediate(loggerObject);
            Object.DestroyImmediate(bossObject);
        }

        [Test]
        public void BossDeath_WhenFightIsActive_ShouldEndSessionAsVictoryAfterBossDeath()
        {
            arenaManager.StartFight("arena-boss-death");

            bossHealth.TakeDamage(100f);

            var bossDeathEvent = provenanceLogger.Graph.Events.Single(e => e.ActionType == "BossDeath");
            var sessionEndEvent = provenanceLogger.Graph.Events.Single(e => e.ActionType == "SessionEnd");

            Assert.That(arenaManager.IsFightActive, Is.False);
            Assert.That(provenanceLogger.Graph.Session.Result, Is.EqualTo("victory"));
            Assert.That(sessionEndEvent.ParentEventId, Is.EqualTo(bossDeathEvent.EventId));
            Assert.That(provenanceLogger.Graph.Events[^2].ActionType, Is.EqualTo("BossDeath"));
            Assert.That(provenanceLogger.Graph.Events[^1].ActionType, Is.EqualTo("SessionEnd"));
        }

        [Test]
        public void EndFightAsVictory_WhenBossDeathAlreadyEndedFight_ShouldNotDuplicateSessionEnd()
        {
            arenaManager.StartFight("arena-single-victory");
            bossHealth.TakeDamage(100f);

            arenaManager.EndFightAsVictory();

            Assert.That(provenanceLogger.Graph.Events.Count(e => e.ActionType == "BossDeath"), Is.EqualTo(1));
            Assert.That(provenanceLogger.Graph.Events.Count(e => e.ActionType == "SessionEnd"), Is.EqualTo(1));
        }
    }
}
