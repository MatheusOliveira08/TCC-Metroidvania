using System.IO;
using NUnit.Framework;
using TerraSilente.Boss;
using TerraSilente.Player;
using TerraSilente.Provenance;
using UnityEngine;

namespace TerraSilente.Tests.Provenance
{
    public class ProvenanceLoggerTests
    {
        private GameObject loggerObject;
        private GameObject playerObject;
        private GameObject bossObject;
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
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(bossObject);
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
        public void StartSession_WhenCalled_ShouldAddSessionStartEvent()
        {
            logger.StartSession("session-logger");

            Assert.That(logger.Graph.Session.SessionId, Is.EqualTo("session-logger"));
            Assert.That(logger.Graph.Session.Result, Is.EqualTo("unfinished"));
            Assert.That(logger.Graph.Events, Has.Count.EqualTo(1));
            Assert.That(logger.Graph.Events[0].ActorId, Is.EqualTo("System"));
            Assert.That(logger.Graph.Events[0].ActionType, Is.EqualTo("SessionStart"));
        }

        [Test]
        public void LogPlayerJump_AfterSessionStart_ShouldLinkToSessionStart()
        {
            logger.StartSession("session-player-root");
            var sessionStartEventId = logger.Graph.Events[0].EventId;

            logger.LogPlayerJump();

            Assert.That(logger.Graph.Events, Has.Count.EqualTo(2));
            Assert.That(logger.Graph.Events[1].ActionType, Is.EqualTo("PlayerJump"));
            Assert.That(logger.Graph.Events[1].ParentEventId, Is.EqualTo(sessionStartEventId));
        }

        [Test]
        public void LogBossAttack_AfterSessionStart_ShouldLinkToSessionStart()
        {
            logger.StartSession("session-boss-root");
            var sessionStartEventId = logger.Graph.Events[0].EventId;

            logger.LogBossAttack();

            Assert.That(logger.Graph.Events, Has.Count.EqualTo(2));
            Assert.That(logger.Graph.Events[1].ActionType, Is.EqualTo("BossAttack"));
            Assert.That(logger.Graph.Events[1].ParentEventId, Is.EqualTo(sessionStartEventId));
        }

        [Test]
        public void LogBossDamageTaken_AfterSessionStartWithoutPlayerEvent_ShouldLinkToSessionStart()
        {
            logger.StartSession("session-boss-damage-root");
            var sessionStartEventId = logger.Graph.Events[0].EventId;

            logger.LogBossDamageTaken(10f);

            Assert.That(logger.Graph.Events, Has.Count.EqualTo(2));
            Assert.That(logger.Graph.Events[1].ActionType, Is.EqualTo("BossDamageTaken"));
            Assert.That(logger.Graph.Events[1].ParentEventId, Is.EqualTo(sessionStartEventId));
            Assert.That(logger.Graph.Events[1].Value, Is.EqualTo(10f));
        }

        [Test]
        public void LogPlayerDamageTaken_AfterSessionStartWithoutBossEvent_ShouldLinkToSessionStart()
        {
            logger.StartSession("session-player-damage-root");
            var sessionStartEventId = logger.Graph.Events[0].EventId;

            logger.LogPlayerDamageTaken(7f);

            Assert.That(logger.Graph.Events, Has.Count.EqualTo(2));
            Assert.That(logger.Graph.Events[1].ActionType, Is.EqualTo("PlayerDamageTaken"));
            Assert.That(logger.Graph.Events[1].ParentEventId, Is.EqualTo(sessionStartEventId));
            Assert.That(logger.Graph.Events[1].Value, Is.EqualTo(7f));
        }

        [Test]
        public void EndSession_WhenCalled_ShouldAddSessionEndEventAndStoreResult()
        {
            logger.ExportOnSessionEnd = false;
            logger.StartSession("session-end");
            logger.LogPlayerAttack();

            logger.EndSession("victory", 75f, 0f, 100f, 20f);

            Assert.That(logger.Graph.Session.Result, Is.EqualTo("victory"));
            Assert.That(logger.Graph.Session.PlayerRemainingHealth, Is.EqualTo(75f));
            Assert.That(logger.Graph.Session.BossRemainingHealth, Is.EqualTo(0f));
            Assert.That(logger.Graph.Events[^1].ActionType, Is.EqualTo("SessionEnd"));
        }

        [Test]
        public void LogBossDamageTaken_AfterPlayerAttack_ShouldLinkDamageToPlayerAttack()
        {
            logger.LogPlayerAttack();
            var attackEventId = logger.Graph.Events[0].EventId;

            logger.LogBossDamageTaken(10f);

            Assert.That(logger.Graph.Events, Has.Count.EqualTo(2));
            Assert.That(logger.Graph.Events[1].ActorId, Is.EqualTo("Boss"));
            Assert.That(logger.Graph.Events[1].ActionType, Is.EqualTo("BossDamageTaken"));
            Assert.That(logger.Graph.Events[1].ParentEventId, Is.EqualTo(attackEventId));
            Assert.That(logger.Graph.Events[1].Value, Is.EqualTo(10f));
        }

