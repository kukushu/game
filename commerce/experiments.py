import csv
import json
import math
import statistics
from dataclasses import asdict
from pathlib import Path

from econlab.experiments import bootstrap_interval, write_csv
from .model import City, Settings

# Ex ante cases; there are no scenario-specific subsidies or consumer retail tastes.
CONTEXTS = {
    "separated": {},
    "cheap_access": {"customer_edge_hours": .02},
    "near_industry": {"industry": "near"},
    "dispersed": {"residential": "dispersed"},
    "sparse": {"households": 32, "residential": "dispersed"},
    "small_only": {"zoning": "small_only"},
    "large_edge": {"zoning": "large_edge"},
    "large_central": {"zoning": "large_central"},
    "freight_road": {"corridor": True},
    "expensive_freight": {"freight_multiplier": 6.0},
    "no_chaining": {"chain_trips": False},
    "equal_assortment": {"small_multigood": True},
    "matched_edge": {"zoning": "matched_edge"},
}
CORE_CONTEXTS = [name for name in CONTEXTS if name != 'matched_edge']
METRICS = ("food_coverage", "goods_coverage", "retail_share", "shopping_hours", "labor_hours", "hours_per_unit",
           "paid_price", "stores", "small", "large", "retail_land", "truck_trips", "road_load", "waste",
           "production_exposure", "commercial_exposure", "retail_profit", "fund_cash", "service_rejections", "budget_shortfall")


def clustering(city, draws=120):
    active = [s for s in city.stores if s.active]
    if len(active) < 2:
        return {"cluster_ratio_uniform": None, "cluster_ratio_demand": None}
    def nearest(nodes):
        return statistics.fmean(min(city.manhattan(a,b) for j,b in enumerate(nodes) if i!=j) for i,a in enumerate(nodes))
    actual = nearest([s.node for s in active])
    values = {}
    demand_weights = {n:.01+sum(1/(1+city.manhattan(h.node,n))**2 for h in city.residents) for n in range(35)}
    # Condition both nulls on surviving sizes, zoning, and no duplicate occupied parcels.
    # Demand-weighted null distinguishes residential attraction from extra shop clustering.
    for name, weighted in (("uniform", False), ("demand", True)):
        nulls = []
        for draw in range(draws):
            r = city.rng("cluster_"+name,draw)
            used, nodes = set(), []
            for s in sorted(active,key=lambda x:-x.area):
                legal = [n for n in range(35) if n not in used and s.area in city.legal_areas(n)]
                if weighted:
                    weights = [demand_weights[n] for n in legal]
                    node = r.choices(legal,weights=weights,k=1)[0]
                else:
                    node = r.choice(legal)
                used.add(node)
                nodes.append(node)
            nulls.append(nearest(nodes))
        values["cluster_ratio_"+name] = actual/statistics.fmean(nulls)
    return values


def size_summary(city, label, seed):
    rows=[]
    for area in (1,4):
        stores=[s for s in city.stores if s.area==area]
        # Exclude right-censored stores younger than 12 periods that are still alive.
        eligible=[s for s in stores if not s.active or s.age>=12]
        observations=[x for x in city.observations if x['area']==area]
        rows.append({"context":label,"channel":city.settings.channel,"seed":seed,"area":area,
            "entries":len(stores),"alive":sum(s.active for s in stores),"eligible_12":len(eligible),
            "survived_12":sum(s.age>=12 for s in eligible),
            "sales_per_operating_month":statistics.fmean(x['sales'] for x in observations) if observations else 0,
            "profit_per_operating_month":statistics.fmean(x['profit_cents']/100 for x in observations) if observations else 0})
    return rows


def export_city(city, out):
    out=Path(out)
    out.mkdir(parents=True,exist_ok=True)
    (out/'snapshot.json').write_text(json.dumps(city.snapshot(),indent=2),encoding='utf-8')
    write_csv(out/'metrics.csv',city.metrics)
    write_csv(out/'stores.csv',[{**asdict(s),"cash":city.accounts[f'r{s.id}']} for s in city.stores])
    write_csv(out/'residents.csv',[{**asdict(h),"cash":city.accounts[f'h{h.id}']} for h in city.residents])
    write_csv(out/'store_months.csv',city.observations)
    write_csv(out/'events.csv',city.events)
    if city.trace:
        write_csv(out/'transactions.csv',city.ledger)


