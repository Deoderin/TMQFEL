using UnityEngine;
using UnityEngine.InputSystem;

namespace TMQFEL.Player
{
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private Player player;
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float jumpSpeed = 7f;
        [SerializeField] private float dashSpeed = 8f;
        [SerializeField] private float dashDuration = 0.18f;
        [SerializeField] private float dashCooldown = 0.35f;
        [SerializeField] private float obstacleProbeDistance = 0.08f;
        [SerializeField] private Vector2 moveDirection = Vector2.right;

        private bool _jumpQueued;
        private bool _dashQueued;
        private bool _isDashing;
        private float _dashTimer;
        private float _dashCooldownTimer;

        private void Start()
        {
            player.Spawn();
        }

        private void Update()
        {
            if (WasJumpPressedThisFrame())
            {
                _jumpQueued = true;
            }

            if (WasDashPressedThisFrame())
            {
                _dashQueued = true;
            }
        }

        private void FixedUpdate()
        {
            var deltaTime = Time.fixedDeltaTime;
            _dashCooldownTimer = Mathf.Max(0f, _dashCooldownTimer - deltaTime);

            if (_isDashing)
            {
                _dashTimer -= deltaTime;
                var dashDirection = GetDashDirection();
                if (player.HasObstacleInDirection(dashDirection, obstacleProbeDistance))
                {
                    _isDashing = false;
                    player.StopDash();
                    _jumpQueued = false;
                    _dashQueued = false;
                    return;
                }

                player.StartDash(dashDirection, dashSpeed);

                if (_dashTimer <= 0f)
                {
                    _isDashing = false;
                    player.StopDash();
                }

                _jumpQueued = false;
                _dashQueued = false;
                return;
            }

            var moveDirectionX = GetMoveDirectionX();
            if (player.HasObstacleInDirection(new Vector2(moveDirectionX, 0f), obstacleProbeDistance))
            {
                player.SetHorizontalSpeed(0f);
            }
            else
            {
                player.SetHorizontalSpeed(moveDirectionX * moveSpeed);
            }

            if (_jumpQueued && player.IsGrounded())
            {
                player.Jump(jumpSpeed);
            }

            if (_dashQueued && _dashCooldownTimer <= 0f)
            {
                _isDashing = true;
                _dashTimer = dashDuration;
                _dashCooldownTimer = dashCooldown;
                player.StartDash(GetDashDirection(), dashSpeed);
            }

            _jumpQueued = false;
            _dashQueued = false;
        }

        private float GetMoveDirectionX()
        {
            var direction = moveDirection.normalized;
            return Mathf.Abs(direction.x) > 0f ? direction.x : 1f;
        }

        private Vector2 GetDashDirection()
        {
            var direction = moveDirection.normalized;
            return direction == Vector2.zero ? Vector2.right : direction;
        }

        private static bool WasJumpPressedThisFrame()
        {
            return (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
        }

        private static bool WasDashPressedThisFrame()
        {
            return (Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame)
                || (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame);
        }
    }
}
