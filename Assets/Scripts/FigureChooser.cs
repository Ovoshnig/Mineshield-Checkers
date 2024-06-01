using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class FigureChooser : MonoBehaviour
{
    [SerializeField] private AnimationCurve _movementCurve; 
    [SerializeField] private GameObject[] _figurePrefabs;
    [SerializeField] private AudioClip _swingClip;
    [SerializeField] private float _offset;
    [SerializeField] private float _duration;

    private AudioSource _audioSource;
    private PlayerInput _playerInput;
    private int _currentIndex = 0;
    private bool _isMoving = false;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.clip = _swingClip;

        _playerInput = new PlayerInput();
        _playerInput.FigureChoice.SwapLeft.performed += SwapLeft;
        _playerInput.FigureChoice.SwapRight.performed += SwapRight;
        _playerInput.FigureChoice.Choose.performed += Choose;
    }

    private void OnEnable() => _playerInput.Enable();

    private void OnDisable() => _playerInput.Disable();

    private void Start()
    {
        for (int i = 0; i < _figurePrefabs.Length; i++)
        {
            Vector3 position = new(i * _offset, 0, 0);
            GameObject figure = Instantiate(_figurePrefabs[i], position, Quaternion.Euler(0, 0, 0));
            figure.transform.parent = transform;
        }
    }

    private void SwapLeft(InputAction.CallbackContext context)
    {
        if (_currentIndex > 0 && !_isMoving)
        {
            _currentIndex--;
            Move(sign: 1).Forget();
            _isMoving = true;

            _audioSource.Play();
        }
    }

    private void SwapRight(InputAction.CallbackContext context)
    {
        if (_currentIndex < _figurePrefabs.Length - 1 && !_isMoving)
        {
            _currentIndex++;
            Move(sign: -1).Forget();
            _isMoving = true;

            _audioSource.Play();
        }
    }

    private async UniTask Move(int sign)
    {
        float expiredTime = 0;
        float startX = transform.position.x;

        while (expiredTime < _duration)
        {
            float progress = expiredTime / _duration;
            float currentX = startX + sign * _offset * _movementCurve.Evaluate(progress);
            transform.position = new Vector3(currentX, 0, 0);
            expiredTime += Time.deltaTime;

            await UniTask.Yield();
        }
        transform.position = new Vector3(-_offset * _currentIndex, 0, 0);

        _isMoving = false;
    }

    private void Choose(InputAction.CallbackContext context)
    {
        GameObject chosenFigure = _figurePrefabs[_currentIndex];
        CheckersVisualizer._playerFigures[0] = chosenFigure;

        SceneManager.LoadScene(1);
    }
}
