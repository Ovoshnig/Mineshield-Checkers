using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class AudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioResource _putResource;
    [SerializeField] private AudioResource _dragResource;
    [SerializeField] private AudioResource _chopResource;
    [SerializeField] private AudioResource _damCreatedResource;
    [SerializeField] private AudioClip _winClip;
    [SerializeField] private AudioClip _lossClip;
    [SerializeField] private CheckersLogic _logic;
    [SerializeField] private AudioSource _audioSource;

    private int _clipIndex;

    private void Awake() => _audioSource = GetComponent<AudioSource>();

    private void OnEnable()
    {
        _logic.FigurePlaced += PlayPutSound;
        _logic.FigureMoved += PlayMoveSound;
        _logic.FigureChopped += PlayChopSound;
        _logic.DamCreated += PlayDamCreatedSound;
        _logic.GameEnding += PlayGameEndingSound;
    }

    private void OnDisable()
    {
        _logic.FigurePlaced -= PlayPutSound;
        _logic.FigureMoved -= PlayMoveSound;
        _logic.FigureChopped -= PlayChopSound;
        _logic.DamCreated -= PlayDamCreatedSound;
        _logic.GameEnding -= PlayGameEndingSound;
    }

    private async UniTask PlayPutSound(int i, int j, int index)
    {
        await UniTask.Yield();

        _audioSource.resource = _putResource;
        _audioSource.Play();
    }

    private async UniTask PlayMoveSound(List<int> moveIndex)
    {
        await UniTask.Yield();

        _audioSource.resource = _dragResource;
        _audioSource.Play();
    }

    private async UniTask PlayChopSound(List<int> move)
    {
        var (i, j, rivalI, rivalJ) = (move[0], move[1], move[4], move[5]);

        Vector3 startPosition = CoordinateTranslator.Indexes2Position(i, j);
        Vector3 rivalPosition = CoordinateTranslator.Indexes2Position(rivalI, rivalJ);
        float distance = Vector3.Distance(startPosition, rivalPosition);
        float chopDelay = (distance / _logic.MoveSpeed);
        await UniTask.WaitForSeconds(chopDelay);

        _audioSource.resource = _chopResource;
        _audioSource.Play();
    }

    private async UniTask PlayDamCreatedSound(int i, int j)
    {
        await UniTask.Yield();

        _audioSource.resource = _damCreatedResource;
        _audioSource.Play();
    }

    private async UniTask PlayGameEndingSound(int winnerTurn, float gameEndingDuration, CancellationToken token)
    {
        _audioSource.clip = winnerTurn % 2 == 0 ? _winClip : _lossClip;
        await UniTask.Yield(cancellationToken: token);

        _audioSource.Play();
    }
}
