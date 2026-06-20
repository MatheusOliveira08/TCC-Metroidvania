using NUnit.Framework;
using TerraSilente.Boss;
using UnityEngine;

namespace TerraSilente.Tests.Combat
{
    public class BossHealthTests
    {
        private GameObject bossObject;
        private BossHealth bossHealth;

        [SetUp]
        public void SetUp()
        {
            bossObject = new GameObject("Boss Health Test");
            bossHealth = bossObject.AddComponent<BossHealth>();
            bossHealth.ResetHealth();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(bossObject);
        }

        [Test]
        public void TakeDamage_WhenDamageIsBelowCurrentHealth_ShouldReduceHealthAndRaiseDamageEvent()
        {
            var damageEvents = 0;
            var receivedDamage = 0f;
            bossHealth.OnBossDamageTaken += damageAmount =>
            {
                damageEvents++;
                receivedDamage = damageAmount;
            };

            bossHealth.TakeDamage(25f);

            Assert.That(bossHealth.CurrentHealth, Is.EqualTo(75f));
            Assert.That(bossHealth.IsDead, Is.False);
            Assert.That(damageEvents, Is.EqualTo(1));
            Assert.That(receivedDamage, Is.EqualTo(25f));
        }

        [Test]
        public void TakeDamage_WhenDamageIsLethal_ShouldClampHealthToZeroAndRaiseDeathOnce()
        {
            var deathEvents = 0;
            bossHealth.OnBossDeath += () => deathEvents++;

            bossHealth.TakeDamage(150f);
            bossHealth.TakeDamage(10f);

            Assert.That(bossHealth.CurrentHealth, Is.EqualTo(0f));
            Assert.That(bossHealth.IsDead, Is.True);
            Assert.That(deathEvents, Is.EqualTo(1));
        }

        [Test]
        public void TakeDamage_WhenDamageIsZeroOrNegative_ShouldNotChangeHealthOrRaiseEvents()
        {
            var damageEvents = 0;
            var deathEvents = 0;
            bossHealth.OnBossDamageTaken += _ => damageEvents++;
            bossHealth.OnBossDeath += () => deathEvents++;

            bossHealth.TakeDamage(0f);
            bossHealth.TakeDamage(-10f);

            Assert.That(bossHealth.CurrentHealth, Is.EqualTo(100f));
            Assert.That(bossHealth.IsDead, Is.False);
            Assert.That(damageEvents, Is.EqualTo(0));
            Assert.That(deathEvents, Is.EqualTo(0));
        }
    }
}
