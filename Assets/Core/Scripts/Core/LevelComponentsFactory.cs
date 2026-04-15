using TMQFEL.Gameplay;
using TMQFEL.Levels;
using TMQFEL.Player;
using UnityEngine;
using PlayerComponent = TMQFEL.Player.Player;

namespace TMQFEL.Core
{
    [DisallowMultipleComponent]
    public sealed class LevelComponentsFactory : MonoBehaviour
    {
        [SerializeField] private PlayerComponent playerPrefab;
        [SerializeField] private TreasureFinishPoint treasurePrefab;
        [SerializeField] private Transform componentsRoot;

        private LevelSystem _levelSystem;
        private PlayerComponent _playerInstance;
        private TreasureFinishPoint _treasureInstance;

        public PlayerComponent PlayerInstance => _playerInstance;

        public TreasureFinishPoint TreasureInstance => _treasureInstance;

        private LevelSystem LevelSystem => _levelSystem ??= SystemsService.Instance.Get<LevelSystem>();

        private Transform SpawnParent => componentsRoot != null ? componentsRoot : transform;

        private void Awake()
        {
            SystemsService.Instance.Register(this);
        }

        public void DestroyPlayer()
        {
            if (_playerInstance == null)
            {
                return;
            }

            Destroy(_playerInstance.gameObject);
            _playerInstance = null;
        }

        public void PrepareRun()
        {
            EnsurePlayerExists();
            EnsureTreasureExists();
            ResetRunState();
        }

        private void EnsurePlayerExists()
        {
            if (_playerInstance == null)
            {
                _playerInstance = SpawnPlayer();
            }
        }

        private void EnsureTreasureExists()
        {
            if (_treasureInstance == null)
            {
                _treasureInstance = SpawnTreasure();
            }
        }

        private void ResetRunState()
        {
            _playerInstance?.Spawn();
            _treasureInstance?.ResetState();
        }

        private PlayerComponent SpawnPlayer()
        {
            if (playerPrefab == null)
            {
                Debug.LogWarning("LevelComponentsFactory has no Player prefab assigned.", this);
                return null;
            }

            var playerComponent = Instantiate(playerPrefab, SpawnParent);
            playerComponent.name = playerPrefab.name;
            DestroyOtherPlayers(playerComponent);

            return playerComponent;
        }

        private TreasureFinishPoint SpawnTreasure()
        {
            if (treasurePrefab == null)
            {
                Debug.LogWarning("LevelComponentsFactory has no Treasure prefab assigned.", this);
                return null;
            }

            if (!LevelSystem.TryGetTreasureWorldPosition(out var treasureWorldPosition))
            {
                Debug.LogWarning("Level does not contain Treasure cell.", this);
                return null;
            }

            var treasureComponent = Instantiate(treasurePrefab, treasureWorldPosition, Quaternion.identity, SpawnParent);
            treasureComponent.name = treasurePrefab.name;
            return treasureComponent;
        }

        private static void DestroyOtherPlayers(PlayerComponent spawnedPlayer)
        {
            var existingPlayers = FindObjectsByType<PlayerComponent>(FindObjectsSortMode.None);
            foreach (var existingPlayer in existingPlayers)
            {
                if (existingPlayer == null || existingPlayer == spawnedPlayer)
                {
                    continue;
                }

                Destroy(existingPlayer.gameObject);
            }
        }
    }
}
