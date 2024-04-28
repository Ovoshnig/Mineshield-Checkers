using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip[] _putClips;
    [SerializeField] private AudioClip[] _dragClips;
    [SerializeField] private AudioClip[] _chopClips;
    [SerializeField] private AudioClip _winClip;
    [SerializeField] private AudioClip _lossClip;

    [SerializeField] private CheckersLogic _logic;
    [SerializeField] private AudioSource _audioSource;

    private int _clipIndex;

    private void OnValidate()
    {
        _logic ??= FindObjectOfType<CheckersLogic>();
        _audioSource ??= GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        _logic.FigurePlacedEvent += PlayPutSound;
        _logic.FigureMovedEvent += PlayMoveSound;
        _logic.FigureChoppedEvent += PlayChopSound;
        _logic.GameEndedEvent += PlayGameEndingSound;
    }

    private void OnDisable()
    {
        _logic.FigurePlacedEvent -= PlayPutSound;
        _logic.FigureMovedEvent -= PlayMoveSound;
        _logic.FigureChoppedEvent -= PlayChopSound;
        _logic.GameEndedEvent -= PlayGameEndingSound;
    }

    private async UniTaskVoid PlayPutSound(int i, int j, int index)
    {
        _clipIndex = Random.Range(0, _putClips.Length);

        await UniTask.Yield();

        _audioSource.PlayOneShot(_putClips[_clipIndex]);
    }

    private async UniTask PlayMoveSound(List<int> moveIndex)
    {
        _clipIndex = Random.Range(0, _dragClips.Length);

        await UniTask.Yield();

        _audioSource.PlayOneShot(_dragClips[_clipIndex]);
    }

    private async UniTaskVoid PlayChopSound(List<int> chopIndex, int chopDelay)
    {
        _clipIndex = Random.Range(0, _chopClips.Length);

        await UniTask.Delay(chopDelay);

        _audioSource.PlayOneShot(_chopClips[_clipIndex]);
    }

    private async UniTaskVoid PlayGameEndingSound(int winnerTurn, int gameEndingDuration)
    {
        _audioSource.clip = winnerTurn == 1 ? _winClip : _lossClip;

        await UniTask.Yield();

        _audioSource.Play();
    }
}
