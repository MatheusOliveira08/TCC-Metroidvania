using NUnit.Framework;
using TerraSilente.Boss;
using UnityEngine;
using UnityEngine.TestTools;

namespace TerraSilente.Tests.Boss
{
    public class BossFsmControllerTests
    {
        private GameObject bossObject;
        private GameObject playerObject;
        private Rigidbody2D bossRigidbody;
        private SpriteRenderer bossRenderer;
        private BossHealth bossHealth;
        private BossFsmController bossFsm;

        [SetUp]
        public void SetUp()
        {
            bossObject = new GameObject("Boss FSM Test");
            bossRigidbody = bossObject.AddComponent<Rigidbody2D>();
            bossRenderer = bossObject.AddComponent<SpriteRenderer>();
            bossRenderer.color = new Color(0.45f, 0.45f, 0.45f, 1f);
            bossHealth = bossObject.AddComponent<BossHealth>();
            bossHealth.ResetHealth();
            bossFsm = bossObject.AddComponent<BossFsmController>();

            playerObject = new GameObject("Player Target Test");
            bossFsm.BindDependencies(playerObject.transform, bossHealth);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(bossObject);
            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void Tick_WhenPlayerIsOutsideAttackRange_ShouldChaseTowardPlayer()
        {
            bossObject.transform.position = Vector3.zero;
            playerObject.transform.position = new Vector3(5f, 0f, 0f);

            bossFsm.Tick(0.1f);

            Assert.That(bossFsm.CurrentState, Is.EqualTo(BossFsmState.Chase));
            Assert.That(bossRigidbody.linearVelocity.x, Is.GreaterThan(0f));
        }

        [Test]
        public void Tick_WhenPlayerIsInAttackRange_ShouldRaiseAttackAndEnterAttackState()
        {
            var attackEvents = 0;
            bossFsm.OnBossAttackPerformed += () => attackEvents++;
            bossObject.transform.position = Vector3.zero;
            playerObject.transform.position = new Vector3(0.5f, 0f, 0f);

            bossFsm.Tick(0.02f);

            Assert.That(bossFsm.CurrentState, Is.EqualTo(BossFsmState.Attack));
            Assert.That(attackEvents, Is.EqualTo(1));
            Assert.That(bossRigidbody.linearVelocity.x, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_WhenEnteringAttack_ShouldShowPassiveFeedbackAndLogAction()
        {
            var baseColor = bossRenderer.color;
            bossObject.transform.position = Vector3.zero;
            playerObject.transform.position = new Vector3(0.5f, 0f, 0f);

            LogAssert.Expect(LogType.Log, "Boss FSM Attack");
            bossFsm.Tick(0.02f);

            Assert.That(bossRenderer.color, Is.Not.EqualTo(baseColor));
        }

        [Test]
        public void Tick_WhenAttackFeedbackDurationEnds_ShouldRestoreOriginalColor()
        {
            var baseColor = bossRenderer.color;
            bossObject.transform.position = Vector3.zero;
            playerObject.transform.position = new Vector3(0.5f, 0f, 0f);

            LogAssert.Expect(LogType.Log, "Boss FSM Attack");
            bossFsm.Tick(0.02f);
            Assert.That(bossRenderer.color, Is.Not.EqualTo(baseColor));

            bossFsm.Tick(0.16f);

            Assert.That(bossRenderer.color, Is.EqualTo(baseColor));
        }

        [Test]
        public void Tick_WhenAttackPauseEnds_ShouldRetreatAwayFromPlayer()
        {
            bossObject.transform.position = Vector3.zero;
            playerObject.transform.position = new Vector3(0.5f, 0f, 0f);

            bossFsm.Tick(0.02f);
            bossFsm.Tick(0.3f);

            Assert.That(bossFsm.CurrentState, Is.EqualTo(BossFsmState.Retreat));
            Assert.That(bossRigidbody.linearVelocity.x, Is.LessThan(0f));
        }

        [Test]
        public void Tick_WhenBossIsDead_ShouldNotMoveOrAttack()
        {
            var attackEvents = 0;
            bossFsm.OnBossAttackPerformed += () => attackEvents++;
            bossRigidbody.linearVelocity = new Vector2(3f, 0f);
            playerObject.transform.position = new Vector3(0.5f, 0f, 0f);
            bossHealth.TakeDamage(100f);

            bossFsm.Tick(0.1f);

            Assert.That(bossFsm.CurrentState, Is.EqualTo(BossFsmState.Idle));
            Assert.That(attackEvents, Is.EqualTo(0));
            Assert.That(bossRigidbody.linearVelocity.x, Is.EqualTo(0f));
        }
    }
}
