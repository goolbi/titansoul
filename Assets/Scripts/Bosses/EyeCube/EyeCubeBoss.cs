using System.Collections;
using TitanSoul.Combat;
using UnityEngine;

namespace TitanSoul.Bosses.EyeCube
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(Health))]
    public sealed class EyeCubeBoss : MonoBehaviour
    {
        private static readonly int StateId = Animator.StringToHash("State");
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int HurtId = Animator.StringToHash("Hurt");
        private static readonly int DeadId = Animator.StringToHash("Dead");

        private enum BossState
        {
            Sleeping = 0,
            Opening = 1,
            Moving = 2,
            Shooting = 3,
            Laser = 4,
            Hurt = 5,
            Dead = 6
        }

        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private Transform eyeMuzzle;
        [SerializeField] private EyeCubeProjectile projectilePrefab;
        [SerializeField] private EyeCubeLaser laser;

        [Header("Activation")]
        [SerializeField] private bool activateOnStart = true;
        [SerializeField, Min(0f)] private float openingSeconds = 1.1f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 2.2f;
        [SerializeField, Min(0f)] private float preferredDistance = 5f;
        [SerializeField, Min(0f)] private float moveSeconds = 2.2f;
        [SerializeField] private Vector2 arenaMin = new(-8f, -4f);
        [SerializeField] private Vector2 arenaMax = new(8f, 4f);

        [Header("Projectile Pattern")]
        [SerializeField, Min(1)] private int radialProjectileCount = 12;
        [SerializeField, Min(1)] private int aimedBurstCount = 3;
        [SerializeField, Min(0f)] private float burstInterval = 0.18f;
        [SerializeField, Min(0f)] private float shootAnticipation = 0.35f;

        [Header("Laser Pattern")]
        [SerializeField, Min(0f)] private float laserChargeSeconds = 1.25f;
        [SerializeField, Min(0f)] private float laserFireSeconds = 0.8f;
        [SerializeField, Min(0f)] private float laserTrackingDegreesPerSecond = 100f;

        [Header("Pacing")]
        [SerializeField, Min(0f)] private float restBetweenActions = 0.65f;

        private Rigidbody2D body;
        private Animator animator;
        private Health health;
        private Coroutine behaviour;
        private BossState state;
        private bool phaseTwo;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
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
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    target = player.transform;
            }

            SetState(BossState.Sleeping);
            if (activateOnStart)
                Activate();
        }

        private void OnDisable()
        {
            health.HealthChanged -= OnHealthChanged;
            behaviour = null;
            body.linearVelocity = Vector2.zero;
        }

        private void FixedUpdate()
        {
            if (state != BossState.Moving || target == null)
            {
                body.linearVelocity = Vector2.zero;
                animator.SetFloat(SpeedId, 0f);
                return;
            }

            Vector2 toTarget = (Vector2)target.position - body.position;
            Vector2 tangent = new(-toTarget.y, toTarget.x);
            float distanceCorrection = Mathf.Sign(toTarget.magnitude - preferredDistance);
            Vector2 movement = (tangent.normalized + toTarget.normalized * distanceCorrection * 0.35f)
                .normalized;

            Vector2 next = body.position + movement * (moveSpeed * Time.fixedDeltaTime);
            next.x = Mathf.Clamp(next.x, arenaMin.x, arenaMax.x);
            next.y = Mathf.Clamp(next.y, arenaMin.y, arenaMax.y);
            body.MovePosition(next);
            animator.SetFloat(SpeedId, movement.magnitude);
        }

        [ContextMenu("Activate Boss")]
        public void Activate()
        {
            if (behaviour == null && health.IsAlive)
                behaviour = StartCoroutine(BossLoop());
        }

        public void NotifyHurt()
        {
            if (state != BossState.Dead)
                animator.SetTrigger(HurtId);
        }

        public void NotifyDeath()
        {
            if (behaviour != null)
                StopCoroutine(behaviour);

            behaviour = null;
            body.linearVelocity = Vector2.zero;
            SetState(BossState.Dead);
            animator.SetTrigger(DeadId);
            foreach (Collider2D hitbox in GetComponentsInChildren<Collider2D>())
                hitbox.enabled = false;
        }

        private IEnumerator BossLoop()
        {
            SetState(BossState.Opening);
            yield return new WaitForSeconds(openingSeconds);

            int actionIndex = 0;
            while (health.IsAlive)
            {
                SetState(BossState.Moving);
                yield return new WaitForSeconds(phaseTwo ? moveSeconds * 0.65f : moveSeconds);

                if (actionIndex % 2 == 0)
                    yield return ShootPattern();
                else
                    yield return LaserPattern();

                actionIndex++;
                yield return new WaitForSeconds(phaseTwo
                    ? restBetweenActions * 0.55f
                    : restBetweenActions);
            }
        }

        private IEnumerator ShootPattern()
        {
            SetState(BossState.Shooting);
            body.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(shootAnticipation);

            int burstCount = phaseTwo ? aimedBurstCount + 2 : aimedBurstCount;
            for (int i = 0; i < burstCount; i++)
            {
                Vector2 direction = target == null
                    ? Vector2.down
                    : ((Vector2)target.position - (Vector2)eyeMuzzle.position).normalized;
                SpawnProjectile(direction, phaseTwo ? 1.25f : 1f);
                yield return new WaitForSeconds(burstInterval);
            }

            int count = phaseTwo ? radialProjectileCount + 4 : radialProjectileCount;
            float offset = Random.Range(0f, 360f);
            for (int i = 0; i < count; i++)
            {
                float angle = offset + 360f * i / count;
                Vector2 direction = new(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad));
                SpawnProjectile(direction, phaseTwo ? 1.15f : 0.9f);
            }
        }

        private IEnumerator LaserPattern()
        {
            if (laser == null || target == null)
                yield break;

            SetState(BossState.Laser);
            body.linearVelocity = Vector2.zero;
            yield return laser.Play(
                eyeMuzzle,
                target,
                phaseTwo ? laserChargeSeconds * 0.7f : laserChargeSeconds,
                phaseTwo ? laserFireSeconds * 1.35f : laserFireSeconds,
                laserTrackingDegreesPerSecond);
        }

        private void SpawnProjectile(Vector2 direction, float speedMultiplier)
        {
            if (projectilePrefab == null || eyeMuzzle == null)
                return;

            EyeCubeProjectile projectile = Instantiate(
                projectilePrefab,
                eyeMuzzle.position,
                Quaternion.identity);
            projectile.Launch(direction, speedMultiplier);
        }

        private void OnHealthChanged(int current, int maximum)
        {
            if (current <= 0)
            {
                NotifyDeath();
                return;
            }

            phaseTwo = current <= maximum / 2;
            NotifyHurt();
        }

        private void SetState(BossState next)
        {
            state = next;
            animator.SetInteger(StateId, (int)state);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Vector3 center = (arenaMin + arenaMax) * 0.5f;
            Vector3 size = arenaMax - arenaMin;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
