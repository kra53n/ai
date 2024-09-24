using System.Collections;
using System.Runtime.InteropServices;

interface ISearcher<T>
{
    public T? Search();
}

class Searcher : ISearcher<List<State>>
{
    public enum Type
    {
        Breadth,
        Depth,
        Bidirectional
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
        openNodes.Push(new State(Sokoban.baseState.boxes, Sokoban.baseState.worker));

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
    public List<Block> boxes;
    public Worker worker;
    public State? prv;
    public int hash;

    public State(List<Block> _boxes, Worker _worker)
    {
        boxes = _boxes;
        worker = _worker;

        //var item = (int)Sokoban.Block.Worker;
        //(item, map.map[worker.y, worker.x]) = (map.map[worker.y, worker.x], item);
        //char[] str = new char[map.map.Length];
        //Buffer.BlockCopy(map.map, 0, str, 0, map.map.Length);
        //hash = new string(str).GetHashCode();
        //map.map[worker.y, worker.x] = item;

        string str = "";
        foreach (Block b in boxes)
        {
            str += b.x;
            str += b.y;
        }
        str += worker.x;
        str += worker.y;
        hash = str.GetHashCode();
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
        return hash == state.hash;
    }

    public bool IsGoal()
    {
        return Sokoban.map.Complete(boxes);
    }

    public IEnumerable<State> GetGeneratedStates()
    {
        List<State> states = new List<State>();
        foreach (Worker.Direction direction in Worker.directions)
        {
            var b = Block.CloneBlocks(boxes);
            Worker w = (Worker)worker.Clone();
            w.Move(direction, b);
            if (w.x != worker.x || w.y != worker.y)
            {
                yield return new State(b, w);
            }
        }
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
            case Searcher.Type.Bidirectional:
                s = "Результаты двунаправленного поиска";
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
