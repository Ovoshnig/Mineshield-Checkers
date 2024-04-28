using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CheckersVisualizer))]
[RequireComponent(typeof(AudioPlayer))]
[RequireComponent(typeof(PlayerInput))]
public class CheckersLogic : MonoBehaviour
{
    [SerializeField] private int _placementDelay;
    [SerializeField] private int _gameEndingDuration;

    public delegate UniTaskVoid FigurePlaced(int i, int j, int playerIndex);
    public delegate UniTaskVoid GameEnded(int winnerTurn, int gameEndingDuration);

    public static event FigurePlaced FigurePlacedEvent;
    public static event GameEnded GameEndedEvent;

    private List<int> _inputStartPosition;

    private CheckersVisualizer _visualizer;
    private AudioPlayer _audioPlayer;
    private PlayerInput _playerInput;

    private readonly int[,] _board = new int[8, 8];
    private readonly int[] _figureCounts = { 12, 12 };

    private readonly int[] _directions = { -1, 1 };

    private int _turn = 1;

    private void Awake()
    {
        _visualizer = GetComponent<CheckersVisualizer>();
        _audioPlayer = GetComponent<AudioPlayer>();
        _playerInput = GetComponent<PlayerInput>();
    }

    private void Start()
    {
        StartPlacement().Forget();
    }

    private async UniTaskVoid StartPlacement()
    {
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                _board[i, j] = 0;

        foreach (int delta in new int[] { 0, 5 })
        {
            int playerIndex = delta == 0 ? 0 : 1;

            for (int i = 0; i < 8; i++)
            {
                for (int j = delta; j < 3 + delta; j++)
                {
                    if (i % 2 == j % 2)
                    {
                        _board[i, j] = delta == 0 ? 1 : 2;
                        FigurePlacedEvent?.Invoke(i, j, playerIndex);
                    }

                    await UniTask.Delay(_placementDelay);
                }
            }
        }

        //EnumerateMoves();
        Win(1).Forget();
    }

    private bool IsCanMove(int i, int j)
    {
        return (i is > -1 and < 8) && 
               (j is > -1 and < 8);
    }

    private bool IsRival(int i, int j)
    {
        int rivalFigure;
        int rivalDam;

        if (_turn == 1)
        {
            rivalFigure = 2;
            rivalDam = 4;
        }
        else
        {
            rivalFigure = 1;
            rivalDam = 3;
        }

        return (_board[i, j] == rivalFigure ||
                _board[i, j] == rivalDam);
    }

