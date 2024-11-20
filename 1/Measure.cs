using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


class Measure
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

        for (int depth = 3; depth <= 10; depth++)
		{
			for (int boxes = 1; boxes <= 1; boxes++)
			{
				var map = GenerateMap(depth, boxes, 10, 10);
				var begState = GenerateBegState(map);
				foreach (var search in searches)
				{
					var filename = $"{search.name}_{depth}_{boxes}.txt";
					measure(search.s, begState, filename, depth);
				}
			}
		}
	}

	public static void measure(ISearcher<List<State>> search, State begState, string filename, int depth)
	{
	}

	public static Map GenerateMap(int depth, int boxes, int wdt, int hgt)
	{
		return new Map(0 ,0);
	}

	public static State GenerateBegState(Map map)
	{
		return new State();
	}
}
