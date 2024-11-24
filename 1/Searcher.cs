using System.Collections;
using System.Runtime.InteropServices;
using static Sokoban;
using static System.Net.WebRequestMethods;

interface ISearcher<T>
{
    public T? Search(State begState);
    public int GetIters();
    public int GetN();
}

class Searcher : ISearcher<List<State>>
{
    public enum Type
    {
        Breadth,
        Depth
    };

    private ISequence<State> openNodes;
    private ISequence<State> closeNodes;
    private Type type;
    private Statistic statistic;

    public Searcher(Type _type)
    {
        switch (_type)
        {
            case Type.Breadth:
                openNodes = new QueueAdapter<State>();
                closeNodes = new QueueAdapter<State>();
                break;
            case Type.Depth:
                openNodes = new StackAdapter<State>();
                closeNodes = new StackAdapter<State>();
                break;
            default:
                throw new Exception("type has not specified. Only Breadth and Depth values are possible");
        }
        type = _type;
    }

    public List<State>? Search(State begState)
    {
        statistic = new Statistic();

        openNodes.Clear();
        closeNodes.Clear();
        openNodes.Push(begState);

        while (!openNodes.Empty())
        {
            State state = openNodes.Pop();
            statistic.Collect(openNodes, closeNodes);
            if (state.IsGoal())
            {
                statistic.Print(type);
                return state.Unwrap();
            }
            closeNodes.Push(state);
            foreach (State s in state.GetGeneratedStates())
            {
                if (!openNodes.Contains(s) && !closeNodes.Contains(s))
                {
                    s.prv = state;
                    openNodes.Push(s);
                }
            }
        }
        return null;
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

public partial class State
{
    public (byte x, byte y)[] boxes;
    public Worker worker;
    public State? prv;
    public int hash;
	public Map map;

    public State((byte x, byte y)[] _boxes, Worker _worker, Map? _map)
    {
        boxes = _boxes;
        worker = _worker;

        map = _map;
        if (map == null)
        {
            map = Sokoban.map;
        }
        var m = (Map)map.Clone();
        m.SetCell(worker.y, worker.x, (byte)Block.Type.Worker);
        foreach (var b in boxes)
        {
            m.SetCell(b.y, b.x, (byte)Block.Type.Box);
        }
        for (int row = 0; row < m.GetRowsNum(); row++)
        {
            for (int col = 0; col < m.GetColsNum(); col++)
            {
                hash = (hash * 10781 + (int)m.GetCell(row, col));
            }
        }
    }

    public override int GetHashCode()
    {
        return hash;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }
        State state = (State)obj;
        foreach (var b1 in boxes)
        {
            if (!state.boxes.Contains(b1))
            {
                return false;
            }
        }
        if (worker.x != state.worker.x || worker.y != state.worker.y)
        {
            return false;
        }
        return true;
        //return (obj as State).str == str;
    }

    public bool IsGoal()
    {
        return map.Complete(boxes);
    }

    public virtual IEnumerable<State> GetGeneratedStates()
    {
        foreach (Worker.Direction direction in Worker.directions)
        {
            var b = ((byte, byte)[])boxes.Clone();
            Worker w = (Worker)worker.Clone();
            w.Move(direction, b);
            if (w.x != worker.x || w.y != worker.y)
            {
                var state = new State(b, w, map);
                yield return state;
            }
        }
    }

    public List<State> Unwrap()
    {
        return Unwrap(out _);
    }

    public List<State> Unwrap(out int pathLen)
    {
        List<State> states = new List<State>();
        State? s = this;
        pathLen = 0;
        while (s != null)
        {
            states.Insert(0, s);
            pathLen++;
            s = s.prv;
        }
        return states;
    }
}

partial class Statistic
{
    public int iters = 0;
    protected int currOpenNodesNum = 0;
    protected int maxOpenNodesNum = 0;
    protected int currClosedNodesNum = 0;
    protected int maxClosedNodesNum = 0;
    public int maxNodesNum = 0;
    public int pathLength = 0;

    public void Collect(ISequence<State> openNodes, ISequence<State> closeNodes)
    {
        iters++;
        currOpenNodesNum = openNodes.Count();
        currClosedNodesNum = closeNodes.Count();
        maxOpenNodesNum = Math.Max(maxOpenNodesNum, currOpenNodesNum);
        maxClosedNodesNum = Math.Max(maxClosedNodesNum, currClosedNodesNum);
        maxNodesNum = Math.Max(maxNodesNum, currOpenNodesNum + currClosedNodesNum);
    }

    public void Print(Searcher.Type type)
    {
        string s = "\n\tРезультат поиска в ";
        switch (type)
        {
            case Searcher.Type.Breadth:
                s += "ширину";
                break;
            case Searcher.Type.Depth:
                s += "глубину";
                break;
        }
        s += "\n\n";
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

interface ISequence<T>
{
    void Push(T item);
    T Pop();
    void Clear();
    bool Empty();
    int Count();
    bool Contains(T item);
}

class StackAdapter<T> : ISequence<T>
{
    private Stack<T> stack = new Stack<T>();

    public void Push(T item)
    {
        stack.Push(item);
    }

    public T Pop()
    {
        return stack.Pop();
    }

    public void Clear()
    {
        stack.Clear();
    }

    public bool Empty()
    {
        return stack.Count == 0;
    }

    public int Count()
    {
        return stack.Count;
    }

    public bool Contains(T item)
    {
        foreach (T i in stack)
        {
            if (i.Equals(item))
            {
                return true;
            }
        }
        return false;
    }
}

class QueueAdapter<T> : ISequence<T>, IEnumerable<T>
{
    private Queue<T> queue;
    public QueueAdapter()
    {
        queue = new();
    }

    public QueueAdapter(IEnumerable<T> collection)
    {
        queue = new(collection);
    }

    public void Push(T item)
    {
        queue.Enqueue(item);
    }

    public void Clear()
    {
        queue.Clear();
    }

    public T Pop()
    {
        return queue.Dequeue();
    }

    public bool Empty()
    {
        return queue.Count == 0;
    }

    public int Count()
    {
        return queue.Count;
    }

    public bool Contains(T item)
    {
        foreach (T i in queue)
        {
            if (i.Equals(item))
            {
                return true;
            }
        }
        return false;
    }

    public T? GetItem(T item)
    {
        foreach (T i in queue)
        {
            if (i.Equals(item))
            {
                return i;
            }
        }
        return default(T);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)queue).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)queue).GetEnumerator();
    }
}