        [Test]
        public void CombatEvents_WhenPlayerAttackHitsBoss_ShouldLogDamageDealtAndBossDamageTakenWithCausalChain()
        {
            var playerCombat = CreatePlayerCombat();
            var bossHealth = CreateBossHealth(new Vector3(0.5f, 0f, 0f));
            RecreateLoggerAfterCombatObjects(playerCombat, bossHealth);
            logger.StartSession("session-combat-hit");
            var sessionStartEventId = logger.Graph.Events[0].EventId;

            playerCombat.PerformAttack();

            Assert.That(bossHealth.CurrentHealth, Is.EqualTo(90f));
            Assert.That(logger.Graph.Events, Has.Count.EqualTo(4));
            Assert.That(logger.Graph.Events[1].ActorId, Is.EqualTo("Player"));
            Assert.That(logger.Graph.Events[1].ActionType, Is.EqualTo("PlayerAttack"));
            Assert.That(logger.Graph.Events[1].ParentEventId, Is.EqualTo(sessionStartEventId));
            Assert.That(logger.Graph.Events[2].ActorId, Is.EqualTo("Player"));
            Assert.That(logger.Graph.Events[2].ActionType, Is.EqualTo("PlayerDamageDealt"));
            Assert.That(logger.Graph.Events[2].ParentEventId, Is.EqualTo(logger.Graph.Events[1].EventId));
            Assert.That(logger.Graph.Events[2].Value, Is.EqualTo(10f));
            Assert.That(logger.Graph.Events[3].ActorId, Is.EqualTo("Boss"));
            Assert.That(logger.Graph.Events[3].ActionType, Is.EqualTo("BossDamageTaken"));
            Assert.That(logger.Graph.Events[3].ParentEventId, Is.EqualTo(logger.Graph.Events[2].EventId));
            Assert.That(logger.Graph.Events[3].Value, Is.EqualTo(10f));
        }

        [Test]
        public void CombatEvents_WhenBossDies_ShouldLogBossDeathAfterDamageTaken()
        {
            var bossHealth = CreateBossHealth(Vector3.zero);
            RecreateLoggerAfterCombatObjects(null, bossHealth);
            logger.StartSession("session-boss-death");
            var sessionStartEventId = logger.Graph.Events[0].EventId;

            bossHealth.TakeDamage(100f);

            Assert.That(logger.Graph.Events, Has.Count.EqualTo(3));
            Assert.That(logger.Graph.Events[1].ActorId, Is.EqualTo("Boss"));
            Assert.That(logger.Graph.Events[1].ActionType, Is.EqualTo("BossDamageTaken"));
            Assert.That(logger.Graph.Events[1].ParentEventId, Is.EqualTo(sessionStartEventId));
            Assert.That(logger.Graph.Events[1].Value, Is.EqualTo(100f));
            Assert.That(logger.Graph.Events[2].ActorId, Is.EqualTo("Boss"));
            Assert.That(logger.Graph.Events[2].ActionType, Is.EqualTo("BossDeath"));
            Assert.That(logger.Graph.Events[2].ParentEventId, Is.EqualTo(logger.Graph.Events[1].EventId));
        }

        [Test]
        public void EndSession_WithExportEnabled_ShouldWriteSessionJson()
        {
            var outputDirectory = Path.Combine(Application.temporaryCachePath, "provenance-logger-tests");
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }

            try
            {
                logger.OutputDirectory = outputDirectory;
                logger.ExportOnSessionEnd = true;
                logger.StartSession("session-auto-export");
                logger.LogPlayerAttack();

                logger.EndSession("victory");

                Assert.That(logger.LastExportedFilePath, Is.EqualTo(Path.Combine(outputDirectory, "session-auto-export.json")));
                Assert.That(File.Exists(logger.LastExportedFilePath), Is.True);

                var json = File.ReadAllText(logger.LastExportedFilePath);
                Assert.That(json, Does.Contain("session-auto-export"));
                Assert.That(json, Does.Contain("victory"));
                Assert.That(json, Does.Contain("SessionEnd"));
            }
            finally
            {
                if (Directory.Exists(outputDirectory))
                {
                    Directory.Delete(outputDirectory, true);
                }
            }
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

        private PlayerCombat CreatePlayerCombat()
        {
            playerObject = new GameObject("Player Combat Provenance Test");
            playerObject.transform.position = Vector3.zero;
            return playerObject.AddComponent<PlayerCombat>();
        }

        private BossHealth CreateBossHealth(Vector3 position)
        {
            bossObject = new GameObject("Boss Provenance Test");
            bossObject.transform.position = position;
            bossObject.AddComponent<BoxCollider2D>();
            var bossHealth = bossObject.AddComponent<BossHealth>();
            bossHealth.ResetHealth();
            Physics2D.SyncTransforms();
            return bossHealth;
        }

        private void RecreateLoggerAfterCombatObjects(PlayerCombat playerCombat, BossHealth bossHealth)
        {
            Object.DestroyImmediate(loggerObject);
            loggerObject = new GameObject("Provenance Logger Test");
            loggerObject.SetActive(false);
            logger = loggerObject.AddComponent<ProvenanceLogger>();
            logger.ExportOnSessionEnd = false;
            logger.BindCombatSources(playerCombat, bossHealth);
            loggerObject.SetActive(true);
        }
    }
}
