using TMQFEL.Core;
using TMQFEL.Levels;
using UnityEngine;

namespace TMQFEL.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class Player : MonoBehaviour
    {
        private const int ContactBufferSize = 8;
        private const int CastBufferSize = 4;
        private const float GroundNormalThreshold = 0.45f;
        private const float ObstacleNormalThreshold = 0.45f;
        private const float UpwardGroundedVelocityThreshold = 0.01f;

        private readonly ContactPoint2D[] _bodyContacts = new ContactPoint2D[ContactBufferSize];
        private readonly ContactPoint2D[] _groundContacts = new ContactPoint2D[ContactBufferSize];
        private readonly RaycastHit2D[] _obstacleHits = new RaycastHit2D[CastBufferSize];
        
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float sizeMultiplier = 0.5f;
        [SerializeField] private Rigidbody2D playerRigidbody;
        [SerializeField] private BoxCollider2D boxCollider;
        
        private LevelSystem _levelSystem;
        private ContactFilter2D _contactFilter;
        private float _gravityScale = 4f;

        private void Awake()
        {
            ConfigurePhysics();
        }

        private void InitializeSystems()
        {
            _levelSystem = SystemsService.Instance.Get<LevelSystem>();
        }

        public void Spawn()
        {
            InitializeSystems();
            transform.position = _levelSystem.GetSpawnWorldPosition();
            ResetMotion();
            ApplyView();
        }

        public void SetHorizontalSpeed(float speed) => SetVelocityX(speed);

        public void Jump(float jumpSpeed) => SetVelocityY(jumpSpeed);

        public void WallJump(float horizontalSpeed, float verticalSpeed) =>
            playerRigidbody.linearVelocity = new Vector2(horizontalSpeed, verticalSpeed);

        public void ApplyWallSlide(float slideSpeed)
        {
            var velocity = playerRigidbody.linearVelocity;
            velocity.x = 0f;

            if (velocity.y <= 0f)
            {
                velocity.y = -slideSpeed;
            }

            playerRigidbody.linearVelocity = velocity;
        }


        public bool HasObstacleInDirection(Vector2 direction, float distance)
        {
            if (direction == Vector2.zero)
            {
                return false;
            }

            var normalizedDirection = direction.normalized;
            return HasBlockingContact(normalizedDirection) || HasBlockingCastHit(normalizedDirection, distance);
        }

        public bool IsGrounded() =>
            !(playerRigidbody.linearVelocity.y > UpwardGroundedVelocityThreshold) && HasGroundContact();

        private void ConfigurePhysics()
        {
            playerRigidbody.freezeRotation = true;
            playerRigidbody.gravityScale = _gravityScale;

            boxCollider.size = Vector2.one;
            boxCollider.offset = Vector2.zero;

            _contactFilter = new ContactFilter2D
            {
                useTriggers = false
            };
        }

        private void ResetMotion()
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
            playerRigidbody.gravityScale = _gravityScale;
        }

        private void ApplyView()
        {
            var playerSize = _levelSystem.CellSize * sizeMultiplier;
            visualRoot.localScale = Vector3.one * playerSize;
            boxCollider.size = Vector2.one * playerSize;
        }


        private void SetVelocityX(float x)
        {
            var velocity = playerRigidbody.linearVelocity;
            velocity.x = x;
            playerRigidbody.linearVelocity = velocity;
        }

        private void SetVelocityY(float y)
        {
            var velocity = playerRigidbody.linearVelocity;
            velocity.y = y;
            playerRigidbody.linearVelocity = velocity;
        }

        private bool HasBlockingContact(Vector2 normalizedDirection)
        {
            var contactCount = playerRigidbody.GetContacts(_bodyContacts);
            for (var i = 0; i < contactCount; i++)
            {
                if (Vector2.Dot(_bodyContacts[i].normal, -normalizedDirection) > ObstacleNormalThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasBlockingCastHit(Vector2 normalizedDirection, float distance)
        {
            var hitCount = boxCollider.Cast(normalizedDirection, _contactFilter, _obstacleHits, distance);
            for (var i = 0; i < hitCount; i++)
            {
                if (Vector2.Dot(_obstacleHits[i].normal, -normalizedDirection) > ObstacleNormalThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasGroundContact()
        {
            var contactCount = playerRigidbody.GetContacts(_groundContacts);
            for (var i = 0; i < contactCount; i++)
            {
                if (_groundContacts[i].normal.y > GroundNormalThreshold)
                {
                    return true;
                }
            }

            return false;
        }
    }
}