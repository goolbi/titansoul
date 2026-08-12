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

        [Header("Four Face Roll")]
        [SerializeField, Min(0.1f)] private float faceStep = 3.2f;
        [SerializeField, Min(0.05f)] private float rollSeconds = 0.42f;
        [SerializeField, Min(0f)] private float pauseBetweenRolls = 0.15f;
        [SerializeField] private Vector2 arenaMin = new(-11f, -9f);
        [SerializeField] private Vector2 arenaMax = new(11f, 5.5f);

        [Header("Eye Laser")]
        [SerializeField, Min(0f)] private float chargeSeconds = 0.9f;
        [SerializeField, Min(0f)] private float fireSeconds = 0.35f;
        [SerializeField, Min(0f)] private float restAfterLaser = 0.65f;

        private static readonly Vector2[] Directions =
        {
            Vector2.right,
            Vector2.up,
            Vector2.left,
            Vector2.down
        };

        private Rigidbody2D body;
        private Health health;
        private Coroutine behaviour;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
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

                if (laser != null && eyeMuzzle != null)
                {
                    yield return laser.PlayDirection(
                        eyeMuzzle,
                        eyeMuzzle.up,
                        chargeSeconds,
                        fireSeconds);
                }

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

            while (elapsed < rollSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / rollSeconds));
                body.MovePosition(Vector2.Lerp(start, destination, t));
                if (rollingVisual != null)
                    rollingVisual.localRotation = Quaternion.Euler(0f, 0f, startAngle + signedQuarterTurn * t);
                yield return null;
            }

            body.position = destination;
            if (rollingVisual != null)
                rollingVisual.localRotation = Quaternion.Euler(0f, 0f, startAngle + signedQuarterTurn);
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
