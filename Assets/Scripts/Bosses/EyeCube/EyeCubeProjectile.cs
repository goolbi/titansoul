using TitanSoul.Combat;
using UnityEngine;

namespace TitanSoul.Bosses.EyeCube
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class EyeCubeProjectile : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float speed = 7f;
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField, Min(0.1f)] private float lifetime = 6f;
        [SerializeField] private LayerMask targetLayers;
        [SerializeField] private GameObject impactEffect;

        private Rigidbody2D body;
        private Vector2 direction;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
        }

        private void OnEnable()
        {
            Destroy(gameObject, lifetime);
        }

        public void Launch(Vector2 launchDirection, float speedMultiplier = 1f)
        {
            direction = launchDirection.sqrMagnitude > 0f
                ? launchDirection.normalized
                : Vector2.down;
            body.linearVelocity = direction * speed * speedMultiplier;
            transform.right = direction;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if ((targetLayers.value & (1 << other.gameObject.layer)) == 0)
                return;

            if (!DamageUtility.TryDamage(other, damage, transform.position, direction))
                return;

            if (impactEffect != null)
                Instantiate(impactEffect, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }
}
