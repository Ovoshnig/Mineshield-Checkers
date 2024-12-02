using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip[] _putClips;
    [SerializeField] private AudioClip[] _dragClips;
    [SerializeField] private AudioClip[] _chopClips;
    [SerializeField] private AudioClip _damCreatedClip;
    [SerializeField] private AudioClip _winClip;
    [SerializeField] private AudioClip _lossClip;
    [SerializeField] private CheckersLogic _logic;

    private AudioSource _audioSource;

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

    private void PlaySound(AudioClip[] clips) => _audioSource.PlayOneShotRandomly(clips, (0.6f, 1f), (0.95f, 1.05f));

    private async UniTask PlayPutSound(int i, int j, int index)
    {
        await UniTask.Yield();
        PlaySound(_putClips);
    }

    private async UniTask PlayMoveSound(List<int> moveIndex)
    {
        await UniTask.Yield();
        PlaySound(_dragClips);
    }

    private async UniTask PlayChopSound(List<int> move)
    {
        var (i, j, rivalI, rivalJ) = (move[0], move[1], move[4], move[5]);

        Vector3 startPosition = CoordinateTranslator.Indexes2Position(i, j);
        Vector3 rivalPosition = CoordinateTranslator.Indexes2Position(rivalI, rivalJ);
        float distance = Vector3.Distance(startPosition, rivalPosition);
        float chopDelay = (distance / _logic.MoveSpeed);

        await UniTask.WaitForSeconds(chopDelay);
        PlaySound(_chopClips);
    }

    private async UniTask PlayDamCreatedSound(int i, int j)
    {
        await UniTask.Yield();
        _audioSource.PlayOneShot(_damCreatedClip);
    }

    private async UniTask PlayGameEndingSound(int winnerTurn, float gameEndingDuration, CancellationToken token)
    {
        _audioSource.clip = winnerTurn % 2 == 0 ? _winClip : _lossClip;
        await UniTask.Yield(cancellationToken: token);

        _audioSource.Play();
    }
}
