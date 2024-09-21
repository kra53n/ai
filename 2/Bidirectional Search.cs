using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var wXNew = w.x + direction.GetX();
            var wYNew = w.y + direction.GetY();
            var checkBoxX = w.x - direction.GetX();
            var checkBoxY = w.y - direction.GetY();
            if (m.GetCell(wXNew, wYNew) == (int)Sokoban.Block.Box || m.GetCell(wXNew, wYNew) == (int)Sokoban.Block.BoxOnMark || m.GetCell(w.y, w.x) != (int)Sokoban.Block.Wall)
            {
                continue;
            }
            w.Move(m, direction);
            yield return new State(m, w);
            if (m.GetCell(checkBoxX, checkBoxY) == (int)Sokoban.Block.BoxOnMark)
            {
                m.SetCell(checkBoxX, checkBoxY, (int)Sokoban.Block.Mark);
            }
            else if (m.GetCell(checkBoxX, checkBoxY) == (int)Sokoban.Block.Box)
            {
                m.SetCell(checkBoxX, checkBoxY, (int)Sokoban.Block.Floor);
            } 
            else
            {
                continue;
            }
            m.SetCell(w.x - direction.GetX(), w.y - direction.GetY(), (int)Sokoban.Block.Box);
            yield return new State(m, w);
        }
    }
}


class BidirectionalSearch
{
    public List<State>? Search()
    {
        Statistic statistic = new Statistic();

        QueueAdapter<State> openNodes = new(), openNodesReversed = new(), closedNodes = new(), closedNodesReversed = new();

        openNodes.Push(new State(Sokoban.map, Sokoban.worker));
        foreach (var state in GenerateFinalStates(Sokoban.map, Sokoban.worker))
        {
            openNodesReversed.Push(state);
        }

        while (true)
        {
            State state = openNodes.Pop();
            statistic.Collect(state, openNodes, closedNodes);
            closedNodes.Push(state);
            foreach (State s in state.GetGeneratedStates())
            {
                if (!openNodes.Contains(s) && !closedNodes.Contains(s))
                {
                    s.prv = state;
                    openNodes.Push(s);
                }
            }

            state = openNodesReversed.Pop();
            statistic.Collect(state, openNodesReversed, closedNodesReversed);

            closedNodesReversed.Push(state);
            foreach (State s in state.GenerateReversedStates())
            {
                var item = openNodes.GetItem(s);
                if (item != null)
                {
                    state.prv = item;
                    
                    State? curr = state;
                    bool flag;
                    do
                    {
                        flag = false;
                        foreach (var st in closedNodesReversed)
                        {
                            if (st.prv == curr)
                            {
                                curr = st;
                                flag = true;
                            }
                        }
                    } while (flag);

                    statistic.Print(Searcher.Type.Breadth);
                    return curr.Unwrap();
                }

                if (!openNodesReversed.Contains(s) && !closedNodesReversed.Contains(s))
                {
                    state.prv = s;
                    openNodesReversed.Push(s);
                }
            }
        }
    }

    private List<State> GenerateFinalStates(Map map, Worker worker)
    {
        var states = new List<State>();
        foreach ((int x, int y) in map.FindBlocks(Sokoban.Block.Box))
        {
            map.SetCell(x, y, (int)Sokoban.Block.Floor);
        }
        foreach ((int x, int y) in map.FindBlocks(Sokoban.Block.Mark))
        {
            map.SetCell(x, y, (int)Sokoban.Block.BoxOnMark);
        }

        foreach ((int x, int y) in map.FindBlocks(Sokoban.Block.BoxOnMark))
        {
            foreach (Worker.Direction direction in Worker.directions)
            {
                var checkFreeX = x + direction.GetX();
                var checkFreeY = y + direction.GetY();
                if (map.GetCell(checkFreeX, checkFreeY) == (int)Sokoban.Block.Floor)
                {
                    Worker w = (Worker)worker.Clone();
                    w.x = checkFreeX;
                    w.y = checkFreeY;
                    states.Add(new State((Map)map.Clone(), w));
                }
            }
        }
        return states;
    }
}
