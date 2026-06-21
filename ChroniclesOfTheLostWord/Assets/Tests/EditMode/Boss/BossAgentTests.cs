using System;
using NUnit.Framework;
using TerraSilente.Boss;
using TerraSilente.Provenance;
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
        private GameObject rewardObject;
        private BossAgent bossAgent;
        private ProvenanceRewardShaper rewardShaper;

        [SetUp]
        public void SetUp()
        {
            bossObject = new GameObject("BossAgentTests");
            bossObject.AddComponent<Rigidbody2D>();
            var bossHealth = bossObject.AddComponent<BossHealth>();
            bossHealth.ResetHealth();

            rewardObject = new GameObject("RewardShaperTests");
            rewardShaper = rewardObject.AddComponent<ProvenanceRewardShaper>();
            rewardShaper.SequenceMatchReward = 2f;
            rewardShaper.LoadFromJson(WinningSequencesJson);

            bossAgent = bossObject.AddComponent<BossAgent>();
            bossAgent.BindDependencies(null, bossHealth, rewardShaper);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(bossObject);
            UnityEngine.Object.DestroyImmediate(rewardObject);
        }

        [Test]
        public void Awake_WhenCreated_ShouldConfigureBehaviorParametersForPpo()
        {
            var behaviorParameters = bossObject.GetComponent<BehaviorParameters>();

            Assert.That(behaviorParameters, Is.Not.Null);
            Assert.That(behaviorParameters.BehaviorName, Is.EqualTo("BossAgent"));
            Assert.That(behaviorParameters.BrainParameters.VectorObservationSize, Is.EqualTo(BossAgent.ObservationCount));
            Assert.That(behaviorParameters.BrainParameters.ActionSpec.NumDiscreteActions, Is.EqualTo(1));
            Assert.That(behaviorParameters.BrainParameters.ActionSpec.BranchSizes[0], Is.EqualTo(BossAgent.DiscreteActionCount));
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
    }
}
