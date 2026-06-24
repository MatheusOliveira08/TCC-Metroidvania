using System;
using TerraSilente.Combat;
using UnityEngine;

namespace TerraSilente.Player
{
    [DisallowMultipleComponent]
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        public event Action<float> OnPlayerDamageTaken;
        public event Action OnPlayerDeath;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead { get; private set; }

        private void Awake()
        {
            ResetHealth();
        }

        public void ResetHealth()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = maxHealth;
            IsDead = false;
        }

        public void TakeDamage(float damageAmount)
        {
            if (IsDead || damageAmount <= 0f)
            {
                return;
            }

            var appliedDamage = Mathf.Min(damageAmount, currentHealth);
            currentHealth = Mathf.Max(0f, currentHealth - damageAmount);
            OnPlayerDamageTaken?.Invoke(appliedDamage);

            if (currentHealth > 0f)
            {
                return;
            }

            IsDead = true;
            OnPlayerDeath?.Invoke();
        }
    }
}
