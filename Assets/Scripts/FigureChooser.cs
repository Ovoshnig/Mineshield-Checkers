using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class FigureChooser : MonoBehaviour
{
    [SerializeField] private GameObject[] _figurePrefabs;
    [SerializeField] private AudioClip _swingClip;
    [SerializeField] private float _offset = 1f;
    [SerializeField] private float _duration = 0.5f;

    private AudioSource _audioSource;
    private PlayerInput _playerInput;
    private int _currentIndex;
    private bool _isMoving;

    public static GameObject ChosenFigure { get; private set; } = null;

    private void Awake()
    {
        InitializeAudioSource();
        InitializeInput();
    }

    private void OnEnable() => _playerInput.Enable();

    private void OnDisable() => _playerInput.Disable();

    private void Start() => CreateFigureInstances();

    private void InitializeAudioSource()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.clip = _swingClip;
    }

    private void InitializeInput()
    {
        _playerInput = new PlayerInput();
        _playerInput.FigureChoice.SwapLeft.performed += ctx => SwapFigure(-1).Forget();
        _playerInput.FigureChoice.SwapRight.performed += ctx => SwapFigure(1).Forget();
        _playerInput.FigureChoice.Choose.performed += ChooseFigure;
    }

    private void CreateFigureInstances()
    {
        for (int i = 0; i < _figurePrefabs.Length; i++)
        {
            Vector3 position = new(i * _offset, 0, 0);
            GameObject figure = Instantiate(_figurePrefabs[i], position, Quaternion.identity);
            figure.transform.SetParent(transform);
        }
    }

    private async UniTask SwapFigure(int direction)
    {
        if (_isMoving || !CanSwap(direction)) 
            return;

        _isMoving = true;
        _audioSource.Play();

        _currentIndex += direction;
        float targetPositionX = transform.position.x + direction * -_offset;
        await transform.DOMoveX(targetPositionX, _duration)
            .SetEase(Ease.InOutSine)
            .AsyncWaitForCompletion();

        _isMoving = false;
    }

    private bool CanSwap(int direction) =>
        (direction < 0 && _currentIndex > 0) || (direction > 0 && _currentIndex < _figurePrefabs.Length - 1);

    private void ChooseFigure(InputAction.CallbackContext context)
    {
        GameObject chosenFigure = _figurePrefabs[_currentIndex];
        OnFigureChosen(chosenFigure);
    }

    private void OnFigureChosen(GameObject figure)
    {
        ChosenFigure = figure;
        SceneManager.LoadScene(1);
    }
}
