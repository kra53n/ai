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
		List<(byte x, byte y)> boxesPos = new();
		List<(byte x, byte y)> markesPos = new();
		(byte x, byte y) workPos;

        boxesPos.Add(((byte)rand.Next(size), (byte)rand.Next(size)));
		while (boxesPos.Count != boxes)
		{
			var newBoxPos = ((byte)rand.Next(size), (byte)rand.Next(size));
			if (boxesPos.Contains(newBoxPos)) continue;
			boxesPos.Add(newBoxPos);

        }
        while (markesPos.Count != boxes)
        {
            var newMarkPos = ((byte)rand.Next(size), (byte)rand.Next(size));
            if (boxesPos.Contains(newMarkPos)) continue;
            if (markesPos.Contains(newMarkPos)) continue;
			markesPos.Add(newMarkPos);
        }
		do
		{
			workPos = ((byte)rand.Next(size), (byte)rand.Next(size));
        } while (boxesPos.Contains(workPos) || markesPos.Contains(workPos));

		do
		{
			var map = Map.GetEmptySquareMap(size + 2, size + 2);
			foreach (var boxPos in boxesPos) map.map[boxPos.y + 1, boxPos.x + 1] = (byte)Block.Type.Box;
			foreach (var markPos in markesPos) map.map[markPos.y + 1, markPos.x + 1] = (byte)Block.Type.Mark;
			map.map[workPos.y + 1, workPos.x + 1] = (byte)Block.Type.Worker;
			map.Load(map.map);

			var state = new State(boxesPos.ToArray(), new Worker(workPos.x, workPos.y, map), map);

			var search = new BidirectionalSearch();
			search.Search(state);
        } while (true);

        return new Map(0 ,0);
	}

	//public static State GenerateBegState(Map map)
	//{
	//	return new State();
	//}

	public static bool CanGenerateMap(int depth, int boxes, int size)
	{
		if (depth == 1 && boxes == 1) return true;
		if (depth < 1 + 2*(boxes-1)) return false;
		if (size > boxes) return true;
		return false;
	}
}
