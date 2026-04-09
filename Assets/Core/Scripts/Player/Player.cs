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
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float sizeMultiplier = 0.5f;
        [SerializeField] private bool debugLogs = false;
        [SerializeField] private Rigidbody2D rigidbody;
        [SerializeField] private BoxCollider2D boxCollider;
        
        private readonly ContactPoint2D[] _groundContacts = new ContactPoint2D[8];
        private readonly RaycastHit2D[] _obstacleHits = new RaycastHit2D[4];

        private LevelSystem _levelSystem;
        private ContactFilter2D _contactFilter;
        private float _gravityScale = 4f;

        private void Awake()
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

        public void Spawn()
        {
            transform.position = GetLevelSystem().GetSpawnWorldPosition();
            rigidbody.linearVelocity = Vector2.zero;
            rigidbody.angularVelocity = 0f;
            rigidbody.gravityScale = _gravityScale;
            ApplyView();
        }

        public void SetHorizontalSpeed(float speed)
        {
            var velocity = rigidbody.linearVelocity;
            velocity.x = speed;
            rigidbody.linearVelocity = velocity;
        }

        public void Jump(float jumpSpeed)
        {
            var velocity = rigidbody.linearVelocity;
            velocity.y = jumpSpeed;
            rigidbody.linearVelocity = velocity;
        }

        public void ApplyWallSlide(float slideSpeed)
        {
            var velocity = rigidbody.linearVelocity;
            velocity.x = 0f;
            if (velocity.y < -slideSpeed)
            {
                velocity.y = -slideSpeed;
            }

            rigidbody.linearVelocity = velocity;
        }

        public void StartDash(Vector2 direction, float dashSpeed)
        {
            rigidbody.gravityScale = 0f;
            rigidbody.linearVelocity = direction.normalized * dashSpeed;
        }

        public void StopDash()
        {
            rigidbody.gravityScale = _gravityScale;
        }

        public bool HasObstacleInDirection(Vector2 direction, float distance)
        {
            if (direction == Vector2.zero)
            {
                return false;
            }

            var normalizedDirection = direction.normalized;
            var hitCount = boxCollider.Cast(normalizedDirection, _contactFilter, _obstacleHits, distance);
            for (var i = 0; i < hitCount; i++)
            {
                if (Vector2.Dot(_obstacleHits[i].normal, -normalizedDirection) > 0.45f)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsGrounded()
        {
            if (rigidbody.linearVelocity.y > 0.01f)
            {
                return false;
            }

            var contactCount = rigidbody.GetContacts(_groundContacts);
            for (var i = 0; i < contactCount; i++)
            {
                if (_groundContacts[i].normal.y > 0.45f)
                {
                    return true;
                }
            }

            return false;
        }


        private void ApplyView()
        {
            var playerSize = GetLevelSystem().CellSize * sizeMultiplier;
            visualRoot.localScale = Vector3.one * playerSize;
            boxCollider.size = Vector2.one * playerSize;
        }

        private LevelSystem GetLevelSystem()
        {
            return _levelSystem ??= SystemsService.Instance.Get<LevelSystem>();
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
