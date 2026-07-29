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
        private static readonly int MoveXId = Animator.StringToHash("MoveX");
        private static readonly int MoveYId = Animator.StringToHash("MoveY");
        private static readonly int AimXId = Animator.StringToHash("AimX");
        private static readonly int AimYId = Animator.StringToHash("AimY");
        private static readonly int ShootId = Animator.StringToHash("Shoot");
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

        [Header("Combat")]
        [SerializeField, Min(0.01f)] private float secondsPerShot = 0.18f;

        private Rigidbody2D body;
        private Health health;
        private Vector2 moveInput;
        private Vector2 aimDirection = Vector2.right;
        private Vector2 dashDirection;
        private float nextShotTime;
        private float nextDashTime;
        private bool isDashing;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            if (gameplayCamera == null)
                gameplayCamera = Camera.main;
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

            if (mouse != null
                && mouse.leftButton.isPressed
                && Time.time >= nextShotTime)
            {
                Shoot();
            }

            bool dashPressed = keyboard != null
                && (keyboard.spaceKey.wasPressedThisFrame
                    || keyboard.leftShiftKey.wasPressedThisFrame);
            if (dashPressed && Time.time >= nextDashTime && !isDashing)
                StartCoroutine(Dash());
        }

        private void Shoot()
        {
            nextShotTime = Time.time + secondsPerShot;
            if (projectilePrefab != null && firePoint != null)
            {
                PlayerProjectile projectile = Instantiate(
                    projectilePrefab,
                    firePoint.position,
                    Quaternion.identity);
                projectile.Launch(aimDirection);
            }

            if (animator != null)
                animator.SetTrigger(ShootId);
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
            if (animator == null)
                return;

            animator.SetBool(MovingId, moveInput.sqrMagnitude > 0.01f);
            animator.SetFloat(MoveXId, moveInput.x);
            animator.SetFloat(MoveYId, moveInput.y);
            animator.SetFloat(AimXId, aimDirection.x);
            animator.SetFloat(AimYId, aimDirection.y);
        }

        private void OnHealthChanged(int current, int maximum)
        {
            if (animator == null)
                return;

            animator.SetTrigger(current <= 0 ? DeadId : HurtId);
            if (current <= 0)
                body.linearVelocity = Vector2.zero;
        }
    }
}