    private void EnumerateMoves()
    {
        int zForwardCoefficient = _turn == 1 ? 1 : -1;

        List<List<int>> chopIndexes = new();
        List<List<int>> moveIndexes = new();

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (_board[i, j] == _turn) // Фигура текущего игрока
                {
                    int jDelta = zForwardCoefficient;

                    foreach (int iDelta in _directions) // Проверки всех вариантов ходов вперёд
                    {
                        if (IsCanMove(i + iDelta, j + jDelta))
                        {
                            if (IsRival(i + iDelta, j + jDelta))
                            {
                                if (IsCanMove(i + 2 * iDelta, j + 2 * jDelta) &&
                                       _board[i + 2 * iDelta, j + 2 * jDelta] == 0)
                                {
                                    chopIndexes.Add(new List<int> { i, j, 2 * iDelta, 2 * jDelta, i + iDelta, j + jDelta });
                                }
                            }
                            else if (_board[i + iDelta, j + jDelta] == 0)
                            {
                                moveIndexes.Add(new List<int> { i, j, iDelta, jDelta });
                            }
                        }
                    }

                    jDelta = -jDelta;

                    foreach (int iDelta in _directions) // Проверки, есть ли сзади противник, которого можно срубить
                    {
                        if (IsCanMove(i + iDelta, j + jDelta) &&
                              IsRival(i + iDelta, j + jDelta))
                        {
                            if (IsCanMove(i + 2 * iDelta, j + 2 * jDelta) &&
                                   _board[i + 2 * iDelta, j + 2 * jDelta] == 0)
                            {
                                chopIndexes.Add(new List<int> { i, j, 2 * iDelta, 2 * jDelta, i + iDelta, j + jDelta });
                            }
                        }
                    }
                }
                else if (_board[i, j] == _turn + 2) // Дамка текущего игрока
                {
                    foreach (int iDelta in _directions)
                    {
                        foreach (int jDelta in _directions)
                        {
                            List<int> rivalIndexes = new();

                            int moveLength = 1;
                            int rivalCount = 0;

                            while (IsCanMove(i + moveLength * iDelta, j + moveLength * jDelta))
                            {
                                if (IsRival(i + moveLength * iDelta, j + moveLength * jDelta))
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
                                else if (_board[i + moveLength * iDelta, j + moveLength * jDelta] == 0)
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

        // Принятие решения хода
        if (chopIndexes.Count > 0)
        {
            if (_turn == 1)
                WaitPlayerInput(chopIndexes, true).Forget();
            else
                SelectRandomMove(chopIndexes, true);
        }
        else if (moveIndexes.Count > 0)
        {
            if (_turn == 1)
                WaitPlayerInput(moveIndexes, false).Forget();
            else
                SelectRandomMove(moveIndexes, false);
        }
        else
        {
            int winnerTurn = _turn == 1 ? 2 : 1;
            Win(winnerTurn).Forget();
        }
    }

    private void TryChop(int i, int j)
    {
        List<List<int>> chopIndexes = new();

        if (_board[i, j] == _turn) // Ходит фигура
        {
            foreach (int iDelta in _directions) // Проверки, есть ли противник, которого можно срубить
            {
                foreach (int jDelta in _directions)
                {
                    if (IsCanMove(i + iDelta, j + jDelta) &&
                          IsRival(i + iDelta, j + jDelta) &&
                        IsCanMove(i + 2 * iDelta, j + 2 * jDelta) &&
                           _board[i + 2 * iDelta, j + 2 * jDelta] == 0)
                    {
                        chopIndexes.Add(new List<int> { i, j, 2 * iDelta, 2 * jDelta, i + iDelta, j + jDelta });
                    }
                }
            }
        }
        else if (_board[i, j] == _turn + 2) // Ходит дамка
        {
            foreach (int iDelta in _directions) // Проверки, есть ли сзади противник, которого можно срубить
            {
                foreach (int jDelta in _directions)
                {
                    List<int> rivalIndexes = new();

                    int moveLength = 1;
                    int rivalCount = 0;

                    while (IsCanMove(i + moveLength * iDelta, j + moveLength * jDelta))
                    {
                        if (IsRival(i + moveLength * iDelta, j + moveLength * jDelta))
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
                        else if (_board[i + moveLength * iDelta, j + moveLength * jDelta] == 0)
                        {
                            if (rivalCount == 1)
                            {
                                chopIndexes.Add(new List<int> { i, j, moveLength * iDelta, moveLength * jDelta, rivalIndexes[0], rivalIndexes[1] });
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

        // Принятие решения хода
        if (chopIndexes.Count > 0)
        {
            if (_turn == 1)
            {
                _inputStartPosition = new List<int>() { i, j };
                WaitPlayerChopInput(chopIndexes).Forget();
            }
            else
            {
                SelectRandomMove(chopIndexes, true);
            }
        }
        else
        {
            _turn = _turn == 1 ? 2 : 1;
            EnumerateMoves();
        }
    }

    private async UniTaskVoid WaitPlayerInput(List<List<int>> turnIndexes, bool isChopping = false)
    {
        List<int> inputPosition;
        _inputStartPosition = new();

        bool isStartPositionReceived = false;

        while (true)
        {
            inputPosition = new();

            await _playerInput.GetPlayerInput(inputPosition);

            if (turnIndexes.Find(x => x.GetRange(0, 2).SequenceEqual(inputPosition)) != null)
            {
                _inputStartPosition = new List<int>(inputPosition);
                isStartPositionReceived = true;

                _visualizer.SetSelection(_inputStartPosition);
            }
            else if (isStartPositionReceived)
            {
                var (iStart, jStart) = (_inputStartPosition[0], _inputStartPosition[1]);
                var (iEnd, jEnd) = (inputPosition[0], inputPosition[1]);
                int iDelta = iEnd - iStart;
                int jDelta = jEnd - jStart;
                List<int> playerIndexes = new() { iStart, jStart, iDelta, jDelta };

                var finding = turnIndexes.Find(x => x.GetRange(0, 4).SequenceEqual(playerIndexes));

                if (finding != null)
                {
                    if (isChopping)
                        MakeChopMove(finding).Forget();
                    else
                        MakeMove(finding).Forget();

                    _visualizer.RemoveSelection();

                    return;
                }
                else
                {
                    isStartPositionReceived = false;
                    _inputStartPosition = new();

                    _visualizer.RemoveSelection();
                }
            }
        }
    }

    private async UniTaskVoid WaitPlayerChopInput(List<List<int>> turnIndexes)
    {
        _visualizer.SetSelection(_inputStartPosition);

        List<int> inputEndPosition = new();
        List<int> finding = null;

        while (finding == null)
        {
            inputEndPosition = new();

            await _playerInput.GetPlayerInput(inputEndPosition);

            var (iStart, jStart) = (_inputStartPosition[0], _inputStartPosition[1]);
            var (iEnd, jEnd) = (inputEndPosition[0], inputEndPosition[1]);
            int iDelta = iEnd - iStart;
            int jDelta = jEnd - jStart;
            List<int> playerIndexes = new() { iDelta, jDelta };

            finding = turnIndexes.Find(x => x.GetRange(2, 2).SequenceEqual(playerIndexes));
        }

        MakeChopMove(finding).Forget();

        _visualizer.RemoveSelection();
    }

    private void SelectRandomMove(List<List<int>> turnIndexes, bool isChopping = false)
    {
        int randomIndex = UnityEngine.Random.Range(0, turnIndexes.Count);
        List<int> randomTurn = turnIndexes[randomIndex];

        if (isChopping)
            MakeChopMove(randomTurn).Forget();
        else
            MakeMove(randomTurn).Forget();
    }

    private async UniTaskVoid MakeMove(List<int> turnIndex)
    {
        var (i, j, iDelta, jDelta) = (turnIndex[0], turnIndex[1], turnIndex[2], turnIndex[3]);

        _audioPlayer.PlayMoveSound().Forget();
        await _visualizer.Move(turnIndex);

        int oppositeBoardSide = _turn == 1 ? 7 : 0;

        if (_board[i, j] == _turn)
        {
            if (j + jDelta == oppositeBoardSide)
            {
                _board[i + iDelta, j + jDelta] = _turn + 2;

                _visualizer.CreateDam();
            }
            else
            {
                _board[i + iDelta, j + jDelta] = _turn;
            }
        }
        else if (_board[i, j] == _turn + 2)
        {
            _board[i + iDelta, j + jDelta] = _turn + 2;
        }

        _board[i, j] = 0;

        _turn = _turn == 1 ? 2 : 1;
        EnumerateMoves();
    }

    private async UniTaskVoid MakeChopMove(List<int> turnIndex)
    {
        var (i, j, iDelta, jDelta, rivalI, rivalJ) = 
            (turnIndex[0], turnIndex[1], turnIndex[2], turnIndex[3], turnIndex[4], turnIndex[5]);

        _board[rivalI, rivalJ] = 0;

        _audioPlayer.PlayMoveSound().Forget();

        _visualizer.Chop(rivalI, rivalJ).Forget();
        await _visualizer.Move(turnIndex);

        int oppositeBoardSide = _turn == 1 ? 7 : 0;

        if (_board[i, j] == _turn)
        {
            if (j + jDelta == oppositeBoardSide)
            {
                _board[i + iDelta, j + jDelta] = _turn + 2;

                _visualizer.CreateDam();
            }
            else
            {
                _board[i + iDelta, j + jDelta] = _turn;
            }
        }
        else if (_board[i, j] == _turn + 2)
        {
            _board[i + iDelta, j + jDelta] = _turn + 2;
        }

        _board[i, j] = 0;

        int rivalIndex = _turn == 1 ? 1 : 0;

        _figureCounts[rivalIndex]--;

        if (_figureCounts[rivalIndex] == 0)
            Win(_turn).Forget();
        else
            TryChop(i + iDelta, j + jDelta);
    }

    private async UniTaskVoid Win(int winnerTurn)
    {
        GameEndedEvent?.Invoke(winnerTurn, _gameEndingDuration);

        await UniTask.Delay(_gameEndingDuration);
        SceneManager.LoadScene(0);
    }
}
