using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

public interface IBotAlgorithm
{
    UniTask<List<int>> GetMoveAsync(int[,] board, int turn, CheckersLogic logic, CancellationToken cancellationToken);
}
