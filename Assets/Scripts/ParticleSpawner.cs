using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class ParticleSpawner : MonoBehaviour
{
    [SerializeField] private ParticleSystem _winParticleSystem;
    [SerializeField] private ParticleSystem _loseParticleSystem;
    [SerializeField] private CheckersLogic _logic;

    private void OnEnable() => _logic.GameEnding += PlayEndParticles;

    private void OnDisable() => _logic.GameEnding -= PlayEndParticles;

    private async UniTask PlayEndParticles(int winnerTurn, float gameEndingDuration, CancellationToken token)
    {
        await UniTask.Yield(cancellationToken: token);

        if (winnerTurn % 2 == 0)
            _winParticleSystem.Play();
        else
            _loseParticleSystem.Play();
    }
}
