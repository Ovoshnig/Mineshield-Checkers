using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class FigureChooser : MonoBehaviour
{
    [SerializeField] private GameObject[] _figurePrefabs;
    [SerializeField] private Slider _difficultySlider;
    [SerializeField] private float _offset = 1f;
    [SerializeField] private float _swapDuration = 0.6f;
    [SerializeField] private float _rotationDuration = 5f;

    private readonly List<GameObject> _figures = new();
    private AudioSource _audioSource;
    private PlayerInput _playerInput;
    private CancellationTokenSource _cts = new();
    private int _currentIndex;
    private bool _isMoving;

    public static GameObject ChosenFigure { get; private set; } = null;
    public static int ChosenDifficulty { get; private set; } = 3;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();

        _playerInput = new PlayerInput();
        _playerInput.FigureChoice.SwapLeft.performed += ctx => SwapFigure(-1).Forget();
        _playerInput.FigureChoice.SwapRight.performed += ctx => SwapFigure(1).Forget();
        _playerInput.FigureChoice.Choose.performed += OnFigureChosen;
    }

    private void OnEnable()
    {
        _playerInput.Enable();

        _difficultySlider.onValueChanged.AddListener(value => ChosenDifficulty = (int)value);
    }

    private void OnDisable()
    {
        _playerInput.Disable();

        _difficultySlider.onValueChanged.RemoveListener(value => ChosenDifficulty = (int)value);
    }

    private void Start()
    {
        CreateFigureInstances();
        RotateCurrentFigure();
    }

    private void OnDestroy()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts = new CancellationTokenSource();
        }
    }

    private void CreateFigureInstances()
    {
        for (int i = 0; i < _figurePrefabs.Length; i++)
        {
            Vector3 position = new(i * _offset, 0, 0);
            GameObject figure = Instantiate(_figurePrefabs[i], position, Quaternion.identity);
            figure.transform.SetParent(transform);
            _figures.Add(figure);
        }
    }

    private async UniTask SwapFigure(int direction)
    {
        if (_isMoving || !CanSwap(direction))
            return;

        if (_cts != null)
        {
            _cts.Cancel();
            _cts = new CancellationTokenSource();
            _figures[_currentIndex].transform.localRotation = Quaternion.identity;
        }

        _isMoving = true;

        _audioSource
            .SetRandomVolume()
            .SetRandomPitch()
            .Play();

        _currentIndex += direction;
        float targetPositionX = transform.position.x + direction * -_offset;
        using CancellationTokenSource cts = new();

        await transform.DOMoveX(targetPositionX, _swapDuration)
            .SetEase(Ease.InOutSine)
            .ToUniTask(cancellationToken: cts.Token);

        RotateCurrentFigure();

        _isMoving = false;
    }

    private void RotateCurrentFigure()
    {
        Transform currentFigureTransform = _figures[_currentIndex].transform;
        Vector3 targetRotation = new(0f, 360f, 0f);

        currentFigureTransform.DORotate(targetRotation, _rotationDuration, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Incremental)
            .SetEase(Ease.Linear)
            .ToUniTask(cancellationToken: _cts.Token)
            .Forget();
    }

    private void OnFigureChosen(InputAction.CallbackContext _)
    {
        ChosenFigure = _figurePrefabs[_currentIndex];
        SceneManager.LoadScene(1);
    }

    private bool CanSwap(int direction) =>
        (direction < 0 && _currentIndex > 0) || (direction > 0 && _currentIndex < _figurePrefabs.Length - 1);
}
