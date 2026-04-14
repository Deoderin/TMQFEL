using System;
using UnityEngine;
using UnityEngine.Events;
using PlayerComponent = TMQFEL.Player.Player;

namespace TMQFEL.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class TreasureFinishPoint : MonoBehaviour
    {
        [SerializeField] private BoxCollider2D boxCollider;
        [SerializeField] private UnityEvent onFinishReached;

        private bool _isReached;

        public event Action<PlayerComponent> FinishReached;
        

        private void Awake()
        {

            boxCollider.isTrigger = true;
        }

        public void ResetState()
        {
            _isReached = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isReached)
            {
                return;
            }

            var player = other.GetComponentInParent<PlayerComponent>();
            if (player == null)
            {
                return;
            }

            _isReached = true;
            FinishReached?.Invoke(player);
            onFinishReached?.Invoke();
        }
    }
}
