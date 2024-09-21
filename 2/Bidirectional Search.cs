using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

class BidirectionalSearch
{
    private List<State> GenerateStates(State state)
    {
        var states = new List<State>();

        foreach (Worker.Direction direction in Worker.directions)
        {
            Map m = (Map)state.map.Clone();
            Worker w = (Worker)state.worker.Clone();
            var wXNew = w.x + direction.GetX();
            var wYNew = w.y + direction.GetY();
            var checkBoxX = w.x - direction.GetX();
            var checkBoxY = w.y - direction.GetY();
            if (m.GetCell(wXNew, wYNew) == (int)Sokoban.Block.Box || m.GetCell(wXNew, wYNew) == (int)Sokoban.Block.BoxOnMark || m.GetCell(w.y, w.x) != (int)Sokoban.Block.Wall)
            {
                continue;
            }
            w.Move(m, direction);
            states.Add(new State(m, w));
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
            states.Add(new State(m, w));
        }
        return states;
    }
}

