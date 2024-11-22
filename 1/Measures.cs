using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


class Measures
{
    public static void Do()
    {
        (string name, ISearcher<List<State>> s)[] searches = new(string, ISearcher<List<State>>)[]
        {
            new("", new Searcher(Searcher.Type.Breadth)),
            new("", new Searcher(Searcher.Type.Depth)),
            new("", new DepthFirstSearch()),
            new("", new BidirectionalSearch()),
            new("", new InformedSearch(InformedSearch.BestHeuristic, "лучшего")),
            new("", new InformedSearch(InformedSearch.BetterHeuristic, "получше")),
            new("", new InformedSearch(InformedSearch.MidHeuristic, "среднего")),
            new("", new InformedSearch(InformedSearch.WorstHeuristic, "худшего")),
        };

        GenerateMap(10, 1, 10);
        return;

        for (int depth = 3; depth <= 10; depth++)
        {
            for (int boxes = 1; boxes <= 1; boxes++)
            {
                var map = GenerateMap(depth, boxes, 10);
                if (map == null) continue;
                //var begState = GenerateBegState(map);
                //foreach (var search in searches)
                //{
                //	var filename = $"{search.name}_{depth}_{boxes}.txt";
                //	measure(search.s, begState, filename, depth);
                //}
            }
        }
    }

    public static void measure(ISearcher<List<State>> search, State begState, string filename, int depth)
    {
    }

    public static Map? GenerateMap(int depth, int boxes, int size)
    {
        if (!CanGenerateMap(depth, boxes, size)) return null;

        var rand = new Random();
        do
        {
            List<(byte x, byte y)> boxesPos = new();
            List<(byte x, byte y)> markesPos = new();
            (byte x, byte y) workPos;

            boxesPos.Add(((byte)rand.Next(1, size - 1), (byte)rand.Next(1, size - 1)));
            while (boxesPos.Count != boxes)
            {
                var newBoxPos = ((byte)rand.Next(1, size - 1), (byte)rand.Next(1, size - 1));
                if (boxesPos.Contains(newBoxPos)) continue;
                boxesPos.Add(newBoxPos);

            }
            while (markesPos.Count != boxes)
            {
                var newMarkPos = ((byte)rand.Next(1, size - 1), (byte)rand.Next(1, size - 1));
                if (boxesPos.Contains(newMarkPos)) continue;
                if (markesPos.Contains(newMarkPos)) continue;
                markesPos.Add(newMarkPos);
            }
            do
            {
                workPos = ((byte)rand.Next(1, size - 1), (byte)rand.Next(1, size - 1));
            } while (boxesPos.Contains(workPos) || markesPos.Contains(workPos));

            var map = Map.GetEmptySquareMap(size + 2, size + 2);
            foreach (var boxPos in boxesPos) map.map[boxPos.y, boxPos.x] = (byte)Block.Type.Box;
            foreach (var markPos in markesPos) map.map[markPos.y, markPos.x] = (byte)Block.Type.Mark;
            map.map[workPos.y, workPos.x] = (byte)Block.Type.Worker;
            map.Load(map.map);
            var state = new State(boxesPos.ToArray(), new Worker(workPos.x, workPos.y, map), map);

            var search = new BidirectionalSearch();
            search.Search(state);
            Console.WriteLine($"hello {search.statistic.pathLength}");

            if (search.statistic.pathLength == depth)
            {
                Console.WriteLine("hello");
                return map;
            }
        } while (true);
    }

    public static bool CanGenerateMap(int depth, int boxes, int size)
    {
        if (depth == 1 && boxes == 1) return true;
        if (depth < 1 + 2*(boxes-1)) return false;
        if (size > boxes) return true;
        return false;
    }
}
