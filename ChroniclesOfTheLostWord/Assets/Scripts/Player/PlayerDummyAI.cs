using System;
using UnityEngine;

namespace TerraSilente.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlayerDummyAI : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float attackRange = 1.25f;
        [SerializeField] private float attackCooldown = 0.8f;

        private Rigidbody2D rb;
        private float cooldownTimer;

        public event Action OnDummyAttackPerformed;

        private void Awake()
        {
            ResolveReferences();
        }

        private void FixedUpdate()
        {
            Tick(Time.fixedDeltaTime);
        }

        public void BindTarget(Transform newTarget)
        {
            target = newTarget;
            ResolveReferences();
        }

        public void ResetDummy(Vector3 spawnPosition)
        {
            ResolveReferences();
            transform.position = spawnPosition;
            cooldownTimer = 0f;

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        public void Tick(float deltaTime)
        {
            ResolveReferences();

            var safeDeltaTime = Mathf.Max(0f, deltaTime);
            cooldownTimer = Mathf.Max(0f, cooldownTimer - safeDeltaTime);

            if (target == null)
            {
                SetHorizontalVelocity(0f);
                return;
            }

            var horizontalDistance = target.position.x - transform.position.x;
            if (Mathf.Abs(horizontalDistance) <= attackRange)
            {
                SetHorizontalVelocity(0f);
                TryAttack();
                return;
            }

            var direction = Mathf.Sign(horizontalDistance);
            SetHorizontalVelocity(direction * moveSpeed);
        }

        private void ResolveReferences()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
            }
        }

        private void TryAttack()
        {
            if (cooldownTimer > 0f)
            {
                return;
            }

            cooldownTimer = attackCooldown;
            OnDummyAttackPerformed?.Invoke();
        }

        private void SetHorizontalVelocity(float velocityX)
        {
            if (rb == null)
            {
                return;
            }

            rb.linearVelocity = new Vector2(velocityX, rb.linearVelocity.y);
        }
    }
}
