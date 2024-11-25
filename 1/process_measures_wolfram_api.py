from pathlib import Path
import wolframalpha as wa
from collections import defaultdict
import asyncio
import datetime
import os


p = Path("measures")
ds = (2, 7, 12, 17, 22)
searches = ("breadth", "depth", "depth_iterative", "bidirectional", "heuristic1", "heuristic2", "heuristic3")
searches = { key: { 'iters' : [0]*len(ds), 'Ns' : [0]*len(ds), 'bs' : [0]*len(ds)} for key in searches }
maps = tuple(range(1, 11))

app_id = os.environ["WOLF_APP"]
client = wa.Client(app_id)

for search, val in searches.items():
    maps_count = [0] * len(ds)
    for d_index, d in enumerate(ds):
        for m in maps:
            try:
                with open(p / f'{search}_level{d}-{m}.txt', 'r') as f:
                    iterr, N = map(int, (i.strip().split()[1] for i in f.readlines()))
                val['iters'][d_index] += iterr
                val['Ns'][d_index] += N
                maps_count[d_index] += 1
            except FileNotFoundError:
                continue
    val['iters'] = [f'{float(v) / i:.3f}' for i, v in zip(maps_count, val['iters'])]
    val['Ns'] = [f'{float(v) / i:.3f}' for i, v in zip(maps_count, val['Ns'])]


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
        b_list[ds.index(d)] = f'{b:.3f}'
    except UnboundLocalError as e:
        print(e)


async def main():
    groups = []
    for search, val in searches.items():
        for i, d in enumerate(ds):
            groups.append(calc_b(val['bs'], float(val['Ns'][i]), d))
    await asyncio.gather(*groups)
    with open("logs.txt", 'a') as f:
        f.write(f"\n{f' {str(datetime.datetime.now())} ':-^90}\n\n")
        for search, val in searches.items():
            f.write(search.capitalize() + '\n')
            f.write("d\t" + '\t'.join(map(str, ds)) + '\n')
            f.write("iters\t" + '\t'.join(val['iters']) + '\n')
            f.write("N\t" + '\t'.join(val['Ns']) + '\n')
            f.write("b\t" + '\t'.join(val['bs']) + '\n\n')


asyncio.run(main())