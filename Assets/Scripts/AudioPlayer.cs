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

    private AudioSource _audioSource;

    private int _clipIndex;

    private void OnEnable()
    {
        CheckersLogic.FigurePlacedEvent += PlayPutSound;
        CheckersLogic.FigureMovedEvent += PlayMoveSound;
        CheckersLogic.FigureChoppedEvent += PlayChopSound;
        CheckersLogic.GameEndedEvent += PlayGameEndingSound;
    }

    private void OnDisable()
    {
        CheckersLogic.FigurePlacedEvent -= PlayPutSound;
        CheckersLogic.FigureMovedEvent -= PlayMoveSound;
        CheckersLogic.FigureChoppedEvent -= PlayChopSound;
        CheckersLogic.GameEndedEvent -= PlayGameEndingSound;
    }

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private async UniTaskVoid PlayPutSound(int i, int j, int index)
    {
        _clipIndex = Random.Range(0, _putClips.Length);
        _audioSource.clip = _putClips[_clipIndex];

        await UniTask.Yield();

        _audioSource.Play();
    }

    private async UniTask PlayMoveSound(List<int> moveIndex)
    {
        _clipIndex = Random.Range(0, _dragClips.Length);
        _audioSource.clip = _dragClips[_clipIndex];

        await UniTask.Yield();

        _audioSource.Play();
    }

    private async UniTaskVoid PlayChopSound(List<int> chopIndex)
    {
        _clipIndex = Random.Range(0, _chopClips.Length);
        _audioSource.clip = _chopClips[_clipIndex];

        await UniTask.Yield();

        _audioSource.Play();
    }

    private async UniTaskVoid PlayGameEndingSound(int winnerTurn, int gameEndingDuration)
    {
        _audioSource.clip = winnerTurn == 1 ? _winClip : _lossClip;

        await UniTask.Yield();

        _audioSource.Play();
    }
}
