using UnityEngine;
using UnityEngine.SceneManagement;

namespace Battleship
{
    public class SceneLoader : MonoBehaviour
    {
        int _currentSceneIndex;

        void Start()
        {
            _currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        }

        public void LoadNextScene()
        {
            SceneManager.LoadScene(_currentSceneIndex + 1);
        }

        public void LoadScene(int buildIndex)
        {
            SceneManager.LoadScene(buildIndex);
        }

        public void RestartScene()
        {
            SceneManager.LoadScene(_currentSceneIndex);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

    }
}
