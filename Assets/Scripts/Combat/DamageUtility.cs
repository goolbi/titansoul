using UnityEngine;

namespace TitanSoul.Combat
{
    public static class DamageUtility
    {
        public static bool TryDamage(
            Collider2D target,
            int damage,
            Vector2 hitPoint,
            Vector2 hitDirection)
        {
            if (target == null)
                return false;

            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.IsAlive)
                return false;

            damageable.TakeDamage(damage, hitPoint, hitDirection);
            return true;
        }
    }
}
