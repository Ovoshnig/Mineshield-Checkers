using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckersVisualizer : MonoBehaviour
{
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _figureSize;

    [SerializeField] private AnimationCurve _jumpCurve;
    [SerializeField] private float _jumpDuration;
    [SerializeField] private float _jumpHeigh;

    [SerializeField] private List<GameObject> _figurePrefabs;
    [SerializeField] private GameObject _crownPrefab;
    [SerializeField] private GameObject _selectionCube;

    public static readonly GameObject[] _playerFigures = new GameObject[2];

    private readonly Transform[,] _figureTransforms = new Transform[8, 8];

    private Transform _figureTransform;

    private const float _cellSize = 2.5f;

    private Vector3 startPosition;
    private Vector3 endPosition;

    private void OnEnable()
    {
        CheckersLogic.FigurePlacedEvent += PlaceFigure;
    }

    private void OnDisable()
    {
        CheckersLogic.FigurePlacedEvent -= PlaceFigure;
    }

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

        _selectionCube.SetActive(false);
    }

    private Vector3 Indexes2Position(int i, int j)
    {
        float iFloat = i + 0.5f;
        float jFloat = j + 0.5f;

        iFloat -= 4f;
        jFloat -= 4f;

        iFloat *= _cellSize;
        jFloat *= _cellSize;

        Vector3 position = new(iFloat, 0f, jFloat);
        return position;
    }

    public void PlaceFigure(int i, int j, int index)
    {
        Vector3 position = Indexes2Position(i, j);
        var newFigure = Instantiate(_playerFigures[index], position, Quaternion.Euler(0, 0, 0));
        newFigure.name = _playerFigures[index].name;
        newFigure.transform.localScale *= _figureSize;
        _figureTransforms[i, j] = newFigure.transform;
    }

    public void SetSelection(List<int> playerIndexes)
    {
        var (i, j) = (playerIndexes[0], playerIndexes[1]);

        Vector3 position = Indexes2Position(i, j);
        _selectionCube.SetActive(true);
        _selectionCube.transform.localPosition = position;
    }

    public void RemoveSelection()
    {
        _selectionCube.SetActive(false);
    }

    public IEnumerator MoveFigure(List<int> turnIndex) 
    {
        var (i, j, iDelta, jDelta) = (turnIndex[0], turnIndex[1], turnIndex[2], turnIndex[3]);

        _figureTransform = _figureTransforms[i, j];

        startPosition = _figureTransform.position;
        endPosition = Indexes2Position(i + iDelta, j + jDelta);
        float distance = Vector3.Distance(startPosition, endPosition);
        float moveDuration = distance / _moveSpeed;

        StartCoroutine(MoveFigure(startPosition, endPosition, moveDuration));
        yield return new WaitForSeconds(moveDuration);

        _figureTransforms[i + iDelta, j + jDelta] = _figureTransform;
        _figureTransforms[i, j] = null;
    }

    public void ChopFigure(int rivalI, int rivalJ)
    {
        Vector3 rivalPosition = Indexes2Position(rivalI, rivalJ);
        float distance = Vector3.Distance(startPosition, rivalPosition);
        float removeDuration = distance / _moveSpeed;
        StartCoroutine(RemoveFigure(_figureTransforms[rivalI, rivalJ], removeDuration));
    }

    public IEnumerator MoveFigure(Vector3 startPosition, Vector3 endPosition, float moveDuration)
    {
        float elapsedTime = 0f;
        float t;

        while (elapsedTime < moveDuration)
        {
            t = elapsedTime / moveDuration;
            _figureTransform.position = Vector3.Lerp(startPosition, endPosition, t);

            elapsedTime += Time.deltaTime;

            yield return null;
        }

        _figureTransform.position = endPosition;
    }

    public IEnumerator RemoveFigure(Transform figureTransform, float dutarion)
    {
        yield return new WaitForSeconds(dutarion);

        Destroy(figureTransform.gameObject);
    }

    public void CreateDam()
    {
        Transform childFigureTransform = _figureTransform.GetChild(0);
        Vector3 figurePosition = _figureTransform.position;
        Renderer renderer = childFigureTransform.GetComponent<Renderer>();
        Bounds bounds = renderer.bounds;
        Vector3 crownPosition = figurePosition + new Vector3(0, bounds.center.y * 0.6f + bounds.extents.y, 0);

        GameObject crown = Instantiate(_crownPrefab, crownPosition, Quaternion.Euler(-90, 0, 0));
        crown.transform.parent = childFigureTransform;
    }

    public IEnumerator PlayFigureAnimation(int i, int j, float startDelay)
    {
        yield return new WaitForSeconds(startDelay);

        Transform figureTransform = _figureTransforms[i, j];
        Vector3 figurePosition = figureTransform.position;

        float expiredTime = 0;

        while (expiredTime < _jumpDuration)
        {
            float progress = expiredTime / _jumpDuration;
            float currentY = _jumpHeigh * _jumpCurve.Evaluate(progress);
            figureTransform.position = new Vector3(figurePosition.x, currentY, figurePosition.z);
            expiredTime += Time.deltaTime;

            yield return null;
        }
        figureTransform.position = figurePosition;
    }
}
