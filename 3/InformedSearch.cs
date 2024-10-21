using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class InformedState : State
{
    public int f = 0, g = 0;
    public InformedState((byte x, byte y)[] _boxes, Worker _worker) : base(_boxes, _worker)
    {
        this.f = 0;
        this.g = 0;
    }
    public InformedState((byte x, byte y)[] _boxes, Worker _worker, int f, int g) : base(_boxes, _worker)
    {
        this.f = f;
        this.g = g;
    }

    public InformedState(State state) : base(state.boxes, state.worker)
    {
        this.f = 0;
        this.g = 0;
        prv = state.prv;
    }

    public InformedState(State state, int f, int g) : base(state.boxes, state.worker)
    {
        this.f = f;
        this.g = g;
        prv = state.prv;
    }

    public override IEnumerable<InformedState> GetGeneratedStates()
    {
        foreach (var state in base.GetGeneratedStates())
        {
            yield return new InformedState(state);
        }
    }
}

partial class InformedStatistic : Statistic
{
    public string name;

    public InformedStatistic(string _name)
    {
        name = _name;
    }

    public void Collect<T>(ICollection<T> openNodes, ICollection<T> closeNodes)
    {
        iters++;
        currOpenNodesNum = openNodes.Count();
        currClosedNodesNum = closeNodes.Count();
        maxOpenNodesNum = Math.Max(maxOpenNodesNum, currOpenNodesNum);
        maxClosedNodesNum = Math.Max(maxClosedNodesNum, currClosedNodesNum);
        maxNodesNum = Math.Max(maxNodesNum, currOpenNodesNum + currClosedNodesNum);
    }

    public void Print()
    {
        string s = $"\n\tРезультат {name} эвристического поиска:\n\n";
        s += $"Итераций: {iters}\n";
        s += $"Открытые узлы:\n";
        s += $"\tКоличество при завершении: {currOpenNodesNum}\n";
        s += $"\tМаксимальное количество: {maxOpenNodesNum}\n";
        s += $"Закрыте узлы:\n";
        s += $"\tКоличество при завершении: {currClosedNodesNum}\n";
        s += $"\tМаксимальное количество: {maxClosedNodesNum}\n";
        s += $"Максимальное количество хранимых в памяти узлов: {maxNodesNum}\n";
        s += "\n";
        Console.WriteLine(s);
    }
}

public class InformedSearch : ISearcher<List<State>>
{
    private double printRate, lastFrame;
    private InformedStatistic? statistic;
    private OrderedSet<InformedState, int>? openNodes;
    private HashSet<InformedState>? closedNodes;
    private Func<State, int> h;

    public InformedSearch(Func<State, int> heuristicFunc, string name)
    {
        statistic = new(name);
        h = heuristicFunc;
        State startState = new(Sokoban.baseState.boxes, Sokoban.baseState.worker);
        openNodes = new(state => state.f)
        {
            new InformedState(startState, h(startState), 0)
        };
        closedNodes = new();
    }

    public List<State>? Search()
    {
        printRate = 1;
        lastFrame = 0;
        while (openNodes.Count > 0)
        {
            var curr = openNodes.Pop();
            if (curr.IsGoal())
            {
                statistic.Print();
                return curr.Unwrap();
            }
            statistic.Collect(openNodes, closedNodes);
            closedNodes.Add(curr);
            foreach (InformedState state in curr.GetGeneratedStates())
            {
                var item = openNodes.GetItem(state);
                if (item != null)
                {
                    var score = h(state) + curr.g + 1;
                    if (score < item.f)
                    {
                        item.f = score;
                        item.g = curr.g + 1;
                        item.prv = curr;
                    }
                    continue;
                }
                closedNodes.TryGetValue(state, out item);
                if (item != null)
                {
                    var score = h(state) + curr.g + 1;
                    if (score < item.f)
                    {
                        closedNodes.Remove(item);
                        item.f = score;
                        item.g = curr.g + 1;
                        item.prv = curr;
                        openNodes.Add(item);
                    }
                    continue;
                }
                state.g = curr.g + 1;
                state.f = h(state) + state.g;
                state.prv = curr;
                openNodes.Add(state);


                var newFrame = Raylib.GetTime();
                if (newFrame - lastFrame >= printRate)
                {
                    //Console.Clear();
                    //Console.WriteLine($"On.count = {openNodes.Count()}");
                    //Console.WriteLine($"Cn.count = {closedNodes.Count()}");
                    lastFrame = newFrame;
                }
            }
        }
        return null;
    }

    static public int BestHeuristic(State state)
    {
        return state.IsGoal() ? 1 : 0;
    }

    static public int MidHeuristic(State state)
    {
        int counter = 0;
        foreach ((byte x, byte y) b in state.boxes)
        {
            if (Sokoban.map.GetCell(b.y, b.x) != (byte)Block.Type.Mark)
            {
                counter++;
            }
        }
        return counter;
    }

    static public int WorstHeuristic(State state)
    {
        if (Sokoban.map.marks == null)
        {
            return BestHeuristic(state);
        }
        int res = 0;
        (byte x, byte y)[] marks = Sokoban.map.marks;
        foreach ((byte x, byte y) b in state.boxes)
        {
            foreach ((byte x, byte y) m in marks)
            {
                res += Sphere.Dist((m.x, m.y), (b.x, b.y));
            }
        }
        return res;
    }
}
