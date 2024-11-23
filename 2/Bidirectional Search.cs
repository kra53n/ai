using Microsoft.VisualBasic;
using Raylib_cs;
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
    public byte GetCell(int row, int col, (byte x, byte y)[] boxes)
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

public partial class State
{
    public IEnumerable<State> GenerateReversedStates()
    {
        foreach (Worker.Direction direction in Worker.directions)
        {
            var b = ((byte x, byte y)[])boxes.Clone();
            Worker w = (Worker)worker.Clone();
            var wRowNew = w.y + direction.GetY();
            var wColNew = w.x + direction.GetX();
            var checkBoxRow = w.y - direction.GetY();
            var checkBoxCol = w.x - direction.GetX();
            var block = map.GetCell(wRowNew, wColNew, b);
            if (block != (byte)Block.Type.Floor && block != (byte)Block.Type.Mark)
            {
                continue;
            }
            w.x = wColNew;
            w.y = wRowNew;
            yield return new State(b, w, map);
            
            if (map.GetCell(checkBoxRow, checkBoxCol, b) != (byte)Block.Type.Box)
            {
                continue;
            }
            b = ((byte x, byte y)[])boxes.Clone();
            w = (Worker)w.Clone();
            for (int i = 0; i < b.Length; i++)
            {
                if (b[i].x == checkBoxCol && b[i].y == checkBoxRow)
                {
                    b[i].y = (byte)(w.y - direction.GetY());
                    b[i].x = (byte)(w.x - direction.GetX());
                    break;
                }
            }
            yield return new State(b, w, map);
        }
    }
}

partial class BidirectionalStatistic : Statistic
{
    protected int currOpenNodesNumR = 0;
    protected int maxOpenNodesNumR = 0;
    protected int currClosedNodesNumR = 0;
    protected int maxClosedNodesNumR = 0;

    public void Collect(ICollection<State> openNodes, ICollection<State> closeNodes)
    {
        iters++;
        currOpenNodesNum = openNodes.Count();
        currClosedNodesNum = closeNodes.Count();
        maxOpenNodesNum = Math.Max(maxOpenNodesNum, currOpenNodesNum);
        maxClosedNodesNum = Math.Max(maxClosedNodesNum, currClosedNodesNum);
        maxNodesNum = Math.Max(maxNodesNum, currOpenNodesNum + currClosedNodesNum + currClosedNodesNumR + currOpenNodesNumR);
    }

    public void CollectReversed(ICollection<State> openNodes, ICollection<State> closeNodes)
    {
        iters++;
        currOpenNodesNumR = openNodes.Count();
        currClosedNodesNumR = closeNodes.Count();
        maxOpenNodesNumR = Math.Max(maxOpenNodesNumR, currOpenNodesNumR);
        maxClosedNodesNumR = Math.Max(maxClosedNodesNumR, currClosedNodesNumR);
        maxNodesNum = Math.Max(maxNodesNum, currOpenNodesNum + currClosedNodesNum + currClosedNodesNumR + currOpenNodesNumR);
    }

    public void Print()
    {
        string s = "\n\tРезультаты двунаправленного поиска";
        s += "\n\n";
        s += $"Длина пути: {pathLength}\n";
        s += $"Итераций: {iters}\n";
        s += $"Открытые узлы:\n";
        s += $"\tКоличество при завершении: {currOpenNodesNum + currOpenNodesNumR}\n";
        s += $"\tМаксимальное количество: {maxOpenNodesNum + maxClosedNodesNumR}\n";
        s += $"Закрыте узлы:\n";
        s += $"\tКоличество при завершении: {currClosedNodesNum + currClosedNodesNumR}\n";
        s += $"\tМаксимальное количество: {maxClosedNodesNum + maxClosedNodesNumR}\n";
        s += $"Максимальное количество хранимых в памяти узлов: {maxNodesNum}\n";
        s += "\n";
        Console.WriteLine(s);
    }
}


class BidirectionalSearch : ISearcher<List<State>>
{
    private double printRate, lastFrame;
    public BidirectionalStatistic? statistic;
    private HashSet<State>? openNodes;
    private HashSet<State>? openNodesReversed;
    private HashSet<State>? closedNodes;
    private HashSet<State>? closedNodesReversed;
    private Map map;

    public BidirectionalSearch()
    {
    }

    private List<State>? NormalIteration()
    {
        HashSet<State> newO = new();
        foreach (var state in openNodes)
        {
            statistic.Collect(openNodes, closedNodes);
            closedNodes.Add(state);
            foreach (State s in state.GetGeneratedStates())
            {
                openNodesReversed.TryGetValue(s, out var item);
                if (item != null)
                {
                    List<State> l = item.Unwrap(out statistic.pathLength);
                    l.Reverse();
                    var res = state.Unwrap(out int secondPathLen);
                    statistic.pathLength += secondPathLen - 1;
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
            statistic.CollectReversed(openNodesReversed, closedNodesReversed);
            closedNodesReversed.Add(state);
            foreach (State s in state.GenerateReversedStates())
            {
                openNodes.TryGetValue(s, out var item);
                if (item != null)
                {
                    List<State> l = state.Unwrap(out statistic.pathLength);
                    l.Reverse();
                    var res = item.Unwrap(out int secondPathLen);
                    statistic.pathLength += secondPathLen - 1;
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

    public List<State>? Search(State begState)
    {
        printRate = 1;
        lastFrame = 0;

        statistic = new();
        map = begState.map;

        openNodes = [new State(map.boxes, map.worker, begState.map)];
        openNodesReversed = [.. GenerateFinalStates()];
        closedNodes = new();
        closedNodesReversed = new();

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

            var newFrame = Raylib.GetTime();
            if (newFrame - lastFrame >= printRate || result != null)
            {
                Console.Clear();
                Console.WriteLine($"On.count = {openNodes.Count()}");
                Console.WriteLine($"Or.count = {openNodesReversed.Count()}");
                Console.WriteLine($"Cn.count = {closedNodes.Count()}");
                Console.WriteLine($"Cr.count = {closedNodesReversed.Count()}");
                lastFrame = newFrame;
            }

            if (result != null)
            {
                statistic.Print();
                return result;
            }
        }

    }

    private IEnumerable<State> GenerateFinalStates()
    {
        (byte x, byte y)[] boxes = new (byte x, byte y)[map.boxes.Length];

        var nextBox = 0;
        foreach ((int col, int row) in map.FindBlocks(Block.Type.Mark))
        {
            boxes[nextBox++] = ((byte, byte))(col, row);
        }

        foreach (var b in boxes)
        {
            foreach (Worker.Direction direction in Worker.directions)
            {
                var checkFreeRow = b.y + direction.GetY();
                var checkFreeCol = b.x + direction.GetX();
                if (map.GetCell(checkFreeRow, checkFreeCol, boxes) == (byte)Block.Type.Floor)
                {
                    Worker w = (Worker)map.worker.Clone();
                    w.x = checkFreeCol;
                    w.y = checkFreeRow;
                    yield return new State(((byte, byte)[])boxes.Clone(), w, map);
                }
            }
        }
    }

    public int GetIters()
    {
        return statistic.iters;
    }

    public int GetN()
    {
        return statistic.maxNodesNum;
    }
}