def compare(out,seeds,months=72,tail=24,contexts=None,plot=False):
    if not seeds or len(set(seeds))!=len(seeds) or not 0<tail<=months:
        raise ValueError('Require unique seeds, positive months and tail <= months')
    contexts=contexts or list(CORE_CONTEXTS)
    for name in contexts:
        if name not in CONTEXTS:
            raise ValueError(f'Unknown context {name}')
    out=Path(out)
    out.mkdir(parents=True,exist_ok=True)
    manifest={"schema":1,"seeds":seeds,"months":months,"tail":tail,"contexts":{n:CONTEXTS[n] for n in contexts},
        "base_settings":asdict(Settings()),"channels":['direct','retail','mixed'],
        "comparison":"same keyed initial residents, producer technologies, household cash, and investment pool within context",
        "uncertainty":"1500 paired seed bootstrap resamples, not model uncertainty"}
    (out/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    rows,series,sizes=[],[],[]
    for name in contexts:
        for seed in seeds:
            initial=None
            for mode in ('direct','retail','mixed'):
                city=City(seed,Settings(channel=mode,**CONTEXTS[name]))
                profile=([(h.node,h.value_time) for h in city.residents],city.accounts.copy())
                if initial is None:
                    initial=profile
                else:
                    assert profile==initial,'Channel initial conditions differ'
                for _ in range(months):
                    city.step()
                export_city(city,out/'runs'/name/mode/f'seed-{seed}')
                series.extend({"context":name,"channel":mode,"seed":seed,**r} for r in city.metrics)
                row={"context":name,"channel":mode,"seed":seed,
                    **{k:statistics.fmean(r[k] for r in city.metrics[-tail:]) for k in METRICS},**clustering(city),
                    "entries":len(city.stores),"exits":sum(not s.active for s in city.stores),
                    "max_cash_error":max(abs(r['cash_error']) for r in city.metrics)}
                rows.append(row)
                sizes.extend(size_summary(city,name,seed))
        print(f'Finished {name}: {len(seeds)*3} runs',flush=True)
    effects=[]
    for name in contexts:
        baseline={r['seed']:r for r in rows if r['context']==name and r['channel']=='direct'}
        for mode in ('retail','mixed'):
            group=[r for r in rows if r['context']==name and r['channel']==mode]
            for key in METRICS:
                values=[r[key]-baseline[r['seed']][key] for r in group]
                low,high=bootstrap_interval(values)
                effects.append({"context":name,"channel":mode,"metric":key,
                    "mean":statistics.fmean(r[key] for r in group),"delta_vs_direct":statistics.fmean(values),
                    "ci_low":low,"ci_high":high,"positive_seeds":sum(v>1e-8 for v in values),"seeds":len(seeds)})
    write_csv(out/'per_seed.csv',rows)
    write_csv(out/'timeseries.csv',series)
    write_csv(out/'effects.csv',effects)
    write_csv(out/'sizes.csv',sizes)
    report(out,rows,effects,manifest)
    if plot:
        plots(out)
    return rows,effects


def report(out,rows,effects,manifest):
    lines=['# 商业中介实验：自动结果','',f"种子 {manifest['seeds']}，运行 {manifest['months']} 期，末尾 {manifest['tail']} 期均值。",
        'direct=只许工厂直销；retail=只许商店销售；mixed=居民自主选渠道。没有零售消费偏好或批发折扣。','',
        '| 城市条件 | 渠道 | 食物满足 | 可选品满足 | 零售份额 | 总小时/件 | 成交价 | 小店 | 大店 |',
        '|---|---|---:|---:|---:|---:|---:|---:|---:|']
    for name in manifest['contexts']:
        for mode in manifest['channels']:
            group=[r for r in rows if r['context']==name and r['channel']==mode]
            avg=lambda k:statistics.fmean(r[k] for r in group)
            lines.append(f"| {name} | {mode} | {avg('food_coverage'):.1%} | {avg('goods_coverage'):.1%} | {avg('retail_share'):.1%} | {avg('hours_per_unit'):.3f} | {avg('paid_price'):.3f} | {avg('small'):.2f} | {avg('large'):.2f} |")
    lines+=['','## 聚集诊断','', '最近邻距离 ÷ 相同店数/规模/规划约束随机布局的最近邻距离；小于 1 表示比该随机基准更近。',
        'demand 版本按住宅密度加权，不应把所有低于 uniform 基准的集中都当成商店相互吸引。','',
        '| 城市条件 | 渠道 | uniform 比率 | demand 比率 |','|---|---|---:|---:|']
    for name in manifest['contexts']:
        for mode in ('retail','mixed'):
            group=[r for r in rows if r['context']==name and r['channel']==mode and r['cluster_ratio_uniform'] is not None]
            if group:
                lines.append(f"| {name} | {mode} | {statistics.fmean(r['cluster_ratio_uniform'] for r in group):.3f} | {statistics.fmean(r['cluster_ratio_demand'] for r in group):.3f} |")
    lines+=['','## 不要把这些结果读成现实结论','',
        '- 小/大店规模、存储容量、基础人员、地租、运输技术均为模型输入；实际进入/退出/销量/选址才是结果。',
        '- 总小时包括制造、销售、货运和购物；不包含道路建设、地租机会成本或污染健康损失，不能称为完整社会福利。',
        '- 行业、住宅位置和工厂位置外生固定；没有居民迁居和内生工业选址。',
        '- 需求固定两类各两件，价格/库存公开，最多各比较三家。没有广告吸引力或真正的信息搜索学习。',
        '- 零售商用有误差的局部估计选址，允许真实亏损退出。存活和利润会受初始投资池及观察窗口影响。',
        '- 渠道是从同分布初态开始的长期制度对照，不是已经建好城市里的即时政策冲击。',
        '- 保留每个种子的主体快照、店铺月表和事件。统计区间只针对所选种子的配对差异。']
    (out/'report.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')


def plots(out):
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    out=Path(out)
    with (out/'per_seed.csv').open(encoding='utf-8-sig') as f:
        rows=list(csv.DictReader(f))
    with (out/'manifest.json').open() as f:
        manifest=json.load(f)
    names=list(manifest['contexts'])
    colors={'direct':'#566879','retail':'#d99929','mixed':'#238c80'}
    fig,axes=plt.subplots(2,2,figsize=(15,10),layout='constrained')
    for ax,key,title in zip(axes.flat,['hours_per_unit','paid_price','retail_share','large'],
            ['Total labor + shopping hours / unit','Paid price / unit (excluding time)','Retail share of final consumption','Active large stores']):
        for j,mode in enumerate(manifest['channels']):
            groups=[[float(r[key]) for r in rows if r['context']==n and r['channel']==mode] for n in names]
            means=[statistics.fmean(v) for v in groups]
            errors=[[m-min(v) for m,v in zip(means,groups)],[max(v)-m for m,v in zip(means,groups)]]
            ax.bar([i+(j-1)*.25 for i in range(len(names))],means,.24,label=mode,color=colors[mode],yerr=errors,capsize=2)
        ax.set_xticks(range(len(names)),names,rotation=40,ha='right',fontsize=8)
        ax.set_title(title)
        ax.grid(axis='y',alpha=.2)
    axes[0,0].legend()
    fig.suptitle('Retail intermediation lab | seed means, whiskers: min/max | uncalibrated')
    fig.savefig(out/'comparison.png',dpi=140)
    fig.savefig(out/'comparison.svg')
    plt.close(fig)

    # Actual, selected end state. Different marker shapes distinguish shop size.
    chosen=out/'runs/separated/mixed'/f"seed-{manifest['seeds'][0]}"/'snapshot.json'
    if not chosen.exists():
        return
    city=City.restore(json.loads(chosen.read_text()))
    fig,ax=plt.subplots(figsize=(9,6),layout='constrained')
    counts={n:sum(h.node==n for h in city.residents) for n in range(35)}
    for n,count in counts.items():
        if count:
            x,y=city.xy(n)
            ax.scatter(x,y,s=count*18,color='#adc7e1',alpha=.65)
            ax.text(x,y+.15,str(count),ha='center',fontsize=8)
    for s in city.stores:
        if s.active:
            x,y=city.xy(s.node)
            ax.scatter(x,y,s=150 if s.area==4 else 75,marker='s' if s.area==4 else '^',color='#218c74')
            ax.annotate(f"R{s.id}:{'/'.join(s.assortment)}",(x,y),xytext=(4,-13),textcoords='offset points',fontsize=7)
    for p in city.producers:
        x,y=city.xy(p.node)
        ax.scatter(x,y,marker='X',s=150,color='#c44c42')
        ax.text(x+.07,y+.12,p.good,fontsize=8)
    ax.set(xlim=(-.6,6.6),ylim=(-.6,4.7),xticks=range(7),yticks=range(5),
           title=f'End state, separated/mixed, seed {city.seed} | circles=residents; triangles=small; squares=large; X=factory')
    ax.grid(alpha=.2)
    fig.savefig(out/'locations.png',dpi=150)
    plt.close(fig)
