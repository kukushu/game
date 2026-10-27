import argparse
import json
from dataclasses import asdict
from pathlib import Path

from .experiments import CONTEXTS, CORE_CONTEXTS, compare, export_city, plots
from .model import City, Settings


def main():
    parser=argparse.ArgumentParser(description='Retail intermediation laboratory, independent of econlab baseline')
    sub=parser.add_subparsers(dest='command',required=True)
    p=sub.add_parser('compare')
    p.add_argument('--seeds',default='1,2,3,4,5,6,7,8')
    p.add_argument('--contexts',default=','.join(CORE_CONTEXTS))
    p.add_argument('--months',type=int,default=72)
    p.add_argument('--tail',type=int,default=24)
    p.add_argument('--out',default='outputs/commerce')
    p.add_argument('--plot',action='store_true')
    p=sub.add_parser('run')
    p.add_argument('--seed',type=int,default=1)
    p.add_argument('--channel',choices=['direct','retail','mixed'],default='mixed')
    p.add_argument('--context',choices=list(CONTEXTS),default='separated')
    p.add_argument('--config',help='JSON fields override context settings')
    p.add_argument('--resume')
    p.add_argument('--months',type=int,default=72,help='Absolute end month for resumed snapshots')
    p.add_argument('--trace',action='store_true')
    p.add_argument('--out',default='outputs/commerce-run')
    p=sub.add_parser('plot')
    p.add_argument('out')
    p=sub.add_parser('inspect')
    p.add_argument('snapshot')
    group=p.add_mutually_exclusive_group()
    group.add_argument('--store',type=int)
    group.add_argument('--resident',type=int)
    args=parser.parse_args()
    try:
        if args.command=='inspect':
            city=City.restore(json.loads(Path(args.snapshot).read_text(encoding='utf-8-sig')))
            if args.store is not None:
                store=city.stores[args.store]
                print(json.dumps({**asdict(store),'cash':city.accounts[f'r{store.id}'],
                    'last_six_observations':[x for x in city.observations if x['store']==store.id][-6:],
                    'events':[x for x in city.events if x['store']==store.id]},indent=2))
            elif args.resident is not None:
                h=city.residents[args.resident]
                print(json.dumps({**asdict(h),'cash':city.accounts[f'h{h.id}']},indent=2))
            else:
                print(json.dumps(city.metrics[-1] if city.metrics else {'month':0},indent=2))
            return
        elif args.command=='compare':
            compare(args.out,[int(x) for x in args.seeds.split(',')],args.months,args.tail,args.contexts.split(','),args.plot)
        elif args.command=='plot':
            plots(args.out)
        else:
            if args.months < 1:
                raise ValueError('months must be positive')
            if args.resume:
                if args.config:
                    raise ValueError('Do not override saved config when resuming')
                city=City.restore(json.loads(Path(args.resume).read_text(encoding='utf-8-sig')),args.trace)
            else:
                config={'channel':args.channel,**CONTEXTS[args.context]}
                if args.config:
                    config.update(json.loads(Path(args.config).read_text(encoding='utf-8-sig')))
                city=City(args.seed,Settings(**config),args.trace)
            if args.months<city.month:
                raise ValueError('End month is before saved month')
            while city.month<args.months:
                city.step()
            export_city(city,args.out)
            print(json.dumps(city.metrics[-1],indent=2))
        print('Results:',Path(args.out).resolve())
    except (ValueError,TypeError,OSError,IndexError) as error:
        parser.error(str(error))


if __name__=='__main__':
    main()
