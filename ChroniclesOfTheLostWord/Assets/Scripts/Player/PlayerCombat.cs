using System;
using TerraSilente.Combat;
using UnityEngine;

namespace TerraSilente.Player
{
    [DisallowMultipleComponent]
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private Vector2 attackSize = new(1.5f, 1f);
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private LayerMask targetLayers = Physics2D.DefaultRaycastLayers;
        [SerializeField] private float attackCooldown = 0.25f;

        private global::PlayerController playerController;
        private float lastAttackTime = float.NegativeInfinity;

        public event Action OnPlayerAttackPerformed;

        public float AttackDamage => attackDamage;

        private void Awake()
        {
            playerController = GetComponent<global::PlayerController>();

            if (attackOrigin == null)
            {
                attackOrigin = transform;
            }
        }

        private void OnEnable()
        {
            if (playerController == null)
            {
                playerController = GetComponent<global::PlayerController>();
            }

            if (playerController != null)
            {
                playerController.OnPlayerAttack += PerformAttack;
            }
        }

        private void OnDisable()
        {
            if (playerController != null)
            {
                playerController.OnPlayerAttack -= PerformAttack;
            }
        }

        public void PerformAttack()
        {
            if (Time.time < lastAttackTime + attackCooldown)
            {
                return;
            }

            lastAttackTime = Time.time;
            OnPlayerAttackPerformed?.Invoke();

            var origin = attackOrigin != null ? attackOrigin : transform;
            var hits = Physics2D.OverlapBoxAll(origin.position, attackSize, 0f, targetLayers);

            foreach (var hit in hits)
            {
                if (hit == null || hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                var behaviours = hit.GetComponentsInParent<MonoBehaviour>();
                foreach (var behaviour in behaviours)
                {
                    if (behaviour is not IDamageable damageable)
                    {
                        continue;
                    }

                    damageable.TakeDamage(attackDamage);
                    return;
                }
            }
        }
    }
}
