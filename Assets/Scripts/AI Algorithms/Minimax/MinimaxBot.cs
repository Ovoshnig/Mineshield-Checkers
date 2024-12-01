using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

public class MinimaxBot : IBotAlgorithm
{
    private int _maxDepth = 4;

    public async UniTask<List<int>> GetMoveAsync(int[,] board, int playerIndex, List<List<int>> allowedMoves, CancellationToken cancellationToken)
    {
        await UniTask.Yield();

        GameBoard gameBoard = new GameBoard(board);

        int bestEval = int.MinValue;
        List<int> bestMove = null;

        foreach (var move in allowedMoves)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            GameBoard newBoard = new(gameBoard.CloneBoard());
            newBoard.ApplyMove(move, playerIndex);

            int eval = Minimax(newBoard, _maxDepth - 1, int.MinValue, int.MaxValue, false, playerIndex, cancellationToken);

            if (eval > bestEval)
            {
                bestEval = eval;
                bestMove = move;
            }
        }

        return bestMove;
    }

    private int Minimax(GameBoard gameBoard, int depth, int alpha, int beta, bool maximizingPlayer, int playerIndex, CancellationToken cancellationToken)
    {
        if (depth == 0 || IsTerminalNode(gameBoard))
            return EvaluateBoard(gameBoard.Board, playerIndex);

        if (cancellationToken.IsCancellationRequested)
            return 0;

        int currentPlayer = maximizingPlayer ? playerIndex : 1 - playerIndex;

        gameBoard.EnumerateMoves(currentPlayer, out List<List<int>> chopMoves, out List<List<int>> normalMoves);
        List<List<int>> moves = chopMoves.Count > 0 ? chopMoves : normalMoves;

        if (moves.Count == 0)
            return maximizingPlayer ? int.MinValue + depth : int.MaxValue - depth;

        if (maximizingPlayer)
        {
            int maxEval = int.MinValue;

            foreach (var move in moves)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                GameBoard newBoard = new GameBoard(gameBoard.CloneBoard());
                newBoard.ApplyMove(move, currentPlayer);

                int eval = Minimax(newBoard, depth - 1, alpha, beta, false, playerIndex, cancellationToken);
                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);

                if (beta <= alpha)
                    break; // Beta cut-off
            }
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (var move in moves)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                GameBoard newBoard = new GameBoard(gameBoard.CloneBoard());
                newBoard.ApplyMove(move, currentPlayer);

                int eval = Minimax(newBoard, depth - 1, alpha, beta, true, playerIndex, cancellationToken);
                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);

                if (beta <= alpha)
                    break; // Alpha cut-off
            }

            return minEval;
        }
    }

    private bool IsTerminalNode(GameBoard gameBoard)
    {
        bool player0HasPieces = false;
        bool player1HasPieces = false;

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                int piece = gameBoard.Board[i, j];

                if (piece == 0 || piece == 2)
                    player0HasPieces = true;
                else if (piece == 1 || piece == 3)
                    player1HasPieces = true;
            }
        }

        return !player0HasPieces || !player1HasPieces;
    }

    private int EvaluateBoard(int[,] board, int playerIndex)
    {
        int score = 0;

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                int piece = board[i, j];

                if (piece == playerIndex)
                    score += 5;
                else if (piece == playerIndex + 2)
                    score += 10;
                else if (piece == 1 - playerIndex)
                    score -= 5;
                else if (piece == 1 - playerIndex + 2)
                    score -= 10;
            }
        }

        return score;
    }
}
