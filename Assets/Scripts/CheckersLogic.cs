using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CheckersVisualizer))]
[RequireComponent(typeof(AudioPlayer))]
[RequireComponent(typeof(PlayerInput))]
public class CheckersLogic : MonoBehaviour
{
    [SerializeField] private float _placementDelay;
    [SerializeField] private float _gameEndingDuration;

    public delegate void FigurePlaced(int i, int j, int playerIndex);
    public static event FigurePlaced FigurePlacedEvent;

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
        StartCoroutine(StartPlacement());
    }

    public IEnumerator StartPlacement()
    {
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                _board[i, j] = 0;

        WaitForSeconds waitPlacementDelay = new(_placementDelay);

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

                    yield return waitPlacementDelay;
                }
            }
        }

        StartCoroutine(EnumerateMoves());
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

    private IEnumerator EnumerateMoves()
    {
        int zForwardCoefficient = _turn == 1 ? 1 : -1;

        List<List<int>> chopIndexes = new();
        List<List<int>> moveIndexes = new();

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (_board[i, j] == _turn) // ������ �������� ������
                {
                    int jDelta = zForwardCoefficient;

                    foreach (int iDelta in _directions) // �������� ���� ��������� ����� �����
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

                    foreach (int iDelta in _directions) // ��������, ���� �� ����� ���������, �������� ����� �������
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
                else if (_board[i, j] == _turn + 2) // ����� �������� ������
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

        yield return null;

        // �������� ������� ����
        if (chopIndexes.Count > 0)
        {
            if (_turn == 1)
                StartCoroutine(WaitPlayerInput(chopIndexes, true));
            else
                SelectRandomMove(chopIndexes, true);
        }
        else if (moveIndexes.Count > 0)
        {
            if (_turn == 1)
                StartCoroutine(WaitPlayerInput(moveIndexes, false));
            else
                SelectRandomMove(moveIndexes, false);
        }
        else
        {
            int winnerTurn = _turn == 1 ? 2 : 1;
            StartCoroutine(Win(winnerTurn));
        }
    }

    private IEnumerator TryChop(int i, int j)
    {
        List<List<int>> chopIndexes = new();

        if (_board[i, j] == _turn) // ����� ������
        {
            foreach (int iDelta in _directions) // ��������, ���� �� ����� ���������, �������� ����� �������
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
        else if (_board[i, j] == _turn + 2) // ����� �����
        {
            foreach (int iDelta in _directions) // ��������, ���� �� ����� ���������, �������� ����� �������
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

        yield return null;

        // �������� ������� ����
        if (chopIndexes.Count > 0)
        {
            if (_turn == 1)
            {
                _inputStartPosition = new List<int>() { i, j };
                StartCoroutine(WaitPlayerContinueInput(chopIndexes));
            }
            else
            {
                SelectRandomMove(chopIndexes, true);
            }
        }
        else
        {
            _turn = _turn == 1 ? 2 : 1;
            StartCoroutine(EnumerateMoves());
        }
    }

    private IEnumerator WaitPlayerInput(List<List<int>> turnIndexes, bool isChoping = false)
    {
        List<int> inputPosition;
        _inputStartPosition = new();

        bool isStartPositionReceived = false;

        while (true)
        {
            inputPosition = new();

            yield return StartCoroutine(_playerInput.GetPlayerInput(inputPosition));

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
                    if (isChoping)
                        StartCoroutine(MakeChopMove(finding));
                    else
                        StartCoroutine(MakeMove(finding));

                    _visualizer.RemoveSelection();

                    yield break;
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

    private IEnumerator WaitPlayerContinueInput(List<List<int>> turnIndexes)
    {
        _visualizer.SetSelection(_inputStartPosition);

        List<int> inputEndPosition = new();
        List<int> finding = null;

        while (finding == null)
        {
            inputEndPosition = new();

            yield return StartCoroutine(_playerInput.GetPlayerInput(inputEndPosition));

            var (iStart, jStart) = (_inputStartPosition[0], _inputStartPosition[1]);
            var (iEnd, jEnd) = (inputEndPosition[0], inputEndPosition[1]);
            int iDelta = iEnd - iStart;
            int jDelta = jEnd - jStart;
            List<int> playerIndexes = new() { iDelta, jDelta };

            finding = turnIndexes.Find(x => x.GetRange(2, 2).SequenceEqual(playerIndexes));
        }

        StartCoroutine(MakeChopMove(finding));

        _visualizer.RemoveSelection();
    }

    private void SelectRandomMove(List<List<int>> turnIndexes, bool isChoping = false)
    {
        int randomIndex = UnityEngine.Random.Range(0, turnIndexes.Count);
        List<int> randomTurn = turnIndexes[randomIndex];

        if (isChoping)
            StartCoroutine(MakeChopMove(randomTurn));
        else
            StartCoroutine(MakeMove(randomTurn));
    }

    private IEnumerator MakeMove(List<int> turnIndex)
    {
        var (i, j, iDelta, jDelta) = (turnIndex[0], turnIndex[1], turnIndex[2], turnIndex[3]);

        _audioPlayer.PlayMoveSound();
        yield return StartCoroutine(_visualizer.MoveFigure(turnIndex));

        int oppositeBoardSide = _turn == 1 ? 7 : 0;

        if (_board[i, j] == _turn)
        {
            if (j + jDelta == oppositeBoardSide)
            {
                _board[i + iDelta, j + jDelta] = _turn + 2;

                _visualizer.CreateDam();
                yield return null;
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
        StartCoroutine(EnumerateMoves());
    }

    private IEnumerator MakeChopMove(List<int> turnIndex)
    {
        var (i, j, iDelta, jDelta, rivalI, rivalJ) = 
            (turnIndex[0], turnIndex[1], turnIndex[2], turnIndex[3], turnIndex[4], turnIndex[5]);

        var moveFigureRoutine = StartCoroutine(_visualizer.MoveFigure(turnIndex));
        _audioPlayer.PlayMoveSound();

        _board[rivalI, rivalJ] = 0;
        _visualizer.ChopFigure(rivalI, rivalJ);

        yield return moveFigureRoutine;

        int oppositeBoardSide = _turn == 1 ? 7 : 0;

        if (_board[i, j] == _turn)
        {
            if (j + jDelta == oppositeBoardSide)
            {
                _board[i + iDelta, j + jDelta] = _turn + 2;

                _visualizer.CreateDam();
                yield return null;
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
        {
            yield return moveFigureRoutine;

            StartCoroutine(Win(_turn));
        }
        else
        {
            StartCoroutine(TryChop(i + iDelta, j + jDelta));
        }
    }

    private IEnumerator Win(int winnerTurn)
    {
        _audioPlayer.PlayGameEndingSound(winnerTurn);

        float startDelay;

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (_board[i, j] == winnerTurn)
                {
                    startDelay = UnityEngine.Random.Range(0, _gameEndingDuration);

                    StartCoroutine(_visualizer.PlayFigureAnimation(i, j, startDelay));
                }
            }
        }

        yield return new WaitForSeconds(_gameEndingDuration);
        SceneManager.LoadScene("Figure choosing");
    }
}