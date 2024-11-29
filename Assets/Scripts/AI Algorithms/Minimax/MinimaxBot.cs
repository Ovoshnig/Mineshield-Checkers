using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System;

public class MinimaxBot : IBotAlgorithm
{
    private const int MaxDepth = 4;

    public async UniTask<List<int>> GetMoveAsync(int[,] board, int turn, CheckersLogic logic, CancellationToken cancellationToken)
    {
        List<int> bestMove = null;
        int bestValue = int.MinValue;

        List<List<int>> chopIndexes, moveIndexes;
        logic.EnumerateMoves(board, turn, out chopIndexes, out moveIndexes);
        var possibleMoves = chopIndexes.Count > 0 ? chopIndexes : moveIndexes;

        foreach (var move in possibleMoves)
        {
            if (cancellationToken.IsCancellationRequested) 
                return null;

            int[,] simulatedBoard = (int[,])board.Clone();
            var figureCounts = (int[])logic.FigureCounts.Clone();
            logic.SimulateMove(simulatedBoard, figureCounts, move, chopIndexes.Count > 0);

            int moveValue = await MinimaxAsync(simulatedBoard, figureCounts, MaxDepth - 1, false, turn, logic, cancellationToken);

            if (moveValue > bestValue)
            {
                bestValue = moveValue;
                bestMove = move;
            }
        }

        return bestMove;
    }

    private async UniTask<int> MinimaxAsync(int[,] board, int[] figureCounts, int depth, 
        bool isMaximizing, int turn, CheckersLogic logic, CancellationToken cancellationToken)
    {
        if (depth == 0 || cancellationToken.IsCancellationRequested)
            return EvaluateBoard(board, figureCounts, turn % 2);

        List<List<int>> chopIndexes, moveIndexes;
        logic.EnumerateMoves(board, turn % 2, out chopIndexes, out moveIndexes);
        var possibleMoves = chopIndexes.Count > 0 ? chopIndexes : moveIndexes;

        if (possibleMoves.Count == 0)
            return isMaximizing ? int.MinValue : int.MaxValue;

        int bestValue = isMaximizing ? int.MinValue : int.MaxValue;

        foreach (var move in possibleMoves)
        {
            if (cancellationToken.IsCancellationRequested) return 0;

            // �������� ����� � ���������� �����
            int[,] simulatedBoard = (int[,])board.Clone();
            var simulatedFigureCounts = (int[])figureCounts.Clone();

            // ��������� ��������� ����
            logic.SimulateMove(simulatedBoard, simulatedFigureCounts, move, chopIndexes.Count > 0);

            // ���� ��� �� ������� �����, ����������� ����� ����
            int nextTurn = turn;

            if (chopIndexes.Count == 0)
                nextTurn++;

            // ���������� �������� ��������
            int value = await MinimaxAsync(simulatedBoard, simulatedFigureCounts, depth - 1,
                !isMaximizing, nextTurn, logic, cancellationToken);

            // ��������� ������ ������
            if (isMaximizing)
                bestValue = Math.Max(bestValue, value);
            else
                bestValue = Math.Min(bestValue, value);
        }

        return bestValue;
    }


    private int EvaluateBoard(int[,] board, int[] figureCounts, int playerIndex)
    {
        playerIndex %= 2;
        int rivalIndex = 1 - playerIndex;
        int score = 0;

        // ��������� ������� � ���������� �����
        score += 10 * (figureCounts[playerIndex] - figureCounts[rivalIndex]);

        // ��������� ������� �����
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (board[i, j] == playerIndex) // ������� ������ ����
                {
                    score += 5 + j; // �������� �������� �����
                }
                else if (board[i, j] == playerIndex + 2) // ����� ����
                {
                    score += 20; // ����� ������
                }
                else if (board[i, j] == rivalIndex) // ������� ������ ���������
                {
                    score -= 5 + (7 - j); // ���������� �� ����������� � �����
                }
                else if (board[i, j] == rivalIndex + 2) // ����� ���������
                {
                    score -= 20; // ����� ��������� �������
                }
            }
        }

        return score;
    }
}
