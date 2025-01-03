using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

public class CheckersVisualizer : MonoBehaviour
{
    [SerializeField] private float _initialFigureSize;
    [SerializeField] private float _appearanceDuration;
    [SerializeField] private float _disappearanceDuration;
    [SerializeField] private float _crownAppearanceDuration;
    [SerializeField] private float _jumpDuration;
    [SerializeField] private float _jumpPower;
    [SerializeField] private Vector3 _expansedFigureScale;
    [SerializeField] private List<GameObject> _figurePrefabs;
    [SerializeField] private GameObject _crownPrefab;
    [SerializeField] private GameObject _selectionCube;
    [SerializeField] private CheckersLogic _logic;

    private readonly GameObject[] _playerFigures = new GameObject[2];
    private readonly Transform[,] _figureTransforms = new Transform[8, 8];
    private Transform _figureTransform;
    private Vector3 _endPosition;

    private void Awake()
    {
        int index;

        if (FigureSelectionModel.ChosenFigure == null)
        {
            index = Random.Range(0, _figurePrefabs.Count);
            _playerFigures[0] = _figurePrefabs[index];
        }
        else
        {
            _playerFigures[0] = FigureSelectionModel.ChosenFigure;
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

    private async UniTask PlaceFigure(int i, int j, int index, CancellationToken token)
    {
        Vector3 position = CoordinateTranslator.Indexes2Position(i, j);

        GameObject figure = Instantiate(_playerFigures[index], position, Quaternion.identity);
        figure.name = _playerFigures[index].name;
        Transform figureTransform = figure.transform;
        _figureTransforms[i, j] = figureTransform;

        figureTransform.localScale = _initialFigureSize * Vector3.one;
        await figure.transform.DOScale(Vector3.one, _appearanceDuration)
            .ToUniTask(cancellationToken: token);
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

    private async UniTask Move(List<int> move, CancellationToken token) 
    {
        (int i, int j, int iDelta, int jDelta) = (move[0], move[1], move[2], move[3]);

        _figureTransform = _figureTransforms[i, j];

        Vector3 startPosition = _figureTransform.position;
        _endPosition = CoordinateTranslator.Indexes2Position(i + iDelta, j + jDelta);
        float distance = Vector3.Distance(startPosition, _endPosition);
        float moveDuration = distance / _logic.MoveSpeed;

        await _figureTransform.DOMove(_endPosition, moveDuration)
            .SetEase(Ease.Linear)
            .ToUniTask(cancellationToken: token);

        _figureTransforms[i + iDelta, j + jDelta] = _figureTransform;
        _figureTransforms[i, j] = null;
    }

    private async UniTask Chop(List<int> move, CancellationToken token)
    {
        var (i, j, rivalI, rivalJ) = (move[0], move[1], move[4], move[5]);

        Vector3 startPosition = CoordinateTranslator.Indexes2Position(i, j);
        Vector3 rivalPosition = CoordinateTranslator.Indexes2Position(rivalI, rivalJ);
        float distance = Vector3.Distance(startPosition, rivalPosition);
        float chopDelay = (distance / _logic.MoveSpeed);
        Transform choppedFigureTransform = _figureTransforms[rivalI, rivalJ];

        await UniTask.WaitForSeconds(chopDelay, cancellationToken: token);
        await choppedFigureTransform.DOScale(_initialFigureSize * Vector3.one, _disappearanceDuration)
            .ToUniTask(cancellationToken: token);

        Destroy(choppedFigureTransform.gameObject);
        _figureTransforms[rivalI, rivalJ] = null;
    }

    private async UniTask CreateDam(int i, int j, CancellationToken token)
    {
        Transform figureTransform = _figureTransforms[i, j];
        Vector3 figurePosition = figureTransform.position;

        Renderer renderer = figureTransform.GetComponent<Renderer>();
        Bounds bounds = renderer.bounds;
        float positionY = bounds.center.y * 0.6f + bounds.extents.y;
        Vector3 crownPosition = figurePosition + new Vector3(0, positionY + 15f, 0);

        GameObject crown = Instantiate(_crownPrefab, crownPosition, Quaternion.identity, figureTransform);
        Transform crownTransform = crown.transform;

        await crownTransform.DOMoveY(positionY, _crownAppearanceDuration)
            .SetEase(Ease.InQuad)
            .ToUniTask(cancellationToken: token);
    }

    private async UniTask PlayEndingAnimation(int winnerTurn, float gameEndingDuration, CancellationToken token)
    {
        string winnerName = _playerFigures[winnerTurn % 2].name;
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

        Tween expansion = figureTransform.DOScale(_expansedFigureScale, _jumpDuration * 0.5f)
            .SetEase(Ease.InOutSine);
        Tween jump = figureTransform.DOJump(figureTransform.position, _jumpPower, 1, _jumpDuration * 0.5f)
            .SetEase(Ease.OutQuad);
        Tween compression = figureTransform.DOScale(Vector3.one, _jumpDuration * 0.2f)
            .SetEase(Ease.InOutSine);

        Sequence jumpSequence = DOTween.Sequence();

        await jumpSequence
            .Append(expansion)
            .Append(jump)
            .Join(compression)
            .ToUniTask(cancellationToken: token);
    }
}
