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
        [SerializeField] private float wallSlideSpeed = 1.5f;
        [SerializeField] private Vector2 moveDirection = Vector2.right;

        private bool _actionQueued;
        private bool _isDashing;
        private bool _debugJumpTraceActive;
        private int _debugJumpTraceFrame;
        private float _dashTimer;
        private float _dashCooldownTimer;

        private void Start()
        {
            player.Spawn();
        }

        private void Update()
        {
            if (WasActionPressedThisFrame())
            {
                _actionQueued = true;
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
                    _actionQueued = false;
                    return;
                }

                player.StartDash(dashDirection, dashSpeed);

                if (_dashTimer <= 0f)
                {
                    _isDashing = false;
                    player.StopDash();
                }

                _actionQueued = false;
                return;
            }

            var moveDirectionX = GetMoveDirectionX();
            var isGrounded = player.IsGrounded();
            var direction = new Vector2(moveDirectionX, 0f);
            var hasObstacleAhead = player.HasObstacleInDirection(direction, obstacleProbeDistance);

            if (_actionQueued && isGrounded)
            {
                player.Jump(jumpSpeed);
                _debugJumpTraceActive = true;
                _debugJumpTraceFrame = 0;
            }

            if (hasObstacleAhead)
            {
                if (isGrounded)
                {
                    player.SetHorizontalSpeed(0f);
                }
                else
                {
                    player.ApplyWallSlide(wallSlideSpeed);
                }
            }
            else
            {
                player.SetHorizontalSpeed(moveDirectionX * moveSpeed);
            }

            if (_actionQueued && !isGrounded && _dashCooldownTimer <= 0f)
            {
                _isDashing = true;
                _dashTimer = dashDuration;
                _dashCooldownTimer = dashCooldown;
                player.StartDash(GetDashDirection(), dashSpeed);
            }

            TraceJump(direction);
            _actionQueued = false;
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

        private static bool WasActionPressedThisFrame()
        {
            return (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
        }

        private void TraceJump(Vector2 direction)
        {
            if (!_debugJumpTraceActive)
            {
                return;
            }

            _debugJumpTraceFrame++;

            var isGrounded = player.IsGrounded();

            if (isGrounded && _debugJumpTraceFrame > 1)
            {
                _debugJumpTraceActive = false;
            }
        }
    }
}
