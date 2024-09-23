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
            Map m = (Map)map.Clone();
            Worker w = (Worker)worker.Clone();
            var wRowNew = w.y + direction.GetY(); 
            var wColNew = w.x + direction.GetX();
            var checkBoxRow = w.y - direction.GetY();
            var checkBoxCol = w.x - direction.GetX();
            if (m.GetCell(wRowNew, wColNew) != (int)Sokoban.Block.Floor && m.GetCell(wRowNew, wColNew) != (int)Sokoban.Block.Mark)
            {
                continue;
            }
            w.Move(m, direction);
            yield return new State(m, w);
            m = (Map)m.Clone();
            w = (Worker)w.Clone();
            if (m.GetCell(checkBoxRow, checkBoxCol) == (int)Sokoban.Block.BoxOnMark)
            {
                m.SetCell(checkBoxRow, checkBoxCol, (int)Sokoban.Block.Mark);
            }
            else if (m.GetCell(checkBoxRow, checkBoxCol) == (int)Sokoban.Block.Box)
            {
                m.SetCell(checkBoxRow, checkBoxCol, (int)Sokoban.Block.Floor);
            } 
            else
            {
                continue;
            }
            m.SetCell(w.y - direction.GetY(), w.x - direction.GetX(), (int)Sokoban.Block.Box);
            yield return new State(m, w);
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
    private BidirectionalStatistic? statistic;
    private HashSet<State>? openNodes;
    private HashSet<State>? openNodesReversed;
    private HashSet<State>? closedNodes;
    private HashSet<State>? closedNodesReversed;

    public BidirectionalSearch()
    {
        statistic = new();

        openNodes = new();
        openNodes.Add(new State(Sokoban.baseState.map, Sokoban.baseState.worker));
        openNodesReversed = new();
        GenerateFinalStates(Sokoban.baseState.map, Sokoban.baseState.worker).ForEach(i => openNodesReversed.Add(i));
        closedNodes = new();
        closedNodesReversed = new();
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
            statistic.CollectReversed(openNodesReversed, closedNodesReversed);
            closedNodesReversed.Add(state);
            foreach (State s in state.GenerateReversedStates())
            {
                openNodes.TryGetValue(s, out var item);
                if (item != null)
                {
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
                statistic.Print();
                return result;
            }
        }
        
    }

    private List<State> GenerateFinalStates(Map m, Worker worker)
    {
        Map map = (Map)m.Clone();
        var states = new List<State>();
        foreach ((int col, int row) in map.FindBlocks(Sokoban.Block.Box))
        {
            map.SetCell(row, col, (int)Sokoban.Block.Floor);
        }
        foreach ((int col, int row) in map.FindBlocks(Sokoban.Block.Mark))
        {
            map.SetCell(row, col, (int)Sokoban.Block.BoxOnMark);
        }

        foreach ((int col, int row) in map.FindBlocks(Sokoban.Block.BoxOnMark))
        {
            foreach (Worker.Direction direction in Worker.directions)
            {
                var checkFreeRow = row + direction.GetY();
                var checkFreeCol = col + direction.GetX();
                if (map.GetCell(checkFreeRow, checkFreeCol) == (int)Sokoban.Block.Floor || map.GetCell(checkFreeRow, checkFreeCol) == (int)Sokoban.Block.Mark)
                {
                    Worker w = (Worker)worker.Clone();
                    w.x = checkFreeCol;
                    w.y = checkFreeRow;
                    states.Add(new State((Map)map.Clone(), w));
                }
            }
        }
        return states;
    }
}
