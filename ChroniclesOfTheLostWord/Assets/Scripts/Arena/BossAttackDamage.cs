using TerraSilente.Boss;
using TerraSilente.Player;
using UnityEngine;

namespace TerraSilente.Arena
{
    [DisallowMultipleComponent]
    public class BossAttackDamage : MonoBehaviour
    {
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private BossFsmController bossFsmController;
        [SerializeField] private BossAgent bossAgent;
        [SerializeField] private float damageAmount = 10f;
        [SerializeField] private float attackRange = 1.25f;
        [SerializeField] private float damageCooldown = 0.8f;

        private bool isSubscribedToSources;
        private float lastDamageTime = float.NegativeInfinity;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToSources();
        }

        private void OnDisable()
        {
            UnsubscribeFromSources();
        }

        public void BindSources(
            PlayerHealth newPlayerHealth,
            Transform newAttackOrigin,
            BossFsmController newBossFsmController = null,
            BossAgent newBossAgent = null)
        {
            UnsubscribeFromSources();

            playerHealth = newPlayerHealth;
            attackOrigin = newAttackOrigin;
            bossFsmController = newBossFsmController;
            bossAgent = newBossAgent;

            ResolveReferences();
            SubscribeToSources();
        }

        public bool TryApplyDamage(float currentTimeSeconds)
        {
            ResolveReferences();

            if (playerHealth == null || playerHealth.IsDead)
            {
                return false;
            }

            if (currentTimeSeconds < lastDamageTime + Mathf.Max(0f, damageCooldown))
            {
                return false;
            }

            var origin = attackOrigin != null ? attackOrigin : transform;
            if (Vector2.Distance(origin.position, playerHealth.transform.position) > Mathf.Max(0f, attackRange))
            {
                return false;
            }

            playerHealth.TakeDamage(damageAmount);
            lastDamageTime = currentTimeSeconds;
            return true;
        }

        private void ResolveReferences()
        {
            if (playerHealth == null)
            {
                playerHealth = FindFirstObjectByType<PlayerHealth>();
            }

            if (attackOrigin == null)
            {
                attackOrigin = transform;
            }

            if (bossFsmController == null)
            {
                bossFsmController = GetComponent<BossFsmController>();
            }

            if (bossAgent == null)
            {
                bossAgent = GetComponent<BossAgent>();
            }
        }

        private void SubscribeToSources()
        {
            if (isSubscribedToSources)
            {
                return;
            }

            var hasSubscription = false;

            if (bossFsmController != null)
            {
                bossFsmController.OnBossAttackPerformed += ApplyDamageFromFsmAttack;
                hasSubscription = true;
            }

            if (bossAgent != null)
            {
                bossAgent.OnDiscreteActionApplied += ApplyDamageFromBossAgentAction;
                hasSubscription = true;
            }

            isSubscribedToSources = hasSubscription;
        }

        private void UnsubscribeFromSources()
        {
            if (!isSubscribedToSources)
            {
                return;
            }

            if (bossFsmController != null)
            {
                bossFsmController.OnBossAttackPerformed -= ApplyDamageFromFsmAttack;
            }

            if (bossAgent != null)
            {
                bossAgent.OnDiscreteActionApplied -= ApplyDamageFromBossAgentAction;
            }

            isSubscribedToSources = false;
        }

        private void ApplyDamageFromFsmAttack()
        {
            TryApplyDamage(Time.time);
        }

        private void ApplyDamageFromBossAgentAction(int actionIndex)
        {
            if (actionIndex == BossAgent.AttackMeleeAction)
            {
                TryApplyDamage(Time.time);
            }
        }
    }
}
