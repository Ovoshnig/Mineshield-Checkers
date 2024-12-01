using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

public interface IBotAlgorithm
{
    UniTask<List<int>> GetMoveAsync(int[,] board, int playerIndex, List<List<int>> allowedMoves, CancellationToken cancellationToken);
}