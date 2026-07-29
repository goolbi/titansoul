using UnityEngine;

namespace TitanSoul.Combat
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(int amount, Vector2 hitPoint, Vector2 hitDirection);
    }
}
