using System.Collections;
using TitanSoul.Combat;
using UnityEngine;

namespace TitanSoul.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class PlayerProjectile : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float speed = 12f;
        [SerializeField, Min(1)] private int damage = 5;
        [SerializeField, Min(0.1f)] private float maxDistance = 7f;
        [SerializeField] private LayerMask targetLayers;
        [SerializeField] private GameObject impactEffect;

        private Rigidbody2D body;
        private Vector2 direction;
        private Transform ownerRoot;
        private PlayerController ownerController;
        private Vector2 launchPosition;
        private bool landed;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
        }

        public void Launch(
            Vector2 launchDirection,
            Transform owner = null,
            PlayerController controller = null)
        {
            ownerRoot = owner != null ? owner.root : null;
            ownerController = controller;
            launchPosition = transform.position;
            direction = launchDirection.sqrMagnitude > 0f
                ? launchDirection.normalized
                : Vector2.right;
            body.linearVelocity = direction * speed;
            transform.right = direction;
        }

        private void Update()
        {
            if (!landed
                && Vector2.Distance(launchPosition, transform.position) >= maxDistance)
            {
                transform.position = launchPosition + direction * maxDistance;
                Land();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (ownerRoot != null && other.transform.root == ownerRoot)
            {
                if (landed && ownerController != null)
                {
                    ownerController.RecoverArrow();
                    Destroy(gameObject);
                }
                return;
            }

            if (landed)
                return;

            if ((targetLayers.value & (1 << other.gameObject.layer)) == 0)
                return;

            if (!DamageUtility.TryDamage(other, damage, transform.position, direction))
                return;

            if (impactEffect != null)
                Instantiate(impactEffect, transform.position, Quaternion.identity);

            Land();
        }

        private void Land()
        {
            if (landed)
                return;

            landed = true;
            body.linearVelocity = Vector2.zero;
            body.bodyType = RigidbodyType2D.Kinematic;
        }
    }
}
