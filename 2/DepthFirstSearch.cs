using System.Data;

class DepthFirstSearch : ISearcher<List<State>>
{
    // TODO(kra53n): delete *.Clear() in other Search's
    public List<State>? Search()
    {
        DepthFirstSearchStatistic statistic = new DepthFirstSearchStatistic();

        Stack<DepthFirstSearchState> openNodes = new Stack<DepthFirstSearchState>();
        Stack<DepthFirstSearchState> closedNodes = new Stack<DepthFirstSearchState>();
        Stack<DepthFirstSearchState> nxtOpenNodes = new Stack<DepthFirstSearchState>();

        nxtOpenNodes.Push(new DepthFirstSearchState(Sokoban.baseState.boxes, Sokoban.baseState.worker, 0));

        for (int lvl = 0; ; lvl++)
        {
            openNodes = nxtOpenNodes;
            nxtOpenNodes = new Stack<DepthFirstSearchState>();
            while (openNodes.Count > 0)
            {
                var state = openNodes.Pop();
                if (state.lvl == lvl)
                {
                    nxtOpenNodes.Push(state);
                    continue;
                }
                statistic.Collect(state, openNodes, closedNodes, lvl);
                if (state.IsGoal())
                {
                    statistic.Print();
                    return state.Unwrap();
                }
                closedNodes.Push(state);
                foreach (DepthFirstSearchState s in state.GetGeneratedStates())
                {
                    if (!openNodes.Contains(s) && !nxtOpenNodes.Contains(s) && !closedNodes.Contains(s))
                    {
                        s.prv = state;
                        openNodes.Push(s);
                    }
                }
            }
        }
    }
}

class DepthFirstSearchState : State
{
    public int lvl;

    public DepthFirstSearchState(List<Block> boxes, Worker _worker, int _lvl) : base(boxes, _worker)
    {
        lvl = _lvl;
    }

    public DepthFirstSearchState(State state, int _lvl) : base(state.boxes, state.worker)
    {
        lvl = _lvl;
    }

    public new IEnumerable<DepthFirstSearchState> GetGeneratedStates()
    {
        foreach (State s in base.GetGeneratedStates())
        {
            yield return new DepthFirstSearchState(s, lvl + 1);
        }
    }
}

class DepthFirstSearchStatistic
{
    private int iters = 0;
    private int currOpenNodesNum = 0;
    private int maxOpenNodesNum = 0;
    private int currCloseNodesNum = 0;
    private int maxCloseNodesNum = 0;
    private int maxNodesNum = 0;
    private int currLvl;

    public void Collect(State currState, Stack<DepthFirstSearchState> openNodes, Stack<DepthFirstSearchState> closeNodes, int lvl)
    {
        iters++;
        currOpenNodesNum = openNodes.Count();
        currCloseNodesNum = closeNodes.Count();
        maxOpenNodesNum = Math.Max(maxOpenNodesNum, currOpenNodesNum);
        maxCloseNodesNum = Math.Max(maxCloseNodesNum, currCloseNodesNum);
        maxNodesNum = Math.Max(maxNodesNum, currOpenNodesNum + currCloseNodesNum);
        currLvl = Math.Max(currLvl, lvl);
    }

    public void Print()
    {
        string s = "\n\tРезультат поиска с итеративным углублением ";
        s += "\n\n";
        s += $"Итераций: {iters}\n";
        s += $"Уровень: {currLvl}\n";
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
