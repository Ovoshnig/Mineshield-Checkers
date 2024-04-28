using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip[] _putClips;
    [SerializeField] private AudioClip[] _dragClips;
    [SerializeField] private AudioClip _winClip;
    [SerializeField] private AudioClip _lossClip;

    private AudioSource _audioSource;

    private int _clipIndex;

    private void OnEnable()
    {
        CheckersLogic.FigurePlacedEvent += PlayPutSound;
    }

    private void OnDisable()
    {
        CheckersLogic.FigurePlacedEvent -= PlayPutSound;
    }

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    public async UniTaskVoid PlayPutSound(int i, int j, int index)
    {
        _clipIndex = Random.Range(0, _putClips.Length);
        _audioSource.clip = _putClips[_clipIndex];

        await UniTask.Yield();

        _audioSource.Play();
    }

    public async UniTaskVoid PlayMoveSound()
    {
        _clipIndex = Random.Range(0, _dragClips.Length);
        _audioSource.clip = _dragClips[_clipIndex];

        await UniTask.Yield();

        _audioSource.Play();
    }

    public async UniTaskVoid PlayGameEndingSound(int winnerTurn)
    {
        _audioSource.clip = winnerTurn == 1 ? _winClip : _lossClip;

        await UniTask.Yield();

        _audioSource.Play();
    }
}
