using System.Collections;
using System.Collections.Generic;
using TitanSoul.Combat;
using UnityEngine;

namespace TitanSoul.Bosses.EyeCube
{
    public sealed class EyeCubeLaser : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private LineRenderer line;
        [SerializeField] private Gradient chargingColor;
        [SerializeField] private Gradient firingColor;
        [SerializeField, Min(0.01f)] private float chargingWidth = 0.08f;
        [SerializeField, Min(0.01f)] private float firingWidth = 0.6f;

        [Header("Hit")]
        [SerializeField] private LayerMask obstacleLayers;
        [SerializeField] private LayerMask targetLayers;
        [SerializeField, Min(1)] private int damage = 2;
        [SerializeField, Min(0.01f)] private float damageInterval = 0.2f;
        [SerializeField, Min(1f)] private float maxDistance = 30f;

        private readonly Dictionary<IDamageable, float> nextDamageTime = new();
        private Transform origin;
        private Transform target;
        private Vector2 lockedDirection;

        private void Awake()
        {
            if (line == null)
                line = GetComponent<LineRenderer>();

            line.positionCount = 2;
            line.useWorldSpace = true;
            line.enabled = false;
        }

        private void OnDisable()
        {
            if (line != null)
                line.enabled = false;

            nextDamageTime.Clear();
        }

        public IEnumerator Play(
            Transform beamOrigin,
            Transform trackedTarget,
            float chargeSeconds,
            float fireSeconds,
            float trackingSpeedDegrees)
        {
            origin = beamOrigin;
            target = trackedTarget;
            line.enabled = true;
            line.colorGradient = chargingColor;
            line.widthMultiplier = chargingWidth;

            float elapsed = 0f;
            lockedDirection = DirectionToTarget();
            while (elapsed < chargeSeconds)
            {
                elapsed += Time.deltaTime;
                Vector2 desired = DirectionToTarget();
                lockedDirection = RotateTowards(
                    lockedDirection,
                    desired,
                    trackingSpeedDegrees * Time.deltaTime);
                Draw(lockedDirection, false);
                yield return null;
            }

            line.colorGradient = firingColor;
            line.widthMultiplier = firingWidth;
            elapsed = 0f;
            while (elapsed < fireSeconds)
            {
                elapsed += Time.deltaTime;
                Draw(lockedDirection, true);
                yield return null;
            }

            line.enabled = false;
            nextDamageTime.Clear();
        }

        public IEnumerator PlayDirection(
            Transform beamOrigin,
            Vector2 worldDirection,
            float chargeSeconds,
            float fireSeconds)
        {
            origin = beamOrigin;
            target = null;
            lockedDirection = worldDirection.sqrMagnitude > 0f
                ? worldDirection.normalized
                : Vector2.up;
            line.enabled = true;
            line.colorGradient = chargingColor;
            line.widthMultiplier = chargingWidth;

            float elapsed = 0f;
            while (elapsed < chargeSeconds)
            {
                elapsed += Time.deltaTime;
                lockedDirection = origin != null
                    ? (Vector2)origin.up
                    : lockedDirection;
                Draw(lockedDirection, false);
                yield return null;
            }

            line.colorGradient = firingColor;
            line.widthMultiplier = firingWidth;
            elapsed = 0f;
            while (elapsed < fireSeconds)
            {
                elapsed += Time.deltaTime;
                Draw(lockedDirection, true);
                yield return null;
            }

            line.enabled = false;
            nextDamageTime.Clear();
        }

        private void Draw(Vector2 direction, bool canDamage)
        {
            Vector2 start = origin.position;
            RaycastHit2D obstacle = Physics2D.Raycast(
                start,
                direction,
                maxDistance,
                obstacleLayers);
            float distance = obstacle.collider == null ? maxDistance : obstacle.distance;
            Vector2 end = start + direction * distance;

            line.SetPosition(0, start);
            line.SetPosition(1, end);

            if (!canDamage)
                return;

            RaycastHit2D[] hits = Physics2D.CircleCastAll(
                start,
                firingWidth * 0.5f,
                direction,
                distance,
                targetLayers);
            foreach (RaycastHit2D hit in hits)
            {
                IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive)
                    continue;

                if (nextDamageTime.TryGetValue(damageable, out float nextTime)
                    && Time.time < nextTime)
                    continue;

                damageable.TakeDamage(damage, hit.point, direction);
                nextDamageTime[damageable] = Time.time + damageInterval;
            }
        }

        private Vector2 DirectionToTarget()
        {
            if (target == null)
                return lockedDirection.sqrMagnitude > 0f ? lockedDirection : Vector2.down;

            return ((Vector2)target.position - (Vector2)origin.position).normalized;
        }

        private static Vector2 RotateTowards(Vector2 from, Vector2 to, float maxDegrees)
        {
            float signedAngle = Vector2.SignedAngle(from, to);
            float step = Mathf.Clamp(signedAngle, -maxDegrees, maxDegrees);
            return (Quaternion.Euler(0f, 0f, step) * from).normalized;
        }
    }
}
