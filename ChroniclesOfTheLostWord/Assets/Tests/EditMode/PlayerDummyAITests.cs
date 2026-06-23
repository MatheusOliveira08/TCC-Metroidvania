using NUnit.Framework;
using TerraSilente.Player;
using UnityEngine;

namespace TerraSilente.Tests.Player
{
    public class PlayerDummyAITests
    {
        private GameObject dummyObject;
        private GameObject targetObject;
        private Rigidbody2D dummyRigidbody;
        private PlayerDummyAI dummyAI;

        [SetUp]
        public void SetUp()
        {
            dummyObject = new GameObject("PlayerDummyAITests");
            dummyRigidbody = dummyObject.AddComponent<Rigidbody2D>();
            dummyObject.AddComponent<BoxCollider2D>();
            dummyAI = dummyObject.AddComponent<PlayerDummyAI>();

            targetObject = new GameObject("PlayerDummyAITarget");
            dummyAI.BindTarget(targetObject.transform);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(dummyObject);
            Object.DestroyImmediate(targetObject);
        }

        [Test]
        public void Tick_WhenTargetIsOutsideAttackRange_ShouldMoveTowardTarget()
        {
            dummyObject.transform.position = Vector3.zero;
            targetObject.transform.position = new Vector3(5f, 0f, 0f);

            dummyAI.Tick(0.02f);

            Assert.That(dummyRigidbody.linearVelocity.x, Is.GreaterThan(0f));
        }

        [Test]
        public void Tick_WhenTargetIsInsideAttackRange_ShouldRaiseAttackAndStopMoving()
        {
            var attackEvents = 0;
            dummyAI.OnDummyAttackPerformed += () => attackEvents++;
            dummyObject.transform.position = Vector3.zero;
            targetObject.transform.position = new Vector3(0.5f, 0f, 0f);

            dummyAI.Tick(1f);

            Assert.That(attackEvents, Is.EqualTo(1));
            Assert.That(dummyRigidbody.linearVelocity.x, Is.Zero);
        }
    }
}
