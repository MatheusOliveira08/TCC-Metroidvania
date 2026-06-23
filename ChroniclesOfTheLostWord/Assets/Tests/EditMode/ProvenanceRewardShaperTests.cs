using System.IO;
using System.Reflection;
using NUnit.Framework;
using TerraSilente.Provenance;
using UnityEngine;

namespace TerraSilente.Tests.Provenance
{
    public class ProvenanceRewardShaperTests
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

        private GameObject gameObject;
        private ProvenanceRewardShaper rewardShaper;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("ProvenanceRewardShaperTests");
            rewardShaper = gameObject.AddComponent<ProvenanceRewardShaper>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void LoadFromJson_WhenWinningSequencesProvided_ShouldExposeSequenceMetadata()
        {
            rewardShaper.LoadFromJson(WinningSequencesJson);

            Assert.That(rewardShaper.SequenceLength, Is.EqualTo(3));
            Assert.That(rewardShaper.LoadedSequenceCount, Is.EqualTo(1));
        }

        [Test]
        public void RecordAction_WhenRecentActionsMatchWinningSequence_ShouldReturnConfiguredReward()
        {
            rewardShaper.SequenceMatchReward = 2f;
            rewardShaper.LoadFromJson(WinningSequencesJson);

            Assert.That(rewardShaper.RecordAction("PlayerJump"), Is.Zero);
            Assert.That(rewardShaper.RecordAction("PlayerDash"), Is.Zero);
            Assert.That(rewardShaper.RecordAction("PlayerAttack"), Is.EqualTo(2f).Within(0.001f));
        }

        [Test]
        public void RecordAction_WhenRecentActionsDoNotMatchWinningSequence_ShouldReturnZero()
        {
            rewardShaper.SequenceMatchReward = 2f;
            rewardShaper.LoadFromJson(WinningSequencesJson);

            rewardShaper.RecordAction("PlayerAttack");
            rewardShaper.RecordAction("PlayerDash");

            Assert.That(rewardShaper.RecordAction("PlayerJump"), Is.Zero);
        }

        [Test]
        public void OnEnable_WhenSerializedWinningSequencesFileExists_ShouldLoadSequences()
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), "terra-silente-winning-sequences-test.json");
            File.WriteAllText(tempFilePath, WinningSequencesJson);

            var loaderObject = new GameObject("AutoLoadingRewardShaperTests");
            try
            {
                var autoLoadingShaper = loaderObject.AddComponent<ProvenanceRewardShaper>();
                typeof(ProvenanceRewardShaper)
                    .GetField("winningSequencesFilePath", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(autoLoadingShaper, tempFilePath);

                typeof(ProvenanceRewardShaper)
                    .GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(autoLoadingShaper, null);

                Assert.That(autoLoadingShaper.LoadedSequenceCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(loaderObject);
                File.Delete(tempFilePath);
            }
        }
    }
}
