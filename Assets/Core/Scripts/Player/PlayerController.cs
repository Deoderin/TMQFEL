using UnityEngine;
using UnityEngine.InputSystem;

namespace TMQFEL.Player
{
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private Player player;
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float jumpSpeed = 7f;
        [SerializeField] private float obstacleProbeDistance = 0.08f;
        [SerializeField] private float wallSlideSpeed = 1.5f;
        [SerializeField] private Vector2 moveDirection = Vector2.right;

        private bool _actionQueued;

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
            var moveDirectionX = GetHorizontalDirection();
            var isGrounded = player.IsGrounded();
            var hasObstacleAhead = player.HasObstacleInDirection(new Vector2(moveDirectionX, 0f), obstacleProbeDistance);
            var isWallSliding = hasObstacleAhead && !isGrounded;

            if (TryHandleAction(isGrounded, isWallSliding, moveDirectionX))
            {
                _actionQueued = false;
                return;
            }

            ApplyMovement(moveDirectionX, isGrounded, hasObstacleAhead);
            _actionQueued = false;
        }

        private bool TryHandleAction(bool isGrounded, bool isWallSliding, float moveDirectionX)
        {
            if (!_actionQueued)
            {
                return false;
            }

            if (isGrounded)
            {
                player.Jump(jumpSpeed);
                return false;
            }

            if (!isWallSliding)
            {
                return false;
            }

            PerformWallJump(moveDirectionX);
            return true;
        }

        private void PerformWallJump(float moveDirectionX)
        {
            var nextDirectionX = -moveDirectionX;
            moveDirection = new Vector2(nextDirectionX, 0f);
            player.WallJump(nextDirectionX * moveSpeed, jumpSpeed);
        }

        private void ApplyMovement(float moveDirectionX, bool isGrounded, bool hasObstacleAhead)
        {
            if (!hasObstacleAhead)
            {
                player.SetHorizontalSpeed(moveDirectionX * moveSpeed);
                return;
            }

            if (isGrounded)
            {
                player.SetHorizontalSpeed(0f);
                return;
            }

            player.ApplyWallSlide(wallSlideSpeed);
        }

        private float GetHorizontalDirection()
        {
            var direction = moveDirection.normalized;
            return Mathf.Abs(direction.x) > 0f ? direction.x : 1f;
        }

        private bool WasActionPressedThisFrame()
        {
            return (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
        }
    }
}
