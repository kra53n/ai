using Microsoft.VisualBasic;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class InformedState : State
{
    public int f = 0, g = 0;
    public InformedState((byte x, byte y)[] _boxes, Worker _worker, Map map) : base(_boxes, _worker, map)
    {
        this.f = 0;
        this.g = 0;
    }
    public InformedState((byte x, byte y)[] _boxes, Worker _worker, Map map, int f, int g) : base(_boxes, _worker, map)
    {
        this.f = f;
        this.g = g;
    }

    public InformedState(State state, Map map) : base(state.boxes, state.worker, map)
    {
        this.f = 0;
        this.g = 0;
        prv = state.prv;
    }

    public InformedState(State state, Map map, int f, int g) : base(state.boxes, state.worker, map)
    {
        this.f = f;
        this.g = g;
        prv = state.prv;
    }

    public override IEnumerable<InformedState> GetGeneratedStates()
    {
        foreach (var state in base.GetGeneratedStates())
        {
            yield return new InformedState(state, map);
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
        s += $"Длина пути: {pathLength}\n";
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

    public void nil()
    {
        iters = 0;
        currOpenNodesNum = 0;
        currClosedNodesNum = 0;
        maxOpenNodesNum = 0;
        maxClosedNodesNum = 0;
        maxNodesNum = 0;
    }
}

public class InformedSearch : ISearcher<List<State>>
{
    private double printRate, lastFrame;
    private InformedStatistic? statistic;
    private OrderedSet<InformedState>? openNodes;
    private HashSet<InformedState>? closedNodes;
    private Func<State, int> h;

    public InformedSearch(Func<State, int> heuristicFunc, string name)
    {
        statistic = new(name);
        h = heuristicFunc;
    }

    public List<State>? Search(State begState, bool print = false)
    {
        printRate = 1;
        lastFrame = 0;

        statistic.nil();

        State startState = new(begState.map.boxes, begState.worker, begState.map);
        openNodes = new(state => state.f)
        {
            new InformedState(startState, begState.map, h(startState), 0)
        };
        closedNodes = new();

        while (openNodes.Count > 0)
        {
            var curr = openNodes.Pop();
            if (curr.f - curr.g == 0)
            {
                statistic.pathLength = curr.g;
                if (print)
                {
                    statistic.Print();
                }
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
            }
            if (print)
            {
                var newFrame = Raylib.GetTime();
                if (newFrame - lastFrame >= printRate)
                {
                    Console.Clear();
                    Console.WriteLine($"On.count = {openNodes.Count()}");
                    Console.WriteLine($"Cn.count = {closedNodes.Count()}");
                    lastFrame = newFrame;
                }
            }
        }
        //statistic.Print();
        return null;
    }

    static public int BestHeuristic(State state)
    {
        (byte x, byte y) nearestBox = (0, 0);
        (byte x, byte y)[] marks = state.map.marks;
        var boxOnMarks = Enumerable.Repeat(false, marks.Length).ToArray();
        int min = int.MaxValue;
        for (int i = 0; i < state.boxes.Length; i++)
        {
			var b = state.boxes[i];
            bool skip = false;
            for (int j = 0; j < marks.Length; j++)
            {
				var m = marks[j];
                if (b == m)
                {
                    skip = true;
					boxOnMarks[j] = true;
                    break;
                }
            }
            if (skip)
            {
                continue;
            }
            int dist = Sphere.Dist(((byte)state.worker.x, (byte)state.worker.y), (b.x, b.y));
            if (min > dist)
            {
                min = dist;
                nearestBox = b;
            }
        }

        if (boxOnMarks.Count(x => x == true) == marks.Length)
        {
            return 0;
        }
        min = int.MaxValue;
        for (int i = 0; i < marks.Length; i++)
        {
			if (boxOnMarks[i])
			{
				continue;
			}
			var m = marks[i];
            int dist = Sphere.Dist((nearestBox.x, nearestBox.y), (m.x, m.y));
            min = Math.Min(min, dist);
        }
        return min;
    }

    static public int BetterHeuristic(State state)
    {
        int res = 0;
        (byte x, byte y)[] marks = state.map.marks;
        foreach ((byte x, byte y) b in state.boxes)
        {
            List<int> dists = new();
            foreach ((byte x, byte y) m in marks)
            {
                dists.Add(Sphere.Dist((m.x, m.y), (b.x, b.y)));
            }
            res += dists.Min();
        }
        return res;
    }

    static public int MidHeuristic(State state)
    {
        int counter = 0;
        foreach ((byte x, byte y) b in state.boxes)
        {
            if (state.map.GetCell(b.y, b.x) != (byte)Block.Type.Mark)
            {
                counter++;
            }
        }
        return counter;
    }

    static public int WorstHeuristic(State state)
    {
        if (state.map.marks == null)
        {
            return BestHeuristic(state);
        }
        int res = 0;
        (byte x, byte y)[] marks = state.map.marks;
        foreach ((byte x, byte y) b in state.boxes)
        {
            foreach ((byte x, byte y) m in marks)
            {
                res += Sphere.Dist((m.x, m.y), (b.x, b.y));
            }
        }
        return res;
    }

    static public int HorribleHeuristic(State state)
    {
        (byte x, byte y)[] marks = state.map.marks;
        int res = 0;
        foreach (var b in state.boxes)
        {
            if (marks.Contains(b))
            {
                continue;
            }
            res += Sphere.Dist(((byte)state.worker.x, (byte)state.worker.y), (b.x, b.y)) - 1;
        }
        foreach ((byte x, byte y) b in state.boxes)
        {
            int min = int.MaxValue;
            foreach ((byte x, byte y) m in marks)
            {
                min = Math.Min(min, Sphere.Dist((m.x, m.y), (b.x, b.y)));
            }
            res += min;
        }
        return res;
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
