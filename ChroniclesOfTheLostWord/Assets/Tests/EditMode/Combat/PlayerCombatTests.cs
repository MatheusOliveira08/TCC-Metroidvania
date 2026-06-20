using NUnit.Framework;
using TerraSilente.Boss;
using TerraSilente.Player;
using UnityEngine;

namespace TerraSilente.Tests.Combat
{
    public class PlayerCombatTests
    {
        private GameObject playerObject;
        private GameObject bossObject;
        private PlayerCombat playerCombat;
        private BossHealth bossHealth;

        [SetUp]
        public void SetUp()
        {
            playerObject = new GameObject("Player Combat Test");
            playerObject.transform.position = Vector3.zero;
            playerCombat = playerObject.AddComponent<PlayerCombat>();

            bossObject = new GameObject("Boss Target Test");
            bossObject.transform.position = new Vector3(0.5f, 0f, 0f);
            bossObject.AddComponent<BoxCollider2D>();
            bossHealth = bossObject.AddComponent<BossHealth>();
            bossHealth.ResetHealth();

            Physics2D.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(bossObject);
        }

        [Test]
        public void PerformAttack_WhenBossIsInRange_ShouldApplyDamageToBoss()
        {
            playerCombat.PerformAttack();

            Assert.That(bossHealth.CurrentHealth, Is.EqualTo(90f));
        }

        [Test]
        public void PerformAttack_WhenBossIsOutsideRange_ShouldNotDamageBoss()
        {
            bossObject.transform.position = new Vector3(5f, 0f, 0f);
            Physics2D.SyncTransforms();

            playerCombat.PerformAttack();

            Assert.That(bossHealth.CurrentHealth, Is.EqualTo(100f));
        }
    }
}
