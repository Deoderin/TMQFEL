using TMQFEL.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TMQFEL.Player
{
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float jumpSpeed = 7f;
        [SerializeField] private float obstacleProbeDistance = 0.08f;
        [SerializeField] private float wallSlideSpeed = 1.5f;
        [SerializeField] private Vector2 moveDirection = Vector2.right;

        private bool _actionQueued;
        private LevelComponentsFactory _levelComponentsFactory;
        private Vector2 _runtimeMoveDirection;

        public Player CurrentPlayer => _levelComponentsFactory != null ? _levelComponentsFactory.PlayerInstance : null;

        private void Awake()
        {
            SystemsService.Instance.Register(this);
            ResetRunState();
        }

        private void Start()
        {
            _levelComponentsFactory = SystemsService.Instance.Get<LevelComponentsFactory>();
        }

        public void ResetRunState()
        {
            _actionQueued = false;
            _runtimeMoveDirection = NormalizeDirection(moveDirection);
        }

        public void DestroyPlayer()
        {
            _actionQueued = false;

            if (_levelComponentsFactory != null)
            {
                _levelComponentsFactory.DestroyPlayer();
            }
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
            var currentPlayer = CurrentPlayer;
            if (currentPlayer == null)
            {
                return;
            }

            var moveDirectionX = GetHorizontalDirection();
            var isGrounded = currentPlayer.IsGrounded();
            var hasObstacleAhead = currentPlayer.HasObstacleInDirection(new Vector2(moveDirectionX, 0f), obstacleProbeDistance);
            var isWallSliding = hasObstacleAhead && !isGrounded;

            if (TryHandleAction(currentPlayer, isGrounded, isWallSliding, moveDirectionX))
            {
                _actionQueued = false;
                return;
            }

            ApplyMovement(currentPlayer, moveDirectionX, isGrounded, hasObstacleAhead);
            _actionQueued = false;
        }

        private bool TryHandleAction(Player currentPlayer, bool isGrounded, bool isWallSliding, float moveDirectionX)
        {
            if (!_actionQueued)
            {
                return false;
            }

            if (isGrounded)
            {
                currentPlayer.Jump(jumpSpeed);
                return false;
            }

            if (!isWallSliding)
            {
                return false;
            }

            PerformWallJump(currentPlayer, moveDirectionX);
            return true;
        }

        private void PerformWallJump(Player currentPlayer, float moveDirectionX)
        {
            var nextDirectionX = -moveDirectionX;
            _runtimeMoveDirection = new Vector2(nextDirectionX, 0f);
            currentPlayer.WallJump(nextDirectionX * moveSpeed, jumpSpeed);
        }

        private void ApplyMovement(Player currentPlayer, float moveDirectionX, bool isGrounded, bool hasObstacleAhead)
        {
            if (!hasObstacleAhead)
            {
                currentPlayer.SetHorizontalSpeed(moveDirectionX * moveSpeed);
                return;
            }

            if (isGrounded)
            {
                currentPlayer.SetHorizontalSpeed(0f);
                return;
            }

            currentPlayer.ApplyWallSlide(wallSlideSpeed);
        }

        private float GetHorizontalDirection()
        {
            var direction = _runtimeMoveDirection.normalized;
            return Mathf.Abs(direction.x) > 0f ? direction.x : 1f;
        }

        private static Vector2 NormalizeDirection(Vector2 direction)
        {
            return direction == Vector2.zero ? Vector2.right : direction.normalized;
        }

        private bool WasActionPressedThisFrame()
        {
            return (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
        }
    }
}
