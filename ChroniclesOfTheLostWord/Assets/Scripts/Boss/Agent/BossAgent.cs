using TerraSilente.Provenance;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace TerraSilente.Boss
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BehaviorParameters))]
    public class BossAgent : Agent
    {
        public const int ObservationCount = 10;
        public const int DiscreteActionCount = 6;

        public const int IdleAction = 0;
        public const int MoveLeftAction = 1;
        public const int MoveRightAction = 2;
        public const int JumpAction = 3;
        public const int AttackMeleeAction = 4;
        public const int DashAction = 5;

        private const string PlayerJumpActionType = "PlayerJump";
        private const string PlayerAttackActionType = "PlayerAttack";
        private const string PlayerDashActionType = "PlayerDash";

        [SerializeField] private Transform playerTarget;
        [SerializeField] private BossHealth bossHealth;
        [SerializeField] private ProvenanceRewardShaper rewardShaper;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float jumpVelocity = 6f;
        [SerializeField] private float dashSpeed = 8f;

        private Rigidbody2D rb;
        private float lastHorizontalDirection = 1f;

        public float LastProvenanceReward { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            ResolveReferences();
            ConfigureBehaviorParameters();
        }

        public void BindDependencies(Transform newPlayerTarget, BossHealth newBossHealth, ProvenanceRewardShaper newRewardShaper)
        {
            playerTarget = newPlayerTarget;
            bossHealth = newBossHealth;
            rewardShaper = newRewardShaper;
            ResolveReferences();
            ConfigureBehaviorParameters();
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            ResolveReferences();

            var bossPosition = transform.position;
            var bossVelocity = rb != null ? rb.linearVelocity : Vector2.zero;
            var relativePlayerPosition = playerTarget != null
                ? playerTarget.position - bossPosition
                : Vector3.zero;
            var normalizedBossHealth = bossHealth != null
                ? Mathf.Clamp01(bossHealth.CurrentHealth / bossHealth.MaxHealth)
                : 1f;

            sensor.AddObservation(bossPosition.x);
            sensor.AddObservation(bossPosition.y);
            sensor.AddObservation(bossVelocity.x);
            sensor.AddObservation(bossVelocity.y);
            sensor.AddObservation(relativePlayerPosition.x);
            sensor.AddObservation(relativePlayerPosition.y);
            sensor.AddObservation(normalizedBossHealth);
            sensor.AddObservation(1f); // Player HP placeholder until PlayerHealth exists.
            sensor.AddObservation(IsGrounded() ? 1f : 0f);
            sensor.AddObservation(0f); // Dash cooldown placeholder for the training slice.
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            var actionIndex = actions.DiscreteActions.Length > 0
                ? actions.DiscreteActions[0]
                : IdleAction;

            var reward = ApplyDiscreteAction(actionIndex);
            if (reward > 0f)
            {
                AddReward(reward);
            }
        }

        public float ApplyDiscreteAction(int actionIndex)
        {
            ResolveReferences();
            LastProvenanceReward = 0f;

            switch (actionIndex)
            {
                case MoveLeftAction:
                    SetHorizontalVelocity(-moveSpeed);
                    lastHorizontalDirection = -1f;
                    break;
                case MoveRightAction:
                    SetHorizontalVelocity(moveSpeed);
                    lastHorizontalDirection = 1f;
                    break;
                case JumpAction:
                    SetVerticalVelocity(jumpVelocity);
                    LastProvenanceReward = RecordProvenanceAction(PlayerJumpActionType);
                    break;
                case AttackMeleeAction:
                    SetHorizontalVelocity(0f);
                    LastProvenanceReward = RecordProvenanceAction(PlayerAttackActionType);
                    break;
                case DashAction:
                    SetHorizontalVelocity(lastHorizontalDirection * dashSpeed);
                    LastProvenanceReward = RecordProvenanceAction(PlayerDashActionType);
                    break;
                default:
                    SetHorizontalVelocity(0f);
                    break;
            }

            return LastProvenanceReward;
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

            if (rewardShaper == null)
            {
                rewardShaper = GetComponent<ProvenanceRewardShaper>();
            }
        }

        private void ConfigureBehaviorParameters()
        {
            var behaviorParameters = GetComponent<BehaviorParameters>();
            if (behaviorParameters == null)
            {
                return;
            }

            behaviorParameters.BehaviorName = "BossAgent";
            behaviorParameters.BrainParameters.VectorObservationSize = ObservationCount;
            behaviorParameters.BrainParameters.ActionSpec = ActionSpec.MakeDiscrete(DiscreteActionCount);
        }

        private float RecordProvenanceAction(string actionType)
        {
            return rewardShaper != null ? rewardShaper.RecordAction(actionType) : 0f;
        }

        private bool IsGrounded()
        {
            return rb == null || Mathf.Abs(rb.linearVelocity.y) <= 0.001f;
        }

        private void SetHorizontalVelocity(float velocityX)
        {
            if (rb == null)
            {
                return;
            }

            rb.linearVelocity = new Vector2(velocityX, rb.linearVelocity.y);
        }

        private void SetVerticalVelocity(float velocityY)
        {
            if (rb == null)
            {
                return;
            }

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, velocityY);
        }
    }
}
