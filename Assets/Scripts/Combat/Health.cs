using System;
using UnityEngine;
using UnityEngine.Events;

namespace TitanSoul.Combat
{
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private int maxHealth = 10;
        [SerializeField] private float invulnerabilitySeconds = 0.15f;
        [SerializeField] private UnityEvent onDamaged;
        [SerializeField] private UnityEvent onDied;

        public event Action<int, int> HealthChanged;
        public int CurrentHealth { get; private set; }
        public int MaxHealth => maxHealth;
        public bool IsAlive => CurrentHealth > 0;

        private float invulnerableUntil;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(int amount, Vector2 hitPoint, Vector2 hitDirection)
        {
            if (!IsAlive || amount <= 0 || Time.time < invulnerableUntil)
                return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            invulnerableUntil = Time.time + invulnerabilitySeconds;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (CurrentHealth == 0)
                onDied?.Invoke();
            else
                onDamaged?.Invoke();
        }

        [ContextMenu("Restore Full Health")]
        public void RestoreFullHealth()
        {
            CurrentHealth = maxHealth;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }
    }
}
