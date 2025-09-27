using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System.Threading;

public class FigureView
{
    private readonly Transform _parent;
    private readonly GameObject[] _figurePrefabs;
    private readonly float _offset;
    private readonly List<GameObject> _figures = new();

    public FigureView(Transform parent, GameObject[] figurePrefabs, float offset)
    {
        _parent = parent;
        _figurePrefabs = figurePrefabs;
        _offset = offset;
    }

    public void CreateFigureInstances()
    {
        for (int i = 0; i < _figurePrefabs.Length; i++)
        {
            Vector3 position = new(i * _offset, 0, 0);
            GameObject figure = Object.Instantiate(_figurePrefabs[i], position, Quaternion.identity, _parent);
            _figures.Add(figure);
        }
    }

    public GameObject GetCurrentFigure(int currentIndex) => _figures[currentIndex];

    public async UniTask AnimateSwap(Transform transform,
        int direction,
        float swapDuration,
        CancellationToken token)
    {
        float targetPositionX = transform.position.x + direction * -_offset;
        await transform.DOMoveX(targetPositionX, swapDuration)
            .SetEase(Ease.InOutSine)
            .ToUniTask(cancellationToken: token);
    }
}
