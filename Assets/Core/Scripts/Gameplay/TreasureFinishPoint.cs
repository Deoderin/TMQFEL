using System;
using TMQFEL.Core;
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
        
        private readonly UnityEvent _onFinishReached =  new UnityEvent();
        private GameCycle _gameCycle;
        private bool _isReached;
        

        private void Start()
        {
            _gameCycle = SystemsService.Instance.Get<GameCycle>();
            
            _onFinishReached.AddListener(() => _gameCycle.SetClickScreen(true));
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
            _onFinishReached?.Invoke();
        }
    }
}
