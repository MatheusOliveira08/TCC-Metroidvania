using System.IO;
using System.Linq;
using NUnit.Framework;
using TerraSilente.Arena;
using TerraSilente.Boss;
using TerraSilente.Player;
using TerraSilente.Provenance;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        [Test]
        public void BossDeath_WhenDamageWasLogged_ShouldKeepAggregatedDamageTotals()
        {
            arenaManager.StartFight("arena-aggregate-damage");
            provenanceLogger.LogPlayerDamageDealt(25f);

            bossHealth.TakeDamage(100f);

            Assert.That(provenanceLogger.Graph.Session.TotalDamageDealtByPlayer, Is.EqualTo(25f));
        }

        [Test]
        public void AppendSession_WhenCsvDoesNotExist_ShouldCreateDirectoryHeaderAndInvariantRow()
        {
            var outputDirectory = Path.Combine(Application.temporaryCachePath, "game-metrics-create-test");
            DeleteDirectoryIfExists(outputDirectory);

            try
            {
                var session = new GameMetricsSession
                {
                    SessionId = "session-ppo",
                    BossType = "PPO",
                    Result = "victory",
                    StartTimeSeconds = 1.5f,
                    EndTimeSeconds = 5.75f,
                    EpisodeSteps = 42,
                    BossDamageTaken = 30.5f,
                    PlayerDamageTaken = 7.25f,
                    PlayerJumpCount = 1,
                    PlayerAttackCount = 2,
                    PlayerDashCount = 3,
                    BossAttackCount = 4,
                    PpoIdleCount = 5,
                    PpoMoveLeftCount = 6,
                    PpoMoveRightCount = 7,
                    PpoJumpCount = 8,
                    PpoAttackCount = 9,
                    PpoDashCount = 10
                };

                var csvPath = GameMetricsExporter.AppendSession(session, outputDirectory);
                var lines = File.ReadAllLines(csvPath);

                Assert.That(csvPath, Is.EqualTo(Path.Combine(outputDirectory, GameMetricsExporter.DefaultFileName)));
                Assert.That(lines[0], Is.EqualTo(GameMetricsExporter.Header));
                Assert.That(lines[1], Is.EqualTo("session-ppo,PPO,victory,1.5,5.75,4.25,42,30.5,7.25,1,2,3,4,5,6,7,8,9,10"));
            }
            finally
            {
                DeleteDirectoryIfExists(outputDirectory);
            }
        }

        [Test]
        public void AppendSession_WhenCsvAlreadyExists_ShouldAppendWithoutDuplicatingHeader()
        {
            var outputDirectory = Path.Combine(Application.temporaryCachePath, "game-metrics-append-test");
            DeleteDirectoryIfExists(outputDirectory);

            try
            {
                GameMetricsExporter.AppendSession(new GameMetricsSession
                {
                    SessionId = "session-fsm-1",
                    BossType = "FSM",
                    Result = "defeat"
                }, outputDirectory);

                var csvPath = GameMetricsExporter.AppendSession(new GameMetricsSession
                {
                    SessionId = "session-fsm-2",
                    BossType = "FSM",
                    Result = "victory"
                }, outputDirectory);

                var lines = File.ReadAllLines(csvPath);

                Assert.That(lines, Has.Length.EqualTo(3));
                Assert.That(lines.Count(line => line == GameMetricsExporter.Header), Is.EqualTo(1));
                Assert.That(lines[1], Does.StartWith("session-fsm-1,FSM,defeat,"));
                Assert.That(lines[2], Does.StartWith("session-fsm-2,FSM,victory,"));
            }
            finally
            {
                DeleteDirectoryIfExists(outputDirectory);
            }
        }

        [Test]
        public void GameMetrics_WhenSessionEnds_ShouldExportRecordedCounts()
        {
            var outputDirectory = Path.Combine(Application.temporaryCachePath, "game-metrics-component-test");
            DeleteDirectoryIfExists(outputDirectory);
            var metricsObject = new GameObject("Game Metrics Test");

            try
            {
                var metrics = metricsObject.AddComponent<GameMetrics>();
                metrics.Configure("PPO", outputDirectory);
                metrics.BeginSession("session-component", 10f);

                metrics.RecordBossDamageTaken(12f);
                metrics.RecordPlayerDamageTaken(3f);
                metrics.RecordPlayerJump();
                metrics.RecordPlayerAttack();
                metrics.RecordPlayerDash();
                metrics.RecordBossAttack();
                metrics.RecordPpoAction(GameMetricsPpoAction.Idle);
                metrics.RecordPpoAction(GameMetricsPpoAction.MoveLeft);

                var csvPath = metrics.EndSession("victory", 14.5f, 99);
                var lines = File.ReadAllLines(csvPath);

                Assert.That(lines[1], Is.EqualTo("session-component,PPO,victory,10,14.5,4.5,99,12,3,1,1,1,1,1,1,0,0,0,0"));
            }
            finally
            {
                Object.DestroyImmediate(metricsObject);
                DeleteDirectoryIfExists(outputDirectory);
            }
        }

        [Test]
        public void ArenaManager_WhenMetricsAreBound_ShouldExportCsvWhenFightEnds()
        {
            var outputDirectory = Path.Combine(Application.temporaryCachePath, "arena-manager-metrics-test");
            DeleteDirectoryIfExists(outputDirectory);
            var metricsObject = new GameObject("Arena Manager Metrics Test");

            try
            {
                var metrics = metricsObject.AddComponent<GameMetrics>();
                metrics.Configure("FSM", outputDirectory);
                arenaManager.BindMetrics(metrics);

                arenaManager.StartFight("arena-metrics-session");
                arenaManager.EndFightAsVictory();

                var csvPath = Path.Combine(outputDirectory, GameMetricsExporter.DefaultFileName);
                var lines = File.ReadAllLines(csvPath);

                Assert.That(lines[1], Does.StartWith("arena-metrics-session,FSM,victory,"));
            }
            finally
            {
                Object.DestroyImmediate(metricsObject);
                DeleteDirectoryIfExists(outputDirectory);
            }
        }

        [Test]
        public void GameMetrics_WhenCombatSourcesEmitEvents_ShouldExportRecordedCombatMetrics()
        {
            var outputDirectory = Path.Combine(Application.temporaryCachePath, "game-metrics-source-test");
            DeleteDirectoryIfExists(outputDirectory);

            var playerObject = new GameObject("Metrics Player Source");
            var bossObject = new GameObject("Metrics Boss Source");
            var metricsObject = new GameObject("Metrics Source Test");

            try
            {
                playerObject.AddComponent<Rigidbody2D>();
                playerObject.AddComponent<BoxCollider2D>();
                var playerController = playerObject.AddComponent<global::PlayerController>();
                var playerCombat = playerObject.AddComponent<PlayerCombat>();

                bossObject.AddComponent<Rigidbody2D>();
                var sourceBossHealth = bossObject.AddComponent<BossHealth>();
                sourceBossHealth.ResetHealth();
                var bossFsmController = bossObject.AddComponent<BossFsmController>();
                bossFsmController.BindDependencies(playerObject.transform, sourceBossHealth);

                var metrics = metricsObject.AddComponent<GameMetrics>();
                metrics.Configure("FSM", outputDirectory);
                metrics.BindSources(playerController, playerCombat, sourceBossHealth, bossFsmController);
                metrics.BeginSession("source-session", 0f);

                playerCombat.PerformAttack();
                sourceBossHealth.TakeDamage(7f);
                bossFsmController.Tick(1f);

                var csvPath = metrics.EndSession("victory", 3f, 12);
                var lines = File.ReadAllLines(csvPath);

                Assert.That(lines[1], Is.EqualTo("source-session,FSM,victory,0,3,3,12,7,0,0,1,0,1,0,0,0,0,0,0"));
            }
            finally
            {
                Object.DestroyImmediate(metricsObject);
                Object.DestroyImmediate(bossObject);
                Object.DestroyImmediate(playerObject);
                DeleteDirectoryIfExists(outputDirectory);
            }
        }

        [Test]
        public void ArenaChefeScene_ShouldContainGameMetricsConfiguredForFsmEvaluation()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ArenaChefe.unity", OpenSceneMode.Single);

            var metrics = FindSingleComponent<GameMetrics>(scene);
            var manager = FindSingleComponent<ArenaManager>(scene);
            var serializedMetrics = new SerializedObject(metrics);
            var serializedManager = new SerializedObject(manager);

            Assert.That(serializedMetrics.FindProperty("bossType").stringValue, Is.EqualTo("FSM"));
            Assert.That(serializedMetrics.FindProperty("outputDirectory").stringValue, Is.EqualTo("TreinamentoML/evaluation_data"));
            Assert.That(serializedMetrics.FindProperty("outputFileName").stringValue, Is.EqualTo(GameMetricsExporter.DefaultFileName));
            Assert.That(serializedManager.FindProperty("gameMetrics").objectReferenceValue, Is.EqualTo(metrics));
        }

        [Test]
        public void BossArenaPpoScene_ShouldContainGameMetricsConfiguredForPpoEvaluation()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/BossArena_PPO.unity", OpenSceneMode.Single);

            var metrics = FindSingleComponent<GameMetrics>(scene);
            var manager = FindSingleComponent<ArenaManager>(scene);
            var serializedMetrics = new SerializedObject(metrics);
            var serializedManager = new SerializedObject(manager);

            Assert.That(serializedMetrics.FindProperty("bossType").stringValue, Is.EqualTo("PPO"));
            Assert.That(serializedMetrics.FindProperty("outputDirectory").stringValue, Is.EqualTo("TreinamentoML/evaluation_data"));
            Assert.That(serializedMetrics.FindProperty("outputFileName").stringValue, Is.EqualTo(GameMetricsExporter.DefaultFileName));
            Assert.That(serializedManager.FindProperty("gameMetrics").objectReferenceValue, Is.EqualTo(metrics));
        }

        private static void DeleteDirectoryIfExists(string outputDirectory)
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }
        }

        private static T FindSingleComponent<T>(Scene scene) where T : Component
        {
            var components = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();

            Assert.That(components, Has.Length.EqualTo(1));
            return components[0];
        }
    }
}
