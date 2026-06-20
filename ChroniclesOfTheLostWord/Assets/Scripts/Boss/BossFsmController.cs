using System;
using UnityEngine;

namespace TerraSilente.Boss
{
    public enum BossFsmState
    {
        Idle,
        Chase,
        Attack,
        Retreat
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class BossFsmController : MonoBehaviour
    {
        [SerializeField] private Transform playerTarget;
        [SerializeField] private BossHealth bossHealth;
        [SerializeField] private float chaseSpeed = 2f;
        [SerializeField] private float attackRange = 1.25f;
        [SerializeField] private float attackDuration = 0.2f;
        [SerializeField] private float retreatDuration = 0.45f;
        [SerializeField] private float retreatSpeed = 1.5f;
        [SerializeField] private float attackCooldown = 0.8f;

        private Rigidbody2D rb;
        private float stateTimer;
        private float cooldownTimer;

        public event Action OnBossAttackPerformed;

        public BossFsmState CurrentState { get; private set; } = BossFsmState.Idle;

        private void Awake()
        {
            ResolveReferences();
        }

        private void FixedUpdate()
        {
            Tick(Time.fixedDeltaTime);
        }

        public void BindDependencies(Transform newPlayerTarget, BossHealth newBossHealth)
        {
            playerTarget = newPlayerTarget;
            bossHealth = newBossHealth;
            ResolveReferences();
        }

        public void Tick(float deltaTime)
        {
            ResolveReferences();

            var safeDeltaTime = Mathf.Max(0f, deltaTime);
            cooldownTimer = Mathf.Max(0f, cooldownTimer - safeDeltaTime);

            if (bossHealth != null && bossHealth.IsDead || playerTarget == null)
            {
                EnterIdle();
                return;
            }

            if (CurrentState == BossFsmState.Attack)
            {
                stateTimer -= safeDeltaTime;
                SetHorizontalVelocity(0f);

                if (stateTimer <= 0f)
                {
                    EnterRetreat();
                }

                return;
            }

            if (CurrentState == BossFsmState.Retreat)
            {
                stateTimer -= safeDeltaTime;
                MoveAwayFromTarget();

                if (stateTimer > 0f)
                {
                    return;
                }
            }

            if (GetHorizontalDistanceToTarget() <= attackRange)
            {
                if (cooldownTimer <= 0f)
                {
                    EnterAttack();
                }
                else
                {
                    EnterIdle();
                }

                return;
            }

            CurrentState = BossFsmState.Chase;
            MoveTowardTarget();
        }

        private void ResolveReferences()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
            }

            if (bossHealth == null)
            {
                bossHealth = GetComponent<BossHealth>();
            }
        }

        private void EnterIdle()
        {
            CurrentState = BossFsmState.Idle;
            SetHorizontalVelocity(0f);
        }

        private void EnterAttack()
        {
            CurrentState = BossFsmState.Attack;
            stateTimer = attackDuration;
            cooldownTimer = attackCooldown;
            SetHorizontalVelocity(0f);
            OnBossAttackPerformed?.Invoke();
        }

        private void EnterRetreat()
        {
            CurrentState = BossFsmState.Retreat;
            stateTimer = retreatDuration;
            MoveAwayFromTarget();
        }

        private void MoveTowardTarget()
        {
            var direction = Mathf.Sign(playerTarget.position.x - transform.position.x);
            SetHorizontalVelocity(direction * chaseSpeed);
        }

        private void MoveAwayFromTarget()
        {
            var direction = Mathf.Sign(transform.position.x - playerTarget.position.x);
            if (Mathf.Approximately(direction, 0f))
            {
                direction = -1f;
            }

            SetHorizontalVelocity(direction * retreatSpeed);
        }

        private float GetHorizontalDistanceToTarget()
        {
            return Mathf.Abs(playerTarget.position.x - transform.position.x);
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
