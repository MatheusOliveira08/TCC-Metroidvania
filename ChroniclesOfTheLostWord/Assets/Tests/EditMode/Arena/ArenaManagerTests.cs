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
        public void AppendSession_WhenOutputDirectoryIsRelative_ShouldResolveFromRepositoryRoot()
        {
            var relativeDirectory = Path.Combine("TreinamentoML", "evaluation_data_relative_test");
            var expectedDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", relativeDirectory));
            DeleteDirectoryIfExists(expectedDirectory);

            try
            {
                var csvPath = GameMetricsExporter.AppendSession(new GameMetricsSession
                {
                    SessionId = "relative-session",
                    BossType = "FSM",
                    Result = "victory"
                }, relativeDirectory);

                Assert.That(csvPath, Is.EqualTo(Path.Combine(expectedDirectory, GameMetricsExporter.DefaultFileName)));
                Assert.That(File.Exists(csvPath), Is.True);
            }
            finally
            {
                DeleteDirectoryIfExists(expectedDirectory);
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
        public void PlayerHealth_WhenDamageReachesZero_ShouldRaiseDamageAndDeathEvents()
        {
            var playerObject = new GameObject("Player Health Test");

            try
            {
                var playerHealth = playerObject.AddComponent<PlayerHealth>();
                playerHealth.ResetHealth();
                var damageTaken = 0f;
                var deathEvents = 0;
                playerHealth.OnPlayerDamageTaken += amount => damageTaken += amount;
                playerHealth.OnPlayerDeath += () => deathEvents++;

                playerHealth.TakeDamage(35f);
                playerHealth.TakeDamage(100f);
                playerHealth.TakeDamage(10f);

                Assert.That(playerHealth.IsDead, Is.True);
                Assert.That(playerHealth.CurrentHealth, Is.Zero);
                Assert.That(damageTaken, Is.EqualTo(100f));
                Assert.That(deathEvents, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void BossAttackDamage_WhenAppliedDirectly_ShouldDamagePlayerInRangeAndRespectCooldown()
        {
            var bossObject = new GameObject("Boss Damage Dealer Test");
            var playerObject = new GameObject("Player Damage Target Test");

            try
            {
                bossObject.transform.position = Vector3.zero;
                playerObject.transform.position = new Vector3(1f, 0f, 0f);
                var playerHealth = playerObject.AddComponent<PlayerHealth>();
                playerHealth.ResetHealth();
                var bossAttackDamage = bossObject.AddComponent<BossAttackDamage>();
                bossAttackDamage.BindSources(playerHealth, bossObject.transform);

                Assert.That(bossAttackDamage.TryApplyDamage(0f), Is.True);
                Assert.That(playerHealth.CurrentHealth, Is.EqualTo(90f));
                Assert.That(bossAttackDamage.TryApplyDamage(0.4f), Is.False);
                Assert.That(playerHealth.CurrentHealth, Is.EqualTo(90f));
                Assert.That(bossAttackDamage.TryApplyDamage(0.8f), Is.True);
                Assert.That(playerHealth.CurrentHealth, Is.EqualTo(80f));

                playerObject.transform.position = new Vector3(5f, 0f, 0f);

                Assert.That(bossAttackDamage.TryApplyDamage(1.6f), Is.False);
                Assert.That(playerHealth.CurrentHealth, Is.EqualTo(80f));
            }
            finally
            {
                Object.DestroyImmediate(bossObject);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void BossAttackDamage_WhenFsmOrPpoAttackEventsFire_ShouldDamagePlayer()
        {
            var playerObject = new GameObject("Player Boss Event Damage Test");
            var fsmBossObject = new GameObject("FSM Boss Damage Event Test");
            var ppoBossObject = new GameObject("PPO Boss Damage Event Test");

            try
            {
                playerObject.transform.position = Vector3.zero;
                var playerHealth = playerObject.AddComponent<PlayerHealth>();
                playerHealth.ResetHealth();

                fsmBossObject.transform.position = new Vector3(0.5f, 0f, 0f);
                fsmBossObject.AddComponent<Rigidbody2D>();
                var fsmBossHealth = fsmBossObject.AddComponent<BossHealth>();
                fsmBossHealth.ResetHealth();
                var bossFsm = fsmBossObject.AddComponent<BossFsmController>();
                bossFsm.BindDependencies(playerObject.transform, fsmBossHealth);
                var fsmAttackDamage = fsmBossObject.AddComponent<BossAttackDamage>();
                fsmAttackDamage.BindSources(playerHealth, fsmBossObject.transform, bossFsm);

                bossFsm.Tick(0.02f);

                Assert.That(playerHealth.CurrentHealth, Is.EqualTo(90f));

                ppoBossObject.transform.position = new Vector3(0.5f, 0f, 0f);
                ppoBossObject.AddComponent<Rigidbody2D>();
                ppoBossObject.AddComponent<BoxCollider2D>();
                var ppoBossHealth = ppoBossObject.AddComponent<BossHealth>();
                ppoBossHealth.ResetHealth();
                var bossAgent = ppoBossObject.AddComponent<BossAgent>();
                var ppoAttackDamage = ppoBossObject.AddComponent<BossAttackDamage>();
                ppoAttackDamage.BindSources(playerHealth, ppoBossObject.transform, null, bossAgent);

                bossAgent.ApplyDiscreteAction(BossAgent.AttackMeleeAction);

                Assert.That(playerHealth.CurrentHealth, Is.EqualTo(80f));
            }
            finally
            {
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(fsmBossObject);
                Object.DestroyImmediate(ppoBossObject);
            }
        }

        [Test]
        public void ArenaManager_WhenPlayerDies_ShouldEndSessionAsDefeatAndExportPlayerDamage()
        {
            var outputDirectory = Path.Combine(Application.temporaryCachePath, "arena-player-defeat-test");
            DeleteDirectoryIfExists(outputDirectory);
            var metricsObject = new GameObject("Arena Player Defeat Metrics Test");
            var playerObject = new GameObject("Arena Player Defeat Health Test");

            try
            {
                var playerHealth = playerObject.AddComponent<PlayerHealth>();
                playerHealth.ResetHealth();
                var metrics = metricsObject.AddComponent<GameMetrics>();
                metrics.Configure("FSM", outputDirectory);
                metrics.BindSources(null, null, bossHealth, null, null, playerHealth);
                arenaManager.BindMetrics(metrics);
                arenaManager.BindDependencies(provenanceLogger, bossHealth, playerHealth);
                arenaManager.StartFight("arena-player-defeat");

                playerHealth.TakeDamage(100f);

                var csvPath = Path.Combine(outputDirectory, GameMetricsExporter.DefaultFileName);
                var lines = File.ReadAllLines(csvPath);
                Assert.That(arenaManager.IsFightActive, Is.False);
                Assert.That(provenanceLogger.Graph.Session.Result, Is.EqualTo("defeat"));
                Assert.That(lines[1], Does.StartWith("arena-player-defeat,FSM,defeat,"));
                Assert.That(lines[1], Does.Contain(",100,"));
            }
            finally
            {
                Object.DestroyImmediate(metricsObject);
                Object.DestroyImmediate(playerObject);
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
        public void GameMetrics_WhenPpoAgentAppliesActions_ShouldExportRecordedPpoActionMetrics()
        {
            var outputDirectory = Path.Combine(Application.temporaryCachePath, "game-metrics-ppo-actions-test");
            DeleteDirectoryIfExists(outputDirectory);

            var bossObject = new GameObject("Boss PPO Metrics Source");
            var metricsObject = new GameObject("Game Metrics PPO Source Test");

            try
            {
                bossObject.AddComponent<BossHealth>();
                var bossAgent = bossObject.AddComponent<BossAgent>();

                var metrics = metricsObject.AddComponent<GameMetrics>();
                metrics.Configure("PPO", outputDirectory);
                metrics.BindSources(null, null, bossObject.GetComponent<BossHealth>(), null, bossAgent);
                metrics.BeginSession("ppo-source-session", 0f);

                bossAgent.ApplyDiscreteAction(BossAgent.IdleAction);
                bossAgent.ApplyDiscreteAction(BossAgent.MoveLeftAction);
                bossAgent.ApplyDiscreteAction(BossAgent.MoveRightAction);
                bossAgent.ApplyDiscreteAction(BossAgent.JumpAction);
                bossAgent.ApplyDiscreteAction(BossAgent.AttackMeleeAction);
                bossAgent.ApplyDiscreteAction(BossAgent.DashAction);

                var csvPath = metrics.EndSession("victory", 3f, 12);
                var lines = File.ReadAllLines(csvPath);

                Assert.That(lines[1], Is.EqualTo("ppo-source-session,PPO,victory,0,3,3,12,0,0,0,0,0,1,1,1,1,1,1,1"));
            }
            finally
            {
                Object.DestroyImmediate(metricsObject);
                Object.DestroyImmediate(bossObject);
                DeleteDirectoryIfExists(outputDirectory);
            }
        }

        [Test]
        public void ArenaChefeScene_ShouldContainGameMetricsConfiguredForFsmEvaluation()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ArenaChefe.unity", OpenSceneMode.Single);

            var metrics = FindSingleComponent<GameMetrics>(scene);
            var manager = FindSingleComponent<ArenaManager>(scene);
            var playerHealth = FindSingleComponent<PlayerHealth>(scene);
            var bossAttackDamage = FindSingleComponent<BossAttackDamage>(scene);
            var serializedMetrics = new SerializedObject(metrics);
            var serializedManager = new SerializedObject(manager);
            var serializedBossAttackDamage = new SerializedObject(bossAttackDamage);

            Assert.That(serializedMetrics.FindProperty("bossType").stringValue, Is.EqualTo("FSM"));
            Assert.That(serializedMetrics.FindProperty("outputDirectory").stringValue, Is.EqualTo("TreinamentoML/evaluation_data"));
            Assert.That(serializedMetrics.FindProperty("outputFileName").stringValue, Is.EqualTo(GameMetricsExporter.DefaultFileName));
            Assert.That(serializedMetrics.FindProperty("playerHealth").objectReferenceValue, Is.EqualTo(playerHealth));
            Assert.That(serializedManager.FindProperty("gameMetrics").objectReferenceValue, Is.EqualTo(metrics));
            Assert.That(serializedManager.FindProperty("playerHealth").objectReferenceValue, Is.EqualTo(playerHealth));
            Assert.That(serializedBossAttackDamage.FindProperty("playerHealth").objectReferenceValue, Is.EqualTo(playerHealth));
            Assert.That(serializedBossAttackDamage.FindProperty("damageAmount").floatValue, Is.EqualTo(10f).Within(0.001f));
            Assert.That(serializedBossAttackDamage.FindProperty("attackRange").floatValue, Is.EqualTo(1.25f).Within(0.001f));
            Assert.That(serializedBossAttackDamage.FindProperty("damageCooldown").floatValue, Is.EqualTo(0.8f).Within(0.001f));
        }

        [Test]
        public void BossArenaPpoScene_ShouldContainGameMetricsConfiguredForPpoEvaluation()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/BossArena_PPO.unity", OpenSceneMode.Single);

            var metrics = FindSingleComponent<GameMetrics>(scene);
            var manager = FindSingleComponent<ArenaManager>(scene);
            var playerController = FindSingleComponent<global::PlayerController>(scene);
            var playerCombat = FindSingleComponent<PlayerCombat>(scene);
            var playerHealth = FindSingleComponent<PlayerHealth>(scene);
            var bossAgent = FindSingleComponent<BossAgent>(scene);
            var bossAttackDamage = FindSingleComponent<BossAttackDamage>(scene);
            var serializedMetrics = new SerializedObject(metrics);
            var serializedManager = new SerializedObject(manager);
            var serializedPlayerCombat = new SerializedObject(playerCombat);
            var serializedBossAttackDamage = new SerializedObject(bossAttackDamage);

            Assert.That(serializedMetrics.FindProperty("bossType").stringValue, Is.EqualTo("PPO"));
            Assert.That(serializedMetrics.FindProperty("outputDirectory").stringValue, Is.EqualTo("TreinamentoML/evaluation_data"));
            Assert.That(serializedMetrics.FindProperty("outputFileName").stringValue, Is.EqualTo(GameMetricsExporter.DefaultFileName));
            Assert.That(serializedMetrics.FindProperty("playerController").objectReferenceValue, Is.EqualTo(playerController));
            Assert.That(serializedMetrics.FindProperty("playerCombat").objectReferenceValue, Is.EqualTo(playerCombat));
            Assert.That(serializedMetrics.FindProperty("playerHealth").objectReferenceValue, Is.EqualTo(playerHealth));
            Assert.That(serializedMetrics.FindProperty("bossAgent").objectReferenceValue, Is.EqualTo(bossAgent));
            Assert.That(serializedManager.FindProperty("gameMetrics").objectReferenceValue, Is.EqualTo(metrics));
            Assert.That(serializedManager.FindProperty("playerHealth").objectReferenceValue, Is.EqualTo(playerHealth));
            Assert.That(serializedPlayerCombat.FindProperty("targetLayers").FindPropertyRelative("m_Bits").intValue, Is.Not.Zero);
            Assert.That(serializedBossAttackDamage.FindProperty("playerHealth").objectReferenceValue, Is.EqualTo(playerHealth));
            Assert.That(serializedBossAttackDamage.FindProperty("bossAgent").objectReferenceValue, Is.EqualTo(bossAgent));
            Assert.That(serializedBossAttackDamage.FindProperty("damageAmount").floatValue, Is.EqualTo(10f).Within(0.001f));
            Assert.That(serializedBossAttackDamage.FindProperty("attackRange").floatValue, Is.EqualTo(1.25f).Within(0.001f));
            Assert.That(serializedBossAttackDamage.FindProperty("damageCooldown").floatValue, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(FindComponents<PlayerDummyAI>(scene), Is.Empty);
        }

        [TestCase("Assets/Scenes/ArenaChefe.unity", "Chao")]
        [TestCase("Assets/Scenes/BossArena_PPO.unity", "TrainingGround")]
        public void EvaluationScenes_ShouldHaveSafeArenaBounds(string scenePath, string groundName)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var ground = FindGameObject(scene, groundName);
            var leftBoundary = FindGameObject(scene, "LeftArenaBoundary");
            var rightBoundary = FindGameObject(scene, "RightArenaBoundary");

            Assert.That(ground, Is.Not.Null);
            Assert.That(ground.transform.localScale.x, Is.GreaterThanOrEqualTo(45f));
            AssertArenaBoundary(leftBoundary, -22.5f);
            AssertArenaBoundary(rightBoundary, 22.5f);
        }

        [Test]
        public void HorizontalCameraFollow_WhenTargetMoves_ShouldFollowOnlyHorizontalAxis()
        {
            var followType = System.Type.GetType("TerraSilente.Arena.HorizontalCameraFollow, TerraSilente.Arena");
            Assert.That(followType, Is.Not.Null);

            var cameraObject = new GameObject("Horizontal Camera Follow Test");
            var targetObject = new GameObject("Camera Follow Target Test");

            try
            {
                cameraObject.transform.position = new Vector3(1f, 2f, -10f);
                targetObject.transform.position = new Vector3(7f, -3f, 0f);

                var follow = cameraObject.AddComponent(followType);
                var serializedFollow = new SerializedObject(follow);
                serializedFollow.FindProperty("target").objectReferenceValue = targetObject.transform;
                serializedFollow.ApplyModifiedPropertiesWithoutUndo();

                var followMethod = followType.GetMethod("FollowTarget");
                Assert.That(followMethod, Is.Not.Null);
                followMethod.Invoke(follow, null);

                Assert.That(cameraObject.transform.position.x, Is.EqualTo(7f).Within(0.001f));
                Assert.That(cameraObject.transform.position.y, Is.EqualTo(2f).Within(0.001f));
                Assert.That(cameraObject.transform.position.z, Is.EqualTo(-10f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(targetObject);
            }
        }

        [TestCase("Assets/Scenes/ArenaChefe.unity")]
        [TestCase("Assets/Scenes/BossArena_PPO.unity")]
        public void EvaluationScenes_ShouldHaveCameraFollowingElianHorizontally(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var cameraObject = FindGameObject(scene, "Main Camera");
            var playerObject = FindGameObject(scene, "Elian");

            Assert.That(cameraObject, Is.Not.Null);
            Assert.That(playerObject, Is.Not.Null);

            var cameraFollow = cameraObject.GetComponent("HorizontalCameraFollow");
            Assert.That(cameraFollow, Is.Not.Null);

            var serializedFollow = new SerializedObject(cameraFollow);
            Assert.That(serializedFollow.FindProperty("target").objectReferenceValue, Is.EqualTo(playerObject.transform));
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
            var components = FindComponents<T>(scene);

            Assert.That(components, Has.Length.EqualTo(1));
            return components[0];
        }

        private static T[] FindComponents<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
        }

        private static GameObject FindGameObject(Scene scene, string objectName)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(transform => transform.name == objectName)
                ?.gameObject;
        }

        private static void AssertArenaBoundary(GameObject boundary, float expectedPositionX)
        {
            Assert.That(boundary, Is.Not.Null);
            Assert.That(boundary.GetComponent<BoxCollider2D>(), Is.Not.Null);
            Assert.That(boundary.GetComponent<SpriteRenderer>(), Is.Null);
            Assert.That(boundary.transform.position.x, Is.EqualTo(expectedPositionX).Within(0.001f));
            Assert.That(boundary.transform.localScale.y, Is.GreaterThanOrEqualTo(8f));
        }
    }
}
