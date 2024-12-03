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

    private void PlaySoundRandomly(AudioClip[] clips)
    {
        AudioClip clip = clips.GetRandomClip();

        _audioSource
            .SetRandomVolume(0.6f, 1f)
            .SetRandomPitch(0.9f, 1.1f)
            .PlayOneShot(clip);
    }

    private async UniTask PlayPutSound(int i, int j, int index)
    {
        await UniTask.Yield();
        PlaySoundRandomly(_putClips);
    }

    private async UniTask PlayMoveSound(List<int> moveIndex)
    {
        await UniTask.Yield();
        PlaySoundRandomly(_dragClips);
    }

    private async UniTask PlayChopSound(List<int> move)
    {
        var (i, j, rivalI, rivalJ) = (move[0], move[1], move[4], move[5]);

        Vector3 startPosition = CoordinateTranslator.Indexes2Position(i, j);
        Vector3 rivalPosition = CoordinateTranslator.Indexes2Position(rivalI, rivalJ);
        float distance = Vector3.Distance(startPosition, rivalPosition);
        float chopDelay = (distance / _logic.MoveSpeed);

        await UniTask.WaitForSeconds(chopDelay);
        PlaySoundRandomly(_chopClips);
    }

    private async UniTask PlayDamCreatedSound(int i, int j)
    {
        await UniTask.Yield();

        _audioSource
            .SetRandomVolume(0.6f, 1f)
            .SetRandomPitch(0.9f, 1.1f)
            .PlayOneShot(_damCreatedClip);
    }

    private async UniTask PlayGameEndingSound(int winnerTurn, float gameEndingDuration, CancellationToken token)
    {
        _audioSource.volume = 1f;
        _audioSource.pitch = 1f;

        AudioClip clip = winnerTurn % 2 == 0 ? _winClip : _lossClip;
        _audioSource.clip = clip;
        _audioSource.Play();

        await UniTask.WaitForSeconds(clip.length, cancellationToken: token);
    }
}
