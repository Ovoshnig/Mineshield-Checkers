using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

[RequireComponent (typeof(ParticleSystem))]
public class ParticleSpawner : MonoBehaviour
{
    [SerializeField] private ParticleSystem _particleSystem;
    [SerializeField] private CheckersLogic _logic;

    private void OnValidate()
    {
        if (_particleSystem == null)
            _particleSystem = GetComponent<ParticleSystem>();
    }

    private void OnEnable()
    {
        _logic.GameEndedEvent += SpawnEndParticles;
    }

    private void OnDisable()
    {
        _logic.GameEndedEvent -= SpawnEndParticles;
    }

    private async UniTaskVoid SpawnEndParticles(int winnerTurn, int gameEndingDuration, CancellationToken token)
    {
        _particleSystem.Play();
        await UniTask.Yield(cancellationToken: token);
    }
}