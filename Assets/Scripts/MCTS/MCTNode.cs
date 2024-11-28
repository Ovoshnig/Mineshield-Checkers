using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MCTNode
{
    private const float ExplorationFactor = 1.41f;

    public int[,] Board { get; }
    public int Player { get; }
    public List<int> Move { get; }
    public MCTNode Parent { get; }
    public List<MCTNode> Children { get; } = new();
    public int VisitCount { get; set; }
    public int WinCount { get; set; }
    private readonly CheckersLogic _checkersLogic;

    public bool IsLeaf => Children.Count == 0;

    public MCTNode(int[,] board, int player, List<int> move = null, CheckersLogic checkersLogic = null, MCTNode parent = null)
    {
        Board = (int[,])board.Clone();
        Player = player;
        Move = move;
        Parent = parent;
        _checkersLogic = checkersLogic;

        if (move != null && checkersLogic != null)
        {
            var figureCounts = (int[])checkersLogic.FigureCounts.Clone();
            checkersLogic.SimulateMove(Board, figureCounts, move, false);
        }
    }

    public void AddChild(MCTNode child) => Children.Add(child);

    public MCTNode GetChildWithBestUCB1()
    {
        return Children.OrderByDescending(c =>
            (float)c.WinCount / (c.VisitCount + 1e-6f) +
            ExplorationFactor * Mathf.Sqrt(Mathf.Log(VisitCount + 1) / (c.VisitCount + 1e-6f))
        ).First();
    }

    public MCTNode GetBestChild()
    {
        return Children.OrderByDescending(c => (float)c.WinCount / (c.VisitCount + 1e-6f)).First();
    }
}
