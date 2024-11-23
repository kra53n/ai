from pathlib import Path


p = Path("measures")
searches = ("breadth", "depth", "depth_iterative", "bidirectional", "heuristic1", "heuristic2", "heuristic3")
ds = (2, 7, 12, 17, 22)
maps = tuple(range(1, 11))


for search in searches:
    iters = [0]*len(ds)
    Ns = [0]*len(ds)
    for d in ds:
        for m in maps:
            try:
                with open(p / f'{search}_level{d}-{m}.txt', 'r') as f:
                    iterr, N = map(int, (i.strip().split()[1] for i in f.readlines()))
                    iters[ds.index(d)] += iterr
                    Ns[ds.index(d)] += N
            except FileNotFoundError:
                # print(f'{search}_level{d}-{m}.txt')
                continue
    iters = [float(i) / len(maps) for i in iters]
    Ns = [float(i) / len(maps) for i in Ns]
    print(search.capitalize())
    print(*iters)
    print(*Ns)
    print()
