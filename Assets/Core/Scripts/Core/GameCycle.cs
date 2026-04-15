using Cysharp.Threading.Tasks;
using TMQFEL.Player;
using UnityEngine;

namespace TMQFEL.Core
{
    public class GameCycle : MonoBehaviour
    {
        [SerializeField] private GameObject _preparationScreenPrefab;

        private LevelComponentsFactory _levelComponentsFactory;
        private PlayerController _playerController;
        private bool _preparationScreenShown;


        public void SetClickScreen(bool state)
        {
            _preparationScreenShown = state;
        }

        private void Awake()
        {
            SystemsService.Instance.Register(this);
        }

        private void Start()
        {
            _levelComponentsFactory = SystemsService.Instance.Get<LevelComponentsFactory>();
            _playerController = SystemsService.Instance.Get<PlayerController>();
            GameLoop().Forget();
        }


        private async UniTask GameLoop()
        {
            _preparationScreenShown = true;
            
            while (true)
            {
                await new WaitUntil(() => !_preparationScreenShown);

                _playerController.ResetRunState();
                _levelComponentsFactory.PrepareRun();
                _preparationScreenPrefab.SetActive(false);

                await new WaitUntil(() => _preparationScreenShown);

                _playerController.DestroyPlayer();
                _preparationScreenPrefab.SetActive(true);
            }
        }
    }
}
