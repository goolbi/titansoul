using System.Collections;
using TitanSoul.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TitanSoul.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Health))]
    public sealed class PlayerController : MonoBehaviour
    {
        private static readonly int MovingId = Animator.StringToHash("Moving");
        private static readonly int DirectionId = Animator.StringToHash("Direction");
        private static readonly int ArmedId = Animator.StringToHash("Armed");
        private static readonly int DashId = Animator.StringToHash("Dash");
        private static readonly int HurtId = Animator.StringToHash("Hurt");
        private static readonly int DeadId = Animator.StringToHash("Dead");

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform firePoint;
        [SerializeField] private PlayerProjectile projectilePrefab;
        [SerializeField] private Camera gameplayCamera;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(0f)] private float dashSpeed = 12f;
        [SerializeField, Min(0f)] private float dashDuration = 0.16f;
        [SerializeField, Min(0f)] private float dashCooldown = 0.7f;

        private Rigidbody2D body;
        private Health health;
        private Vector2 moveInput;
        private Vector2 aimDirection = Vector2.down;
        private Vector2 facingDirection = Vector2.down;
        private Vector2 dashDirection;
        private float nextDashTime;
        private bool isDashing;
        private bool isAiming;
        private bool hasArrow = true;

        public bool HasArrow => hasArrow;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            AssignDefaultAnimatorController();
            AssignDefaultProjectile();
            if (gameplayCamera == null)
                gameplayCamera = Camera.main;
        }

        private void OnValidate()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            AssignDefaultAnimatorController();
            AssignDefaultProjectile();
        }

        private void OnEnable()
        {
            health.HealthChanged += OnHealthChanged;
        }

        private void OnDisable()
        {
            health.HealthChanged -= OnHealthChanged;
        }

        private void Update()
        {
            if (!health.IsAlive)
                return;

            ReadMovement();
            ReadAim();
            ReadCombat();
            UpdateAnimator();
        }

        private void FixedUpdate()
        {
            if (!health.IsAlive)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            body.linearVelocity = isDashing
                ? dashDirection * dashSpeed
                : moveInput * moveSpeed;
        }

        private void ReadMovement()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                moveInput = Vector2.zero;
                return;
            }

            float horizontal = 0f;
            float vertical = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical += 1f;
            moveInput = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        }

        private void ReadAim()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || gameplayCamera == null)
                return;

            Vector3 mouseWorld = gameplayCamera.ScreenToWorldPoint(mouse.position.ReadValue());
            Vector2 direction = (Vector2)mouseWorld - body.position;
            if (direction.sqrMagnitude > 0.001f)
                aimDirection = direction.normalized;

            if (firePoint != null)
                firePoint.right = aimDirection;
        }

        private void ReadCombat()
        {
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;

            if (!hasArrow)
            {
                isAiming = false;
            }
            else if (mouse != null)
            {
                if (mouse.rightButton.wasPressedThisFrame)
                    isAiming = true;

                if (isAiming && mouse.rightButton.wasReleasedThisFrame)
                {
                    Shoot();
                    isAiming = false;
                }
            }

            bool dashPressed = keyboard != null
                && (keyboard.spaceKey.wasPressedThisFrame
                    || keyboard.leftShiftKey.wasPressedThisFrame);
            if (dashPressed && Time.time >= nextDashTime && !isDashing)
                StartCoroutine(Dash());
        }

        private void Shoot()
        {
            if (!hasArrow || projectilePrefab == null)
                return;

            hasArrow = false;
            Transform spawnPoint = firePoint != null ? firePoint : transform;
            PlayerProjectile projectile = Instantiate(
                projectilePrefab,
                spawnPoint.position,
                Quaternion.identity);
            projectile.Launch(aimDirection, transform, this);
        }

        public void RecoverArrow()
        {
            hasArrow = true;
        }

        private IEnumerator Dash()
        {
            isDashing = true;
            nextDashTime = Time.time + dashCooldown;
            dashDirection = moveInput.sqrMagnitude > 0f ? moveInput : aimDirection;
            if (animator != null)
                animator.SetTrigger(DashId);

            yield return new WaitForSeconds(dashDuration);
            isDashing = false;
        }

        private void UpdateAnimator()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return;

            bool armedPose = isAiming && hasArrow;
            if (armedPose)
                facingDirection = aimDirection;
            else if (moveInput.sqrMagnitude > 0.01f)
                facingDirection = moveInput.normalized;

            bool showMovingPose = moveInput.sqrMagnitude > 0.01f && !armedPose;
            animator.SetBool(MovingId, showMovingPose);
            animator.SetBool(ArmedId, armedPose);
            animator.SetInteger(DirectionId, DirectionToIndex(facingDirection));
        }

        private static int DirectionToIndex(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
                return 0;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            int sectorFromRight = Mathf.RoundToInt(angle / 45f);
            return (sectorFromRight + 2 + 8) % 8;
        }

        private void OnHealthChanged(int current, int maximum)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return;

            animator.SetTrigger(current <= 0 ? DeadId : HurtId);
            if (current <= 0)
                body.linearVelocity = Vector2.zero;
        }

        private void AssignDefaultAnimatorController()
        {
#if UNITY_EDITOR
            if (animator == null || animator.runtimeAnimatorController != null)
                return;

            animator.runtimeAnimatorController =
                UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    "Assets/Animations/Generated/Player/PlayerAnimator.controller");
            if (animator.runtimeAnimatorController != null)
                UnityEditor.EditorUtility.SetDirty(animator);
#endif
        }

        private void AssignDefaultProjectile()
        {
#if UNITY_EDITOR
            PlayerProjectile generatedArrow =
                UnityEditor.AssetDatabase.LoadAssetAtPath<PlayerProjectile>(
                "Assets/Prefabs/Projectiles/Generated/MagicArrow.prefab");
            if (generatedArrow != null && projectilePrefab != generatedArrow)
            {
                projectilePrefab = generatedArrow;
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }
    }
}
