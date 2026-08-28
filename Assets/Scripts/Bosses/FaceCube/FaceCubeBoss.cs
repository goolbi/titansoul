using System.Collections;
using TitanSoul.Combat;
using UnityEngine;

namespace TitanSoul.Bosses.FaceCube
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public sealed class FaceCubeBoss : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform rollingVisual;
        [SerializeField] private Transform eyeMuzzle;
        [SerializeField] private EyeCube.EyeCubeLaser laser;
        [SerializeField] private SpriteRenderer slamShadow;

        [Header("Four Face Roll")]
        [SerializeField, Min(0.1f)] private float faceStep = 3.2f;
        [SerializeField, Min(0.05f)] private float rollSeconds = 0.42f;
        [SerializeField, Min(0f)] private float pauseBetweenRolls = 0.15f;
        [SerializeField] private Vector2 arenaMin = new(-11f, -9f);
        [SerializeField] private Vector2 arenaMax = new(11f, 5.5f);
        [SerializeField, Min(0f)] private float movementVibration = 0.055f;
        [SerializeField, Min(1f)] private float vibrationFrequency = 34f;

        [Header("Eye Laser")]
        [SerializeField, Min(0f)] private float chargeSeconds = 0.9f;
        [SerializeField, Min(0f)] private float fireSeconds = 0.35f;
        [SerializeField, Min(0f)] private float restAfterLaser = 0.65f;

        [Header("Downward Slam")]
        [SerializeField, Min(0.1f)] private float riseSeconds = 0.35f;
        [SerializeField, Min(0f)] private float airborneSeconds = 2.5f;
        [SerializeField, Min(0.1f)] private float fallSeconds = 0.32f;
        [SerializeField, Min(0.1f)] private float riseHeight = 14f;
        [SerializeField, Min(1)] private int slamDamage = 10;
        [SerializeField] private Vector2 slamHitSize = new(3.2f, 3.2f);
        [SerializeField] private LayerMask slamTargetLayers;

        private static readonly Vector2[] Directions =
        {
            Vector2.right,
            Vector2.up,
            Vector2.left,
            Vector2.down
        };

        private Rigidbody2D body;
        private BoxCollider2D bodyCollider;
        private Health health;
        private Coroutine behaviour;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<BoxCollider2D>();
            health = GetComponent<Health>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            if (slamShadow != null)
                slamShadow.enabled = false;
        }

        private void OnEnable()
        {
            health.HealthChanged += OnHealthChanged;
        }

        private void Start()
        {
            if (health.IsAlive)
                behaviour = StartCoroutine(BossLoop());
        }

        private void OnDisable()
        {
            health.HealthChanged -= OnHealthChanged;
            behaviour = null;
            body.linearVelocity = Vector2.zero;
        }

        private IEnumerator BossLoop()
        {
            while (health.IsAlive)
            {
                for (int step = 0; step < 4 && health.IsAlive; step++)
                {
                    yield return RollOneFace(ChooseRollDirection());
                    yield return new WaitForSeconds(pauseBetweenRolls);
                }

                if (!health.IsAlive)
                    yield break;

                yield return PerformEyeAttack();

                yield return new WaitForSeconds(restAfterLaser);
            }
        }

        private IEnumerator RollOneFace(Vector2 direction)
        {
            Vector2 start = body.position;
            Vector2 destination = start + direction * faceStep;
            destination.x = Mathf.Clamp(destination.x, arenaMin.x, arenaMax.x);
            destination.y = Mathf.Clamp(destination.y, arenaMin.y, arenaMax.y);

            float signedQuarterTurn = direction.x != 0f
                ? -90f * Mathf.Sign(direction.x)
                : 90f * Mathf.Sign(direction.y);
            float startAngle = rollingVisual != null
                ? rollingVisual.localEulerAngles.z
                : 0f;
            float elapsed = 0f;
            Vector3 visualRestPosition = rollingVisual != null
                ? rollingVisual.localPosition
                : Vector3.zero;

            while (elapsed < rollSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / rollSeconds));
                body.MovePosition(Vector2.Lerp(start, destination, t));
                if (rollingVisual != null)
                {
                    rollingVisual.localRotation = Quaternion.Euler(0f, 0f, startAngle + signedQuarterTurn * t);
                    float vibration = Mathf.Sin(elapsed * vibrationFrequency) * movementVibration;
                    rollingVisual.localPosition = visualRestPosition + new Vector3(
                        vibration,
                        Mathf.Cos(elapsed * vibrationFrequency * 1.37f) * movementVibration * 0.65f,
                        0f);
                }
                yield return null;
            }

            body.position = destination;
            if (rollingVisual != null)
            {
                rollingVisual.localRotation = Quaternion.Euler(0f, 0f, startAngle + signedQuarterTurn);
                rollingVisual.localPosition = visualRestPosition;
            }
        }

        private IEnumerator PerformEyeAttack()
        {
            if (laser == null || eyeMuzzle == null)
                yield break;

            Vector2 eyeDirection = eyeMuzzle.up;
            if (eyeDirection.y < -0.7f)
            {
                yield return DownwardSlam();
                yield break;
            }

            bool canDamage = eyeDirection.y <= 0.7f;
            // Looking upward deliberately wastes the shot in empty space.
            yield return laser.PlayDirection(eyeMuzzle, eyeDirection, chargeSeconds, fireSeconds, canDamage);
        }

        private IEnumerator DownwardSlam()
        {
            if (rollingVisual == null)
                yield break;

            Vector3 groundPosition = rollingVisual.localPosition;
            Vector3 skyPosition = groundPosition + Vector3.up * riseHeight;
            float elapsed = 0f;

            while (elapsed < riseSeconds && health.IsAlive)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / riseSeconds));
                rollingVisual.localPosition = Vector3.Lerp(groundPosition, skyPosition, t);
                yield return null;
            }

            if (!health.IsAlive)
                yield break;

            rollingVisual.localPosition = skyPosition;
            if (bodyCollider != null)
                bodyCollider.enabled = false;
            yield return new WaitForSeconds(airborneSeconds);

            if (slamShadow != null)
            {
                slamShadow.enabled = true;
                slamShadow.transform.localScale = new Vector3(0.45f, 0.12f, 1f);
            }

            elapsed = 0f;
            while (elapsed < fallSeconds && health.IsAlive)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fallSeconds));
                rollingVisual.localPosition = Vector3.Lerp(skyPosition, groundPosition, t);
                if (slamShadow != null)
                {
                    float scale = Mathf.Lerp(0.45f, 1f, t);
                    slamShadow.transform.localScale = new Vector3(scale, scale * 0.25f, 1f);
                    Color color = slamShadow.color;
                    color.a = Mathf.Lerp(0.18f, 0.55f, t);
                    slamShadow.color = color;
                }
                yield return null;
            }

            rollingVisual.localPosition = groundPosition;
            if (bodyCollider != null)
                bodyCollider.enabled = true;
            if (slamShadow != null)
                slamShadow.enabled = false;

            if (!health.IsAlive)
                yield break;

            Collider2D target = Physics2D.OverlapBox(body.position, slamHitSize, 0f, slamTargetLayers);
            IDamageable damageable = target != null ? target.GetComponentInParent<IDamageable>() : null;
            if (damageable != null && damageable.IsAlive)
                damageable.TakeDamage(slamDamage, body.position, Vector2.down);
        }

        private Vector2 ChooseRollDirection()
        {
            int offset = Random.Range(0, Directions.Length);
            for (int index = 0; index < Directions.Length; index++)
            {
                Vector2 direction = Directions[(offset + index) % Directions.Length];
                Vector2 candidate = body.position + direction * faceStep;
                if (candidate.x >= arenaMin.x && candidate.x <= arenaMax.x
                    && candidate.y >= arenaMin.y && candidate.y <= arenaMax.y)
                    return direction;
            }

            return Vector2.zero;
        }

        private void OnHealthChanged(int current, int maximum)
        {
            if (current > 0)
                return;

            if (behaviour != null)
                StopCoroutine(behaviour);
            behaviour = null;
            body.linearVelocity = Vector2.zero;

            if (laser != null)
                laser.gameObject.SetActive(false);
            if (slamShadow != null)
                slamShadow.enabled = false;
            if (rollingVisual != null)
                rollingVisual.localPosition = Vector3.zero;
            foreach (Collider2D hitbox in GetComponentsInChildren<Collider2D>())
                hitbox.enabled = false;

            if (rollingVisual != null)
                rollingVisual.localScale *= 0.85f;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube((arenaMin + arenaMax) * 0.5f, arenaMax - arenaMin);
        }
    }
}
