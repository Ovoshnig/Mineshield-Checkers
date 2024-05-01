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
    private Vector2 _direction;
    private int _currentIndex = 0;
    private bool _isMoving = false;

    private void OnValidate()
    {
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.clip = _swingClip;
        }
    }

    private void Start()
    {
        for (int i = 0; i < _figurePrefabs.Length; i++)
        {
            Vector3 position = new(i * _offset, 0, 0);
            GameObject figure = Instantiate(_figurePrefabs[i], position, Quaternion.Euler(0, 0, 0));
            figure.transform.parent = transform;
        }
    }

    public void OnSwapClick(InputAction.CallbackContext context)
    {
        _direction = context.action.ReadValue<Vector2>();

        if (_direction.x > 0)
            SwapRight();
        else if (_direction.x < 0)
            SwapLeft();
    }

    public void SwapLeft()
    {
        if (_currentIndex > 0 && !_isMoving)
        {
            _currentIndex--;

            Move(1).Forget();
            _isMoving = true;

            _audioSource.Play();
        }
    }

    public void SwapRight()
    {
        if (_currentIndex < _figurePrefabs.Length - 1 && !_isMoving)
        {
            _currentIndex++;

            Move(-1).Forget();
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

    public void Choose()
    {
        GameObject chosenFigure = _figurePrefabs[_currentIndex];
        CheckersVisualizer._playerFigures[0] = chosenFigure;

        SceneManager.LoadScene(1);
    }
}
