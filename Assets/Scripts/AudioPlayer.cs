using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
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

    private List<AudioSource> _audioSources;

    private void Awake() => _audioSources = GetComponents<AudioSource>().ToList();

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

    private void PlaySound(AudioResource audioResource)
    {
        AudioSource idleAudioSource = _audioSources.FirstOrDefault(a => !a.isPlaying);
        AudioSource audioSource;

        if (idleAudioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            _audioSources.Add(audioSource);
        }
        else
        {
            audioSource = idleAudioSource;
        }

        audioSource.resource = audioResource;
        audioSource.Play();
    }

    private async UniTask PlayPutSound(int i, int j, int index)
    {
        await UniTask.Yield();
        PlaySound(_putResource);
    }

    private async UniTask PlayMoveSound(List<int> moveIndex)
    {
        await UniTask.Yield();
        PlaySound(_dragResource);
    }

    private async UniTask PlayChopSound(List<int> move)
    {
        var (i, j, rivalI, rivalJ) = (move[0], move[1], move[4], move[5]);

        Vector3 startPosition = CoordinateTranslator.Indexes2Position(i, j);
        Vector3 rivalPosition = CoordinateTranslator.Indexes2Position(rivalI, rivalJ);
        float distance = Vector3.Distance(startPosition, rivalPosition);
        float chopDelay = (distance / _logic.MoveSpeed);
        await UniTask.WaitForSeconds(chopDelay);
        PlaySound(_chopResource);
    }

    private async UniTask PlayDamCreatedSound(int i, int j)
    {
        await UniTask.Yield();
        PlaySound(_damCreatedResource);
    }

    private async UniTask PlayGameEndingSound(int winnerTurn, float gameEndingDuration, CancellationToken token)
    {
        _audioSources[0].clip = winnerTurn % 2 == 0 ? _winClip : _lossClip;
        await UniTask.Yield(cancellationToken: token);

        _audioSources[0].Play();
    }
}
