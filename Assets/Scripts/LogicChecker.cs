public static class LogicChecker
{
    public static bool IsCanMove(int i, int j)
    {
        return (i is > -1 and < 8) &&
               (j is > -1 and < 8);
    }

    public static bool IsRival(int[,] board, int turn, int i, int j)
    {
        int rivalFigure;
        int rivalDam;

        if (turn == 1)
        {
            rivalFigure = 2;
            rivalDam = 4;
        }
        else
        {
            rivalFigure = 1;
            rivalDam = 3;
        }

        return (board[i, j] == rivalFigure ||
                board[i, j] == rivalDam);
    }
}

