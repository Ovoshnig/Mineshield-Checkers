using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

public class MonteCarloTreeSearch : IBotAlgorithm
{
    private const int MaxIterations = 1000;

    private readonly CheckersLogic _checkersLogic;

    public MonteCarloTreeSearch(CheckersLogic checkersLogic) => _checkersLogic = checkersLogic;

    public async UniTask<List<int>> GetMoveAsync(int[,] board, int turn, CheckersLogic logic, CancellationToken cancellationToken)
    {
        MCTNode root = new MCTNode(board, turn);

        List<List<int>> chopIndexes, moveIndexes;
        logic.EnumerateMoves(board, turn, out chopIndexes, out moveIndexes);
        var possibleMoves = chopIndexes.Count > 0 ? chopIndexes : moveIndexes;

        foreach (var move in possibleMoves)
        {
            root.AddChild(new MCTNode(board, turn, move, logic));
        }

        for (int i = 0; i < MaxIterations; i++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            MCTNode selectedNode = Select(root);
            int simulationResult = await SimulateAsync(selectedNode, cancellationToken);
            Backpropagate(selectedNode, simulationResult);

            // Периодическая уступка потока другим задачам
            if (i % 100 == 0) 
                await UniTask.Yield(cancellationToken);
        }

        return root.GetBestChild().Move;
    }

    private MCTNode Select(MCTNode node)
    {
        while (!node.IsLeaf)
        {
            node = node.GetChildWithBestUCB1();
        }
        return node;
    }

    private async UniTask<int> SimulateAsync(MCTNode node, CancellationToken cancellationToken)
    {
        int[,] simulatedBoard = (int[,])node.Board.Clone();
        int currentPlayer = node.Player;
        var figureCounts = (int[])_checkersLogic.FigureCounts.Clone();

        for (int depth = 0; depth < 20; depth++)
        {
            if (cancellationToken.IsCancellationRequested) return -1;

            List<List<int>> chopIndexes, moveIndexes;
            _checkersLogic.EnumerateMoves(simulatedBoard, currentPlayer, out chopIndexes, out moveIndexes);

            var possibleMoves = chopIndexes.Count > 0 ? chopIndexes : moveIndexes;

            if (possibleMoves.Count == 0)
                return currentPlayer == 0 ? 1 : 0; // Побеждает другой игрок

            var randomMove = possibleMoves[UnityEngine.Random.Range(0, possibleMoves.Count)];

            _checkersLogic.SimulateMove(simulatedBoard, figureCounts, randomMove, chopIndexes.Count > 0);

            // Если была рубка, проверяем возможность дальнейших рубок
            if (chopIndexes.Count > 0)
            {
                var (i, j) = (randomMove[0] + randomMove[2], randomMove[1] + randomMove[3]);
                _checkersLogic.TryChop(simulatedBoard, currentPlayer, i, j, out var additionalChops);

                while (additionalChops.Count > 0)
                {
                    if (cancellationToken.IsCancellationRequested) return -1;

                    randomMove = additionalChops[UnityEngine.Random.Range(0, additionalChops.Count)];
                    _checkersLogic.SimulateMove(simulatedBoard, figureCounts, randomMove, true);

                    (i, j) = (randomMove[0] + randomMove[2], randomMove[1] + randomMove[3]);
                    _checkersLogic.TryChop(simulatedBoard, currentPlayer, i, j, out additionalChops);
                }
            }
            else
            {
                currentPlayer = 1 - currentPlayer; // Меняем ход, если нет цепочки рубок
            }

            // Периодически уступаем поток
            if (depth % 5 == 0) 
                await UniTask.Yield(cancellationToken);
        }

        return -1; // Ничья
    }

    private void Backpropagate(MCTNode node, int result)
    {
        while (node != null)
        {
            node.VisitCount++;
            if (node.Player == result)
                node.WinCount++;
            node = node.Parent;
        }
    }
}
