using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CheckersVisualizer : MonoBehaviour
{
    public static readonly GameObject[] _playerFigures = new GameObject[2];

    [SerializeField] private float _initialFigureSize;
    [SerializeField] private float _normalFigureSize;
    [SerializeField] private float _appearanceDuration;
    [SerializeField] private float _crownAppearanceDuration;
    [SerializeField] private float _jumpDuration;
    [SerializeField] private float _jumpPower;
    [SerializeField] private List<GameObject> _figurePrefabs;
    [SerializeField] private GameObject _crownPrefab;
    [SerializeField] private GameObject _selectionCube;
    [SerializeField] private CheckersLogic _logic;

    private readonly Transform[,] _figureTransforms = new Transform[8, 8];
    private Transform _figureTransform;
    private Vector3 _endPosition;

    private void Awake()
    {
        int index;

        if (_playerFigures[0] == null)
        {
            index = Random.Range(0, _figurePrefabs.Count);
            _playerFigures[0] = _figurePrefabs[index];
        }

        _figurePrefabs.Remove(_playerFigures[0]);

        index = Random.Range(0, _figurePrefabs.Count);
        _playerFigures[1] = _figurePrefabs[index];
    }

    private void OnEnable()
    {
        _logic.FigurePlaced += PlaceFigure;
        _logic.FigureSelected += ChangeSelection;
        _logic.FigureMoved += Move;
        _logic.FigureChopped += Chop;
        _logic.DamCreated += CreateDam;
        _logic.GameEnding += PlayEndingAnimation;
    }

    private void Start() => _selectionCube.SetActive(false);

    private void OnDisable()
    {
        _logic.FigurePlaced -= PlaceFigure;
        _logic.FigureSelected -= ChangeSelection;
        _logic.FigureMoved -= Move;
        _logic.FigureChopped -= Chop;
        _logic.DamCreated -= CreateDam;
        _logic.GameEnding -= PlayEndingAnimation;
    }

    private async UniTask PlaceFigure(int i, int j, int index)
    {
        Vector3 position = CoordinateTranslator.Indexes2Position(i, j);

        GameObject figure = Instantiate(_playerFigures[index], position, Quaternion.identity);
        figure.name = _playerFigures[index].name;
        Transform figureTransform = figure.transform;
        _figureTransforms[i, j] = figureTransform;

        Vector3 scale = figureTransform.localScale;
        figureTransform.localScale = _initialFigureSize * scale;

        await figure.transform.DOScale(_normalFigureSize * scale, _appearanceDuration)
            .AsyncWaitForCompletion();
    }

    private void ChangeSelection(List<int> indexes, bool shoodSelect)
    {
        if (shoodSelect)
        {
            (int i, int j) = (indexes[0], indexes[1]);

            Vector3 position = CoordinateTranslator.Indexes2Position(i, j);
            _selectionCube.transform.localPosition = position;
        }

        _selectionCube.SetActive(shoodSelect);
    }

    private async UniTask Move(List<int> moveIndex) 
    {
        (int i, int j, int iDelta, int jDelta) = (moveIndex[0], moveIndex[1], moveIndex[2], moveIndex[3]);

        _figureTransform = _figureTransforms[i, j];

        Vector3 startPosition = _figureTransform.position;
        _endPosition = CoordinateTranslator.Indexes2Position(i + iDelta, j + jDelta);
        float distance = Vector3.Distance(startPosition, _endPosition);
        float moveDuration = distance / _logic.MoveSpeed;

        await _figureTransform.DOMove(_endPosition, moveDuration)
            .SetEase(Ease.Linear)
            .AsyncWaitForCompletion();

        _figureTransforms[i + iDelta, j + jDelta] = _figureTransform;
        _figureTransforms[i, j] = null;
    }

    private async UniTask Chop(List<int> moveIndex, float chopDelay)
    {
        (int rivalI, int rivalJ) = (moveIndex[4], moveIndex[5]);

        await UniTask.WaitForSeconds(chopDelay);

        Destroy(_figureTransforms[rivalI, rivalJ].gameObject);
        _figureTransforms[rivalI, rivalJ] = null;
    }

    private async UniTask CreateDam()
    {
        Transform childFigureTransform = _figureTransform.GetChild(0);
        Vector3 figurePosition = _figureTransform.position;
        Renderer renderer = childFigureTransform.GetComponent<Renderer>();
        Bounds bounds = renderer.bounds;
        float positionY = bounds.center.y * 0.6f + bounds.extents.y;
        Vector3 crownPosition = figurePosition + new Vector3(0, positionY + 15f, 0);

        GameObject crown = Instantiate(_crownPrefab, crownPosition, Quaternion.Euler(-90f, 0f, 0f));
        Transform crownTransform = crown.transform;
        crownTransform.SetParent(childFigureTransform);

        await crownTransform.DOMoveY(positionY, _crownAppearanceDuration)
            .SetEase(Ease.InQuad)
            .AsyncWaitForCompletion();
    }

    private async UniTask PlayEndingAnimation(int winnerTurn, float gameEndingDuration, CancellationToken token)
    {
        string winnerName = _playerFigures[winnerTurn - 1].name;
        List<UniTask> tasks = new();

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                Transform figureTransform = _figureTransforms[i, j];

                if (figureTransform != null && figureTransform.name == winnerName)
                {
                    float startJumpDelay = Random.Range(0, gameEndingDuration);
                    tasks.Add(JumpFigure(figureTransform, startJumpDelay, token));
                }
            }
        }

        await UniTask.WhenAll(tasks);
    }

    private async UniTask JumpFigure(Transform figureTransform, float startJumpDelay, CancellationToken token)
    {
        await UniTask.WaitForSeconds(startJumpDelay, cancellationToken: token);

        Tween expansion = figureTransform.DOBlendableScaleBy(new Vector3(0.4f, -0.3f, 0.4f), _jumpDuration * 0.5f)
            .SetEase(Ease.InOutSine);
        Tween jump = figureTransform.DOJump(figureTransform.position, _jumpPower, 1, _jumpDuration * 0.5f)
            .SetEase(Ease.OutQuad);
        Tween compression = figureTransform.DOBlendableScaleBy(new Vector3(-0.4f, 0.3f, -0.4f), _jumpDuration * 0.2f)
            .SetEase(Ease.InOutSine);

        Sequence jumpSequence = DOTween.Sequence();

        await jumpSequence
            .Append(expansion)
            .Append(jump)
            .Join(compression)
            .AsyncWaitForCompletion();
    }
}
