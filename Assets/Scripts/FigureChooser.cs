using System.Collections;
using UnityEngine;
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

    private int _currentIndex = 0;

    private bool _isMoving = false;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.clip = _swingClip;
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            SwapLeft();
        if (Input.GetKeyDown(KeyCode.RightArrow))
            SwapRight();
    }

    public void SwapLeft()
    {
        if (_currentIndex > 0 && !_isMoving)
        {
            _currentIndex--;

            StartCoroutine(Move(1));
            _isMoving = true;

            _audioSource.Play();
        }
    }

    public void SwapRight()
    {
        if (_currentIndex < _figurePrefabs.Length - 1 && !_isMoving)
        {
            _currentIndex++;

            StartCoroutine(Move(-1));
            _isMoving = true;

            _audioSource.Play();
        }
    }

    private IEnumerator Move(int sign)
    {
        float expiredTime = 0;
        float startX = transform.position.x;

        while (expiredTime < _duration)
        {
            float progress = expiredTime / _duration;
            float currentX = startX + sign * _offset * _movementCurve.Evaluate(progress);
            transform.position = new Vector3(currentX, 0, 0);
            expiredTime += Time.deltaTime;

            yield return null;
        }
        transform.position = new Vector3(-_offset * _currentIndex, 0, 0);

        _isMoving = false;
    }

    public void MakeAChoice()
    {
        GameObject chosenFigure = _figurePrefabs[_currentIndex];
        CheckersVisualizer._playerFigures[0] = chosenFigure;

        SceneManager.LoadScene("Checkers");
    }
}
