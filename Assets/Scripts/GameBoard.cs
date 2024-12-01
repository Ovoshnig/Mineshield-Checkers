using System.Collections.Generic;

public class GameBoard
{
    public int[,] Board { get; private set; }

    private readonly IReadOnlyList<int> _directions = new int[] { -1, 1 };

    public GameBoard()
    {
        Board = new int[8, 8];
        InitializeBoard();
    }

    public GameBoard(int[,] board) => Board = (int[,])board.Clone();

    public void InitializeBoard()
    {
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                Board[i, j] = -1;
    }

    public void SetUpStartingPositions()
    {
        foreach (int delta in new int[] { 0, 5 })
        {
            int playerIndex = delta == 0 ? 0 : 1;

            for (int i = 0; i < 8; i++)
                for (int j = delta; j < 3 + delta; j++)
                    if (i % 2 == j % 2)
                        Board[i, j] = playerIndex;
        }
    }

    public void EnumerateMoves(int turn, out List<List<int>> chopIndexes, out List<List<int>> moveIndexes)
    {
        chopIndexes = new List<List<int>>();
        moveIndexes = new List<List<int>>();

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (Board[i, j] == turn % 2)
                    GeneratePieceMoves(i, j, turn, chopIndexes, moveIndexes);
                else if (Board[i, j] == turn % 2 + 2)
                    GenerateKingMoves(i, j, turn, chopIndexes, moveIndexes);
            }
        }
    }

    private void GeneratePieceMoves(int i, int j, int turn, List<List<int>> chopIndexes, List<List<int>> moveIndexes)
    {
        int jDelta = turn % 2 == 0 ? 1 : -1;

        foreach (int iDelta in _directions) // Проверки всех вариантов ходов вперёд
        {
            if (LogicChecker.IsCanMove(i + iDelta, j + jDelta))
            {
                if (LogicChecker.IsRival(Board, turn, i + iDelta, j + jDelta))
                {
                    if (LogicChecker.IsCanMove(i + 2 * iDelta, j + 2 * jDelta))
                    {
                        if (Board[i + 2 * iDelta, j + 2 * jDelta] == -1)
                        {
                            chopIndexes.Add(new List<int> { i, j, 2 * iDelta, 2 * jDelta, i + iDelta, j + jDelta });
                        }
                    }
                }
                else if (Board[i + iDelta, j + jDelta] == -1)
                {
                    moveIndexes.Add(new List<int> { i, j, iDelta, jDelta });
                }
            }
        }

        jDelta = -jDelta;

        foreach (int iDelta in _directions) // Проверки на рубку сзади
            if (LogicChecker.IsCanMove(i + iDelta, j + jDelta))
                if (LogicChecker.IsRival(Board, turn, i + iDelta, j + jDelta))
                    if (LogicChecker.IsCanMove(i + 2 * iDelta, j + 2 * jDelta))
                        if (Board[i + 2 * iDelta, j + 2 * jDelta] == -1)
                            chopIndexes.Add(new List<int> { i, j, 2 * iDelta, 2 * jDelta, i + iDelta, j + jDelta });
    }

    private void GenerateKingMoves(int i, int j, int turn, List<List<int>> chopIndexes, List<List<int>> moveIndexes)
    {
        foreach (int iDelta in _directions)
        {
            foreach (int jDelta in _directions)
            {
                List<int> rivalIndexes = new();

                int moveLength = 1;
                int rivalCount = 0;

                while (LogicChecker.IsCanMove(i + moveLength * iDelta, j + moveLength * jDelta))
                {
                    if (LogicChecker.IsRival(Board, turn, i + moveLength * iDelta, j + moveLength * jDelta))
                    {
                        if (rivalCount == 1)
                        {
                            break;
                        }
                        else
                        {
                            rivalIndexes.Add(i + moveLength * iDelta);
                            rivalIndexes.Add(j + moveLength * jDelta);

                            rivalCount++;
                        }
                    }
                    else if (Board[i + moveLength * iDelta, j + moveLength * jDelta] == -1)
                    {
                        if (rivalCount == 1)
                            chopIndexes.Add(new List<int> { i, j, moveLength * iDelta, moveLength * jDelta, rivalIndexes[0], rivalIndexes[1] });
                        else
                            moveIndexes.Add(new List<int> { i, j, moveLength * iDelta, moveLength * jDelta });
                    }
                    else
                    {
                        break;
                    }

                    moveLength++;
                }
            }
        }
    }

    public void TryChop(int turn, int i, int j, out List<List<int>> chopIndexes)
    {
        chopIndexes = new List<List<int>>();

        if (Board[i, j] == turn % 2)
            GeneratePieceChops(i, j, turn, chopIndexes);
        else if (Board[i, j] == turn % 2 + 2)
            GenerateKingChops(i, j, turn, chopIndexes);
    }

    private void GeneratePieceChops(int i, int j, int turn, List<List<int>> chopIndexes)
    {
        foreach (int iDelta in _directions)
            foreach (int jDelta in _directions)
                if (LogicChecker.IsCanMove(i + iDelta, j + jDelta))
                    if (LogicChecker.IsRival(Board, turn, i + iDelta, j + jDelta))
                        if (LogicChecker.IsCanMove(i + 2 * iDelta, j + 2 * jDelta))
                            if (Board[i + 2 * iDelta, j + 2 * jDelta] == -1)
                                chopIndexes.Add(new List<int> { i, j, 2 * iDelta, 2 * jDelta, i + iDelta, j + jDelta });
    }

    private void GenerateKingChops(int i, int j, int turn, List<List<int>> chopIndexes)
    {
        foreach (int iDelta in _directions)
        {
            foreach (int jDelta in _directions)
            {
                List<int> rivalIndexes = new();

                int moveLength = 1;
                int rivalCount = 0;

                while (LogicChecker.IsCanMove(i + moveLength * iDelta, j + moveLength * jDelta))
                {
                    if (LogicChecker.IsRival(Board, turn, i + moveLength * iDelta, j + moveLength * jDelta))
                    {
                        if (rivalCount == 1)
                        {
                            break;
                        }
                        else
                        {
                            rivalIndexes.Add(i + moveLength * iDelta);
                            rivalIndexes.Add(j + moveLength * jDelta);

                            rivalCount++;
                        }
                    }
                    else if (Board[i + moveLength * iDelta, j + moveLength * jDelta] == -1)
                    {
                        if (rivalCount == 1)
                            chopIndexes.Add(new List<int> { i, j, moveLength * iDelta, moveLength * jDelta, rivalIndexes[0], rivalIndexes[1] });
                    }
                    else
                    {
                        break;
                    }

                    moveLength++;
                }
            }
        }
    }

    public bool ApplyMove(List<int> move, int turn, ref int[] figureCounts)
    {
        var (i, j, iDelta, jDelta) = (move[0], move[1], move[2], move[3]);
        int oppositeBoardSide = turn % 2 == 0 ? 7 : 0;
        bool isDam = Board[i, j] == turn % 2 + 2;
        bool promoted = false;

        if (move.Count >= 6)
        {
            var (rivalI, rivalJ) = (move[4], move[5]);
            RemoveRivalPiece(rivalI, rivalJ, ref figureCounts);
        }

        UpdateBoardAfterMove(i, j, iDelta, jDelta, oppositeBoardSide, isDam, turn);

        if (Board[i + iDelta, j + jDelta] == turn % 2 + 2 && !isDam)
            promoted = true;

        return promoted;
    }

    public void ApplyMove(List<int> move, int turn)
    {
        int[] figureCounts = null;
        ApplyMove(move, turn, ref figureCounts);
    }

    private void UpdateBoardAfterMove(int i, int j, int iDelta, int jDelta, int oppositeBoardSide, bool isDam, int turn)
    {
        if (j + jDelta == oppositeBoardSide && !isDam)
            Board[i + iDelta, j + jDelta] = turn % 2 + 2;
        else
            Board[i + iDelta, j + jDelta] = Board[i, j];

        Board[i, j] = -1;
    }

    private void RemoveRivalPiece(int rivalI, int rivalJ, ref int[] figureCounts)
    {
        int rivalIndex = Board[rivalI, rivalJ] % 2;
        Board[rivalI, rivalJ] = -1;

        if (figureCounts != null)
            figureCounts[rivalIndex]--;
    }

    public int[,] CloneBoard() => (int[,])Board.Clone();
}
