using System;
using NUnit.Framework;
using TerraSilente.Boss;
using TerraSilente.Provenance;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEngine;

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
        private ProvenanceRewardShaper rewardShaper;

        [SetUp]
        public void SetUp()
        {
            bossObject = new GameObject("BossAgentTests");
            bossObject.AddComponent<Rigidbody2D>();
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
        public void Awake_WhenCreated_ShouldConfigureDecisionRequesterForTraining()
        {
            var decisionRequester = bossObject.GetComponent<DecisionRequester>();

            Assert.That(decisionRequester, Is.Not.Null);
            Assert.That(decisionRequester.DecisionPeriod, Is.EqualTo(5));
            Assert.That(decisionRequester.TakeActionsBetweenDecisions, Is.True);
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
    }
}
