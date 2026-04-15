using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TMQFEL.Core
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class InitialPoint : MonoBehaviour
    {
        [SerializeField] private string initialSceneName = "GameScene";

        private void Start()
        {
            SceneManager.LoadScene(initialSceneName, LoadSceneMode.Single);
        }
    }
}
