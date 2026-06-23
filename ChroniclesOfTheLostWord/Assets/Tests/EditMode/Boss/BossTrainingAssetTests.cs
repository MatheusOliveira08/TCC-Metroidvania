using NUnit.Framework;
using TerraSilente.Boss;
using TerraSilente.Provenance;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TerraSilente.Tests.Boss
{
    public class BossTrainingAssetTests
    {
        private const string BossPrefabPath = "Assets/Prefabs/Boss/Boss_PPO.prefab";
        private const string BossArenaPath = "Assets/Scenes/BossArena_PPO.unity";

        [Test]
        public void BossPpoPrefab_ShouldExposeTrainingComponents()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<Rigidbody2D>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<BoxCollider2D>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<BossHealth>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<ProvenanceRewardShaper>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<BossAgent>(), Is.Not.Null);

            var behaviorParameters = prefab.GetComponent<BehaviorParameters>();
            Assert.That(behaviorParameters, Is.Not.Null);
            Assert.That(behaviorParameters.BehaviorName, Is.EqualTo("BossAgent"));
            Assert.That(behaviorParameters.BehaviorType, Is.EqualTo(BehaviorType.InferenceOnly));
            var serializedBehavior = new SerializedObject(behaviorParameters);
            Assert.That(serializedBehavior.FindProperty("m_Model").objectReferenceValue, Is.Not.Null);
            Assert.That(behaviorParameters.BrainParameters.VectorObservationSize, Is.EqualTo(BossAgent.ObservationCount));
            Assert.That(behaviorParameters.BrainParameters.ActionSpec.NumDiscreteActions, Is.EqualTo(1));
            Assert.That(behaviorParameters.BrainParameters.ActionSpec.BranchSizes[0], Is.EqualTo(BossAgent.DiscreteActionCount));

            var decisionRequester = prefab.GetComponent<DecisionRequester>();
            Assert.That(decisionRequester, Is.Not.Null);
            Assert.That(decisionRequester.DecisionPeriod, Is.EqualTo(BossAgent.DefaultDecisionPeriod));
            Assert.That(decisionRequester.TakeActionsBetweenDecisions, Is.True);

            var bossAgent = prefab.GetComponent<BossAgent>();
            Assert.That(bossAgent.MaxStep, Is.EqualTo(BossAgent.DefaultMaxEpisodeSteps));

            var serializedAgent = new SerializedObject(bossAgent);
            Assert.That(serializedAgent.FindProperty("applyEditorTrainingSettings").boolValue, Is.False);
            Assert.That(serializedAgent.FindProperty("editorTrainingTimeScale").floatValue, Is.EqualTo(BossAgent.DefaultEditorTrainingTimeScale).Within(0.001f));
        }

        [Test]
        public void BossArenaPpoScene_ShouldContainTrainingRoots()
        {
            var scene = EditorSceneManager.OpenScene(BossArenaPath, OpenSceneMode.Single);

            Assert.That(scene.IsValid(), Is.True);
            Assert.That(FindInScene(scene, "Boss_PPO"), Is.Not.Null);
            Assert.That(FindInScene(scene, "PlayerDummy_Training"), Is.Not.Null);
            Assert.That(FindInScene(scene, "BossSpawn"), Is.Not.Null);
            Assert.That(FindInScene(scene, "PlayerSpawn"), Is.Not.Null);

            var bossAgent = FindInScene(scene, "Boss_PPO").GetComponent<BossAgent>();
            Assert.That(bossAgent, Is.Not.Null);
        }

        private static GameObject FindInScene(Scene scene, string objectName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.name == objectName)
                    {
                        return transform.gameObject;
                    }
                }
            }

            return null;
        }
    }
}
