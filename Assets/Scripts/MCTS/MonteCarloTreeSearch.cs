using System.Collections.Generic;

public class MonteCarloTreeSearch
{
    private const int MaxIterations = 1000;

    private readonly CheckersLogic _checkersLogic;

    public MonteCarloTreeSearch(CheckersLogic checkersLogic)
    {
        _checkersLogic = checkersLogic;
    }

    public List<int> FindBestMove(int[,] board, List<List<int>> possibleMoves, int player)
    {
        MCTNode root = new(board, player);

        foreach (var move in possibleMoves)
        {
            root.AddChild(new MCTNode(board, player, move, _checkersLogic));
        }

        for (int i = 0; i < MaxIterations; i++)
        {
            MCTNode selectedNode = Select(root);
            int simulationResult = Simulate(selectedNode);
            Backpropagate(selectedNode, simulationResult);
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

    private int Simulate(MCTNode node)
    {
        int[,] simulatedBoard = (int[,])node.Board.Clone();
        int currentPlayer = node.Player;
        var figureCounts = (int[])_checkersLogic.FigureCounts.Clone();

        for (int depth = 0; depth < 20; depth++) // Ограничение по глубине
        {
            List<List<int>> chopIndexes, moveIndexes;
            _checkersLogic.EnumerateMoves(simulatedBoard, currentPlayer, out chopIndexes, out moveIndexes);

            var possibleMoves = chopIndexes.Count > 0 ? chopIndexes : moveIndexes;

            if (possibleMoves.Count == 0)
                return currentPlayer == 0 ? 1 : 0; // Побеждает другой игрок

            var randomMove = possibleMoves[UnityEngine.Random.Range(0, possibleMoves.Count)];

            _checkersLogic.SimulateMove(simulatedBoard, figureCounts, randomMove, chopIndexes.Count > 0);

            // Если была рубка, проверяем, можно ли рубить дальше
            if (chopIndexes.Count > 0)
            {
                var (i, j) = (randomMove[0] + randomMove[2], randomMove[1] + randomMove[3]);
                _checkersLogic.TryChop(simulatedBoard, currentPlayer, i, j, out var additionalChops);

                while (additionalChops.Count > 0)
                {
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
