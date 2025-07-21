from pathlib import Path


p = Path("measures")
searches = ("breadth", "depth", "depth_iterative", "bidirectional", "heuristic1", "heuristic2", "heuristic3")
ds = (2, 7, 12, 17, 22)
maps = tuple(range(1, 11))
files_not_found = []


for search in searches:
    iters = [0]*len(ds)
    Ns = [0]*len(ds)
    for d_idx, d in enumerate(ds):
        for m in maps:
            filename = f'{search}_level{d}-{m}.txt'
            try:
                with open(p / filename, 'r') as f:
                    iterr, N = map(int, (i.split()[1] for i in f.readlines()))
                    iters[d_idx] += iterr
                    Ns[d_idx] += N
            except FileNotFoundError:
                files_not_found.append(filename)
                continue
        iters[d_idx] /= len(maps)
        Ns[d_idx] /= len(maps)
    print(search.capitalize().replace('_', ' '))
    print(*iters)
    print(*Ns)
    print()

print('Files were not found')
print('\n'.join(files_not_found))
