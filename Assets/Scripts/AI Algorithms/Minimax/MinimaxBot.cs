using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

public class MinimaxBot : IBotAlgorithm
{
    private CheckersLogic _logic;
    private int _maxDepth = 4; // Adjust as needed

    public MinimaxBot(CheckersLogic logic)
    {
        _logic = logic;
    }

    public async UniTask<List<int>> GetMoveAsync(int[,] board, int playerIndex, List<List<int>> allowedMoves, CancellationToken cancellationToken)
    {
        await UniTask.Yield();

        // Convert allowedMoves to internal Move representation
        List<Move> possibleMoves = ConvertAllowedMovesToMoves(allowedMoves);

        int bestEval = int.MinValue;
        Move bestMove = null;

        foreach (var move in possibleMoves)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            int[,] newBoard = (int[,])board.Clone();
            ApplyMove(newBoard, move, playerIndex);

            int eval = Minimax(newBoard, _maxDepth - 1, int.MinValue, int.MaxValue, false, playerIndex, cancellationToken);

            if (eval > bestEval)
            {
                bestEval = eval;
                bestMove = move;
            }
        }

        // Convert the bestMove to the format expected by CheckersLogic
        List<int> moveList = ConvertMoveToList(bestMove);

        return moveList;
    }

    private List<Move> ConvertAllowedMovesToMoves(List<List<int>> allowedMoves)
    {
        List<Move> moves = new List<Move>();

        foreach (var moveList in allowedMoves)
        {
            Move move = new Move();
            int iStart = moveList[0];
            int jStart = moveList[1];
            int iDelta = moveList[2];
            int jDelta = moveList[3];
            int iEnd = iStart + iDelta;
            int jEnd = jStart + jDelta;

            move.Positions.Add((iStart, jStart));
            move.Positions.Add((iEnd, jEnd));

            if (moveList.Count >= 6)
            {
                // It's a capture move
                int rivalI = moveList[4];
                int rivalJ = moveList[5];
                move.CapturedPositions.Add((rivalI, rivalJ));
            }

            moves.Add(move);
        }

        return moves;
    }

    private List<int> ConvertMoveToList(Move move)
    {
        List<int> moveList = new List<int>();

        if (move.Positions.Count >= 2)
        {
            int iStart = move.Positions[0].Item1;
            int jStart = move.Positions[0].Item2;
            int iEnd = move.Positions[1].Item1;
            int jEnd = move.Positions[1].Item2;
            int iDelta = iEnd - iStart;
            int jDelta = jEnd - jStart;

            moveList.Add(iStart);
            moveList.Add(jStart);
            moveList.Add(iDelta);
            moveList.Add(jDelta);

            if (move.CapturedPositions.Count > 0)
            {
                int rivalI = move.CapturedPositions[0].Item1;
                int rivalJ = move.CapturedPositions[0].Item2;
                moveList.Add(rivalI);
                moveList.Add(rivalJ);
            }
        }

        return moveList;
    }

    private int Minimax(int[,] board, int depth, int alpha, int beta, bool maximizingPlayer, int playerIndex, CancellationToken cancellationToken)
    {
        if (depth == 0 || IsTerminalNode(board))
        {
            return EvaluateBoard(board, playerIndex);
        }

        if (cancellationToken.IsCancellationRequested)
            return 0;

        List<Move> moves = GenerateAllPossibleMoves(board, maximizingPlayer ? playerIndex : 1 - playerIndex);

        if (moves.Count == 0)
        {
            return maximizingPlayer ? int.MinValue + depth : int.MaxValue - depth;
        }

        if (maximizingPlayer)
        {
            int maxEval = int.MinValue;
            foreach (var move in moves)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                int[,] newBoard = (int[,])board.Clone();
                ApplyMove(newBoard, move, playerIndex);

                int eval = Minimax(newBoard, depth - 1, alpha, beta, false, playerIndex, cancellationToken);
                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);

                if (beta <= alpha)
                {
                    break; // Beta cut-off
                }
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

                int[,] newBoard = (int[,])board.Clone();
                ApplyMove(newBoard, move, 1 - playerIndex);

                int eval = Minimax(newBoard, depth - 1, alpha, beta, true, playerIndex, cancellationToken);
                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);

                if (beta <= alpha)
                {
                    break; // Alpha cut-off
                }
            }
            return minEval;
        }
    }

    private bool IsTerminalNode(int[,] board)
    {
        bool player0HasPieces = false;
        bool player1HasPieces = false;

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                int piece = board[i, j];
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
                {
                    score += 5;
                }
                else if (piece == playerIndex + 2)
                {
                    score += 10;
                }
                else if (piece == 1 - playerIndex)
                {
                    score -= 5;
                }
                else if (piece == 1 - playerIndex + 2)
                {
                    score -= 10;
                }
            }
        }

        return score;
    }

    private void ApplyMove(int[,] board, Move move, int playerIndex)
    {
        var positions = move.Positions;
        int piece = board[positions[0].Item1, positions[0].Item2];
        board[positions[0].Item1, positions[0].Item2] = -1;

        foreach (var (ci, cj) in move.CapturedPositions)
        {
            board[ci, cj] = -1;
        }

        int finalI = positions[positions.Count - 1].Item1;
        int finalJ = positions[positions.Count - 1].Item2;

        int oppositeBoardSide = playerIndex == 0 ? 7 : 0;
        if (finalJ == oppositeBoardSide && piece < 2)
        {
            piece += 2;
        }

        board[finalI, finalJ] = piece;
    }

    private List<Move> GenerateAllPossibleMoves(int[,] board, int playerIndex)
    {
        // Generate all possible moves for Minimax
        List<Move> moves = new List<Move>();
        List<Move> captureMoves = new List<Move>();

        // Similar logic as before but without filtering based on allowed moves
        // Since we are passing allowed moves, we will handle filtering elsewhere

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                int piece = board[i, j];
                if (piece == playerIndex || piece == playerIndex + 2)
                {
                    Move currentMove = new Move();
                    currentMove.Positions.Add((i, j));
                    GenerateCaptures(board, playerIndex, i, j, currentMove, captureMoves, new HashSet<(int, int)>());
                }
            }
        }

        if (captureMoves.Count > 0)
        {
            return captureMoves;
        }
        else
        {
            // Generate normal moves
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    int piece = board[i, j];
                    if (piece == playerIndex || piece == playerIndex + 2)
                    {
                        GenerateNormalMoves(board, playerIndex, i, j, moves);
                    }
                }
            }
            return moves;
        }
    }

    private void GenerateCaptures(int[,] board, int playerIndex, int i, int j, Move currentMove, List<Move> moves, HashSet<(int, int)> visitedCaptures)
    {
        bool anyCapture = false;
        int piece = board[i, j];
        bool isKing = piece >= 2;
        List<(int di, int dj)> directions = new List<(int, int)>
        {
            (-1, -1), (-1, 1), (1, -1), (1, 1)
        };

        foreach (var (di, dj) in directions)
        {
            int ci = i + di;
            int cj = j + dj;
            int ni = i + 2 * di;
            int nj = j + 2 * dj;

            // Check if positions are on the board
            if (ci >= 0 && ci < 8 && cj >= 0 && cj < 8 && ni >= 0 && ni < 8 && nj >= 0 && nj < 8)
            {
                int capturedPiece = board[ci, cj];
                if (capturedPiece != -1 && capturedPiece % 2 != playerIndex % 2 && !visitedCaptures.Contains((ci, cj)))
                {
                    if (board[ni, nj] == -1)
                    {
                        // Valid capture
                        int[,] newBoard = (int[,])board.Clone();
                        int pieceCopy = newBoard[i, j];
                        newBoard[ni, nj] = pieceCopy;
                        newBoard[i, j] = -1;
                        newBoard[ci, cj] = -1;

                        Move newMove = new Move
                        {
                            Positions = new List<(int, int)>(currentMove.Positions),
                            CapturedPositions = new List<(int, int)>(currentMove.CapturedPositions)
                        };
                        newMove.Positions.Add((ni, nj));
                        newMove.CapturedPositions.Add((ci, cj));

                        HashSet<(int, int)> newVisitedCaptures = new HashSet<(int, int)>(visitedCaptures);
                        newVisitedCaptures.Add((ci, cj));

                        GenerateCaptures(newBoard, playerIndex, ni, nj, newMove, moves, newVisitedCaptures);

                        anyCapture = true;
                    }
                }
            }
        }

        if (!anyCapture && currentMove.CapturedPositions.Count > 0)
        {
            // No further captures possible, add the move
            moves.Add(currentMove);
        }
    }

    private void GenerateNormalMoves(int[,] board, int playerIndex, int i, int j, List<Move> moves)
    {
        int piece = board[i, j];
        bool isKing = piece >= 2;
        List<(int di, int dj)> directions = new List<(int, int)>();

        if (isKing)
        {
            directions.AddRange(new (int, int)[] { (-1, -1), (-1, 1), (1, -1), (1, 1) });
        }
        else
        {
            int jDelta = playerIndex == 0 ? 1 : -1;
            directions.Add((1, jDelta));
            directions.Add((-1, jDelta));
        }

        foreach (var (di, dj) in directions)
        {
            int ni = i + di;
            int nj = j + dj;

            if (ni >= 0 && ni < 8 && nj >= 0 && nj < 8 && board[ni, nj] == -1)
            {
                Move move = new Move();
                move.Positions.Add((i, j));
                move.Positions.Add((ni, nj));

                moves.Add(move);
            }
        }
    }

    private class Move
    {
        public List<(int, int)> Positions { get; set; }
        public List<(int, int)> CapturedPositions { get; set; }

        public Move()
        {
            Positions = new List<(int, int)>();
            CapturedPositions = new List<(int, int)>();
        }
    }
}