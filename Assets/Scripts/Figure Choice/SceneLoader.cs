using UnityEngine.SceneManagement;
using UnityEngine;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private int _gameSceneIndex = 1;

    public void LoadGameScene() => SceneManager.LoadScene(_gameSceneIndex);
}
