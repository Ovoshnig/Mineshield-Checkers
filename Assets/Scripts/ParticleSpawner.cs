using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

[RequireComponent (typeof(ParticleSystem))]
public class ParticleSpawner : MonoBehaviour
{
    [SerializeField] private Color[] _colors;
    [SerializeField] private ParticleSystem _particleSystem;
    [SerializeField] private CheckersLogic _logic;

    private void OnValidate()
    {
        if (_particleSystem == null)
            _particleSystem = GetComponent<ParticleSystem>();
    }

    private void OnEnable() => _logic.GameEnding += PlayEndParticles;

    private void OnDisable() => _logic.GameEnding -= PlayEndParticles;

    private async UniTask PlayEndParticles(int winnerTurn, float gameEndingDuration, CancellationToken token)
    {
        _particleSystem.Play();
        await UniTask.Yield(cancellationToken: token);
    }
}
