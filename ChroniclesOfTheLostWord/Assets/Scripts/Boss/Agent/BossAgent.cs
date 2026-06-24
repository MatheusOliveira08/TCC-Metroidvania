using System;
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
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(BehaviorParameters))]
    [RequireComponent(typeof(DecisionRequester))]
    public class BossAgent : Agent
    {
        public const int ObservationCount = 10;
        public const int DiscreteActionCount = 6;
        public const int DefaultDecisionPeriod = 1;
        public const int DefaultMaxEpisodeSteps = 1000;
        public const float DefaultEditorTrainingTimeScale = 100f;

        public const int IdleAction = 0;
        public const int MoveLeftAction = 1;
        public const int MoveRightAction = 2;
        public const int JumpAction = 3;
        public const int AttackMeleeAction = 4;
        public const int DashAction = 5;

        private const string PlayerJumpActionType = "PlayerJump";
        private const string PlayerAttackActionType = "PlayerAttack";
        private const string PlayerDashActionType = "PlayerDash";
        private const float GroundedCheckDistance = 0.08f;

        [SerializeField] private Transform playerTarget;
        [SerializeField] private Transform bossSpawnPoint;
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private BossHealth bossHealth;
        [SerializeField] private ProvenanceRewardShaper rewardShaper;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float jumpVelocity = 6f;
        [SerializeField] private float dashSpeed = 8f;
        [SerializeField] private int decisionPeriod = DefaultDecisionPeriod;
        [SerializeField] private int maxEpisodeSteps = DefaultMaxEpisodeSteps;
        [SerializeField] private SpriteRenderer actionFeedbackRenderer;
        [SerializeField] private Color attackFeedbackColor = new(1f, 0.35f, 0.2f, 1f);
        [SerializeField] private Color dashFeedbackColor = new(0.2f, 0.85f, 1f, 1f);
        [SerializeField] private bool applyEditorTrainingSettings;
        [SerializeField] private float editorTrainingTimeScale = DefaultEditorTrainingTimeScale;

        private Rigidbody2D rb;
        private BoxCollider2D boxCollider;
        private float lastHorizontalDirection = 1f;
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];

        public event Action<int> OnDiscreteActionApplied;

        public float LastProvenanceReward { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            ResolveReferences();
            ConfigureBehaviorParameters();
            ConfigureTrainingRuntime();
        }

        public void BindDependencies(Transform newPlayerTarget, BossHealth newBossHealth, ProvenanceRewardShaper newRewardShaper)
        {
            playerTarget = newPlayerTarget;
            bossHealth = newBossHealth;
            rewardShaper = newRewardShaper;
            ResolveReferences();
            ConfigureBehaviorParameters();
            ConfigureTrainingRuntime();
        }

        public void BindTrainingReset(Transform newBossSpawnPoint, Transform newPlayerSpawnPoint)
        {
            bossSpawnPoint = newBossSpawnPoint;
            playerSpawnPoint = newPlayerSpawnPoint;
        }

        public override void OnEpisodeBegin()
        {
            ResetTrainingEpisode();
            ConfigureEditorTrainingRuntime();
        }

        public void ResetTrainingEpisode()
        {
            ResolveReferences();

            if (bossSpawnPoint != null)
            {
                transform.position = bossSpawnPoint.position;
            }

            if (playerTarget != null && playerSpawnPoint != null)
            {
                playerTarget.position = playerSpawnPoint.position;
            }

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            if (playerTarget != null && playerTarget.TryGetComponent<Rigidbody2D>(out var playerRigidbody))
            {
                playerRigidbody.linearVelocity = Vector2.zero;
            }

            bossHealth?.ResetHealth();
            rewardShaper?.ResetBuffer();
            LastProvenanceReward = 0f;
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
            var recordedActionIndex = actionIndex;

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
                    if (IsGrounded())
                    {
                        SetVerticalVelocity(jumpVelocity);
                    }

                    LastProvenanceReward = RecordProvenanceAction(PlayerJumpActionType);
                    break;
                case AttackMeleeAction:
                    SetHorizontalVelocity(0f);
                    ShowPassiveActionFeedback("Boss PPO Attack", attackFeedbackColor);
                    LastProvenanceReward = RecordProvenanceAction(PlayerAttackActionType);
                    break;
                case DashAction:
                    SetHorizontalVelocity(lastHorizontalDirection * dashSpeed);
                    ShowPassiveActionFeedback("Boss PPO Dash", dashFeedbackColor);
                    LastProvenanceReward = RecordProvenanceAction(PlayerDashActionType);
                    break;
                default:
                    recordedActionIndex = IdleAction;
                    SetHorizontalVelocity(0f);
                    break;
            }

            OnDiscreteActionApplied?.Invoke(recordedActionIndex);
            return LastProvenanceReward;
        }

        private void ResolveReferences()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
            }

            if (boxCollider == null)
            {
                boxCollider = GetComponent<BoxCollider2D>();
            }

            if (bossHealth == null)
            {
                bossHealth = GetComponent<BossHealth>();
            }

            if (rewardShaper == null)
            {
                rewardShaper = GetComponent<ProvenanceRewardShaper>();
            }

            if (actionFeedbackRenderer == null)
            {
                actionFeedbackRenderer = GetComponent<SpriteRenderer>();
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

        private void ConfigureTrainingRuntime()
        {
            MaxStep = Mathf.Max(0, maxEpisodeSteps);

            var decisionRequester = GetComponent<DecisionRequester>();
            if (decisionRequester == null)
            {
                return;
            }

            decisionRequester.DecisionPeriod = Mathf.Max(1, decisionPeriod);
            decisionRequester.DecisionStep = 0;
            decisionRequester.TakeActionsBetweenDecisions = true;
            ConfigureEditorTrainingRuntime();
        }

        private void ConfigureEditorTrainingRuntime()
        {
            if (!applyEditorTrainingSettings)
            {
                return;
            }

            Application.runInBackground = true;
            Time.timeScale = Mathf.Max(1f, editorTrainingTimeScale);
        }

        private float RecordProvenanceAction(string actionType)
        {
            return rewardShaper != null ? rewardShaper.RecordAction(actionType) : 0f;
        }

        private void ShowPassiveActionFeedback(string message, Color feedbackColor)
        {
            if (actionFeedbackRenderer != null)
            {
                actionFeedbackRenderer.color = feedbackColor;
            }

            Debug.Log(message, this);
        }

        private bool IsGrounded()
        {
            if (boxCollider == null)
            {
                return false;
            }

            var contactFilter = new ContactFilter2D
            {
                useTriggers = false
            };
            contactFilter.SetLayerMask(Physics2D.DefaultRaycastLayers);

            var hitCount = boxCollider.Cast(Vector2.down, contactFilter, groundHits, GroundedCheckDistance);
            for (var i = 0; i < hitCount; i++)
            {
                if (groundHits[i].collider != null && groundHits[i].collider != boxCollider)
                {
                    return true;
                }
            }

            return false;
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
