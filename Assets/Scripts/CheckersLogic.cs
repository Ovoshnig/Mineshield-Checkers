using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayerInputHandler))]
public class CheckersLogic : MonoBehaviour
{
    [SerializeField] private float _placementDelay;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _gameEndingDuration;
    [SerializeField] private string _botAlgorithmType = "Minimax";
    [SerializeField] private PlayerInputHandler _playerInput;

    private readonly int[,] _board = new int[8, 8];
    private readonly int[] _figureCounts = { 12, 12 };
    private readonly IReadOnlyList<int> _directions = new int[] { -1, 1 };
    private IBotAlgorithm _botAlgorithm;
    private CancellationTokenSource _cts = new();
    private int _turn = 0;

    public event Func<int, int, int, UniTask> FigurePlaced;
    public event Action<List<int>, bool> FigureSelected;
    public event Func<List<int>, UniTask> FigureMoved;
    public event Func<List<int>, UniTask> FigureChopped;
    public event Func<int, int, UniTask> DamCreated;
    public event Func<int, float, CancellationToken, UniTask> GameEnding;

    public float MoveSpeed => _moveSpeed;
    public int[] FigureCounts => _figureCounts;

    private int RivalIndex => _turn % 2 == 0 ? 1 : 0;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInputHandler>();

