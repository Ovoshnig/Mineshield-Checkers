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
    [SerializeField] private PlayerInputHandler _playerInput;

    private readonly int[,] _board = new int[8, 8];
    private readonly int[] _figureCounts = { 12, 12 };
    private readonly IReadOnlyList<int> _directions = new int[] { -1, 1 };
    private CancellationTokenSource _cts = new();
    private int _turn = 0;
        
    public event Func<int, int, int, UniTask> FigurePlaced;
    public event Action<List<int>, bool> FigureSelected;
    public event Func<List<int>, UniTask> FigureMoved;
    public event Func<List<int>, float, UniTask> FigureChopped;
    public event Func<UniTask> DamCreated;
    public event Func<int, float, CancellationToken, UniTask> GameEnding;

    public float MoveSpeed => _moveSpeed;

    private int RivalIndex => _turn % 2 == 0 ? 1 : 0;

    private void Awake() => _playerInput = GetComponent<PlayerInputHandler>();

    private async void Start()
    {
        await MakeStartPlacement();
        int winnerTurn = await ExecuteGameLoop();
        await Win(winnerTurn);
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
            EnumerateMoves(out chopIndexes, out moveIndexes);

            if (chopIndexes.Count == 0 && moveIndexes.Count == 0)
            {
                winnerTurn = _turn + 1;
                break;
            }

            List<List<int>> indexes = chopIndexes.Count > 0 ? chopIndexes : moveIndexes;

            if (_turn % 2 == 0)
                moveIndex = await GetPlayerMove(indexes);
            else
                moveIndex = GetAIMove(indexes);

            if (chopIndexes.Count > 0)
            {
                while (true)
                {
                    var (i, j) = await MakeChopMove(moveIndex);

                    if (_figureCounts[RivalIndex] == 0)
                    {
                        winnerTurn = _turn;
                        break;
                    }
                    else
                    {
                        TryChop(i, j, out chopIndexes);

                        if (chopIndexes.Count > 0)
                        {
                            if (_turn % 2 == 0)
                            {
                                moveIndex = await GetPlayerChopMove(chopIndexes);
                            }
                            else
                            {
                                moveIndex = GetAIMove(chopIndexes);
                            }
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
                await MakeMove(moveIndex);
            }

            _turn++;
        }

        return winnerTurn;
    }

    private void EnumerateMoves(out List<List<int>> chopIndexes, out List<List<int>> moveIndexes)
    {
        chopIndexes = new List<List<int>>();
        moveIndexes = new List<List<int>>();

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (_board[i, j] == _turn % 2) // ������ �������� ������
                {
                    int jDelta = _turn % 2 == 0 ? 1 : -1;

                    foreach (int iDelta in _directions) // �������� ���� ��������� ����� �����
                    {
                        if (LogicChecker.IsCanMove(i + iDelta, j + jDelta))
                        {
                            if (LogicChecker.IsRival(_board, _turn, i + iDelta, j + jDelta))
                            {
                                if (LogicChecker.IsCanMove(i + 2 * iDelta, j + 2 * jDelta))
                                {
                                    if ( _board[i + 2 * iDelta, j + 2 * jDelta] == -1)
                                    {
                                        chopIndexes.Add(new List<int> { i, j, 2 * iDelta, 2 * jDelta, i + iDelta, j + jDelta });
                                    }
                                }
                            }
                            else if (_board[i + iDelta, j + jDelta] == -1)
                            {
                                moveIndexes.Add(new List<int> { i, j, iDelta, jDelta });
                            }
                        }
                    }

                    jDelta = -jDelta;

                    foreach (int iDelta in _directions) // ��������, ���� �� ����� ���������, �������� ����� �������
                    {
                        if (LogicChecker.IsCanMove(i + iDelta, j + jDelta))
                        {
                            if (LogicChecker.IsRival(_board, _turn, i + iDelta, j + jDelta))
                            {
                                if (LogicChecker.IsCanMove(i + 2 * iDelta, j + 2 * jDelta))
                                {
                                    if (_board[i + 2 * iDelta, j + 2 * jDelta] == -1)
                                    {
                                        chopIndexes.Add(new List<int> { i, j, 2 * iDelta, 2 * jDelta, i + iDelta, j + jDelta });
                                    }
                                }
                            }
                        }
                    }
                }
                else if (_board[i, j] == _turn % 2 + 2) // ����� �������� ������
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
                                if (LogicChecker.IsRival(_board, _turn, i + moveLength * iDelta, j + moveLength * jDelta))
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
                                else if (_board[i + moveLength * iDelta, j + moveLength * jDelta] == -1)
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

    private void TryChop(int i, int j, out List<List<int>> chopIndexes)
    {
        chopIndexes = new List<List<int>>();

        if (_board[i, j] == _turn % 2) // ����� ������
        {
            foreach (int iDelta in _directions) // ��������, ���� �� ���������, �������� ����� �������
            {
                foreach (int jDelta in _directions)
                {
                    if (LogicChecker.IsCanMove(i + iDelta, j + jDelta))
                    {
                        if (LogicChecker.IsRival(_board, _turn, i + iDelta, j + jDelta))
                        {
                            if (LogicChecker.IsCanMove(i + 2 * iDelta, j + 2 * jDelta))
                            {
                                if (_board[i + 2 * iDelta, j + 2 * jDelta] == -1)
                                {
                                    chopIndexes.Add(new List<int> { i, j, 2 * iDelta, 2 * jDelta, i + iDelta, j + jDelta });
                                }
                            }
                        }
                    }
                }
            }
        }
        else if (_board[i, j] == _turn % 2 + 2) // ����� �����
        {
            foreach (int iDelta in _directions) // ��������, ���� �� ����� ���������, �������� ����� �������
            {
                foreach (int jDelta in _directions)
                {
                    List<int> rivalIndexes = new();

                    int moveLength = 1;
                    int rivalCount = 0;

                    while (LogicChecker.IsCanMove(i + moveLength * iDelta, j + moveLength * jDelta))
                    {
                        if (LogicChecker.IsRival(_board, _turn, i + moveLength * iDelta, j + moveLength * jDelta))
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
                        else if (_board[i + moveLength * iDelta, j + moveLength * jDelta] == -1)
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
        List<int> inputStartPosition = turnIndexes[0].GetRange(0, 2); // ���������� ��������� ������� �� �������� ����
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


    private List<int> GetAIMove(List<List<int>> turnIndexes)
    {
        int randomIndex = UnityEngine.Random.Range(0, turnIndexes.Count);
        List<int> randomTurn = turnIndexes[randomIndex];

        return randomTurn;
    }

    private async UniTask WaitUntilMoved(List<int> moveIndex)
    {
        if (FigureMoved != null)
        {
            IEnumerable<Func<List<int>, UniTask>> invocationList = FigureMoved
                .GetInvocationList()
                .Cast<Func<List<int>, UniTask>>();
            List<UniTask> tasks = new();

            foreach (var handler in invocationList)
                tasks.Add(handler.Invoke(moveIndex));

            await UniTask.WhenAll(tasks);
        }
    }

    private async UniTask WaitUntilDamCreated()
    {
        if (DamCreated != null)
        {
            IEnumerable<Func<UniTask>> invocationList = DamCreated
                .GetInvocationList()
                .Cast<Func<UniTask>>();
            List<UniTask> tasks = new();

            foreach (var handler in invocationList)
                tasks.Add(handler.Invoke());

            await UniTask.WhenAll(tasks);
        }
    }

    private async UniTask MakeMove(List<int> moveIndex)
    {
        await WaitUntilMoved(moveIndex);

        var (i, j, iDelta, jDelta) = (moveIndex[0], moveIndex[1], moveIndex[2], moveIndex[3]);
        int oppositeBoardSide = _turn % 2 == 0 ? 7 : 0;

        if (_board[i, j] == _turn % 2)
        {
            if (j + jDelta == oppositeBoardSide)
            {
                _board[i + iDelta, j + jDelta] = _turn % 2 + 2;
                await WaitUntilDamCreated();
            }
            else
            {
                _board[i + iDelta, j + jDelta] = _turn % 2;
            }
        }
        else if (_board[i, j] == _turn % 2 + 2)
        {
            _board[i + iDelta, j + jDelta] = _turn % 2 + 2;
        }

        _board[i, j] = -1;
    }

    private async UniTask<(int, int)> MakeChopMove(List<int> moveIndex)
    {
        var (i, j, iDelta, jDelta, rivalI, rivalJ) =
            (moveIndex[0], moveIndex[1], moveIndex[2], moveIndex[3], moveIndex[4], moveIndex[5]);

        _board[rivalI, rivalJ] = -1;

        Vector3 startPosition = CoordinateTranslator.Indexes2Position(i, j);
        Vector3 rivalPosition = CoordinateTranslator.Indexes2Position(rivalI, rivalJ);
        float distance = Vector3.Distance(startPosition, rivalPosition);
        float chopDelay = (distance / _moveSpeed);

        FigureChopped?.Invoke(moveIndex, chopDelay);

        await WaitUntilMoved(moveIndex);

        int oppositeBoardSide = _turn % 2 == 0 ? 7 : 0;

        if (_board[i, j] == _turn % 2)
        {
            if (j + jDelta == oppositeBoardSide)
            {
                _board[i + iDelta, j + jDelta] = _turn % 2 + 2;
                await WaitUntilDamCreated();
            }
            else
            {
                _board[i + iDelta, j + jDelta] = _turn % 2;
            }
        }
        else if (_board[i, j] == _turn % 2 + 2)
        {
            _board[i + iDelta, j + jDelta] = _turn % 2 + 2;
        }

        _board[i, j] = -1;
        _figureCounts[RivalIndex]--;

        return (i + iDelta, j + jDelta);
    }

    private async UniTask Win(int winnerTurn)
    {
        var token = _cts.Token;

        if (GameEnding != null)
        {
            var invocationList = GameEnding.GetInvocationList().Cast<Func<int, float, CancellationToken, UniTask>>();
            List<UniTask> tasks = new();

            foreach (var handler in invocationList)
                tasks.Add(handler.Invoke(winnerTurn, _gameEndingDuration, token));

            await UniTask.WhenAll(tasks);
        }

        SceneManager.LoadScene(0);
    }
}
