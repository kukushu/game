'use strict';
let state = null, busy = false, selectedNode = 8;
let selection = {kind:'node', id:8}, detail = null, detailWorld = 'player', inspectRequest = 0;
const $ = id => document.getElementById(id);
const esc = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const num = (v,d=2) => Number.isFinite(Number(v)) ? Number(v).toFixed(d) : '—';
const pct = v => `${num(v*100,1)}%`;
const money = v => num(v/100);
const good = v => v === 'food' ? '食物' : '可选品';
const worldName = () => $('world').value;
const world = () => state.worlds[worldName()];
const costs = {permit_large:2,permit_small:1,freight:5,access:5,assortment:3};
const names = {permit_large:'允许综合大店',permit_small:'仅许可新小店',freight:'试行货运优先走廊',access:'改善居民出行',assortment:'放宽新小店品类'};
const purposes = {manufacturing:'生产工资',factory_service:'工厂柜台工资',retail_staff:'店员工资',freight:'货运工资',rent_utilities:'租金与开门费',truck_lease:'车辆租用',wholesale:'进货付款',direct_sale:'工厂直销',retail_sale:'门店销售',equity_investment:'创业投资',lease_setup_fee:'租赁布置费',factory_dividend:'工厂分红',retail_dividend:'零售分红',household_dividend:'家庭权益分配',fund_dividend:'投资池分红',liquidation:'退出返还'};
function seller(account){return account?.startsWith('p') ? `工厂 F${account.slice(1)}` : `商店 S${account?.slice(1)}`;}
function message(text,error=false){$('status').textContent=text;$('status').classList.toggle('error',error);}
function setBusy(value){busy=value;document.body.classList.toggle('busy',value);document.querySelectorAll('button').forEach(b=>b.disabled=value);['load','seed','preset','world','hypothesis'].forEach(id=>$(id).disabled=value);$('busy-label').textContent=value?'Python 正在结算…':'';if(!value&&state)availability();}
async function request(path,body){
  const response=await fetch(path,body===undefined?{}:{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)});
  const result=await response.json();
  if(!response.ok){if(response.status===409){state=await request('/api/state');render();}throw Error(result.error||'请求失败');}
  return result;
}
async function mutate(path,data={},success='已更新'){
  if(busy||!state)return;
  setBusy(true);detail=null;
  try{state=await request(path,{revision:state.revision,...data});render();await inspect();message(success);}
  catch(error){message(error.message,true);}
  finally{setBusy(false);}
}
function availability(){
  const ref=worldName()==='reference', city=state.worlds.player;
  document.querySelectorAll('[data-action]').forEach(b=>{
    const action=b.dataset.action;
    let unavailable=ref||state.points<costs[action];
    if(action.startsWith('permit_'))unavailable ||= selectedNode===null || city.nodes[selectedNode].large_allowed===(action==='permit_large');
    if(action==='freight')unavailable ||= city.settings.corridor;
    if(action==='assortment')unavailable ||= city.settings.small_multigood;
    if(action==='access')unavailable ||= state.actions.some(a=>a.action==='access');
    b.disabled=busy||unavailable;
  });
  document.querySelectorAll('[data-step]').forEach(b=>b.disabled=busy||state.month+Number(b.dataset.step)>240);
  $('parcel-title').textContent=selectedNode===null?'先选地图上的区域':`区域 ${selectedNode} · ${city.nodes[selectedNode].large_allowed?'可开综合大店':'仅新小店'}`;
}
function render(){
  $('month').textContent=`第 ${state.month} 期已结算`;
  $('seed').value=state.seed;
  if(['neighborhood','sparse','easy_access'].includes(state.preset))$('preset').value=state.preset;
  $('points').textContent=state.points;
  const m=world().metrics||{}, count=world().residents.length;
  const cards=[['购买满足',`${pct(m.food_coverage)} / ${pct(m.goods_coverage)}`,'食物 / 可选品'],['购物时间',`${num(m.shopping_hours/count)} 小时`,'平均每个家庭，上期实际行程'],['成交价格',num(m.paid_price),'每件实际消费，不含自身时间'],['经营中的店',`${num(m.small,0)} 小 / ${num(m.large,0)} 大`,'企业自主进入、亏损、退出'],['创业资金池',num(m.fund_cash),'投资者资金，和行动额度不同']];
  $('metrics').innerHTML=cards.map(([label,value,hint])=>`<div class="metric"><span>${esc(label)}</span><strong>${esc(value)}</strong><small>${esc(hint)}</small></div>`).join('');
  $('audit').textContent=m.cash_error===0&&m.stock_error_food===0&&m.stock_error_goods===0?'上期现金 / 实物守恒：通过':'尚未结算';
  renderMap();renderClues();renderComparison();renderActions();availability();
}
const point = node => ({x:54+(node%7)*100,y:50+Math.floor(node/7)*90});
function routeLines(routes,reference=false){
  return routes.map(route=>(route.edges||[]).map(([a,b])=>{
    const p=point(a),q=point(b),color=reference?'#6c7aa1':route.mode==='truck'?'#c7782e':'#267d63';
    return `<line x1="${p.x}" y1="${p.y}" x2="${q.x}" y2="${q.y}" stroke="${color}" stroke-width="${reference?3:4}" ${reference?'stroke-dasharray="6 5"':''} opacity=".9"/>`;
  }).join('')).join('');
}
function renderMap(){
  const city=world(), traffic=new Map(city.traffic.map(([a,b,v])=>[`${Math.min(a,b)}-${Math.max(a,b)}`,v]));
  let roads='';
  for(let a=0;a<35;a++)for(const b of [a%7<6?a+1:-1,a<28?a+7:-1])if(b>=0){
    const p=point(a),q=point(b),flow=traffic.get(`${a}-${b}`)||0,corridor=city.settings.corridor&&Math.floor(a/7)===2&&Math.floor(b/7)===2;
    roads+=`<line x1="${p.x}" y1="${p.y}" x2="${q.x}" y2="${q.y}" stroke="${corridor?'#bd9846':'#c3cec2'}" stroke-width="${2+Math.min(5,flow/20)}"><title>区域 ${a} ↔ ${b}，上期通行计数 ${flow}${corridor?'；货运优先':''}</title></line>`;
  }
  let routes='';
  if(detail&&detailWorld===worldName()){
    routes=routeLines(detail.routes||[]);
    if($('show-reference-route').checked&&detail.kind==='resident')routes+=routeLines(detail.other?.routes||[],true);
  }
  const cells=city.nodes.map(n=>{
    const p=point(n.id),label=`区域 ${n.id}，家庭 ${n.residents}，商店 ${n.stores.length}，${n.large_allowed?'允许综合大店':'仅许可新小店'}`;
    return `<g class="map-cell ${n.large_allowed?'large':''} ${n.id===selectedNode?'selected':''}" tabindex="0" role="button" aria-label="${esc(label)}" data-node="${n.id}">
      <rect x="${p.x-39}" y="${p.y-32}" width="78" height="67" rx="6"/>
      <text x="${p.x-30}" y="${p.y-19}" font-size="9" fill="#859183">${n.id}</text>
      ${n.residents?`<circle cx="${p.x+18}" cy="${p.y-13}" r="${6+Math.min(8,n.residents/4)}" fill="#c2d8e4" opacity=".8"/>`:''}
      <text x="${p.x}" y="${p.y-2}" text-anchor="middle" font-size="11" fill="#527c98">${n.residents?`家 ${n.residents}`:'—'}</text>
      <text x="${p.x}" y="${p.y+13}" text-anchor="middle" font-size="11" fill="#276e60">${n.stores.map(s=>`${s.area===4?'■ 大':'▲ 小'}S${s.id}`).join(' ')||'待进入'}</text>
      <text x="${p.x}" y="${p.y+27}" text-anchor="middle" font-size="10" fill="#a35342">${n.producers.map(f=>`${good(f.good)} F${f.id}`).join(' ')}</text>
    </g>`;
  }).join('');
  $('map').innerHTML=`<svg viewBox="0 0 708 462" role="group" aria-label="35 个城市区域，点击检查和规划">${roads}${routes}${cells}</svg>`;
  document.querySelectorAll('[data-node]').forEach(cell=>{
    cell.addEventListener('click',()=>choose('node',Number(cell.dataset.node)));
    cell.addEventListener('keydown',e=>{if(e.key==='Enter'||e.key===' '){e.preventDefault();choose('node',Number(cell.dataset.node));}});
  });
}
function entityButton(kind,id,label){return `<button data-kind="${kind}" data-id="${id}">${esc(label)}</button>`;}
function bindEntities(container){container.querySelectorAll('[data-kind]').forEach(b=>b.onclick=()=>choose(b.dataset.kind,Number(b.dataset.id)));}
function renderClues(){
  const city=world(),m=city.metrics||{},attention=city.residents.filter(h=>h.food<2||h.goods<2).sort((a,b)=>(a.food+a.goods)-(b.food+b.goods)||b.shopping-a.shopping),missing=attention.length;
  const costly=[...city.residents].sort((a,b)=>b.shopping-a.shopping)[0];
  const losses=city.stores.filter(s=>s.active&&s.profit<0).sort((a,b)=>a.profit-b.profit);
  const entries=city.events.filter(e=>e.event==='entry'&&e.month===state.month-1);
  let html=`<div class="clue"><p>${missing?`${missing} 户家庭上期没有买齐两类需要。先看它们遇到了什么。`:'家庭上期都买齐了。还可以继续追问：谁花了最多时间？'}</p>${entityButton('resident',missing?attention[0].id:costly.id,'看一户')}</div>`;
  html+=`<div class="clue"><p>家庭 H${costly.id} 购物花了 ${num(costly.shopping)} 小时，住在区域 ${costly.node}。</p>${entityButton('resident',costly.id,'看路线')}</div>`;
  html+=`<div class="clue"><p>${losses.length} 家经营中的店本期亏损。${losses.length?`S${losses[0].id} 本期结果 ${money(losses[0].profit)}，需要查客流、货架和现金。`:'盈利不代表所有居民都更方便。'}</p>${losses.length?entityButton('store',losses[0].id,'看月账'):''}</div>`;
  html+=`<p class="tiny">上期新店 ${entries.length} 家；货运 ${num(m.truck_trips,0)} 趟。许可后没有开店，也可能是企业没考察到、预期不赚钱或没有创业资金。</p>`;
  $('clues').innerHTML=html;bindEntities($('clues'));
  $('examples').innerHTML=state.examples.length?`<h3>对照 → 你的城市：谁真的变了？</h3>`+state.examples.map(x=>`<div class="example">家庭 H${x.id}：购物 ${num(x.reference.shopping)} → ${num(x.player.shopping)} 小时；买到食物 ${x.reference.food} → ${x.player.food}，可选品 ${x.reference.goods} → ${x.player.goods}。<br>${entityButton('resident',x.id,'查看实际购物与对照')}</div>`).join(''):'<p class="tiny">目前还没有家庭结果差异。改变条件后推进几期，再检查具体反应。</p>';
  $('examples').querySelectorAll('[data-kind]').forEach(b=>b.onclick=()=>{$('world').value='player';render();choose(b.dataset.kind,Number(b.dataset.id));});
}
function renderComparison(){
  $('comparison-label').textContent=`第 ${state.fork_month} 期从完全相同状态出发；${state.comparison_window?`当前比较之后最近 ${state.comparison_window} 期均值`:'尚未推进，暂不评价结果'}。`;
  const definitions=[['food_coverage','食物满足',pct],['paid_price','实际成交价',v=>num(v)],['shopping_hours','平均家庭购物时间',v=>num(v/state.worlds.player.residents.length)+' 小时']];
  $('comparison').innerHTML=state.comparison_window?`<div class="compare-grid">${definitions.map(([key,label,format])=>{const x=state.comparisons[key];return `<div class="compare-stat"><label>${label}</label><strong>${format(x.player)}</strong><span>对照 ${format(x.reference)}</span></div>`;}).join('')}</div>`:'<p class="notice">先记下一个预期、做一项规划，再推进。对照会同步运行，不会替你采取新行动。</p>';
  const p=state.worlds.player.history,r=state.worlds.reference.history;
  if(!p.length){$('chart').innerHTML='';return;}
  const min=p[0].month,max=p[p.length-1].month,x=m=>8+((m-min)/Math.max(1,max-min))*584,y=v=>82-v*64;
  const line=data=>data.map(a=>`${x(a.month)},${y(a.food_coverage)}`).join(' ');
  $('chart').innerHTML=`<svg viewBox="0 0 600 100" aria-label="食物满足率历史，绿色为你的城市，灰色为对照"><line x1="8" y1="82" x2="592" y2="82" stroke="#dfe5d8"/><line x1="8" y1="18" x2="592" y2="18" stroke="#dfe5d8"/><polyline points="${line(r)}" fill="none" stroke="#95a0a7" stroke-width="2" stroke-dasharray="5 4"/><polyline points="${line(p)}" fill="none" stroke="#276e60" stroke-width="2.5"/><text x="8" y="12" font-size="9" fill="#67766f">食物满足率 100%</text><text x="8" y="96" font-size="9" fill="#67766f">第 ${min+1} 期</text><text x="535" y="96" font-size="9" fill="#67766f">第 ${max+1} 期</text></svg>`;
}
function renderActions(){
  $('actions').innerHTML=state.actions.length?state.actions.map(a=>`<li><strong>第 ${a.month} 期 · ${esc(a.name)}${a.node!==null?` · 区域 ${a.node}`:''}</strong>（${a.cost} 点）<p>${a.note?`我的预期：${esc(a.note)}`:'未记录预期'}</p>${a.month===state.month?'<span class="badge">条件已改变，等待下一期反应</span>':''}</li>`).join(''):'<li>尚未采取行动。先检查一户家庭或一家商店。</li>';
}
async function choose(kind,id){
  if(busy)return;selection={kind,id};
  if(kind==='node')selectedNode=id;
  else if(kind==='resident')selectedNode=world().residents.find(h=>h.id===id)?.node??selectedNode;
  else if(kind==='store')selectedNode=world().stores.find(s=>s.id===id)?.node??selectedNode;
  detail=null;renderMap();availability();await inspect();
  if(window.matchMedia("(max-width:1040px)").matches)document.querySelector(".inspector").scrollIntoView({block:"start"});
}
async function inspect(){
  if(!selection||!state)return;
  const token=++inspectRequest,selectedWorld=worldName(),selectedRevision=state.revision;
  try{
    const data=await request(`/api/inspect?kind=${selection.kind}&id=${selection.id}&world=${selectedWorld}`);
    if(token!==inspectRequest||selectedWorld!==worldName()||selectedRevision!==state.revision)return;
    detail=data;detailWorld=selectedWorld;renderInspector();renderMap();
  }catch(error){$('inspector').textContent=error.message;}
}
function pairs(items){return `<div class="stat-pairs">${items.map(([a,b])=>`<div><small>${esc(a)}</small><strong>${esc(b)}</strong></div>`).join('')}</div>`;}
function paymentTable(payments,account){
  const groups={};for(const p of payments||[]){const sign=p.target===account?1:-1;groups[p.purpose]=(groups[p.purpose]||0)+sign*p.cents;}
  return Object.keys(groups).length?`<table><thead><tr><th>上期现金流</th><th>金额</th></tr></thead><tbody>${Object.entries(groups).map(([key,value])=>`<tr><td>${esc(purposes[key]||key)}</td><td>${value>=0?'+':''}${money(value)}</td></tr>`).join('')}</tbody></table>`:'<p class="empty">这个快照没有上期逐笔记录。推进一期后可以查看。</p>';
}
function renderInspector(){
  if(!detail)return;
  let html='',title='';const d=detail,entity=d.entity;
  if(worldName()==='reference')html+='<p class="notice reference-notice">这是只读对照，规划请切回你的城市。</p>';
  if(d.kind==='node'){
    title=`区域 ${d.id}`;
    html+=pairs([['未来进入许可',d.large_allowed?'小店 / 大店':'仅小店'],['附近租金 / 期',`${money(d.rent_small)} / ${money(d.rent_large)}`]]);
    html+='<p class="tiny">租金按小 / 大面积显示；许可不改变现有店的面积。</p><h3>住在这里的家庭</h3>';
    html+=`<div class="entity-list">${d.residents.map(h=>entityButton('resident',h.id,`H${h.id} · 食${h.food}/2 物${h.goods}/2`)).join('')||'<span class="empty">这里没有家庭。</span>'}</div>`;
    html+='<h3>商店与工厂</h3><div class="entity-list">'+d.stores.map(s=>entityButton('store',s.id,`${s.area===4?'大':'小'}店 S${s.id}`)).join('')+d.producers.map(p=>entityButton('producer',p.id,`${good(p.good)}工厂 F${p.id}`)).join('')+'</div>';
    html+='<h3>上期企业实际考察</h3>';
    html+=d.estimates.length?d.estimates.map(e=>`<div class="receipt">${e.area===4?'大':'小'}店，${e.assortment.map(good).join(' / ')}<br>预期单期利润 ${money(e.profit)}；需投入 ${money(e.capital)}。<br>${e.affordable?'当时投资池足以投入。':'当时投资池不足。'}这是企业的估计，不保证实际盈利。</div>`).join(''):'<p class="empty">上期没有考察到这里。每期企业只抽样部分空地，允许后仍可能等待。</p>';
  }else if(d.kind==='resident'){
    title=`家庭 H${entity.id} · 区域 ${entity.node}`;
    html+=pairs([['本期买到',`食${entity.food}/2 · 物${entity.goods}/2`],['购物耗时',`${num(entity.shopping)} 小时`],['期末现金',money(d.cash)],['本期工作',`${num(entity.work)} 小时`]]);
    html+='<h3>它实际去了哪里？</h3>';
    html+=d.purchases.map(p=>`<div class="receipt">在 ${seller(p.seller)} 买 ${p.quantity} 件${good(p.good)}，每件 ${money(p.price_cents)}，实际支付 ${money(p.cents)}。<br>${entityButton(p.seller[0]==='p'?'producer':'store',Number(p.seller.slice(1)),'检查卖家')}</div>`).join('')||'<p class="empty">本期没有购买记录。</p>';
    if(d.plans.length){const p=d.plans[0];html+=`<h3>为什么选这组店？</h3><p class="tiny">家庭把取得商品的价值，减去实际价格和自身对时间的估值，比较可行组合；不是单纯找最低价。</p><p class="receipt">购物前现金 ${money(p.cash_before)}；选定组合行程 ${num(p.duration)} 小时，净估值 ${money(p.utility)}${p.utility<=0?'，因此放弃购买':''}。</p><table><thead><tr><th>候选组合</th><th>耗时</th><th>净估值</th></tr></thead><tbody>${p.alternatives.map(a=>`<tr><td>${a.sellers.map(seller).join(' + ')}</td><td>${num(a.duration)}</td><td>${money(a.utility)}</td></tr>`).join('')}</tbody></table>`;}
    if(d.blocked.length){const b=d.blocked[0];html+=`<p class="notice">当时没有形成可行的购物组合。现款 ${money(b.cash)}；有货且有柜台能力的候选：食物 ${b.available.food}、可选品 ${b.available.goods}。${b.shortest_available_roundtrip!==null?`最近一家的往返约 ${num(b.shortest_available_roundtrip)} 小时；购物预算 ${num(world().settings.shopping_hours)} 小时。`:'当时没有可用卖家。'}这组事实不一定能识别唯一原因。</p>`;}
    const other=d.other.entity;html+=`<h3>同一家庭的另一条路径</h3><p class="receipt">${worldName()==='player'?'不新增干预的对照':'你的城市'}：购物 ${num(other.shopping)} 小时，食物 ${other.food}/2，可选品 ${other.goods}/2。勾选地图下方选项可看路线。</p>`;
    html+=paymentTable(d.payments,`h${entity.id}`);
  }else if(d.kind==='store'){
    title=`${entity.area===4?'大':'小'}店 S${entity.id} · 区域 ${entity.node}`;
    html+=`<span class="badge ${entity.active?'':'closed'}">${entity.active?'经营中':'已退出'}</span><span class="badge">${entity.assortment.map(good).join(' / ')}</span>`;
    html+=pairs([['期末现金',money(d.cash)],['本期结果',money(entity.last_profit)],['库存 / 容量',`${Object.values(entity.stock).reduce((a,b)=>a+b,0)} / ${d.capacity}`],['上期客流 / 销量',`${entity.customers} / ${entity.sales}`]]);
    html+=`<p class="tiny">每期开门费 ${money(d.rent)}，配置店员 ${num(d.staff_hours)} 小时；不能靠许可直接获得顾客。</p>`;
    html+='<h3>货架和报价</h3>'+entity.assortment.map(g=>`<div class="receipt">${good(g)}：库存 ${entity.stock[g]} 件，报价 ${money(entity.prices[g])}；平均进货含运费 ${money(entity.unit_cost[g])}。企业预计下期卖 ${num(entity.expected[g],1)} 件。</div>`).join('');
    html+='<h3>实际补货</h3>'+(d.shipments.map(p=>`<div class="receipt">从工厂 F${p.supplier} 运来 ${p.quantity} 件${good(p.good)}，${p.trips} 趟，进货款 ${money(p.wholesale_cents)}，运费 ${money(p.freight_cents)}。</div>`).join('')||'<p class="empty">本期没有成功补货；可能已有库存、没有资金或未营业。</p>');
    html+='<h3>经营轨迹</h3><table><thead><tr><th>期</th><th>销量</th><th>库存</th><th>结果</th></tr></thead><tbody>'+d.history.map(h=>`<tr><td>${h.month+1}</td><td>${h.sales}</td><td>${h.stock}</td><td>${money(h.profit_cents)}</td></tr>`).join('')+'</tbody></table><p class="tiny">月表结果是清算前；退出还可能冲销现货价值。</p>';
    html+=d.events.map(e=>`<p class="receipt">第 ${e.month+1} 期${e.event==='entry'?`进入：企业当时预计利润 ${money(e.forecast_profit)}，不保证实现。`:`退出：${e.reason==='cash'?'连续无法支付开门费用':'连续亏损'}。`}</p>`).join('');
    html+=paymentTable(d.payments,`r${entity.id}`);
  }else if(d.kind==='producer'){
    title=`工厂 F${entity.id} · ${good(entity.good)}`;
    html+=pairs([['期末现金',money(d.cash)],['出厂价',money(entity.price)],['库存 / 本期产量',`${entity.stock} / ${entity.production}`],['本期销量 / 直销',`${entity.sold} / ${entity.direct_sold}`]]);
    html+='<p class="tiny">工厂对居民和零售商报同一个出厂价。它依靠自己的销量和库存计划生产。</p>';
    html+=d.production.map(p=>`<p class="receipt">上期卖 ${p.previous_sales} 件，期初库存 ${p.stock_before}；本期更新预期销量为 ${num(p.expected,1)}，实际生产 ${p.produced} 件，柜台配工 ${num(p.counter_hours)} 小时。</p>`).join('');
    html+='<h3>谁来采购？</h3>'+(d.shipments.map(s=>`<div class="receipt">S${s.store} 批量采购 ${s.quantity} 件，运往区域 ${s.destination}。</div>`).join('')||'<p class="empty">本期没有商店补货记录。</p>');
    html+=`<p class="tiny">本期记录到 ${d.purchases.length} 笔居民直销购买。</p>`+paymentTable(d.payments,`p${entity.id}`);
  }
  $('inspector-title').textContent=title;$('inspector').innerHTML=html;bindEntities($('inspector'));
}
document.querySelectorAll('[data-step]').forEach(b=>b.onclick=()=>mutate('/api/advance',{count:Number(b.dataset.step)},`已推进 ${b.dataset.step} 期。先比较具体家庭，再判断这次规划。`));
document.querySelectorAll('[data-action]').forEach(b=>b.onclick=()=>mutate('/api/action',{action:b.dataset.action,node:selectedNode,note:$('hypothesis').value},'规划已记录；经济结果尚未改变，推进后观察实际反应。'));
$('anchor').onclick=()=>mutate('/api/anchor',{},'已从当前相同状态建立新对照；此前的规划条件保留，额度没有恢复。');
$('new').onclick=async()=>{selectedNode=8;selection={kind:'node',id:8};$('world').value='player';await mutate('/api/new',{seed:Number($('seed').value),preset:$('preset').value},'新城市已准备。先检查一户家庭或一个经营问题。');};
$('world').onchange=()=>{detail=null;selection={kind:'node',id:selectedNode??8};render();inspect();};
$('show-reference-route').onchange=renderMap;
$('save').onclick=async()=>{try{const response=await fetch('/api/save');if(!response.ok)throw Error('导出失败');const url=URL.createObjectURL(await response.blob()),a=document.createElement('a');a.href=url;a.download=`city-economy-s${state.seed}-t${state.month}.json`;a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);message('存档已导出，包含你的城市、同步对照和规划记录。');}catch(error){message(error.message,true);}};
$('load').onchange=async e=>{const file=e.target.files[0];if(!file)return;try{if(file.size>16*1024*1024)throw Error('存档超过 16 MiB');const save=JSON.parse(await file.text());selectedNode=8;selection={kind:'node',id:8};$('world').value='player';await mutate('/api/load',{save},'存档已载入。旧商业快照没有逐笔路线时，推进一期即可开始记录。');}catch(error){message(error.message,true);}finally{e.target.value='';}};
(async()=>{setBusy(true);try{state=await request('/api/state');render();await inspect();message('城市已就绪。先观察，再记录一个预期并试一项规划。');}catch(error){message(`无法连接本地模拟：${error.message}`,true);}finally{setBusy(false);}})();
