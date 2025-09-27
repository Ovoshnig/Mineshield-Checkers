using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System.Threading;

public class FigureRotator
{
    private readonly Ease _ease;
    private readonly float _rotationDuration;

    private CancellationToken _token;

    public FigureRotator(float rotationDuration, Ease ease, CancellationToken token)
    {
        _rotationDuration = rotationDuration;
        _token = token;
        _ease = ease;
    }

    public void UpdateCancellationToken(CancellationToken token) => _token = token;

    public void Rotate(GameObject figure)
    {
        figure.transform.DOKill(true);
        figure.transform.localRotation = Quaternion.identity;
        Vector3 targetRotation = new(0f, 360f, 0f);

        figure.transform.DORotate(targetRotation, _rotationDuration, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(_ease)
            .ToUniTask(cancellationToken: _token)
            .Forget();
    }

    public void ResetRotation(GameObject figure) => figure.transform.localRotation = Quaternion.identity;
}
