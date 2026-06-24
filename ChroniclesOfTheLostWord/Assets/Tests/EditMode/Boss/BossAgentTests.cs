using System;
using NUnit.Framework;
using TerraSilente.Boss;
using TerraSilente.Provenance;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace TerraSilente.Tests.Boss
{
    public class BossAgentTests
    {
        private const string WinningSequencesJson = @"
{
  ""sequenceLength"": 3,
  ""allowedActionTypes"": [""PlayerJump"", ""PlayerAttack"", ""PlayerDash""],
  ""summary"": {
    ""sourceFiles"": 1,
    ""victorySessions"": 1,
    ""bossDamageEvents"": 10,
    ""extractedSequences"": 10,
    ""uniqueSequences"": 1
  },
  ""sequences"": [
    {
      ""actions"": [""PlayerJump"", ""PlayerDash"", ""PlayerAttack""],
      ""frequency"": 3
    }
  ]
}";

        private GameObject bossObject;
        private GameObject bossSpawnObject;
        private GameObject playerObject;
        private GameObject playerSpawnObject;
        private GameObject rewardObject;
        private BossAgent bossAgent;
        private BossHealth bossHealth;
        private SpriteRenderer bossRenderer;
        private ProvenanceRewardShaper rewardShaper;
        private float originalTimeScale;
        private bool originalRunInBackground;

        [SetUp]
        public void SetUp()
        {
            originalTimeScale = Time.timeScale;
            originalRunInBackground = Application.runInBackground;

            bossObject = new GameObject("BossAgentTests");
            bossObject.AddComponent<Rigidbody2D>();
            bossRenderer = bossObject.AddComponent<SpriteRenderer>();
            bossRenderer.color = new Color(0.42f, 0.12f, 0.78f, 1f);
            bossHealth = bossObject.AddComponent<BossHealth>();
            bossHealth.ResetHealth();

            playerObject = new GameObject("BossAgentPlayerTarget");
            playerObject.AddComponent<Rigidbody2D>();
            bossSpawnObject = new GameObject("BossAgentBossSpawn");
            playerSpawnObject = new GameObject("BossAgentPlayerSpawn");

            rewardObject = new GameObject("RewardShaperTests");
            rewardShaper = rewardObject.AddComponent<ProvenanceRewardShaper>();
            rewardShaper.SequenceMatchReward = 2f;
            rewardShaper.LoadFromJson(WinningSequencesJson);

            bossAgent = bossObject.AddComponent<BossAgent>();
            bossAgent.BindDependencies(playerObject.transform, bossHealth, rewardShaper);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = originalTimeScale;
            Application.runInBackground = originalRunInBackground;

            UnityEngine.Object.DestroyImmediate(bossObject);
            UnityEngine.Object.DestroyImmediate(bossSpawnObject);
            UnityEngine.Object.DestroyImmediate(playerObject);
            UnityEngine.Object.DestroyImmediate(playerSpawnObject);
            UnityEngine.Object.DestroyImmediate(rewardObject);
        }

        [Test]
        public void Awake_WhenCreated_ShouldConfigureBehaviorParametersForPpo()
        {
            var behaviorParameters = bossObject.GetComponent<BehaviorParameters>();

            Assert.That(behaviorParameters, Is.Not.Null);
            Assert.That(behaviorParameters.BehaviorName, Is.EqualTo("BossAgent"));
            Assert.That(behaviorParameters.BehaviorType, Is.EqualTo(BehaviorType.Default));
            Assert.That(behaviorParameters.BrainParameters.VectorObservationSize, Is.EqualTo(BossAgent.ObservationCount));
            Assert.That(behaviorParameters.BrainParameters.ActionSpec.NumDiscreteActions, Is.EqualTo(1));
            Assert.That(behaviorParameters.BrainParameters.ActionSpec.BranchSizes[0], Is.EqualTo(BossAgent.DiscreteActionCount));
        }

        [Test]
        public void Awake_WhenBehaviorTypeWasSerialized_ShouldPreserveBehaviorType()
        {
            var inferenceBoss = new GameObject("BossAgentInferenceTypeTest");
            inferenceBoss.AddComponent<Rigidbody2D>();
            var behaviorParameters = inferenceBoss.AddComponent<BehaviorParameters>();
            behaviorParameters.BehaviorType = BehaviorType.InferenceOnly;

            try
            {
                inferenceBoss.AddComponent<BossAgent>();

                Assert.That(behaviorParameters.BehaviorType, Is.EqualTo(BehaviorType.InferenceOnly));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(inferenceBoss);
            }
        }

        [Test]
        public void Awake_WhenCreated_ShouldConfigureDecisionRequesterForTraining()
        {
            var decisionRequester = bossObject.GetComponent<DecisionRequester>();

            Assert.That(decisionRequester, Is.Not.Null);
            Assert.That(decisionRequester.DecisionPeriod, Is.EqualTo(BossAgent.DefaultDecisionPeriod));
            Assert.That(decisionRequester.TakeActionsBetweenDecisions, Is.True);
            Assert.That(bossAgent.MaxStep, Is.EqualTo(BossAgent.DefaultMaxEpisodeSteps));
        }

        [Test]
        public void ConfigureRuntime_WhenMaxEpisodeStepsIsZero_ShouldDisableAutomaticEpisodeLimit()
        {
            var serializedAgent = new SerializedObject(bossAgent);
            serializedAgent.FindProperty("maxEpisodeSteps").intValue = 0;
            serializedAgent.ApplyModifiedPropertiesWithoutUndo();

            bossAgent.BindDependencies(playerObject.transform, bossHealth, rewardShaper);

            Assert.That(bossAgent.MaxStep, Is.Zero);
        }

        [Test]
        public void ApplyDiscreteAction_WhenActionsMatchWinningSequence_ShouldReturnProvenanceReward()
        {
            Assert.That(bossAgent.ApplyDiscreteAction(BossAgent.JumpAction), Is.Zero);
            Assert.That(bossAgent.ApplyDiscreteAction(BossAgent.DashAction), Is.Zero);

            var reward = bossAgent.ApplyDiscreteAction(BossAgent.AttackMeleeAction);

            Assert.That(reward, Is.EqualTo(2f).Within(0.001f));
            Assert.That(bossAgent.LastProvenanceReward, Is.EqualTo(2f).Within(0.001f));
        }

        [Test]
        public void ApplyDiscreteAction_WhenJumpActionIsAppliedAwayFromGround_ShouldNotResetVerticalVelocity()
        {
            var bossRigidbody = bossObject.GetComponent<Rigidbody2D>();
            bossObject.AddComponent<BoxCollider2D>();
            bossObject.transform.position = new Vector3(0f, 5f, 0f);
            bossRigidbody.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();

            bossAgent.ApplyDiscreteAction(BossAgent.JumpAction);

            Assert.That(bossRigidbody.linearVelocity.y, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void ApplyDiscreteAction_WhenAttackOrDash_ShouldShowPassiveFeedbackAndLogAction()
        {
            var baseColor = bossRenderer.color;

            LogAssert.Expect(LogType.Log, "Boss PPO Attack");
            bossAgent.ApplyDiscreteAction(BossAgent.AttackMeleeAction);
            var attackColor = bossRenderer.color;

            Assert.That(attackColor, Is.Not.EqualTo(baseColor));

            LogAssert.Expect(LogType.Log, "Boss PPO Dash");
            bossAgent.ApplyDiscreteAction(BossAgent.DashAction);

            Assert.That(bossRenderer.color, Is.Not.EqualTo(attackColor));
        }

        [Test]
        public void OnActionReceived_WhenDiscreteActionProvided_ShouldApplyActionAndStoreLastReward()
        {
            bossAgent.ApplyDiscreteAction(BossAgent.JumpAction);
            bossAgent.ApplyDiscreteAction(BossAgent.DashAction);

            bossAgent.OnActionReceived(new ActionBuffers(Array.Empty<float>(), new[] { BossAgent.AttackMeleeAction }));

            Assert.That(bossAgent.LastProvenanceReward, Is.EqualTo(2f).Within(0.001f));
        }

        [Test]
        public void ResetTrainingEpisode_WhenSpawnPointsAreBound_ShouldResetPositionsHealthAndRewardBuffer()
        {
            var bossRigidbody = bossObject.GetComponent<Rigidbody2D>();
            var playerRigidbody = playerObject.GetComponent<Rigidbody2D>();
            bossObject.transform.position = new Vector3(9f, 0f, 0f);
            playerObject.transform.position = new Vector3(-9f, 0f, 0f);
            bossSpawnObject.transform.position = new Vector3(-2f, 1f, 0f);
            playerSpawnObject.transform.position = new Vector3(2f, 1f, 0f);
            bossRigidbody.linearVelocity = new Vector2(3f, 4f);
            playerRigidbody.linearVelocity = new Vector2(-3f, -4f);
            bossHealth.TakeDamage(25f);
            bossAgent.ApplyDiscreteAction(BossAgent.JumpAction);
            bossAgent.ApplyDiscreteAction(BossAgent.DashAction);
            bossAgent.BindTrainingReset(bossSpawnObject.transform, playerSpawnObject.transform);

            bossAgent.ResetTrainingEpisode();

            Assert.That(bossObject.transform.position, Is.EqualTo(bossSpawnObject.transform.position));
            Assert.That(playerObject.transform.position, Is.EqualTo(playerSpawnObject.transform.position));
            Assert.That(bossRigidbody.linearVelocity, Is.EqualTo(Vector2.zero));
            Assert.That(playerRigidbody.linearVelocity, Is.EqualTo(Vector2.zero));
            Assert.That(bossHealth.CurrentHealth, Is.EqualTo(bossHealth.MaxHealth));
            Assert.That(rewardShaper.BufferedActionCount, Is.Zero);
        }

        [Test]
        public void OnEpisodeBegin_WhenEditorTrainingSettingsAreEnabled_ShouldReapplyFastRuntimeSettings()
        {
            var serializedAgent = new SerializedObject(bossAgent);
            serializedAgent.FindProperty("applyEditorTrainingSettings").boolValue = true;
            serializedAgent.ApplyModifiedPropertiesWithoutUndo();
            Time.timeScale = 1f;
            Application.runInBackground = false;

            bossAgent.OnEpisodeBegin();

            Assert.That(Time.timeScale, Is.EqualTo(BossAgent.DefaultEditorTrainingTimeScale).Within(0.001f));
            Assert.That(Application.runInBackground, Is.True);
        }
    }
}
