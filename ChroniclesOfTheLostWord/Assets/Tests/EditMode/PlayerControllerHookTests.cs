using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TerraSilente.Tests.Player
{
    public class PlayerControllerHookTests
    {
        private GameObject playerObject;
        private global::PlayerController controller;

        [SetUp]
        public void SetUp()
        {
            playerObject = new GameObject("Player Controller Test");
            playerObject.AddComponent<Rigidbody2D>();
            playerObject.AddComponent<BoxCollider2D>();
            controller = playerObject.AddComponent<global::PlayerController>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void HandleAttackInput_WhenCalled_ShouldRaiseOnPlayerAttack()
        {
            var wasRaised = false;
            controller.OnPlayerAttack += () => wasRaised = true;

            InvokePrivateMethod(controller, "HandleAttackInput");

            Assert.That(wasRaised, Is.True);
        }

        [Test]
        public void ApplyJump_WhenGrounded_ShouldRaiseOnPlayerJump()
        {
            var wasRaised = false;
            controller.OnPlayerJump += () => wasRaised = true;
            SetPrivateField(controller, "rb", playerObject.GetComponent<Rigidbody2D>());
            SetPrivateField(controller, "isGrounded", true);

            InvokePrivateMethod(controller, "ApplyJump");

            Assert.That(wasRaised, Is.True);
        }

        [Test]
        public void FixedUpdate_WhenDashRequested_ShouldRaiseOnPlayerDashAndApplyHorizontalVelocity()
        {
            var wasRaised = false;
            var rb = playerObject.GetComponent<Rigidbody2D>();
            controller.OnPlayerDash += () => wasRaised = true;
            SetPrivateField(controller, "rb", rb);
            SetPrivateField(controller, "moveInput", 1f);

            InvokePrivateMethod(controller, "HandleDashInput");
            InvokePrivateMethod(controller, "FixedUpdate");

            Assert.That(wasRaised, Is.True);
            Assert.That(rb.linearVelocity.x, Is.GreaterThan(0f));
        }

        [Test]
        public void FixedUpdate_WhenDashIsOnCooldown_ShouldNotRaiseOnPlayerDashAgain()
        {
            var dashEvents = 0;
            controller.OnPlayerDash += () => dashEvents++;
            SetPrivateField(controller, "rb", playerObject.GetComponent<Rigidbody2D>());
            SetPrivateField(controller, "moveInput", 1f);

            InvokePrivateMethod(controller, "HandleDashInput");
            InvokePrivateMethod(controller, "FixedUpdate");
            SetPrivateField(controller, "isDashing", false);
            InvokePrivateMethod(controller, "HandleDashInput");
            InvokePrivateMethod(controller, "FixedUpdate");

            Assert.That(dashEvents, Is.EqualTo(1));
        }

        private static void InvokePrivateMethod(global::PlayerController target, string methodName)
        {
            var method = typeof(global::PlayerController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);

            method.Invoke(target, null);
        }

        private static void SetPrivateField(global::PlayerController target, string fieldName, object value)
        {
            var field = typeof(global::PlayerController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);

            field.SetValue(target, value);
        }
    }
}
