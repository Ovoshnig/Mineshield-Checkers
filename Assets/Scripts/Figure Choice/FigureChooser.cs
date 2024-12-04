using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using System.Threading;

public class FigureChooser : MonoBehaviour
{
    [SerializeField] private GameObject[] _figurePrefabs;
    [SerializeField] private float _offset = 1f;
    [SerializeField] private float _swapDuration = 0.6f;
    [SerializeField] private float _rotationDuration = 5f;
    [SerializeField] private DifficultySlider _difficultySlider;
    [SerializeField] private SceneLoader _sceneLoader;

    private FigureSelectionModel _model;
    private FigureView _view;
    private FigureRotator _rotator;
    private FigureInputHandler _inputHandler;
    private AudioSource _audioSource;
    private CancellationTokenSource _cts;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _cts = new CancellationTokenSource();

        _model = new FigureSelectionModel(_figurePrefabs);
        _view = new FigureView(transform, _figurePrefabs, _offset);
        _rotator = new FigureRotator(_rotationDuration, _cts.Token);
        _inputHandler = new FigureInputHandler(OnSwapLeft, OnSwapRight, OnFigureChosen);

        _difficultySlider.ValueChanged += diff => FigureSelectionModel.ChosenDifficulty = (int)diff;
    }

    private void OnEnable() => _inputHandler.Enable();

    private void OnDisable() => _inputHandler.Disable();

    private void Start()
    {
        _view.CreateFigureInstances();
        _rotator.Rotate(_view.GetCurrentFigure(_model.CurrentIndex));
    }

    private void OnDestroy()
    {
        _cts.Cancel();
        _cts.Dispose();
        _difficultySlider.ValueChanged -= diff => FigureSelectionModel.ChosenDifficulty = (int)diff;
    }

    private async UniTask OnSwapLeft() => await SwapFigure(-1);

    private async UniTask OnSwapRight() => await SwapFigure(1);

    private async UniTask SwapFigure(int direction)
    {
        if (!_model.CanSwap(direction))
            return;

        _cts.Cancel();
        _cts = new CancellationTokenSource();
        _rotator.UpdateCancellationToken(_cts.Token);

        _audioSource.SetRandomVolume().SetRandomPitch().Play();

        _model.CurrentIndex += direction;
        await _view.AnimateSwap(transform, direction, _swapDuration);

        _rotator.Rotate(_view.GetCurrentFigure(_model.CurrentIndex));
    }

    private void OnFigureChosen(InputAction.CallbackContext _)
    {
        FigureSelectionModel.ChosenFigure = _model.GetCurrentPrefab();
        _sceneLoader.LoadGameScene();
    }
}
