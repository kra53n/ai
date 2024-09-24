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

partial class Map
{
    public byte GetCell(int row, int col, List<Block> boxes)
    {
        foreach (var box in boxes)
        {
            if (box.x == col && box.y == row)
            {
                return (byte)Block.Type.Box;
            }
        }
        return map[row, col];
    }
}

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
            var block = Sokoban.map.GetCell(wRowNew, wColNew, b);
            if (block != (byte)Block.Type.Floor && block != (byte)Block.Type.Mark)
            {
                continue;
            }
            w.x = wColNew;
            w.y = wRowNew;
            yield return new State(b, w);
            
            if (Sokoban.map.GetCell(checkBoxRow, checkBoxCol, b) != (byte)Block.Type.Box)
            {
                continue;
            }
            b = Block.CloneBlocks(b);
            w = (Worker)w.Clone();
            foreach (var box in b)
            {
                if (box.x == checkBoxCol && box.y == checkBoxRow)
                {
                    box.y = (byte)(w.y - direction.GetY());
                    box.x = (byte)(w.x - direction.GetX());
                    break;
                }
            }
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

        openNodes = [new State(Sokoban.baseState.boxes, Sokoban.baseState.worker)];
        openNodesReversed = [.. GenerateFinalStates()];
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

    private IEnumerable<State> GenerateFinalStates()
    {
        List<Block> boxes = new();

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
                if (Sokoban.map.GetCell(checkFreeRow, checkFreeCol, boxes) == (byte)Block.Type.Floor)
                {
                    Worker w = (Worker)Sokoban.worker.Clone();
                    w.x = checkFreeCol;
                    w.y = checkFreeRow;
                    yield return new State(Block.CloneBlocks(boxes), w);
                }
            }
        }
    }
}
