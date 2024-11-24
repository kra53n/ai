from pathlib import Path
import wolframalpha as wa
from collections import defaultdict
import asyncio


p = Path("measures")
ds = (2, 7, 12, 17, 22)
searches = ("breadth", "depth", "depth_iterative", "bidirectional", "heuristic1", "heuristic2", "heuristic3")
searches = { key: { 'iters' : [0]*len(ds), 'Ns' : [0]*len(ds), 'bs' : [0]*len(ds)} for key in searches }
maps = tuple(range(1, 11))

app_id = getfixture('APP_ID')
client = wa.Client(app_id)

def srch(iters, Ns, d, m, search):
    try:
        with open(p / f'{search}_level{d}-{m}.txt', 'r') as f:
            iterr, N = map(int, (i.strip().split()[1] for i in f.readlines()))
        iters[ds.index(d)] += iterr
        Ns[ds.index(d)] += N
        return 1
    except FileNotFoundError:
        return 0

working_files = defaultdict()
working_files.default_factory = list
for search, val in searches.items():
    for d in ds:
        working_files[search].append([])
        for m in maps:
            working_files[search][ds.index(d)].append(srch(val['iters'], val['Ns'], d, m, search))


async def calc_b(b_list, N, d):
    try:
        res = await client.aquery(f'N[{N}+1=Sum[b^n,{{n,0,{d}}}]]', params=[
            ("scanner", "Reduce"),
            ('format', 'plaintext')
        ])
        for pod in res.pods:
            if pod['@id'] not in ('Solution', 'RealSolution'):
                continue
            for subpod in pod.subpods:
                if (b := float(subpod['plaintext'][str(subpod['plaintext']).find('≈') + 1:])) > 0:
                    break
        b_list[ds.index(d)] = b
    except UnboundLocalError as e:
        print(e)


async def main():
    for srch in working_files:
        for i, val in enumerate(working_files[srch]):
            working_files[srch][i] = sum(val)
    groups = []
    for search, val in searches.items():
        val['Ns'] = list(f'{v / working_files[search][i]:.3f}' for i, v in enumerate(val['Ns']))
        for i, d in enumerate(ds):
            groups.append(calc_b(val['bs'], float(val['Ns'][i]), d))
    await asyncio.gather(*groups)
    for search, val in searches.items():
        with open("logs.txt", 'a') as f:
            f.write(search.capitalize() + '\n')
            f.write("d\t" + '\t'.join(map(str, ds)) + '\n')
            f.write("iters\t" + '\t'.join(f'{v / working_files[search][i]:.3f}' for i, v in enumerate(val['iters'])) + '\n')
            f.write("N\t" + '\t'.join(val['Ns']) + '\n')
            f.write("b\t" + '\t'.join(map(lambda x: f'{x:.3f}', val['bs'])) + '\n')


asyncio.run(main())