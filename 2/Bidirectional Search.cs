using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;


partial class State
{
    public IEnumerable<State> GenerateReversedStates()
    {
        foreach (Worker.Direction direction in Worker.directions)
        {
            var b = Block.CloneBlocks(boxes);
            Worker w = (Worker)worker.Clone();
            var wRowNew = w.y + direction.GetY();
            var wColNew = w.x + direction.GetX();
            var checkBoxRow = w.y - direction.GetY();
            var checkBoxCol = w.x - direction.GetX();
            if (Sokoban.map.GetCell(wRowNew, wColNew) != (byte)Block.Type.Floor && Sokoban.map.GetCell(wRowNew, wColNew) != (byte)Block.Type.Mark)
            {
                continue;
            }
            w.Move(direction, b);
            yield return new State(b, w);
            b = Block.CloneBlocks(boxes);
            w = (Worker)w.Clone();
            if (Sokoban.map.GetCell(checkBoxRow, checkBoxCol) == (byte)Block.Type.BoxOnMark)
            {
                Sokoban.map.SetCell(checkBoxRow, checkBoxCol, (byte)Block.Type.Mark);
            }
            else if (Sokoban.map.GetCell(checkBoxRow, checkBoxCol) == (byte)Block.Type.Box)
            {
                Sokoban.map.SetCell(checkBoxRow, checkBoxCol, (byte)Block.Type.Floor);
            }
            else
            {
                continue;
            }
            Sokoban.map.SetCell(w.y - direction.GetY(), w.x - direction.GetX(), (byte)Block.Type.Box);
            yield return new State(b, w);
        }
    }
}


class BidirectionalSearch : ISearcher<List<State>>
{
    private Statistic? statistic;
    private HashSet<State>? openNodes;
    private HashSet<State>? openNodesReversed;
    private HashSet<State>? closedNodes;
    private HashSet<State>? closedNodesReversed;

    public BidirectionalSearch()
    {
        statistic = new();

        openNodes = new();
        openNodes.Add(new State(Sokoban.baseState.boxes, Sokoban.baseState.worker));
        openNodesReversed = new();
        GenerateFinalStates(Sokoban.baseState.boxes, Sokoban.baseState.worker).ForEach(i => openNodesReversed.Add(i));
        closedNodes = new();
        closedNodesReversed = new();
    }

    private List<State>? NormalIteration()
    {
        HashSet<State> newO = new();
        foreach (var state in openNodes)
        {
            //statistic.Collect(state, openNodes, closedNodes);

            closedNodes.Add(state);
            foreach (State s in state.GetGeneratedStates())
            {
                openNodesReversed.TryGetValue(s, out var item);
                //if (item == null) { item = closedNodesReversed.GetItem(s); }
                if (item != null)
                {
                    statistic.Print(Searcher.Type.Bidirectional);
                    List<State> l = item.Unwrap();
                    l.Reverse();
                    var res = state.Unwrap();
                    res.AddRange(l);
                    return res;
                }
                if (!openNodes.Contains(s) && !closedNodes.Contains(s))
                {
                    s.prv = state;
                    newO.Add(s);
                }
            }
        }
        openNodes = newO;
        return null;
    }

    private List<State>? ReversedIteration()
    {
        HashSet<State> newO = new();
        foreach (var state in openNodesReversed)
        {
            //statistic.Collect(state, openNodesReversed, closedNodesReversed);

            closedNodesReversed.Add(state);
            foreach (State s in state.GenerateReversedStates())
            {
                //if (item == null) {
                //    item = closedNodes.GetItem(s);
                //}
                openNodes.TryGetValue(s, out var item);
                //if (item == null) { item = closedNodes.GetItem(s); }
                if (item != null)
                {
                    statistic.Print(Searcher.Type.Bidirectional);
                    List<State> l = state.Unwrap();
                    l.Reverse();
                    var res = item.Unwrap();
                    res.AddRange(l);
                    return res;
                }
                if (!openNodesReversed.Contains(s) && !closedNodesReversed.Contains(s))
                {
                    s.prv = state;
                    newO.Add(s);
                }
            }
        }
        openNodesReversed = newO;
        return null;
    }

    public List<State>? Search()
    {
        while (true)
        {
            List<State>? result = null;
            if (openNodes.Count < openNodesReversed.Count)
            {
                result = NormalIteration();
            }
            else
            {
                result = ReversedIteration();
            }

            Console.Clear();
            Console.WriteLine($"On.count = {openNodes.Count()}");
            Console.WriteLine($"Or.count = {openNodesReversed.Count()}");
            Console.WriteLine($"Cn.count = {closedNodes.Count()}");
            Console.WriteLine($"Cr.count = {closedNodesReversed.Count()}");


            if (result != null)
            {
                return result;
            }
        }

    }

    private List<State> GenerateFinalStates(List<Block> _boxes, Worker worker)
    {
        List<Block> boxes = new();
        var states = new List<State>();

        foreach ((int col, int row) in Sokoban.map.FindBlocks(Block.Type.Mark))
        {
            boxes.Add(new Block(col, row, Block.Type.Box));
        }

        foreach (Block b in boxes)
        {
            foreach (Worker.Direction direction in Worker.directions)
            {
                var checkFreeRow = b.y + direction.GetY();
                var checkFreeCol = b.x + direction.GetX();
                if (Sokoban.map.GetCell(checkFreeRow, checkFreeCol) == (byte)Block.Type.Floor)
                {
                    Worker w = (Worker)worker.Clone();
                    w.x = checkFreeCol;
                    w.y = checkFreeRow;
                    states.Add(new State(Block.CloneBlocks(boxes), w));
                }
            }
        }
        return states;
    }
}
