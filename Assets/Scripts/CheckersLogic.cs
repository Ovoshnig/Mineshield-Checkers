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

    private GameBoard _gameBoard;
    private int[] _figureCounts = { 12, 12 };
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
        _gameBoard = new GameBoard();

        _botAlgorithm = _botAlgorithmType switch
        {
            "Minimax" => new MinimaxBot(),
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
        _gameBoard.InitializeBoard();
        _gameBoard.SetUpStartingPositions();

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                int piece = _gameBoard.Board[i, j];
                if (piece != -1)
                {
                    int playerIndex = piece % 2;
                    FigurePlaced?.Invoke(i, j, playerIndex);
                    await UniTask.WaitForSeconds(_placementDelay);
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
            _gameBoard.EnumerateMoves(_turn, out chopIndexes, out moveIndexes);

            if (chopIndexes.Count == 0 && moveIndexes.Count == 0)
            {
                winnerTurn = _turn + 1;
                break;
            }

            List<List<int>> indexes = chopIndexes.Count > 0 ? chopIndexes : moveIndexes;

            if (_turn % 2 == 0)
                moveIndex = await GetPlayerMove(indexes, chopIndexes.Count > 0);
            else
                moveIndex = await GetAIMove(indexes);

            if (chopIndexes.Count > 0)
            {
                while (true)
                {
                    await MakeMove(moveIndex);

                    if (_figureCounts[RivalIndex] == 0)
                    {
                        winnerTurn = _turn;
                        break;
                    }
                    else
                    {
                        int i = moveIndex[0] + moveIndex[2];
                        int j = moveIndex[1] + moveIndex[3];
                        _gameBoard.TryChop(_turn, i, j, out chopIndexes);

                        if (chopIndexes.Count > 0)
                        {
                            if (_turn % 2 == 0)
                                moveIndex = await GetPlayerMove(chopIndexes, true);
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
                await MakeMove(moveIndex);
            }

            _turn++;
        }

        return winnerTurn;
    }

    private async UniTask<List<int>> GetPlayerMove(List<List<int>> validMoves, bool isChopMove)
    {
        List<int> inputStartPosition = null;

        if (isChopMove)
        {
            inputStartPosition = validMoves[0].GetRange(0, 2);
            FigureSelected?.Invoke(inputStartPosition, true);
        }

        List<int> finding = null;

        void OnClick(Vector2 mousePosition)
        {
            Vector3 hitPoint = Vector3.zero;
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                hitPoint = hit.point;
                (int i, int j) = CoordinateTranslator.Position2Indexes(hitPoint);

                List<int> inputPosition = new() { i, j };

                if (inputStartPosition == null)
                {
                    if (validMoves.Any(x => x.GetRange(0, 2).SequenceEqual(inputPosition)))
                    {
                        inputStartPosition = new List<int>(inputPosition);
                        FigureSelected?.Invoke(inputStartPosition, true);
                    }
                }
                else
                {
                    var (iStart, jStart) = (inputStartPosition[0], inputStartPosition[1]);
                    var (iEnd, jEnd) = (i, j);
                    int iDelta = iEnd - iStart;
                    int jDelta = jEnd - jStart;

                    List<int> playerIndexes = new() { iStart, jStart, iDelta, jDelta };
                    finding = validMoves.Find(x => x.GetRange(0, 4).SequenceEqual(playerIndexes));

                    if (finding == null)
                    {
                        FigureSelected?.Invoke(inputStartPosition, false);
                        inputStartPosition = null;
                    }
                    else
                    {
                        FigureSelected?.Invoke(inputStartPosition, false);
                    }
                }
            }
            else
            {
                if (inputStartPosition != null)
                {
                    FigureSelected?.Invoke(inputStartPosition, false);
                    inputStartPosition = null;
                }
            }
        }

        _playerInput.ClickPerformed += OnClick;

        while (finding == null)
            await UniTask.Yield();

        _playerInput.ClickPerformed -= OnClick;

        return finding;
    }

    private async UniTask<List<int>> GetAIMove(List<List<int>> allowedMoves)
    {
        using CancellationTokenSource cts = new();

        return await _botAlgorithm.GetMoveAsync(_gameBoard.CloneBoard(), _turn % 2, allowedMoves, cts.Token);
    }

    public async UniTask MakeMove(List<int> move)
    {
        bool isChop = move.Count >= 6;

        if (isChop)
            FigureChopped?.Invoke(move);

        bool promoted = _gameBoard.ApplyMove(move, _turn, ref _figureCounts);

        await FigureMoved.InvokeAndWaitAsync(move);

        int i = move[0] + move[2];
        int j = move[1] + move[3];

        if (promoted)
            await DamCreated.InvokeAndWaitAsync(i, j);
    }

    private async UniTask Win(int winnerTurn, CancellationToken token)
    {
        await GameEnding.InvokeAndWaitAsync(winnerTurn, _gameEndingDuration, token);

        SceneManager.LoadScene(0);
    }
}