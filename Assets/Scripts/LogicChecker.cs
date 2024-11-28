public static class LogicChecker
{
    public static bool IsCanMove(int i, int j) => 
        (i is > -1 and < 8) && (j is > -1 and < 8);

    public static bool IsRival(int[,] board, int turn, int i, int j) =>
        board[i, j] != -1 && board[i, j] % 2 != turn % 2;
}
