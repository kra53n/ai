using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


class Measures
{
    public static void Do()
    {
        (string name, Func<ISearcher<List<State>>> factory)[] searches = {
            ("breadth", () => new Searcher(Searcher.Type.Breadth)),
            ("depth", () => new Searcher(Searcher.Type.Depth)),
            ("depth_iterative", () => new DepthFirstSearch()),
            //("bidirectional", () => new BidirectionalSearch()),
            ("heuristic1", () => new InformedSearch(InformedSearch.MidHeuristic, "mid")),
            //("heuristic2", () => new InformedSearch(InformedSearch.BetterHeuristic, "получше")),
            //("heuristic3", () => new InformedSearch(InformedSearch.HorribleHeuristic, "худшего")),
        };

        Directory.CreateDirectory("../../../measures");
        Parallel.ForEach(Directory.GetFiles("../../../levels-course"), filename =>
        {
            Map map = new(0, 0);
            map.Load(Sokoban.LoadMapContentFromFile(filename));
            Parallel.ForEach(searches, kv =>
            {
                var search = kv.factory();
                search.Search(new State(map.boxes, map.worker, map));
                WriteMeasureToFile(kv.name, Path.GetFileName(filename), search);
            });
        });
    }

    public static void WriteMeasureToFile(string searchName, string levelFilename, ISearcher<List<State>> search)
    {
        File.WriteAllLines($"../../../measures/{searchName + "_" + levelFilename}", [$"iters {search.GetIters()}", $"N {search.GetN()}"]);
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
