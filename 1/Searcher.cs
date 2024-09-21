using System.Collections;
using System.Runtime.InteropServices;

class Searcher
{
    public enum Type
    {
        Breadth,
        Depth,
    };

    private ISequence<State> openNodes;
    private ISequence<State> closeNodes;
    private Type type;

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

    public List<State>? Search()
    {
        Statistic statistic = new Statistic();

        openNodes.Clear();
        closeNodes.Clear();
        openNodes.Push(new State(Sokoban.map, Sokoban.worker));

        while (!openNodes.Empty())
        {
            State state = openNodes.Pop();
            statistic.Collect(state, openNodes, closeNodes);
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
}

partial class State
{
    public Map map;
    public Worker worker;
    public State? prv;

    public State(Map _map, Worker _worker)
    {
        map = _map;
        worker = _worker;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }
        State state = (State)obj;
        for (int row = 0; row < map.GetRowsNum(); row++)
        {
            for (int col = 0; col < map.GetColsNum(); col++)
            {
                if (map.GetCell(row, col) != state.map.GetCell(row, col))
                {
                    return false;
                }
            }
        }
        if (worker.x != state.worker.x || worker.y != state.worker.y)
        { 
            return false; 
        }
        return true;
    }

    public bool IsGoal()
    {
        return map.Complete();
    }

    public List<State> GetGeneratedStates()
    {
        List<State> states = new List<State>();
        foreach (Worker.Direction direction in Worker.directions)
        {
            Map m = (Map)map.Clone();
            Worker w = (Worker)worker.Clone();
            w.Move(m, direction);
            if (w.x != worker.x || w.y != worker.y)
            {
                states.Add(new State(m, w));
            }
        }
        return states;
    }

    public List<State> Unwrap()
    {
        List<State> states = new List<State>();
        State? s = this;
        while (s != null)
        {
            states.Insert(0, s);
            s = s.prv;
        }
        return states;
    }
}

class Statistic
{
    private int iters = 0;
    private int currOpenNodesNum = 0;
    private int maxOpenNodesNum = 0;
    private int currCloseNodesNum = 0;
    private int maxCloseNodesNum = 0;
    private int maxNodesNum = 0;

    public void Collect(State currState, ISequence<State> openNodes, ISequence<State> closeNodes)
    {
        iters++;
        currOpenNodesNum = openNodes.Count();
        currCloseNodesNum = closeNodes.Count();
        maxOpenNodesNum = Math.Max(maxOpenNodesNum, currOpenNodesNum);
        maxCloseNodesNum = Math.Max(maxCloseNodesNum, currCloseNodesNum);
        maxNodesNum = Math.Max(maxNodesNum, currOpenNodesNum + currCloseNodesNum);
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
        s += $"\tКоличество при завершении: {currCloseNodesNum}\n";
        s += $"\tМаксимальное количество: {maxCloseNodesNum}\n";
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
