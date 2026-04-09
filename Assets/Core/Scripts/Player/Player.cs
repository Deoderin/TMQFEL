using System.Text;
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
        private const float StationaryVelocityThreshold = 0.01f;

        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float sizeMultiplier = 0.5f;
        [SerializeField] private bool debugLogs = false;
        [SerializeField] private Rigidbody2D rigidbody;
        [SerializeField] private BoxCollider2D boxCollider;

        private readonly ContactPoint2D[] _bodyContacts = new ContactPoint2D[ContactBufferSize];
        private readonly ContactPoint2D[] _groundContacts = new ContactPoint2D[ContactBufferSize];
        private readonly RaycastHit2D[] _obstacleHits = new RaycastHit2D[CastBufferSize];

        private LevelSystem _levelSystem;
        private ContactFilter2D _contactFilter;
        private float _gravityScale = 4f;

        private void Awake()
        {
            ConfigurePhysics();
        }

        public void Spawn()
        {
            transform.position = GetLevelSystem().GetSpawnWorldPosition();
            ResetMotion();
            ApplyView();
            LogDebug($"Spawn. {GetDebugState()}");
        }

        public void SetHorizontalSpeed(float speed)
        {
            SetVelocityX(speed);
        }

        public void Jump(float jumpSpeed)
        {
            LogDebug($"Jump speed={jumpSpeed:F2}. {GetDebugState()}");
            SetVelocityY(jumpSpeed);
        }

        public void WallJump(float horizontalSpeed, float verticalSpeed)
        {
            LogDebug($"WallJump horizontal={horizontalSpeed:F2} vertical={verticalSpeed:F2}. {GetDebugState()}");
            rigidbody.linearVelocity = new Vector2(horizontalSpeed, verticalSpeed);
        }

        public void ApplyWallSlide(float slideSpeed)
        {
            LogDebug($"WallSlide speed={slideSpeed:F2}. {GetDebugState()}");

            var velocity = rigidbody.linearVelocity;
            velocity.x = 0f;

            if (velocity.y <= 0f)
            {
                velocity.y = -slideSpeed;
            }

            rigidbody.linearVelocity = velocity;
        }

        public void StartDash(Vector2 direction, float dashSpeed)
        {
            LogDebug($"Dash start direction=({direction.x:F2}, {direction.y:F2}) speed={dashSpeed:F2}. {GetDebugState()}");
            rigidbody.gravityScale = 0f;
            rigidbody.linearVelocity = direction.normalized * dashSpeed;
        }

        public void StopDash()
        {
            LogDebug($"Dash stop. {GetDebugState()}");
            rigidbody.gravityScale = _gravityScale;
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

        public bool IsGrounded()
        {
            if (rigidbody.linearVelocity.y > UpwardGroundedVelocityThreshold)
            {
                return false;
            }

            return HasGroundContact();
        }

        public string GetDebugState()
        {
            var position = transform.position;
            var velocity = rigidbody.linearVelocity;
            return $"position=({position.x:F3}, {position.y:F3}, {position.z:F3}) velocity=({velocity.x:F3}, {velocity.y:F3}) grounded={IsGrounded()}";
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            LogCollision("Enter", collision);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            LogCollision("Exit", collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (!debugLogs || !IsStationary())
            {
                return;
            }

            LogCollision("Stay", collision);
        }

        private void ConfigurePhysics()
        {
            rigidbody.freezeRotation = true;
            rigidbody.gravityScale = _gravityScale;

            boxCollider.size = Vector2.one;
            boxCollider.offset = Vector2.zero;

            _contactFilter = new ContactFilter2D
            {
                useTriggers = false
            };
        }

        private void ResetMotion()
        {
            rigidbody.linearVelocity = Vector2.zero;
            rigidbody.angularVelocity = 0f;
            rigidbody.gravityScale = _gravityScale;
        }

        private void ApplyView()
        {
            var playerSize = GetPlayerSize();
            visualRoot.localScale = Vector3.one * playerSize;
            boxCollider.size = Vector2.one * playerSize;
        }

        private float GetPlayerSize()
        {
            return GetLevelSystem().CellSize * sizeMultiplier;
        }

        private LevelSystem GetLevelSystem()
        {
            return _levelSystem ??= SystemsService.Instance.Get<LevelSystem>();
        }

        private void SetVelocityX(float x)
        {
            var velocity = rigidbody.linearVelocity;
            velocity.x = x;
            rigidbody.linearVelocity = velocity;
        }

        private void SetVelocityY(float y)
        {
            var velocity = rigidbody.linearVelocity;
            velocity.y = y;
            rigidbody.linearVelocity = velocity;
        }

        private bool HasBlockingContact(Vector2 normalizedDirection)
        {
            var contactCount = rigidbody.GetContacts(_bodyContacts);
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
            var contactCount = rigidbody.GetContacts(_groundContacts);
            for (var i = 0; i < contactCount; i++)
            {
                if (_groundContacts[i].normal.y > GroundNormalThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsStationary()
        {
            var velocity = rigidbody.linearVelocity;
            return Mathf.Abs(velocity.x) < StationaryVelocityThreshold
                && Mathf.Abs(velocity.y) < StationaryVelocityThreshold;
        }

        private void LogDebug(string message)
        {
            if (!debugLogs)
            {
                return;
            }

            Debug.Log($"[Player] {message}", this);
        }

        private void LogCollision(string phase, Collision2D collision)
        {
            if (!debugLogs)
            {
                return;
            }

            Debug.Log($"[Player] Collision {phase} collider={collision.collider.name} contacts={FormatContacts(collision)} {GetDebugState()}", this);
        }

        private static string FormatContacts(Collision2D collision)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < collision.contactCount; i++)
            {
                var contact = collision.GetContact(i);
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append($"[{i}] point=({contact.point.x:F3}, {contact.point.y:F3}) normal=({contact.normal.x:F3}, {contact.normal.y:F3}) separation={contact.separation:F3}");
            }

            return builder.ToString();
        }
    }
}