        _botAlgorithm = _botAlgorithmType switch
        {
            "Minimax" => new MinimaxBot(this),
            "MCTS" => new MonteCarloTreeSearch(this),
            _ => throw new InvalidOperationException("Unknown bot algorithm type")
        };
    }

    private async void Start()
    {
        await MakeStartPlacement();
        int winnerTurn = await ExecuteGameLoop();
        await Win(winnerTurn, _cts.Token);
    }

    private void OnDisable()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }

    private async UniTask MakeStartPlacement()
    {
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                _board[i, j] = -1;

        foreach (int delta in new int[] { 0, 5 })
        {
            int playerIndex = delta == 0 ? 0 : 1;

            for (int i = 0; i < 8; i++)
            {
                for (int j = delta; j < 3 + delta; j++)
                {
                    if (i % 2 == j % 2)
                    {
                        _board[i, j] = delta == 0 ? 0 : 1;
                        FigurePlaced?.Invoke(i, j, playerIndex);

                        await UniTask.WaitForSeconds(_placementDelay);
                    }
                }
            }
        }
    }

    private async UniTask<int> ExecuteGameLoop()
    {
        List<List<int>> chopIndexes, moveIndexes;
        List<int> moveIndex;
        int winnerTurn = 0;

        while (true)
        {
            EnumerateMoves(_board, _turn, out chopIndexes, out moveIndexes);

            if (chopIndexes.Count == 0 && moveIndexes.Count == 0)
            {
                winnerTurn = _turn + 1;
                break;
            }

            List<List<int>> indexes = chopIndexes.Count > 0 ? chopIndexes : moveIndexes;

            if (_turn % 2 == 0)
                moveIndex = await GetPlayerMove(indexes);
            else
                moveIndex = await GetAIMove(indexes);

            if (chopIndexes.Count > 0)
            {
                while (true)
                {
                    await MakeMove(moveIndex, isChop: true);

                    if (_figureCounts[RivalIndex] == 0)
                    {
                        winnerTurn = _turn;
                        break;
                    }
                    else
                    {
                        int i = moveIndex[0] + moveIndex[2];
                        int j = moveIndex[1] + moveIndex[3];
                        TryChop(_board, _turn, i, j, out chopIndexes);

                        if (chopIndexes.Count > 0)
                        {
                            if (_turn % 2 == 0)
                                moveIndex = await GetPlayerChopMove(chopIndexes);
                            else
                                moveIndex = await GetAIMove(chopIndexes);
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if (_figureCounts[RivalIndex] == 0)
                    break;
            }
            else
            {
                await MakeMove(moveIndex, isChop: false);
            }

            _turn++;
        }

        return winnerTurn;
    }

    public void EnumerateMoves(int[,] board, int turn, out List<List<int>> chopIndexes, out List<List<int>> moveIndexes)
    {
        chopIndexes = new List<List<int>>();
        moveIndexes = new List<List<int>>();

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (board[i, j] == turn % 2) // Фигура текущего игрока
                {
                    int jDelta = turn % 2 == 0 ? 1 : -1;

                    foreach (int iDelta in _directions) // Проверки всех вариантов ходов вперёд
                    {
                        if (LogicChecker.IsCanMove(i + iDelta, j + jDelta))
                        {
                            if (LogicChecker.IsRival(board, turn, i + iDelta, j + jDelta))
                            {
                                if (LogicChecker.IsCanMove(i + 2 * iDelta, j + 2 * jDelta))
                                {
                                    if (board[i + 2 * iDelta, j + 2 * jDelta] == -1)
                                    {
                                        chopIndexes.Add(new List<int> { i, j, 2 * iDelta, 2 * jDelta, i + iDelta, j + jDelta });
                                    }
                                }
                            }
                            else if (board[i + iDelta, j + jDelta] == -1)
                            {
                                moveIndexes.Add(new List<int> { i, j, iDelta, jDelta });
                            }
                        }
                    }

                    jDelta = -jDelta;

                    foreach (int iDelta in _directions) // Проверки, есть ли сзади противник, которого можно срубить
                    {
                        if (LogicChecker.IsCanMove(i + iDelta, j + jDelta))
                        {
                            if (LogicChecker.IsRival(board, turn, i + iDelta, j + jDelta))
                            {
                                if (LogicChecker.IsCanMove(i + 2 * iDelta, j + 2 * jDelta))
                                {
                                    if (board[i + 2 * iDelta, j + 2 * jDelta] == -1)
                                    {
                                        chopIndexes.Add(new List<int> { i, j, 2 * iDelta, 2 * jDelta, i + iDelta, j + jDelta });
                                    }
                                }
                            }
                        }
                    }
                }
                else if (board[i, j] == turn % 2 + 2) // Дамка текущего игрока
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
                                if (LogicChecker.IsRival(board, turn, i + moveLength * iDelta, j + moveLength * jDelta))
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
                                else if (board[i + moveLength * iDelta, j + moveLength * jDelta] == -1)
                                {
                                    if (rivalCount == 1)
                                    {
                                        chopIndexes.Add(new List<int> { i, j, moveLength * iDelta, moveLength * jDelta, rivalIndexes[0], rivalIndexes[1] });
                                    }
                                    else
                                    {
                                        moveIndexes.Add(new List<int> { i, j, moveLength * iDelta, moveLength * jDelta });
                                    }
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
            }
        }
    }

    public void TryChop(int[,] board, int turn, int i, int j, out List<List<int>> chopIndexes)
    {
        chopIndexes = new List<List<int>>();

        if (board[i, j] == turn % 2) // Ходит фигура
        {
            foreach (int iDelta in _directions) // Проверки, есть ли противник, которого можно срубить
            {
                foreach (int jDelta in _directions)
                {
                    if (LogicChecker.IsCanMove(i + iDelta, j + jDelta))
                    {
                        if (LogicChecker.IsRival(board, turn, i + iDelta, j + jDelta))
                        {
                            if (LogicChecker.IsCanMove(i + 2 * iDelta, j + 2 * jDelta))
                            {
                                if (board[i + 2 * iDelta, j + 2 * jDelta] == -1)
                                {
                                    chopIndexes.Add(new List<int> { i, j, 2 * iDelta, 2 * jDelta, i + iDelta, j + jDelta });
                                }
                            }
                        }
                    }
                }
            }
        }
        else if (board[i, j] == turn % 2 + 2) // Ходит дамка
        {
            foreach (int iDelta in _directions) // Проверки, есть ли сзади противник, которого можно срубить
            {
                foreach (int jDelta in _directions)
                {
                    List<int> rivalIndexes = new();

                    int moveLength = 1;
                    int rivalCount = 0;

                    while (LogicChecker.IsCanMove(i + moveLength * iDelta, j + moveLength * jDelta))
                    {
                        if (LogicChecker.IsRival(board, turn, i + moveLength * iDelta, j + moveLength * jDelta))
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
                        else if (board[i + moveLength * iDelta, j + moveLength * jDelta] == -1)
                        {
                            if (rivalCount == 1)
                            {
                                chopIndexes.Add(new List<int> { i, j, moveLength * iDelta, moveLength * jDelta,
                                    rivalIndexes[0], rivalIndexes[1] });
                            }
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
    }

    private async UniTask<List<int>> GetPlayerMove(List<List<int>> turnIndexes)
    {
        List<int> inputPosition = new();
        List<int> inputStartPosition = new();
        bool isStartPositionReceived = false;

        while (true)
        {
            inputPosition = new();

            await _playerInput.GetPlayerInput(inputPosition);

            if (turnIndexes.Find(x => x.GetRange(0, 2).SequenceEqual(inputPosition)) != null)
            {
                inputStartPosition = new List<int>(inputPosition);
                isStartPositionReceived = true;

                FigureSelected?.Invoke(inputStartPosition, true);
            }
            else if (isStartPositionReceived)
            {
                var (iStart, jStart) = (inputStartPosition[0], inputStartPosition[1]);
                var (iEnd, jEnd) = (inputPosition[0], inputPosition[1]);
                int iDelta = iEnd - iStart;
                int jDelta = jEnd - jStart;
                List<int> playerIndexes = new() { iStart, jStart, iDelta, jDelta };

                var finding = turnIndexes.Find(x => x.GetRange(0, 4).SequenceEqual(playerIndexes));

                if (finding == null)
                {
                    isStartPositionReceived = false;
                    inputStartPosition = new();

                    FigureSelected?.Invoke(inputStartPosition, false);
                }
                else
                {
                    FigureSelected?.Invoke(inputStartPosition, false);

                    return finding;
                }
            }
        }
    }

    private async UniTask<List<int>> GetPlayerChopMove(List<List<int>> turnIndexes)
    {
        List<int> inputEndPosition = new();
        List<int> inputStartPosition = turnIndexes[0].GetRange(0, 2);
        FigureSelected?.Invoke(inputStartPosition, true);

        List<int> finding = null;

        while (finding == null)
        {
            inputEndPosition = new();

            await _playerInput.GetPlayerInput(inputEndPosition);

            var (iStart, jStart) = (inputStartPosition[0], inputStartPosition[1]);
            var (iEnd, jEnd) = (inputEndPosition[0], inputEndPosition[1]);
            int iDelta = iEnd - iStart;
            int jDelta = jEnd - jStart;
            List<int> playerIndexes = new() { iDelta, jDelta };

            finding = turnIndexes.Find(x => x.GetRange(2, 2).SequenceEqual(playerIndexes));
        }

        FigureSelected?.Invoke(inputStartPosition, false);

        return finding;
    }

    private async UniTask<List<int>> GetAIMove(List<List<int>> allowedMoves)
    {
        using CancellationTokenSource cts = new();

        return await _botAlgorithm.GetMoveAsync(_board, _turn % 2, allowedMoves, cts.Token);
    }

    private void UpdateBoardAfterMove(int[,] board, int i, int j, int iDelta, int jDelta, int oppositeBoardSide, bool isDam)
    {
        if (j + jDelta == oppositeBoardSide && !isDam)
            board[i + iDelta, j + jDelta] = _turn % 2 + 2;
        else
            board[i + iDelta, j + jDelta] = board[i, j];

        board[i, j] = -1;
    }

    private void RemoveRivalPiece(int[,] board, int rivalI, int rivalJ, int[] figureCounts)
    {
        board[rivalI, rivalJ] = -1;
        figureCounts[RivalIndex]--;
    }

    private async UniTask PerformMove(List<int> move, bool isChop)
    {
        var (i, j, iDelta, jDelta) = (move[0], move[1], move[2], move[3]);
        int oppositeBoardSide = _turn % 2 == 0 ? 7 : 0;
        bool isDam = _board[i, j] == _turn % 2 + 2;

        if (isChop)
        {
            var (rivalI, rivalJ) = (move[4], move[5]);
            RemoveRivalPiece(_board, rivalI, rivalJ, _figureCounts);
        }

        UpdateBoardAfterMove(_board, i, j, iDelta, jDelta, oppositeBoardSide, isDam);

        if (j + jDelta == oppositeBoardSide && !isDam)
            await DamCreated.InvokeAndWaitAsync(i + iDelta, j + jDelta);
    }

    public async UniTask MakeMove(List<int> move, bool isChop)
    {
        if (isChop)
            FigureChopped?.Invoke(move);

        await FigureMoved.InvokeAndWaitAsync(move);
        await PerformMove(move, isChop);
    }

    public void SimulateMove(int[,] board, int[] figureCounts, List<int> move, bool isChop)
    {
        var (i, j, iDelta, jDelta) = (move[0], move[1], move[2], move[3]);
        int oppositeBoardSide = _turn % 2 == 0 ? 7 : 0;
        bool isDam = _board[i, j] == _turn % 2 + 2;

        if (isChop)
        {
            var (rivalI, rivalJ) = (move[4], move[5]);
            RemoveRivalPiece(board, rivalI, rivalJ, figureCounts);
        }

        UpdateBoardAfterMove(board, i, j, iDelta, jDelta, oppositeBoardSide, isDam);
    }

    private async UniTask Win(int winnerTurn, CancellationToken token)
    {
        await GameEnding.InvokeAndWaitAsync(winnerTurn, _gameEndingDuration, token);

        SceneManager.LoadScene(0);
    }
}
