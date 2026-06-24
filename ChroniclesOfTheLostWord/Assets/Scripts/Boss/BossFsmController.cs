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
        [SerializeField] private SpriteRenderer actionFeedbackRenderer;
        [SerializeField] private Color attackFeedbackColor = new(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private float attackFeedbackDuration = 0.15f;

        private Rigidbody2D rb;
        private float stateTimer;
        private float cooldownTimer;
        private float attackFeedbackTimer;
        private Color originalFeedbackColor;
        private bool hasOriginalFeedbackColor;

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
            UpdateAttackFeedback(safeDeltaTime);

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

            if (actionFeedbackRenderer == null)
            {
                actionFeedbackRenderer = GetComponent<SpriteRenderer>();
            }

            CacheOriginalFeedbackColor();
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
            ShowPassiveAttackFeedback();
            OnBossAttackPerformed?.Invoke();
        }

        private void ShowPassiveAttackFeedback()
        {
            if (actionFeedbackRenderer != null)
            {
                actionFeedbackRenderer.color = attackFeedbackColor;
                attackFeedbackTimer = Mathf.Max(0f, attackFeedbackDuration);
            }

            Debug.Log("Boss FSM Attack", this);
        }

        private void UpdateAttackFeedback(float deltaTime)
        {
            if (attackFeedbackTimer <= 0f)
            {
                return;
            }

            attackFeedbackTimer = Mathf.Max(0f, attackFeedbackTimer - deltaTime);
            if (attackFeedbackTimer <= 0f && actionFeedbackRenderer != null && hasOriginalFeedbackColor)
            {
                actionFeedbackRenderer.color = originalFeedbackColor;
            }
        }

        private void CacheOriginalFeedbackColor()
        {
            if (actionFeedbackRenderer == null || hasOriginalFeedbackColor)
            {
                return;
            }

            originalFeedbackColor = actionFeedbackRenderer.color;
            hasOriginalFeedbackColor = true;
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
