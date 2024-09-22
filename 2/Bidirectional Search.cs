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


class BidirectionalSearch : ISearcher<List<State>>
{
    private Statistic? statistic;
    private OrderedSet<State>? openNodes;
    private OrderedSet<State>? openNodesReversed;
    private OrderedSet<State>? closedNodes;
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
    private void NormalIteration()
    {
        OrderedSet<State> newO = new();
        while (openNodes.Count != 0)
        {
            State state = openNodes.Pop();

            //statistic.Collect(state, openNodes, closedNodes);
            
            closedNodes.Add(state);
            foreach (State s in state.GetGeneratedStates())
            {
                if (!openNodes.Contains(s) && !newO.Contains(s) && !closedNodes.Contains(s))
                {
                    s.prv = state;
                    newO.Add(s);
                }
            }
        }
        openNodes = newO;
    }

    private List<State>? ReversedIteration()
    {
        OrderedSet<State> newO = new();
        while (openNodesReversed.Count != 0)
        {
            State state = openNodesReversed.Pop();
            
            //statistic.Collect(state, openNodesReversed, closedNodesReversed);

            closedNodesReversed.Add(state);
            foreach (State s in state.GenerateReversedStates())
            {
                //if (item == null) {
                //    item = closedNodes.GetItem(s);
                //}
                var item = openNodes.GetItem(s);
                if (item == null) { item = closedNodes.GetItem(s); }
                if (item != null)
                {
                    statistic.Print(Searcher.Type.Bidirectional);
                    List<State> l = state.Unwrap();
                    l.Reverse();
                    var res = item.Unwrap();
                    res.AddRange(l);
                    return res;
                }
                if (!openNodesReversed.Contains(s) && !newO.Contains(s) && !closedNodesReversed.Contains(s))
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
            NormalIteration();
            var result = ReversedIteration();

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
