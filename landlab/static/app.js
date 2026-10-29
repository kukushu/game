'use strict';
const $=id=>document.getElementById(id),esc=x=>String(x??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const n=(v,d=2)=>v!==null&&v!==undefined&&Number.isFinite(Number(v))?Number(v).toFixed(d):'—',cashK=v=>v!==null&&v!==undefined&&Number.isFinite(Number(v))?`${Number(v)<0?'−':''}${Math.abs(Number(v))>0&&Math.abs(Number(v))<1?'＜1':Math.round(Math.abs(Number(v)))}K`:'—',money=v=>v===null||v===undefined?'—':cashK(v/100),hours=v=>`${n(v)} 小时`;
const zoneNames={reserve:'保留空地',home:'住宅许可',shop:'商业许可',factory:'工业许可',mixed:'住商混合许可',open:'自由混合许可'};
const zoneShort={reserve:'留白',home:'住宅',shop:'商业',factory:'工业',mixed:'住商',open:'混合'},zoneColors={reserve:'#9ca58c',home:'#668da5',shop:'#4c8b68',factory:'#b38457',mixed:'#629b92',open:'#9683ad'};
const named={8:'槐树街',9:'东槐街',15:'林荫里',16:'中心里',17:'中街',21:'西南里',22:'南苑',23:'南街',10:'北市街',24:'南市街'};
const place=id=>named[id]||`${['北郊','北侧','中部','南侧','南郊'][Math.floor(id/7)]}${['西街','二街','三街','四街','五街','六街','东街'][id%7]}`;
const point=id=>({x:110+(id%7)*180,y:104+Math.floor(id/7)*140}),edgeId=(a,b)=>Math.min(a,b)*35+Math.max(a,b);
const food=g=>g==='food'?'食品':'日用品',shopName=s=>`${s.assortment?.length===2?'综合':food(s.assortment?.[0])}${s.area===4?'超市':'小店'}`,factoryName=p=>`${food(p.good)}工厂 ${p.id+1}`;
let state=null,tool='inspect',worldKey='player',selection=null,detail=null,zoneChoice='home',showZones=false,busy=false,inspectSequence=0,toastTimer=null,guideRoad=null,lastReport=null,impacts=[],impactVisible=false,mapLayer='none',layersOpen=false,hoveredNode=null,commercialShown=5;
const world=()=>state.worlds[worldKey],readonly=()=>worldKey==='reference',planning=()=>state?.stage==='planning';
function groups(city,node){const people=city.residents.filter(h=>h.node===node).sort((a,b)=>a.id-b.id),out=[];for(let i=0;i<people.length;i+=4)out.push(people.slice(i,i+4));return out;}

// Symbols are buildings only; plots and zoning never instantiate an economic actor.
const artDefs=`<defs>
<symbol id="house-art" viewBox="-22 -28 44 56"><ellipse class="building-shadow" cy="21" rx="21" ry="6" fill="#b8c5a6"/><rect x="-15" y="-9" width="30" height="29" rx="2" fill="#f8e1b8" stroke="#9f9376"/><path d="m-21-8 21-18L21-8Z" fill="#b87651" stroke="#855d49"/><path d="m-18-8 18-14L18-8" fill="#d69a69"/><path d="M9-22v-7h5v11" fill="#8a7766"/><path d="M-11-4h7v8h-7zm16 0h7v8H5zM-11 9h7v7h-7z" fill="#86aab1" stroke="#fff7dd"/><rect x="4" y="7" width="8" height="13" fill="#716f58"/></symbol>
<symbol id="shop-art" viewBox="-33 -26 66 52"><ellipse class="building-shadow" cy="23" rx="33" ry="5" fill="#b4c3a4"/><rect x="-27" y="-10" width="54" height="31" rx="2" fill="#f2ddab" stroke="#af9e78"/><rect x="-29" y="-23" width="58" height="14" rx="2" fill="#697f72"/><rect x="-22" y="-19" width="44" height="6" fill="#e9efdf"/><rect x="-22" y="4" width="28" height="17" fill="#80a4a7" stroke="#f8ecd1" stroke-width="2"/><path d="M-9 5v15" stroke="#dce5d4"/><rect x="11" y="3" width="11" height="18" fill="#6e8a83"/><path d="M-30-10H30l3 12h-66Z" fill="#c87159" stroke="#ad644e"/><path d="m-22-10-2 12m16-12L-9 2M7-10 8 2m15-12 2 12" stroke="#fff0d9" stroke-width="7"/><path d="M-31 3v20m62-20v20" stroke="#8c8062" stroke-width="2"/></symbol>
<symbol id="factory-art" viewBox="-36 -36 72 72"><ellipse class="building-shadow" cy="28" rx="36" ry="7" fill="#b0bba5"/><rect x="-29" y="-9" width="58" height="36" fill="#a6aaa0" stroke="#737e78"/><path d="M-32-6v-12l18 9v-12L5-9v-12L29-6Z" fill="#758e8c" stroke="#546d6d"/><rect x="18" y="-33" width="9" height="31" fill="#a77f68" stroke="#746d5c"/><path d="M17-29h11m-11 7h11" stroke="#d0b39a" stroke-width="2"/><path d="M-24 1h12v9h-12zm18 0H6v9H-6z" fill="#718f94" stroke="#d2d9cb"/><rect x="12" y="10" width="13" height="17" fill="#5f7473"/><path d="M-26 19h28m-28 5h28" stroke="#c4c8b8" stroke-width="2"/></symbol>
</defs>`;
function art(kind){return `<svg viewBox="-50 -45 100 90" aria-hidden="true"><use href="#${kind==='producer'?'factory':kind==='store'?'shop':'house'}-art" x="-35" y="-35" width="70" height="70"/></svg>`;}
function toast(text,error=false){clearTimeout(toastTimer);$('status').textContent=text;$('status').classList.toggle('error',error);$('status').hidden=false;toastTimer=setTimeout(()=>$('status').hidden=true,error?8000:5000);}
function setBusy(v,mode="update"){busy=v;$("busy-overlay").querySelector("strong").textContent=mode==="advance"?"城市正在度过这几个月…":"正在读取或保存你的城市…";$("busy-overlay").querySelector("span").textContent=mode==="advance"?"居民和企业正在自己作决定。":"只有点击推进，经济才会继续运行。";$('busy-overlay').hidden=!v;document.querySelectorAll('button,input,select').forEach(b=>b.disabled=v);if(!v&&state)availability();}
async function api(path,body){const r=await fetch(path,body===undefined?{}:{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)}),data=await r.json();if(!r.ok){if(r.status===409){state=await api('/api/state');detail=null;render();}throw Error(data.error||'操作未完成');}return data;}
function availability(){if(planning()){$('advance-one').disabled=true;$('advance-twelve').disabled=true;$('report-open').disabled=true;$('anchor').disabled=true;}document.querySelectorAll('[data-tool]').forEach(b=>b.disabled=busy||(readonly()&&b.dataset.tool!=='inspect'));$('advance').disabled=busy||state.month+3>240;$('advance-one').disabled=busy||planning()||state.month+1>240;$('advance-twelve').disabled=busy||planning()||state.month+12>240;document.querySelectorAll('.unavailable-layer').forEach(b=>b.disabled=true);const taxLocked=busy||planning()||readonly()||!world().fiscal;$('tax-rate').disabled=taxLocked;document.querySelectorAll('[data-tax-rate]').forEach(b=>b.disabled=taxLocked);$('tax-apply').disabled=taxLocked||Number($('tax-rate').value)===(world().fiscal?.tax_rate||0);const apply=$('apply-action'),pending=pendingAction();if(apply)apply.disabled=busy||readonly()||!pending||world().civic<pending.cost/100;}
function recommendRoad(){if(planning())return null;const city=state.worlds.player,roads=new Set(city.roads.map(([a,b])=>edgeId(a,b)));if(!roads.has(edgeId(9,16)))return {a:9,b:16};const out=[];for(let a=0;a<28;a++)if(!roads.has(edgeId(a,a+7)))out.push({a,b:a+7,score:city.nodes[a].residents+city.nodes[a+7].residents});return out.sort((a,b)=>b.score-a.score)[0]||null;}
function render(){const city=world(),space=city.space.at(-1)||{};if(planning()&&city.nodes.some(n=>n.zone!=='reserve'))showZones=true;$('budget').textContent=cashK(state.worlds.player.civic);$('population').textContent=`${city.residents.length} 户`;$('commute').textContent=planning()?'尚无居民':city.space.length?hours(space.commute):'尚未结算';$('month').textContent=planning()?'规划阶段':`第 ${state.month} 月`;$("advance").innerHTML=planning()?'开放城市 ▶':'推进 3 个月 <span>▶</span>';$("compare-view").hidden=planning();$('seed').value=state.seed;$('preset').value=planning()?'blank':state.mode;$('map-caption').textContent=planning()?'空白场地 · 规划中':readonly()?'未新增干预的同城对照 · 只读':'你的城市 · 已暂停';$('compare-view').textContent=readonly()?'返回你的城市':'查看未新增规划的对照';$('zones-toggle').textContent=showZones?'隐藏用地许可':'显示用地许可';$('zones-toggle').setAttribute('aria-pressed',String(showZones));$('audit').textContent=city.metrics?.cash_error===0?'现金与商品守恒检查通过。':'尚未结算。';document.querySelectorAll('[data-tool]').forEach(b=>{b.classList.toggle('active',b.dataset.tool===tool);b.setAttribute('aria-pressed',String(b.dataset.tool===tool));});$('tool-help').textContent=planning()&&tool==='inspect'?'地图还空着。先选择修路或规划工具，从任意街区开始。':readonly()?'这是未新增规划的对照。切回你的城市后可以施工。':tool==='road'?'点击 ＋ 直接修建，立即扣除 12K；点已有道路可查看或改良。':tool==='zone'?`当前画笔：${zoneShort[zoneChoice]}。点击地块直接规划，每块 约3K；同用途不重复收费。许可不会直接变出建筑。`:'点击一栋建筑，了解住在这里的人或经营状况。';renderFiscal();renderLayers();renderZoneBrush();renderMission();renderMap();renderInspector();availability();}
const mapLayers={
 none:{name:'城市原貌',hint:'查看真实建筑与道路'},
 population:{name:'人口分布',hint:'实际家庭户数',unit:'户',max:32,digits:0,colors:['#e8f3fb','#175584'],description:'实际居住的家庭数，不是规划容量或个人数量。'},
 housing_rent:{name:'住宅租金',hint:'现有住户与住宅用地报价',unit:'K/户/期',currency:true,max:2,digits:2,colors:['#fff2db','#9d551f'],description:'当前住宅租金报价；不是土地售价，也不是家庭实际缴清的金额。现有住户或住宅许可地块显示报价，其余为灰色。'},
 wage_income:{name:'工资收入',hint:'按住址 · 每户税前工资',unit:'K/户/期',currency:true,max:8,digits:2,colors:['#e7f4ea','#276a46'],description:'本期实际工资按家庭住址归集并取每户平均，含未工作家庭的零工资，不含分红。时薪统一为 1K / 小时。'},
 net_income:{name:'税后总收入',hint:'按住址 · 工资与分红扣税后',unit:'K/户/期',currency:true,max:12,capped:true,digits:2,colors:['#e7f4ea','#276a46'],description:'本期实际工资与分红减去收入税，按住址取每户平均；不是存款或扣除购物、房租后的结余。高于12K使用最深色。'},
 commute:{name:'通勤时间',hint:'按住址 · 每户实际通勤',unit:'小时/户/期',max:8,digits:2,colors:['#fff0df','#a54627'],description:'本期实际通勤按住址取每户平均，含没有通勤的家庭。不是到最近工厂的预测距离。'},
 shortage:{name:'购物缺口',hint:'每户未买齐的商品件数',unit:'件/户/期',max:4,digits:2,colors:['#fff0df','#a54627'],description:'每户目标为2件食品和2件日用品。这里显示未买齐的平均件数；买不齐可能与缺货、现金或出行有关。'},
 exposure:{name:'生产干扰',hint:'按真实产量与距离计算',unit:'指数',max:2,capped:true,digits:3,colors:['#fff0df','#a54627'],description:'按最近实际产量和几何距离计算的模型指数，没有现实污染浓度单位。高于2使用最深色。'}
};
function layerValue(node){
 if(!node||mapLayer==='none')return null;
 if(node.layer_data&&Object.hasOwn(node.layer_data,mapLayer))return node.layer_data[mapLayer];
 if(mapLayer==='population')return node.residents;
 if(mapLayer==='housing_rent'&&(node.residents>0||['home','mixed','open'].includes(node.zone)))return node.home_rent;
 return null;
}
function layerText(node){const value=layerValue(node),def=mapLayers[mapLayer];return value===null||value===undefined?'无数据':def.currency?`${cashK(value)} / 户 / 期`:`${n(value,def.digits)} ${def.unit}`;}
function layerColor(node){
 const value=layerValue(node);if(value===null||value===undefined||!Number.isFinite(value))return 'url(#layer-no-data-pattern)';
 const def=mapLayers[mapLayer],t=Math.max(0,Math.min(1,value/def.max));
 const rgb=def.colors.map(c=>[1,3,5].map(i=>parseInt(c.slice(i,i+2),16)));
 return `rgb(${rgb[0].map((v,i)=>Math.round(v+(rgb[1][i]-v)*t)).join(',')})`;
}
function renderLayers(){
 const def=mapLayers[mapLayer],active=mapLayer!=='none';
 $('layers-toggle').textContent=active?`▤ 图层：${def.name}`:'▤ 城市图层';$('layers-toggle').setAttribute('aria-expanded',String(layersOpen));$('layers-panel').hidden=!layersOpen;
 $('layers-options').innerHTML=Object.entries(mapLayers).map(([key,d])=>`<button data-map-layer="${key}" aria-pressed="${key===mapLayer}" class="${key===mapLayer?'active':''}"><span>${d.name}</span><small>${d.hint}</small></button>`).join('');
 $('layer-legend').hidden=!active;if(!active)return;
 $('layer-title').textContent=`${def.name} · ${readonly()?'同城对照':'你的城市'}`;
 $('layer-gradient').style.background=`linear-gradient(to right,${def.colors.join(',')})`;
 $('layer-low').textContent=`0 ${def.unit}`;$('layer-high').textContent=`${def.capped?'≥ ':''}${def.max} ${def.unit}`;
 $('layer-description').textContent=def.description;
 const count=world().nodes.filter(node=>layerValue(node)!==null&&layerValue(node)!==undefined).length;
 $('layer-no-data').textContent=`斜线灰格＝无数据（${35-count}块）。${planning()?'规划阶段没有经济主体。':mapLayer!=='population'&&mapLayer!=='housing_rent'&&state.month===0?'尚未推进首次结算。':'按当前期显示；两城与不同月份使用同一色阶。'}`;
 if(!world().nodes.some(node=>node.layer_data)&&!['population','housing_rent'].includes(mapLayer))$('layer-no-data').textContent='当前 Python 服务尚未提供此数据，请停止并重新启动服务；缺失数据不会显示成零。';
 const node=world().nodes.find(n=>n.id===hoveredNode);$('layer-readout').textContent=node?`${place(node.id)}：${layerText(node)}`:'指向或点选街区查看数值';
}
function setMapLayer(key){if(busy||!mapLayers[key])return;mapLayer=key;layersOpen=false;hoveredNode=null,commercialShown=5;chooseTool('inspect');$('layers-toggle').focus();}
function renderFiscal(){
  const f=world().fiscal,player=state.worlds.player,own=player.fiscal;
  $('budget-note').textContent=planning()?'开城后可设收入税':own?`上期税 ${money(own.tax_income)} / 下期养护 ${money(own.next_upkeep_due)}`:'重启服务以启用财税';
  $('budget-note').classList.toggle('budget-warning',!planning()&&own&&player.civic<own.next_upkeep_due/100);
  $('tax-rate').value=f?.tax_rate||0;$('tax-output').textContent=`${f?.tax_rate||0}%`;
  if(!f){$('fiscal-body').innerHTML=planning()?'<p class="lead">规划阶段没有居民和收入，也不收税。开放城市后，可以设定税率。</p>':'<p class="lead">当前 Python 服务还是旧版本。先运行 stop-land.cmd，再运行 run-land.cmd，即可从当前存档继续并启用财税。</p>';}
  else{
    const warning=world().civic<f.next_upkeep_due/100?'现款不足以下个月养护。调税不会立即生成税款；需先推进时间。':f.maintenance_ratio<1?'现款已可支付下个月养护，实际恢复需等待下个月结算。':'当前现款可以支付下个月的道路养护。';
    $('fiscal-body').innerHTML=`<p class="eyebrow">${readonly()?'同城对照 · 只读':'你的城市'} · 当前税率 ${f.tax_rate}%</p>${facts([['公共现款',cashK(world().civic)],['上期实际税收',money(f.tax_income)],['上期养护实付',money(f.upkeep_paid)],['下个月养护需要',money(f.next_upkeep_due)],['上期居民税前收入',money(f.gross_income)],['上期居民税后收入',money(f.net_income)]])}<div class="reason"><strong>养护状况：${n(f.maintenance_ratio*100,0)}%</strong>${warning}</div><p class="info-note">${f.dividends_enabled===false?'自动分红已暂停：工厂、门店、土地服务账户和投资池保留各自现金；历史分红仍保留。':''}金额按K（千元）取整显示，实际交易保留精度。居民现有现金合计 ${money(f.household_cash)}。税收入库后可用于养护、修路和规划；攒在财政里的钱不会自动回到居民手中。</p>`;
  }
  $('tax-hint').textContent=planning()?'开放城市后才能调整税率。':!f?'需要重启 Python 服务，单独刷新网页不会加载新模拟规则。':readonly()?'这是只读对照，请切回你的城市调整政策。':'应用只改变后续收入的代扣比例，不会立即扣存款。可先试 1%，再推进三个月看实际结果。';
}
function renderMission(){if(planning()){renderPlanningMission();return;}if(state.greenfield&&state.month===0){$('mission-number').textContent='3';$('mission-kicker').textContent='开城初始化完成 / 尚未开始生产交易';$('mission-title').textContent='首批家庭与工厂已按规划落地';$('mission-copy').textContent='这是开城初始化，不是规划时暗中运行的主体。现在推进三个月，开始真实生产与交易。';$('mission-action').textContent='推进 3 个月 →';$('mission-action').dataset.step='2';return;}let step=1,title='让居民少绕一点路',copy='西边住人，东边生产。城内缺少南北连接，一些出行要绕到边缘。',action='试修第一条路 →';const last=state.actions.at(-1);if(last&&last.month===state.month){step=2;title=last.action==='road'?'路修好了，让城市走上几个月':last.action==='tax'?'税率已调整，观察实际收支':'许可已变，等待居民与企业选择';copy=last.action==='road'?'居民、商店和工厂还没有重新作决定。推进 3 个月，看看实际反应。':last.action==='tax'?'实际收入发生时才会代扣。推进三个月，查看养护、税收与居民购买的反应。':'地上不会立即长出建筑。推进 3 个月，看看有没有人愿意来。';action='推进 3 个月 →';}else if(lastReport||last){step=3;title='看看谁受益，谁遇到了新问题';copy='对照出行时间，再点一户家庭或一家店，看看它为什么这样选择。';action='查看城市变化 →';}else if(state.month!==6||state.seed!==1||state.mode!=='adaptive'){title='继续改善你的城市';copy='先查看一处住宅或商店，找出一个出行、购物或用地问题，再试一项改善。';}$('mission-number').textContent=step;$('mission-kicker').textContent=step===1?'新手情景 / 先做一次小改善':step===2?'下一步 / 让真实模拟运行':'观察与判断 / 不保证每项指标都改善';$('mission-title').textContent=title;$('mission-copy').textContent=copy;$('mission-action').textContent=action;$('mission-action').dataset.step=step;}
function routeSvg(routes){return (routes||[]).map(r=>(r.edges||[]).map(([a,b])=>{const p=point(a),q=point(b),color=r.mode==='truck'?'#d38942':r.mode==='commute'?'#558fc0':r.mode==='detour'?'#c58948':'#2e9274';return `<path d="M${p.x} ${p.y}L${q.x} ${q.y}" fill="none" stroke="${color}" stroke-width="5" stroke-linecap="round" ${r.mode==='detour'?'stroke-dasharray="7 5"':''} pointer-events="none"/>`;}).join('')).join('');}
function building(kind,id,node,slot,label,extra={}){const p=point(node),large=kind!=='home'&&extra.area===4,x=p.x+(large?-43:[-57,-25,25,57][slot%4]),y=p.y+(slot<4?-40:40),w=kind==='producer'?70:large?67:kind==='store'?42:32,h=kind==='producer'?67:large?52:kind==='store'?37:42,selected=selection?.kind===kind&&selection.id===id&&selection.node===node,ref=kind==='home'?'house':kind==='producer'?'factory':'shop';return `<g class="city-building" role="button" tabindex="0" data-building="${kind}" data-kind="${kind}" data-id="${id}" data-node="${node}" aria-label="${esc(label)}"><title>${esc(label)}</title><rect class="focus-ring" x="${x-w/2-3}" y="${y-h/2-3}" width="${w+6}" height="${h+6}" rx="5" style="${selected?'stroke:#b37e39;stroke-width:2.5':''}"/><use href="#${ref}-art" x="${x-w/2}" y="${y-h/2}" width="${w}" height="${h}"/>${kind==='producer'&&extra.production>0?`<path d="M${x+20} ${y-36}q-8-3-3-7q4-3 7-1q5 0 5 5q0 4-9 3" fill="#b5bcb2" opacity=".7" pointer-events="none"/>`:''}</g>`;}
function renderMap(){const city=world(),roads=new Map(city.roads.map(([a,b,v])=>[edgeId(a,b),v]));let ground='',streets='',plots='',buildings='';for(const node of city.nodes){const p=point(node.id),overlay=showZones||tool==='zone',thematic=mapLayer!=='none',selected=selection?.kind==='node'&&selection.node===node.id;ground+=`<g class="land-target ${selected?'selected-land':''}" role="button" tabindex="0" data-plot="${node.id}" data-node="${node.id}" aria-label="${place(node.id)}地块，${zoneNames[node.zone]}，${node.residents}户，${node.stores.length}家商店，${node.producers.length}家工厂${thematic?'，'+mapLayers[mapLayer].name+' '+layerText(node):''}"><title>${place(node.id)} · ${zoneNames[node.zone]} · 空余 ${node.free}/8${thematic?' · '+mapLayers[mapLayer].name+' '+layerText(node):''}</title><rect x="${p.x-80}" y="${p.y-61}" width="160" height="122" rx="13" fill="${thematic?layerColor(node):overlay?zoneColors[node.zone]:'#cfdbc0'}" fill-opacity="${thematic?'.88':overlay?'.17':'.28'}" stroke="${selected?'#b9934b':overlay?zoneColors[node.zone]:'transparent'}" stroke-width="${selected?3:1.3}" ${overlay?'stroke-dasharray="5 4"':''}/></g>`;if(!node.residents&&!node.stores.length&&!node.producers.length)plots+=`<g pointer-events="none"><path d="M${p.x-52} ${p.y+24}h29v23h-29zM${p.x+24} ${p.y-48}h29v23h-29z" fill="#dce5cf" stroke="#a8b99a" stroke-dasharray="3 3" stroke-width=".8"/></g>`;if(overlay)plots+=`<g pointer-events="none"><rect x="${p.x-29}" y="${p.y-65}" width="58" height="16" rx="3" fill="#fcfff0" stroke="${zoneColors[node.zone]}" stroke-dasharray="2 2"/><text class="zone-overlay-label" x="${p.x}" y="${p.y-54}" text-anchor="middle">${zoneShort[node.zone]}许可</text></g>`;let slot=0;for(const p of city.producers.filter(x=>x.node===node.id)){buildings+=building('producer',p.id,node.id,slot,`${factoryName(p)}，在${place(node.id)}，本月实际生产${p.production}件`,{area:4,production:p.production});slot+=4;}for(const s of city.stores.filter(x=>x.active&&x.node===node.id)){buildings+=building('store',s.id,node.id,slot,`${shopName(s)} ${s.id+1}，在${place(node.id)}，本月${s.customers}次顾客访问`,s);slot+=s.area;}groups(city,node.id).forEach((g,i)=>{buildings+=building('home',i,node.id,slot,`${place(node.id)}住宅单元 ${i+1}，实际住有${g.length}户家庭`);slot++;});}
  for(let a=0;a<35;a++)for(const b of [a+1,a+7]){if(b>=35||(b===a+1&&a%7===6))continue;const id=edgeId(a,b),level=roads.get(id)||0,p=point(a),q=point(b),path=`M${p.x} ${p.y}L${q.x} ${q.y}`,recommended=guideRoad?.a===a&&guideRoad?.b===b;if(level)streets+=`<path d="${path}" fill="none" stroke="#b5bfae" stroke-width="22" stroke-linecap="round"/><path d="${path}" fill="none" stroke="${level===2?'#6d8582':'#8b9489'}" stroke-width="16" stroke-linecap="round"/><path d="${path}" fill="none" stroke="#e6e7ce" stroke-width="1.2" stroke-dasharray="8 7"/>`;if(!level&&(tool==='road'||recommended))streets+=`<path class="ghost-road" d="${path}" fill="none" stroke="${recommended?'#dca34d':'#77a091'}" stroke-width="${recommended?9:5}" stroke-dasharray="6 6" opacity=".8"/>`;if(selection?.kind==='road'&&selection.id===id)streets+=`<path class="selected-edge" d="${path}"/>`;if(level||tool==='road'||recommended)streets+=`<g class="road-target" role="button" tabindex="${tool==='road'?0:-1}" data-road="${id}" aria-label="${level?'改良':'新建'}道路：${place(a)}到${place(b)}，${level===2?'已改良':`费用${level?9:12}K`}"><title>${place(a)} ↔ ${place(b)} · ${level===2?'已改良':level?'可改良 9K':'可新建 12K'}</title><path class="road-hit" d="${path}"/>${!level&&tool==='road'?`<circle cx="${(p.x+q.x)/2}" cy="${(p.y+q.y)/2}" r="${recommended?15:10}" fill="${recommended?'#db9d43':'#f2f6e9'}" stroke="#81a98c"/><text x="${(p.x+q.x)/2}" y="${(p.y+q.y)/2+4}" font-size="12" text-anchor="middle" fill="${recommended?'#fff':'#608d73'}">${recommended?'12K':'+'}</text>`:''}</g>`;}
  const hx=city.residents.reduce((s,h)=>s+point(h.node).x,0)/city.residents.length,fx=city.producers.reduce((s,p)=>s+point(p.node).x,0)/city.producers.length;const labels=planning()?'<text class="town-label" x="650" y="18" text-anchor="middle">空白场地 · 由你开始铺路与规划</text>':Math.abs(hx-fx)>300?`<text class="town-label" x="${hx}" y="18" text-anchor="middle">住宅街坊</text><text class="town-label" x="${fx}" y="18" text-anchor="middle">生产厂区</text>`:'<text class="town-label" x="650" y="18" text-anchor="middle">居住与生产交织的小城</text>';let pins='';if(impactVisible)impacts.forEach((item,i)=>{const p=point(item.node);pins+=`<g class="impact-pin" role="button" tabindex="0" data-impact="${i}" aria-label="变化${i+1}：${esc(item.title)}"><circle class="impact-ring" cx="${p.x}" cy="${p.y}" r="57"/><circle cx="${p.x+65}" cy="${p.y-53}" r="13" fill="#b77e3f" stroke="#fff7dd" stroke-width="2"/><text x="${p.x+65}" y="${p.y-49}" text-anchor="middle" fill="white" font-size="11">${i+1}</text></g>`;});
  $('map').dataset.tool=tool;$('map').dataset.layer=mapLayer;$('map').innerHTML=`<svg viewBox="0 0 1300 735" aria-label="城市俯视图：真实住宅单元、商店、工厂，道路与未开发地块">${artDefs}<defs><pattern id="layer-no-data-pattern" width="8" height="8" patternUnits="userSpaceOnUse"><rect width="8" height="8" fill="#d7ddd4"/><path d="M-2 2L2-2M0 8L8 0M6 10L10 6" stroke="#b7c0b5" stroke-width="1"/></pattern></defs>${ground}${streets}${plots}${routeSvg(detail?.routes)}${buildings}${labels}${pins}<text class="map-caption-svg" x="27" y="718">住宅每图标至多 4 户 · 区内位置为示意</text>${showZones||tool==='zone'?'<text class="map-caption-svg" x="1273" y="718" text-anchor="end">虚线色框 = 未来许可，不是已建建筑</text>':''}</svg>`;
  $('map').onpointerover=e=>{if(mapLayer==='none')return;const target=e.target.closest('[data-node]');if(target){hoveredNode=Number(target.dataset.node);const node=world().nodes.find(n=>n.id===hoveredNode);$('layer-readout').textContent=`${place(hoveredNode)}：${layerText(node)}`;}};
  $('map').onclick=handleMapClick;$('map').onkeydown=e=>{if(['Enter',' '].includes(e.key)&&e.target.matches('[role="button"]')){e.preventDefault();e.target.dispatchEvent(new MouseEvent('click',{bubbles:true}));}};
}
function handleMapClick(e){if(busy)return;const metricTarget=e.target.closest('[data-node]');if(metricTarget){hoveredNode=Number(metricTarget.dataset.node);if(mapLayer!=='none')renderLayers();}if(tool==='zone'&&!readonly()){const target=e.target.closest('[data-plot],[data-building]');if(target){paintZone(Number(target.dataset.node));return;}}const pin=e.target.closest('[data-impact]');if(pin){openImpact(Number(pin.dataset.impact));return;}const actor=e.target.closest('[data-building]');if(actor){select(tool==='zone'?{kind:'node',id:Number(actor.dataset.node),node:Number(actor.dataset.node)}:{kind:actor.dataset.kind,id:Number(actor.dataset.id),node:Number(actor.dataset.node)});return;}const road=e.target.closest('[data-road]');if(road){const id=Number(road.dataset.road),a=Math.floor(id/35),b=id%35;if(tool==='road'&&!readonly()&&!world().roads.some(([x,y])=>edgeId(x,y)===id)){buildRoad(a,b);return;}select({kind:'road',id});return;}const plot=e.target.closest('[data-plot]');if(plot)select({kind:'node',id:Number(plot.dataset.plot),node:Number(plot.dataset.plot)});}
function renderZoneBrush(){
  $('zone-brush').hidden=tool!=='zone'||readonly();
  document.querySelectorAll('[data-paint-zone]').forEach(b=>{const active=b.dataset.paintZone===zoneChoice;b.classList.toggle('active',active);b.setAttribute('aria-pressed',String(active));});
}
async function paintZone(id){
  if(busy||readonly())return;
  const zone=zoneChoice,node=world().nodes.find(n=>n.id===id);
  if(!node)return;
  if(node.zone===zone){toast(`${place(id)}已经是${zoneNames[zone]}，没有扣款。`);return;}
  if(world().civic<2.5){toast('建设预算不足：变更这块用地需要 约3K，当前预算 '+cashK(world().civic)+'。',true);return;}
  if(await mutate('/api/action',{action:'zone',node:{id,zone},note:'玩家使用地图用地画笔直接规划'},`${place(id)}已改为${zoneNames[zone]}，花费 约3K。可继续点地块规划。`)){
    selection=null;detail=null;guideRoad=null;impactVisible=false;showZones=true;tool='zone';$('map-callout').hidden=true;render();
  }
}
async function buildRoad(a,b){
  if(busy||readonly())return;
  if(world().civic<12){toast('建设预算不足：修建这段道路需要 12K，当前预算 '+cashK(world().civic)+'。',true);return;}
  const ok=await mutate('/api/action',{action:'road',node:{id:a,neighbor:b},note:'玩家点击地图加号直接修建道路'},`${place(a)} ↔ ${place(b)} 已接通，花费 12K。可以继续点击 ＋ 铺路。`);
  if(ok){guideRoad=null;impactVisible=false;selection=null;detail=null;tool='road';$('inspector').hidden=true;$('map-callout').hidden=false;$('map-callout').textContent='道路已接通。点击下一个 ＋ 继续修路，每段 12K。';render();}
}
async function select(item,keepImpacts=false){if(busy)return;commercialShown=5;$("map-callout").hidden=true;if(!keepImpacts)impactVisible=false;const token=++inspectSequence;selection=item;detail=null;$('inspector').hidden=false;renderMap();renderInspector();if(item.kind==='home')return;const key=worldKey,revision=state.revision;try{const result=await api(`/api/inspect?kind=${item.kind}&id=${item.id}&world=${key}`);if(token!==inspectSequence||revision!==state.revision||key!==worldKey)return;detail=result;if(item.kind==='node')zoneChoice=result.zone;renderInspector();renderMap();availability();}catch(e){toast(e.message,true);}}
function facts(items){return '<div class="facts">'+items.map(([a,b])=>`<div><span>${esc(a)}</span><strong>${esc(b)}</strong></div>`).join('')+'</div>';}
function pick(kind,id,node,label,sub='',status=''){return `<button class="${esc(status)}" data-pick="${kind}" data-id="${id}" data-node="${node}"><span>${esc(label)}</span><small>${esc(sub)}</small></button>`;}
function shoppingStatus(people){
 const missing=people.filter(h=>h.food<2||h.goods<2).length;
 const foodMissing=people.filter(h=>h.food<2).length,goodsMissing=people.filter(h=>h.goods<2).length;
 return {missing,foodMissing,goodsMissing,complete:people.length-missing,
  food:people.reduce((v,h)=>v+h.food,0),goods:people.reduce((v,h)=>v+h.goods,0),target:people.length*2};
}
function homePick(people,index,node){
 const s=shoppingStatus(people),pending=state.month===0;
 const sub=pending?'等待首月购物结算':s.missing?`${s.missing}/${people.length} 户未买齐 · ${[s.foodMissing?'缺食品':'',s.goodsMissing?'缺日用品':''].filter(Boolean).join('、')}`:`${people.length} 户都已买齐`;
 return pick('home',index,node,`住宅单元 ${index+1} · ${people.length} 户`,sub,pending?'supply-pending':s.missing?'supply-warning':'supply-ok');
}
function supplySummary(people){
 if(!people.length)return '';
 if(state.month===0)return '<section class="supply-summary"><strong>商品购买情况 · 待结算</strong><p>居民刚入住，尚未经历第一个月的购物，暂不判定买齐或缺货。</p></section>';
 const s=shoppingStatus(people);
 return `<section class="supply-summary ${s.missing?'supply-warning':'supply-ok'}"><strong>${s.missing?`⚠ ${s.missing} 户商品没买齐`:'✓ 本街区所有家庭都买齐了'}</strong><p>本月 ${s.complete}/${people.length} 户买齐食品和日用品。</p>${facts([['食品已买 / 目标',`${s.food} / ${s.target} 件`],['日用品已买 / 目标',`${s.goods} / ${s.target} 件`]])}<p class="info-note">${s.foodMissing} 户缺食品，${s.goodsMissing} 户缺日用品；两项可能是同一户。每户每月目标各2件，未买齐不一定是卖家缺货，也可能与现金或出行有关。点击下方住宅查看具体家庭。</p></section>`;
}

function bindInspector(){if($('audit-more'))$('audit-more').onclick=()=>{commercialShown+=10;renderInspector();};if($('audit-all'))$('audit-all').onclick=()=>{commercialShown=Infinity;renderInspector();};$('inspector-body').querySelectorAll('[data-pick]').forEach(b=>b.onclick=()=>select({kind:b.dataset.pick,id:Number(b.dataset.id),node:Number(b.dataset.node)}));$('inspector-body').querySelectorAll('[data-zone]').forEach(b=>b.onclick=()=>{zoneChoice=b.dataset.zone;renderInspector();});if($('apply-action'))$('apply-action').onclick=applyAction;if($('begin-zoning'))$('begin-zoning').onclick=()=>{chooseTool('zone');};}
function homeDecision(r){if(state.mode==='fixed')return '当前研究模式固定住址，家庭不会自主搬迁。';if(state.mode==='blind')return '当前研究模式随机选择合法地点，不比较生活收益；仍须满足现金、容量与搬迁冷却。';if(!r&&state.greenfield)return '首批家庭按开城的合法用地和通路选择位置，目前还没有新的迁居考察。';if(!r)return '它还没有新的迁居考察记录。下一次被选中时，会比较生活便利、房租与生产干扰。';const parts=[['房租',r.current.rent-r.candidate.rent],['购物',r.current.shopping-r.candidate.shopping],['通勤',r.current.commute-r.candidate.commute],['生产干扰',r.current.environment-r.candidate.environment]].sort((a,b)=>b[1]-a[1]);if(r.reason==='搬迁')return `第 ${r.month+1} 月，它从${place(r.from)}搬到${place(r.to)}。当时最吸引它的是${parts[0][0]}条件；合计预期改善足以补偿搬迁。${r.candidate.environment>r.current.environment?'它也接受了更高的生产干扰。':''}${r.candidate.rent>r.current.rent?'新住处的房租报价更高。':''}`;if(r.reason==='现金不足')return `第 ${r.month+1} 月，它考虑换住处，但现款不足以支付搬迁费，所以留下了。`;if(r.reason==='同地居住')return `第 ${r.month+1} 月，它比较后仍选择原来的住处，暂时没有更值得搬去的地方。`;if(r.move_policy==='stable')return `第 ${r.month+1} 月，换住处预期可节约 ${money(Math.max(0,r.gain))}，未超过本次需要的 ${money(r.required_gain)}，所以它选择留下。这里包含时间和环境的估值，不是单纯现金。`;return `第 ${r.month+1} 月，换住处的预期改善还不足以补偿搬迁代价，所以它没有搬家。`;}
function cashflowPath(purpose,incoming){
 const wages={manufacturing:'生产工资',factory_service:'柜台工资',retail_staff:'店员工资',freight:'货运工资'};
 const dividends={retail_dividend:'门店分红',factory_dividend:'工厂分红',household_dividend:'家庭权益分配',fund_dividend:'投资分红'};
 if(wages[purpose])return incoming?['劳动收入',wages[purpose]]:purpose==='freight'?['经营支出','运输费用',wages[purpose]]:['经营支出','人工费用',wages[purpose]];
 if(dividends[purpose])return [incoming?'权益收入':'收益分配',dividends[purpose]];
 if(purpose==='equity_investment')return [incoming?'资本流入':'资本投入','投资本金'];
 if(purpose==='liquidation')return [incoming?'资本返还':'资本退出','清算返还'];
 if(purpose==='lease_setup_fee')return [incoming?'服务收入':'投资与开业费','开店手续费'];
 if(purpose==='home_move'||purpose==='factory_move')return ['搬迁支出',purpose==='home_move'?'家庭搬迁费':'工厂迁址费'];
 if(purpose==='income_tax')return [incoming?'税收收入':'税费','居民收入税'];
 const incomingNames={direct_sale:'工厂直销',retail_sale:'门店销售',wholesale:'商品批发',housing_rent:'住宅租金',industrial_rent:'厂房租金',rent_utilities:'场地服务收费',truck_lease:'车辆租用'};
 if(incoming&&incomingNames[purpose])return ['经营收入',incomingNames[purpose]];
 const outgoingPaths={direct_sale:['生活消费','工厂购物'],retail_sale:['生活消费','门店购物'],housing_rent:['生活支出','住房租金'],wholesale:['经营支出','进货采购','采购付款'],rent_utilities:['经营支出','场地与固定费','开门费用'],industrial_rent:['经营支出','场地与固定费','厂房租金'],truck_lease:['经营支出','运输费用','车辆租用'],road_build:['公共建设','道路施工'],road_maintenance:['公共养护','道路养护'],planning_admin:['规划支出','土地许可费用']};
 return !incoming&&outgoingPaths[purpose]?outgoingPaths[purpose]:[incoming?'其他收入':'其他支出',purpose];
}
function buildCashTree(rows,account){
 const income={name:'收入',total:0,children:[]},expense={name:'支出',total:0,children:[]};
 let operating=0;
 for(const p of rows||[]){
  if(p.source===p.target||p.cents===0)continue;
  const incoming=p.target===account;if(!incoming&&p.source!==account)continue;
  const path=cashflowPath(p.purpose,incoming),root=incoming?income:expense;
  if(path[0]==='经营收入'||path[0]==='经营支出')operating+=(incoming?1:-1)*p.cents;
  root.total+=p.cents;let node=root;
  for(const name of [...path,incoming?p.source:p.target]){
   let child=node.children.find(x=>x.name===name);
   if(!child){child={name,total:0,children:[]};node.children.push(child);}
   child.total+=p.cents;node=child;
  }
 }
 return {income,expense,net:income.total-expense.total,operating};
}
function ledgerParty(account){
 if(account.startsWith('h')&&/^h[0-9]+$/.test(account))return `家庭 ${Number(account.slice(1))+1}`;
 return {property:'土地服务账户',fund:'投资资金池',civic:'公共账户'}[account]||(/^([pr])[0-9]+$/.test(account)?accountName(account):account);
}
function ledger(rows,account){
 const tree=buildCashTree(rows,account),signed=(value,sign)=>`${sign>0?'+':'−'}${money(value)}`;
 function branch(node,sign,depth=0){
  const label=node.children.length?node.name:ledgerParty(node.name),amount=signed(node.total,sign);
  if(!node.children.length)return `<div class="cash-leaf"><span>${esc(label)}</span><strong>${amount}</strong></div>`;
  return `<details ${depth<2?'open':''}><summary><span>${esc(label)}</span><strong>${amount}</strong></summary><div class="cash-children">${node.children.map(c=>branch(c,sign,depth+1)).join('')}</div></details>`;
 }
 return `<section class="cash-tree"><h3>本月现金收支分析</h3><div class="cash-net"><span>现金净变化</span><strong>${tree.net>=0?'+':'−'}${money(Math.abs(tree.net))}</strong></div>${/^[pr]/.test(account)?`<div class="cash-net"><span>经营现金净额</span><strong>${tree.operating>=0?'+':'−'}${money(Math.abs(tree.operating))}</strong></div>`:''}${tree.income.children.length?`<div class="cash-income">${branch(tree.income,1)}</div>`:'<p class="info-note">本月没有现金收入。</p>'}${tree.expense.children.length?`<div class="cash-expense">${branch(tree.expense,-1)}</div>`:'<p class="info-note">本月没有现金支出。</p>'}<p class="info-note">点击三角逐层展开，可查到具体付款或收款账户。金额以K（千元）取整显示，不足1K显示＜1K；分项与合计分别取整可能产生尾差。精确账目中子项合计等于父项，收入减支出等于现金净变化。投资、分红和清算单列，库存变化与核销不在现金树中，因此现金净额不等于经营利润。</p></section>`;
}
function research(content,data){return `<details><summary>账本与决策细节</summary><div>${content}<details><summary>原始观察记录</summary><pre>${esc(JSON.stringify(data,null,2))}</pre></details></div></details>`;}

'use strict';
const auditReasons={no_eligible_sites:'本轮没有符合许可、道路、容量要求且无营业店的商业候选',no_commercial_permission:'当时没有商业许可',disconnected:'当时没有接通城区',insufficient_land:'当时没有空余土地',no_legal_size:'没有合法店面规模',non_positive_profit:'预期利润不大于零',insufficient_capital:'创业资金不足',entry_disabled:'当前关闭了商业投资',direct_only:'当前为工厂直销模式',store_limit:'营业门店达到模型上限（18家）'};
function auditStatus(p){return p.decision==='selected'?'本期选中并投资':p.decision==='other_site_selected'?'可行，但本期选择了别处':p.decision==='legacy_unknown'?'旧记录未保存最终选择':(p.blockers||[]).map(x=>auditReasons[x]||x).join('；')||'未保存结果';}
function auditPlan(p){
 const c=p.costs,items=p.products||[];
 return `<details class="audit-plan"><summary>${p.area===4?'超市':'小店'} · ${p.assortment.map(food).join('＋')} · 预期利润 ${money(p.profit)}<small>${esc(auditStatus(p))}</small></summary>${c?facts([['预计销售收入',money(c.revenue)],['进货商品成本',money(c.purchases)],['运输成本',money(c.freight)],['店面租金',money(c.rent)],['店员工资',`${money(c.staff_wages)}（${n(c.staff_hours)}小时）`],['固定经营费',money(c.operating_fee)],['预计总成本',money(c.total)],['预计利润',money(p.profit)]]):'<p class="info-note">旧版本没有保存成本拆分，无法恢复当时明细。</p>'}
 <p class="info-note">预期利润 = 销售收入 − 进货 − 运输 − 租金 − 人工 − 固定经营费。金额单位为K（千元）/期，取整显示；这是投资时的预测，可能因缺货、竞争或用工不足而落空。</p>
 ${items.length?'<div class="audit-scroll"><table><thead><tr><th>商品</th><th>预计销量</th><th>售价 / 进价</th><th>运输</th></tr></thead><tbody>'+items.map(x=>`<tr><td>${food(x.good)}</td><td>${n(x.quantity)}件</td><td>${money(x.price)} / ${money(x.base_price)}</td><td>${money(x.freight)} · ${x.trips}车次</td></tr>`).join('')+'</tbody></table></div>'+items.map(x=>`<p class="info-note">${food(x.good)}货源：工厂 ${x.supplier+1}，${place(x.source)}；当时库存 ${x.supplier_stock} 件；预计运输 ${n(x.freight_hours)} 小时。${x.quote_rule==='joint_price_freight'?`统一报价：这家工厂的出厂价 ${money(x.base_price)}；按 ${x.batch_quantity} 件采购批次，商品付款 ${money(x.batch_goods_cost)} ＋ 运费 ${money(x.freight)} ＝ 到店合计 ${money(x.landed_cost)}。预计商品成本按期望销量计提，运费保守计入整个批次。未保证当期库存、预算竞争和运输用工，实际采购仍受约束。`:'旧版历史使用最低出厂价与最近运输货源拼接，口径已修正；该旧估价不代表同一家工厂的有效报价，不重写历史数值。'}</p>`).join(''):''}
 ${facts([['所需启动资金',money(p.capital)],['其中周转资金',p.working_capital===undefined?'旧记录未保存':money(p.working_capital)],['其中一次开业费',p.setup_fee===undefined?'旧记录未保存':money(p.setup_fee)],['资金是否足够',p.affordable?'足够':'不足'],['本期利润 / 启动资金',p.score===undefined?'旧记录未保存':n(p.score,4)]])}<p class="info-note">启动资金与开业费单列，不重复扣入本期经营利润。同月合格方案按“预期利润 / 启动资金”比较，最多投资一家。</p></details>`;
}
function commercialSummary(d){
 const history=d.commercial_history,visit=history?.visits.at(-1),round=history?.last_round;
 let text='还没有保存这个地块的考察记录。商业许可只提供进入资格，仍需推进时间进行考察。';
 if(visit){const p=[...visit.plans].sort((a,b)=>b.profit-a.profit)[0];text=`最近一次记录：第 ${visit.month+1} 月。`+(p?`当次预期利润最高的方案，收入 ${p.costs?money(p.costs.revenue):'未保存'}，总成本 ${p.costs?money(p.costs.total):'未保存'}，预期利润 ${money(p.profit)}。${auditStatus(p)}。`:(visit.blockers||[]).map(x=>auditReasons[x]||x).join('；')+'，未进入利润试算。');}
 if(round&&round.status in auditReasons)text+=` 第 ${round.month+1} 月：${auditReasons[round.status]}，没有开展新考察。`;
 else if(round?.selection_rule==='eligible_least_recent'){text+=` 第 ${round.month+1} 月：合法候选 ${round.eligible_nodes.length} 块，实际考察 ${round.selected_nodes.length} 块。`;if(!round.selected_nodes.includes(d.id))text+=round.eligible_nodes.includes(d.id)?'这里正在轮候，尚未轮到；不代表企业认为会亏损。':'当轮未列入候选；若刚修改许可或道路，下一次推进会重新筛选。';}
 else if(round&&(!visit||round.month>visit.month))text+=` 第 ${round.month+1} 月未抽到这里，这不算一次失败考察。`;
 return `<div class="reason"><strong>允许商业，为什么还没有店？</strong>${esc(text)}<p class="info-note">下方逐次保存全部考察方案与当时数值。</p></div>`;
}
function commercialHistory(d){
 const h=d.commercial_history;if(!h)return '<p class="info-note">当前服务尚未提供完整考察历史。</p>';
 const visits=[...h.visits].reverse();
 return `<details class="audit-history" ${commercialShown>5?'open':''}><summary>${['shop','mixed','open'].includes(d.zone)?'商业考察记录':'历史商业考察（当前无商业许可）'} · ${h.total_visits} 次 / ${h.total_plans} 个方案</summary><p class="info-note">从第 ${h.full_since+1} 月起完整记录。更早的旧存档只保留当时尚未丢失的部分数据。每次真实考察均保留，包括亏损、缺钱、选中与未选中。现版本先筛商业许可、连通和容量；候选最多8块时全看，更多时优先考察从未或最久未看的地块。旧版本历史可能包含无资格访问；资格筛选不冒充经营考察。</p>${visits.slice(0,commercialShown).map(v=>`<details class="audit-visit"><summary>第 ${v.month+1} 月 · ${v.plans.length} 个方案${v.legacy_partial?' · 旧版部分记录':''}</summary>${v.legacy_partial?'<p class="info-note">当时的许可、余额和成本拆分未保存，不用当前状态补算。</p>':facts([['当时土地许可',zoneNames[v.zone]],['当时空余土地',`${v.free_land}/8`],['接入城市道路',v.connected?'是':'否'],['城市家庭 / 可达家庭',`${v.households} / ${v.reachable_households}`],['当时创业资金池',money(v.fund_cash)]])}${v.blockers?.length?`<p>${esc(v.blockers.map(x=>auditReasons[x]||x).join('；'))}，没有利润试算。</p>`:''}${roundDecision(v.comparison,v.node)}${v.plans.map(auditPlan).join('')}${v.winner?`<p class="info-note">本轮实际投资：${place(v.winner.node)}，门店 ${v.winner.store+1}；利润/启动资金 ${n(v.winner.score,4)}。</p>`:''}</details>`).join('')||'<p>尚无真实考察记录。推进时间后，企业才会考察。</p>'}${visits.length>commercialShown?'<button id="audit-more">再显示 10 次</button><button id="audit-all">显示全部历史</button>':''}<p class="info-note">完整历史随“保存城市”导出，加载后继续累积。页面默认只展开最近5次，不删除更早记录。</p></details>`;
}
function roundDecision(r,node){
 if(!r)return '<p class="info-note">旧记录未保存同轮完整比较，无法还原当时为什么胜过其他地点。新推进的选址会保存。</p>';
 if(!Array.isArray(r.proposals))return '<p class="info-note">这一轮只保存了部分结果，没有保存跨地点方案排名，不用当前数据补算。</p>';
 const rows=[...r.proposals].sort((a,b)=>(a.rank??999)-(b.rank??999)),chosen=rows.find(p=>p.decision==='selected'),valid=rows.filter(p=>p.rank!==null),ties=chosen&&valid.filter(p=>p.score===chosen.score).length>1;
 let why=chosen?`本轮比较了 ${r.selected_nodes?.length??new Set(rows.map(p=>p.node)).size} 个地点、${rows.length} 个经营方案，其中 ${valid.length} 个同时满足“利润大于0、资金足够”。胜出的是${place(chosen.node)}的${chosen.area===4?'超市':'小店'}（${chosen.assortment.map(food).join('＋')}）：预计利润 ${money(chosen.profit)} ÷ 启动资金 ${money(chosen.capital)}，按未取整金额计算的分数为 ${n(chosen.score,6)}，是合格方案中的最高值。`:rows.length?'所有方案都未同时满足“利润大于0、资金足够”，所以本轮没有开店。':'本轮没有进入经营试算。';
 if(ties)why+=' 最高分出现完全相同值，按本轮实际考察顺序先到的方案优先。';
 return `<section class="decision-story"><h3>第 ${r.month+1} 月，选址怎么决定？</h3><p>${esc(why)}</p><p class="info-note">先筛许可、通路、容量及已有营业店，再按轮查选最多8个地点。每个地点分别算店面规模和商品组合；合格后按“本期利润 / 启动资金”选一家，不按绝对利润最大选。以下都是当时的预测，非当前重算。</p>${r.eligible_nodes?`<p class="info-note">当轮合法候选 ${r.eligible_nodes.map(place).join('、')||'无'}；本次考察 ${r.selected_nodes.map(place).join('、')||'无'}。</p>`:''}<details open><summary>其他方案为什么没选？</summary><ul>${rows.filter(p=>p.decision!=='selected').map(p=>`<li>${place(p.node)} · ${p.area===4?'超市':'小店'} · ${p.assortment.map(food).join('＋')}：${p.blockers?.length?p.blockers.map(x=>x==='non_positive_profit'?`预计利润 ${money(p.profit)}，不大于0`:x==='insufficient_capital'?`需要资金 ${money(p.capital)}，超过当时资金池 ${money(r.fund_cash)}`:auditReasons[x]||x).join('；'):`分数 ${n(p.score,6)}，${chosen&&p.score===chosen.score?'与胜出方案完全相同，但考察顺序较后':`低于胜出方案 ${n(chosen?.score,6)}`}`}。</li>`).join('')||'<li>本轮没有其他经营方案。</li>'}</ul></details>${r.selected_plan?auditPlan(r.selected_plan):''}<details><summary>展开全部方案数值与排名</summary><div class="decision-table"><table><thead><tr><th>地点 / 方案</th><th>利润 / 资金</th><th>分数</th><th>为什么选或不选</th></tr></thead><tbody>${rows.map(p=>`<tr class="${p.decision==='selected'?'decision-winner':''}"><td>${p.node===node?'● ':''}${place(p.node)}<small>${p.area===4?'超市':'小店'} · ${p.assortment.map(food).join('＋')}</small></td><td>${money(p.profit)} / ${money(p.capital)}</td><td>${n(p.score,6)}</td><td>${p.decision==='selected'?'第1名 · 投资':p.rank!==null?`第${p.rank}名 · 分数未胜出`:(p.blockers||[]).map(x=>auditReasons[x]||x).join('；')}</td></tr>`).join('')}</tbody></table></div></details><p class="info-note">当时资金池 ${money(r.fund_cash)}。● 表示当前查看的地块。并非每个地块都一定有盈利方案；出现分数相同时以保存的实际选中结果为准。</p></section>`;
}
function reviewExplanation(r,factory){
 if(!r)return '';
 const gain=factory?r.candidate.profit-r.current.profit:r.gain,required=r.required_gain,cash=r.cash_at_review,move=r.move_cost;
 const mode=r.decision_mode;
 const fields=factory?[]:[['房租',r.current.rent-r.candidate.rent],['购物估值',r.current.shopping-r.candidate.shopping],['通勤估值',r.current.commute-r.candidate.commute],['干扰估值',r.current.environment-r.candidate.environment]];
 let text=mode==='blind'?'这是随机合法选址对照：候选地点随机选择，不按收益排名。':`${factory?'在合法工业地与原址中，选择预计利润最高的地点':'在合法住宅地与原址中，选择房租＋购物＋通勤＋干扰综合成本最低的地点'}；评分完全相同则优先编号较小的地块。`;
 if(!mode)text='旧记录未保存研究模式，无法确认当时按评分还是随机选择；以下只解释已保存的数值和最终结果。';
 text+=` 当次候选为${place(r.to)}，${factory?'预计利润':'综合成本'}从 ${money(r.current[factory?'profit':'total'])} 变为 ${money(r.candidate[factory?'profit':'total'])}，改善 ${money(gain)}。`;
 if(fields.length)text+=' 分项改善：'+fields.map(([name,v])=>`${name} ${v>=0?'+':''}${money(v)}`).join('、')+'（正数表示更好，时间和干扰是估值，不是实际现金）。';
 if(mode==='blind'&&required!==undefined)text+=` 理性模式的改善门槛为 ${money(required)}，本随机对照不检查这个门槛。`;
 else if(required!==undefined)text+=` 改善必须严格大于 ${money(required)}：本次${gain>required?'超过':'没有超过'}门槛。`;
 if(cash!==undefined&&move!==undefined)text+=` 当时现款 ${money(cash)}，搬迁费 ${money(move)}：${cash>=move?'付得起':'付不起'}。`;
 text+=` 最终结果：${r.reason}。`;
 if(mode==='blind')text+=' 随机对照绕过收益门槛，但仍检查现款；不代表理性收益最优。';
 return `<div class="reason decision-explanation"><strong>第 ${r.month+1} 月，这次为什么这样选？</strong>${esc(text)}<p class="info-note">${factory?'利润越高越好；须有工业许可、道路连通和4单位土地':'综合成本越低越好；须有住宅许可、道路连通和足够居住容量'}，原址允许保留。考察的是当时条件，主体依次行动，不能用现在的报价替代。</p></div>`;
}

function initialDecision(r,factory){
 if(!r)return '<details><summary>开城时的初始落点</summary><p class="info-note">旧存档未记录初次选择，或这是预设小城的固定初态；不能用当前条件倒推。</p></details>';
 const fields=factory?[['distance','到住宅用地平均单程时间']]:[['rent','房租'],['shopping','购物估值'],['commute','通勤估值'],['environment','干扰估值'],['total','综合成本']];
 return `<details class="audit-history"><summary>开城时为什么选${place(r.node)}？</summary><p>${factory?'首批4家工厂依次初始化，只比较同一连通规划片区内允许工业、有4单位土地、且仍能容纳64户的地点。选择到住宅用地平均单程时间最短的地块；不是后续经营利润最大化。':'首批64户依次初始化，只比较片区内允许住宅且有容量的地点，选择房租＋购物＋通勤＋干扰的综合成本最低者；没有原址，不收搬迁费。前一户入住会改变后一户的容量和租金条件。'}完全相同则按较小地块编号选。最终落在${place(r.node)}。这是开城当时保存的记录。</p><div class="decision-table"><table><thead><tr><th>地点</th>${fields.map(([,name])=>`<th>${name}</th>`).join('')}</tr></thead><tbody>${r.alternatives.map(a=>`<tr class="${a.node===r.node?'decision-winner':''}"><td>${place(a.node)}</td>${fields.map(([key])=>`<td>${key==='distance'?hours(a[key]):money(a[key])}</td>`).join('')}</tr>`).join('')}</tbody></table></div></details>`;
}
function reviewHistory(reviews,factory){
 const fields=factory?[['direct','预计直销量'],['wholesale','预计批发量'],['volume','预计产量'],['rent','厂址租金及固定费'],['profit','预计利润']]:[['rent','房租'],['shopping','购物估值'],['commute','通勤估值'],['environment','干扰估值'],['total','综合成本']];
 return `<details class="audit-history"><summary>全部${factory?'厂址':'迁居'}考察 · ${reviews.length} 次</summary><p class="info-note">每次实际评估均保留，包含留下与搬迁；冷却期内未评估不伪造记录。旧存档此前被截断的历史无法恢复。</p>${[...reviews].reverse().map(r=>`<details><summary>第 ${r.month+1} 月 · ${esc(r.reason)}</summary><p>${place(r.from)} → ${place(r.to)}</p>${reviewExplanation(r,factory)}<table><thead><tr><th>当时预测</th><th>原址</th><th>候选</th></tr></thead><tbody>${fields.map(([key,label])=>`<tr><td>${label}</td><td>${['direct','wholesale','volume'].includes(key)?n(r.current[key]):money(r.current[key])}</td><td>${['direct','wholesale','volume'].includes(key)?n(r.candidate[key]):money(r.candidate[key])}</td></tr>`).join('')}</tbody></table>${facts([['预期改善',money(factory?r.candidate.profit-r.current.profit:r.gain)],['所需改善须超过',r.required_gain===undefined?'旧记录未保存':money(r.required_gain)],['当时现款',r.cash_at_review===undefined?'旧记录未保存':money(r.cash_at_review)],['一次搬迁费',r.move_cost===undefined?'旧记录未保存':money(r.move_cost)]])}<details><summary>当次候选地点（保存 ${r.alternatives.length} 个）</summary><div class="decision-table"><table><thead><tr><th>地点</th>${fields.map(([key,label])=>`<th>${label}</th>`).join('')}</tr></thead><tbody>${r.alternatives.map(a=>`<tr class="${a.node===r.to?'decision-winner':''}"><td>${place(a.node)}${a.node===r.to?'（本次候选）':''}</td>${fields.map(([key])=>`<td>${['direct','wholesale','volume'].includes(key)?n(a[key]):money(a[key])}</td>`).join('')}</tr>`).join('')}</tbody></table></div></details></details>`).join('')}</details>`;
}

function accountName(account){
 const id=Number(account.slice(1)),producer=account.startsWith('p'),actor=(producer?world().producers:world().stores).find(a=>a.id===id);
 return actor?(producer?factoryName(actor):`${shopName(actor)} ${id+1}`):account;
}
function residentAttributes(d){
 const h=d.entity,p=d.profile,settled=state.month>0;
 const base=facts([['当前现金',money(d.cash)],['食品已买 / 目标',`${h.food} / 2 件`],['日用品已买 / 目标',`${h.goods} / 2 件`],['时间估值',`${money(h.value_time)} / 小时`]]);
 if(!p)return `<section class="resident-attributes"><h3>家庭属性与价格</h3>${base}<p class="info-note">完整属性接口需要正常重启Python服务。下方仍可查看现有购物、收入和账本记录。</p></section>`;
 const f=p.payments,i=p.income,t=p.time,m=p.move,a=d.appraisal;
 const purposes={manufacturing:'工厂生产',factory_service:'工厂柜台',retail_staff:'商店服务',freight:'货运'};
 return `<section class="resident-attributes"><h3>这户家庭的全部属性</h3><p class="info-note">模型主体是一户家庭。编号 ${h.id+1} · 当前住址 ${place(h.node)}。${settled?`以下实际收支与购买来自第 ${p.period} 月，现金为结算后余额。`:'刚开城，只有初始现金与偏好；工作、收入和购物尚未结算。'}</p>${base}
 <h3>${settled?'本月现金与收入':'初始现金与待结算项目'}</h3>${facts([['期初现金',money(p.cash_open)],['购物前可用现金',p.cash_before_shopping===null?'未记录':money(p.cash_before_shopping)],['税前工资',money(i.wages)],['税前分红',money(i.dividends)],['税前收入合计',money(i.gross)],['实际缴税',money(i.tax)],['税后收入',money(i.net)],['实际购物支出',money(f.shopping)],['实际住房租金',money(f.housing)],['实际搬迁支出',money(f.moving)],['全部现金流入',money(f.incoming)],['全部现金流出',money(f.outgoing)],['本月现金变化',`${f.change>=0?'+':''}${money(f.change)}`],['当前政策税率',`${p.current_tax_rate}%`],['本期实际扣税比例',i.observed_tax_fraction===null?'没有应税收入':`${n(i.observed_tax_fraction*100)}%`]])}<p class="info-note">期末现金 = 期初现金 + 全部流入 − 全部流出。税后收入还没扣购物、房租和搬迁。当前政策可能刚被调整，实际缴税按当时每笔收入代扣。${world().fiscal?.dividends_enabled===false?'当前自动分红已暂停，旧分红记录仍保留。':'分红在购物后结算。'}当前现金不等于购物那一刻可用的钱。</p>
 <h3>商品实际成交价格</h3>${d.purchases.length?d.purchases.map(x=>`<div class="price-card"><strong>${food(x.good)} × ${x.quantity}</strong><p>成交单价 ${money(x.price_cents)} · 实付 ${money(x.cents)}</p><small>${esc(accountName(x.seller))} · ${place(x.node)}</small></div>`).join(''):`<p>${settled?'本月没有实际购买记录，不能显示虚构成交价。':'等待首月实际购买，当前尚无成交价。'}</p>`}
 <h3>工作与出行</h3>${facts([['统一税前时薪',`${money(t.wage_per_hour)} / 小时`],['实际工作',hours(t.work)],['实际通勤',hours(t.commute)],['实际购物用时',hours(t.shopping)],['工作＋通勤上限',hours(t.work_commute_limit)],['购物行程上限',hours(t.shopping_limit)]])}${p.jobs.map(j=>`<div class="price-card"><strong>${esc(accountName(j.seller))} · ${purposes[j.purpose]||esc(j.purpose)}</strong><p>${place(j.node)} · ${hours(j.hours)} · 税前工资 ${money(j.gross_wage)}</p></div>`).join('')||`<p>${settled?'本月没有实际工作收入。':'尚未分配工作。'}</p>`}<p class="info-note">可能为多个雇主工作；当前由招工顺序分配，不是家庭自主择业。购物行程使用单独的时间限制，不能把这些时间相加解释为全天时间预算。</p>
 <h3>住房、偏好与当前生活成本估值</h3>${facts([['当前住房租金报价',money(p.housing.quote)],['时间估值系数',`${money(t.value)} / 小时`],['生产干扰敏感系数',n(p.housing.environment_preference,3)],['环境权重',n(p.housing.environment_weight,3)],['当前生产干扰指数',n(p.housing.exposure,4)],['房租评分',money(a.rent)],['购物评分',money(a.shopping)],['通勤评分',money(a.commute)],['干扰评分',money(a.environment)],['当前综合成本评分',money(a.total)]])}<p class="info-note">评分是用当前报价、路程和工作记录试算的生活成本；时间和干扰被换算为估值，不是扣过的现金。租金报价可能与期初实际缴租不同。这些当前评分不替代过去迁居时的历史评分。</p>
 <h3>迁居状态</h3>${facts([['最近搬迁',m.last_month===null?'没有后续搬家记录':`第 ${m.last_month} 月`],['搬后冷却期',`${m.cooldown} 个月`],['最早再次考察',state.mode==='fixed'?'固定位置模式':`第 ${m.earliest_month} 月起`],['一次搬迁费用',money(m.fee)],['迁居规则',m.policy==='stable'?'稳定迁居 · 改善须超过门槛':'旧版改善门槛'],['选址模式',m.mode==='blind'?'随机合法选址':m.mode==='fixed'?'固定住址':'自主比较生活成本']])}<p class="info-note">达到最早月份也不保证当月会被抽到；是否搬家还要检查候选、现款和改善门槛。</p>
 <h3>当前卖家报价与货架</h3><p class="info-note">以下是此刻报价和结算后库存，不是上面已经发生的成交价，也不保证下一期买得到。</p>${p.posted_offers.map(o=>`<div class="price-card"><strong>${esc(accountName(o.seller))} · ${food(o.good)} ${money(o.price)} / 件</strong><p>${place(o.node)} · 剩余 ${o.stock} 件 · 柜台剩余 ${hours(o.service)}</p><small>当前往返含停留 ${o.roundtrip===null?'未连通':hours(o.roundtrip)} · ${o.within_time_limit?'在购物行程时限内':'超过购物行程时限'}</small></div>`).join('')||'<p>当前没有报价。</p>'}
 <p class="info-note">每户食品和日用品目标各2件，不跨期囤货。模型尚未设置个人年龄、学历、技能、家庭人数或房产价值；这里展示实际存在的属性。</p></section>`;
}

function renderInspector(){
  $('inspector').hidden=!selection;if(!selection)return;
  const city=world(),kind=selection.kind;
  $('selection-type').textContent=readonly()?'同城对照 / 只读':kind==='road'?'道路工程':kind==='node'?'土地与建设许可':'看看这里的生活';
  let html=readonly()?'<div class="ref-banner">这是未新增干预的同城对照。建设操作请切回你的城市。</div>':'';
  const metricNode=city.nodes.find(n=>n.id===(selection.node??(kind==='node'?selection.id:detail?.entity?.node)));
  if(mapLayer!=='none'&&metricNode)html+=`<div class="layer-insight"><span>${mapLayers[mapLayer].name} · ${place(metricNode.id)}</span><strong>${layerText(metricNode)}</strong><small>街区汇总值，不是这栋建筑的单独数值。</small></div>`;
  if(kind==='home'){
    const people=groups(city,selection.node)[selection.id]||[],missing=people.filter(h=>h.food<2||h.goods<2).length;
    if(!people.length){$('inspector-body').innerHTML='<h2>这里的家庭已经搬走</h2><p class="lead">在地图上检查它们的新住处。</p>';return;}
    const shopping=people.reduce((s,h)=>s+h.shopping,0)/people.length,commute=people.reduce((s,h)=>s+h.commute,0)/people.length;
    html+=`<div class="selection-art">${art('home')}</div><h2>${place(selection.node)}住宅</h2><p class="subtitle">这个图标对应实际的 ${people.length} 户家庭。</p><span class="tag ${missing?'warning':''}">${state.month===0?'刚入住，等待首月记录':missing?`${missing} 户没买齐商品`:'家家买齐了日常商品'}</span>${facts([['平均购物',hours(shopping)],['平均通勤',hours(commute)]])}<div class="reason"><strong>${state.month===0?'刚入住，等待第一个月的生活记录':missing?'先看看谁没买齐东西':shopping>1.5?'购物还要花些时间':commute>1.5?'上班需要绕一段路':'这里的生活目前比较方便'}</strong>${state.month===0?'推进时间之后，再查看实际购物、通勤与收支。':missing?'点一户家庭，看看它实际去哪买、是不是缺货或来不及。':'工作地点和购物选择因人而异。点开一户，能看到它自己的路线与选择理由。'}</div><h3>住在这里的人</h3><div class="entity-list">${people.map(h=>pick('resident',h.id,h.node,`家庭 ${String(h.id+1).padStart(2,'0')}`,state.month===0?'等待首月结算':`${h.food>=2&&h.goods>=2?'已买齐':'未买齐'} · 食品 ${h.food}/2 · 日用品 ${h.goods}/2`,state.month===0?'supply-pending':h.food<2||h.goods<2?'supply-warning':'supply-ok')).join('')}</div><p class="info-note">住宅图标每个最多表示 4 户，不额外创建人口或空房。</p>`;
  }else if(!detail){html+='<p class="lead">正在看看这里发生了什么…</p>';
  }else if(kind==='road'){
    const d=detail;
    html+=`<div class="selection-art"><svg viewBox="0 0 120 90" aria-hidden="true"><path d="M15 73 105 17" stroke="#c1c8b6" stroke-width="27"/><path d="M15 73 105 17" stroke="#839386" stroke-width="20"/><path d="M15 73 105 17" stroke="#f5eed6" stroke-width="2" stroke-dasharray="9 7"/></svg></div><h2>${d.level?'改良这段道路':'打通这条连接'}</h2><p class="subtitle">${place(d.a)} ↔ ${place(d.b)}</p><p class="lead">${d.level?'现有道路可以改良，让这一段的基础通行更快。':'相邻的两处街区现在没有直连道路。新路可以让出行少绕到两侧。'}</p>${facts([['一次建设费',d.cost===null?'已改良':money(d.cost)],['两地现有行程',d.duration_before===null?'尚未连通':hours(d.duration_before)]])}`;
    if(d.level<2)html+=`<div class="reason"><strong>它直接改变什么？</strong>${d.duration_before===null?`这里当前没有道路相通。修建后可以直接连通，基础行程约 ${n(d.duration_with_connection)} 小时。`:`按当前交通条件，两地最短行程可从 ${n(d.duration_before)} 缩短至 ${n(d.duration_with_connection)} 小时。`}<br>这是连接两地的试算，不保证所有人的通勤都会下降。橙色虚线是当前绕行。</div><button id="apply-action" class="primary">${d.level?'确认改良':'修建这条路'} · ${money(d.cost)}</button><p class="info-note">建成后这段路每月维护 ${money(d.upkeep_after)}。居民与企业的反应需要推进时间。</p>`;
    else html+='<div class="reason">这段道路已经完成改良。可以看看别处是否还缺少连接。</div>';
  }else if(kind==='node'){
    const d=detail,occupied=d.residents.length+d.stores.length+d.producers.length;
    html+=`<h2>${place(d.id)}</h2><p class="subtitle">${occupied?'已有居民或企业的街区':'尚未开发的地块'} · 空余土地 ${d.free}/8</p>`;
    if(!occupied)html+='<div class="empty-plot"><strong>这里还是空地</strong><p>规划只允许将来使用，不会立刻生成住宅、商店或工厂。</p></div>';
    html+=`<p class="lead"><span class="zone-flag">${zoneShort[d.zone]}</span>${zoneNames[d.zone]}<br><small>虚线地面标记是许可，屋顶和厂房才是实际主体。</small></p>`;
    if(tool==='zone'){
      html+='<h3>希望这里允许什么？</h3><div class="zone-palette">'+Object.keys(zoneNames).map(z=>`<button data-zone="${z}" class="${zoneChoice===z?'active':''}" aria-pressed="${zoneChoice===z}"><i class="zone-swatch" style="color:${zoneColors[z]}"></i>${zoneShort[z]}</button>`).join('')+'</div>';
      const explanation=zoneChoice==='reserve'?'不允许新的用途进入，保留已有主体。':zoneChoice==='shop'?'商家可以来考察；认为能赚钱、资金和容量足够，才可能投资开店。':zoneChoice==='home'?'家庭可以考虑搬来，仍会比较购物、通勤、房租和生产干扰。':zoneChoice==='factory'?'工厂可以考虑搬来。接近需求可能更方便，也可能带来生产干扰。':'多种用途竞争同一片有限土地，便利与环境可能发生冲突。';
      html+=`<div class="reason"><strong>${zoneNames[zoneChoice]}</strong>${explanation}</div><button id="apply-action" class="primary">${zoneChoice===d.zone?'这是当前许可 · 无需提交':'提交许可 · 约3K'}</button><p class="info-note">只影响未来资格，已有建筑与住户不会自动消失。</p>`;
    }else html+='<button id="begin-zoning" class="secondary">规划这里的土地用途</button>';
    if(!planning()&&['shop','mixed','open'].includes(d.zone)&&!d.stores.length){
      html+=commercialSummary(d);
    }
    if(d.connected===false&&!planning())html+='<div class="reason"><strong>还没有接入城市道路</strong>请把这里连接到已开放的城区，主体才会考虑进入。</div>';
    if(!planning())html+=supplySummary(city.residents.filter(h=>h.node===d.id));
    if(occupied)html+='<h3>实际已经在这里的</h3><div class="entity-list">'+groups(city,d.id).map((g,i)=>homePick(g,i,d.id)).join('')+d.stores.map(s=>pick('store',s.id,s.node,shopName(s))).join('')+d.producers.map(p=>pick('producer',p.id,p.node,factoryName(p))).join('')+'</div>';
    if(!planning())html+=(d.commercial_history?.visits.length?roundDecision(d.commercial_history.visits.at(-1).comparison,d.id):'')+(['shop','mixed','open'].includes(d.zone)||d.commercial_history?.total_visits?commercialHistory(d):'')+research(`<p>住房报价 ${money(d.home_rent)} / 户；生产干扰 ${n(d.exposure)}；住 ${d.usage.housing_land} / 商 ${d.usage.commercial_land} / 工 ${d.usage.industrial_land} 单位土地。</p>`,{...d,commercial_history:d.commercial_history?{...d.commercial_history,visits:'见上方完整考察记录'}:null});
  }else if(kind==='resident'){
    const d=detail,h=d.entity,missing=h.food<2||h.goods<2;
    html+=`<div class="selection-art">${art('home')}</div><h2>家庭 ${String(h.id+1).padStart(2,'0')}</h2><p class="subtitle">现在住在${place(h.node)}</p><span class="tag ${missing?'warning':''}">${state.month===0?'刚入住，等待首月记录':missing?'本月没有买齐商品':'本月买齐了日常商品'}</span>${residentAttributes(d)}<div class="reason"><strong>为什么住在这里？</strong>${homeDecision(d.reviews.at(-1))}${d.move_rule?.policy==='stable'&&state.mode==='adaptive'?'<p class="info-note">现行迁居规则：房租、购物、通勤和生产干扰的预期综合成本，需下降超过 10%，且足以补偿搬迁，才值得换住处。搬后至少等待 4 个月再考虑。</p>':''}</div>${d.income?`<p class="info-note">本期税前收入 ${money(d.income.gross)} · 已纳税 ${money(d.income.tax)} · 税后收入 ${money(d.income.gross-d.income.tax)}（不是存款余额）</p>`:''}<h3>实际去哪里买了东西？</h3><div class="entity-list">`;
    const sellers=[...new Set(d.purchases.map(p=>p.seller))];
    html+=sellers.map(account=>{const rows=d.purchases.filter(p=>p.seller===account),producer=account[0]==='p',id=Number(account.slice(1)),actor=producer?city.producers.find(p=>p.id===id):city.stores.find(s=>s.id===id);return pick(producer?'producer':'store',id,actor.node,producer?factoryName(actor):shopName(actor),rows.map(p=>`${p.quantity}件${food(p.good)}`).join('、'));}).join('')||'<p class="info-note">本月没有实际购买。</p>';
    html+='</div><p class="route-legend">地图绿色是实际购物，蓝色是实际通勤。</p>';
    if(missing)html+=`<p class="lead">${d.blocked.length?'当时没有形成可行的购物组合。库存、路程、柜台或现款都可能有关，不能只凭一个指标确定原因。':'实际购买没有满足全部需要，可以检查候选组合和卖家的货架。'}</p>`;
    html+=initialDecision(d.initial_location,false)+reviewExplanation(d.reviews.at(-1),false)+reviewHistory(d.reviews,false)+research(ledger(d.payments,`h${h.id}`)+`<p>现款 ${money(d.cash)}，实际购买食品 ${h.food}/2、日用品 ${h.goods}/2。时间与干扰估值不是现金支出。</p>`,{entity:h,profile:d.profile,purchases:d.purchases,payments:d.payments,history:d.history,reviews:d.reviews,plans:d.plans,blocked:d.blocked,reference:d.other});
  }else if(kind==='producer'){
    const d=detail,p=d.entity,r=d.reviews.at(-1),nearby=city.residents.filter(h=>Math.abs(h.node%7-p.node%7)+Math.abs(Math.floor(h.node/7)-Math.floor(p.node/7))<=1).length;
    const why=state.mode==='fixed'?'当前研究模式固定工厂位置，不进行自主搬迁。':state.mode==='blind'?'当前研究模式随机选择合法厂址，不按利润预期挑选。':!r?'还没有新的搬迁考察记录，每个月只轮查一家工厂。':r.reason==='搬迁'?`第 ${r.month+1} 月从${place(r.from)}搬来。当时企业预计销路、采购竞争和用工带来的改善足以补偿搬迁。`:r.reason==='现金不足'?'企业考虑过搬迁，但现款不够支付费用。':r.reason==='原址更合适'?'最近考察中，企业仍认为原址更合适。':'最近考察中，候选地点的预期收益改善还不足以补偿搬迁。';
    html+=`<div class="selection-art">${art('producer')}</div><h2>${factoryName(p)}</h2><p class="subtitle">${place(p.node)} · ${state.month===0?'刚落地，等待首月生产':p.production?'本月实际开工':'本月没有生产'}</p><div class="balance-card"><span>当前现金余额</span><strong>${money(d.cash)}</strong></div>${facts([['本月产量',`${p.production} 件`],['本月售出',`${p.sold} 件`]])}<div class="reason"><strong>企业为什么在这里？</strong>${why}</div><p class="lead">${nearby?`所在和相邻街区住有 ${nearby} 户家庭。${p.production?'生产干扰会进入这些家庭的居住考虑。':'本月没有生产。'}`:'所在和相邻街区目前没有住宅家庭。'}</p><h3>它怎样卖货？</h3><p class="info-note">直接向居民卖出 ${p.direct_sold} 件，另记录了 ${d.shipments.length} 笔商店采购。</p>`;
    html+=initialDecision(d.initial_location,true)+reviewExplanation(d.reviews.at(-1),true)+reviewHistory(d.reviews,true)+research(ledger(d.payments,`p${p.id}`)+`<p>库存 ${p.stock}，出厂价 ${money(p.price)}。选址利润是预期，不保证实现。</p>`,{reviews:d.reviews,production:d.production});
  }else if(kind==='store'){
    const d=detail,s=d.entity,exit=d.events.filter(e=>e.event==='exit').at(-1),stock=Object.values(s.stock).reduce((a,b)=>a+b,0),problem=!s.active?'这家店已经停止营业':s.service<=0?'这家店本月没有柜台服务':s.last_profit<0?'这家店本月没有赚到钱':stock===0?'货架上目前没有存货':'这家店目前正在经营';
    html+=`<div class="selection-art">${art('store')}</div><h2>${shopName(s)}</h2><p class="subtitle">${place(s.node)} · 门店 ${s.id+1}</p><span class="tag ${!s.active?'closed':s.last_profit<0?'warning':''}">${!s.active?'已退出经营':s.service<=0?'本月柜台未运作':'实际营业门店'}</span><div class="balance-card"><span>当前现金余额</span><strong>${money(d.cash)}</strong></div>${facts([['顾客访问',`${s.customers} 次`],['本月售出',`${s.sales} 件`]])}<div class="reason"><strong>${problem}</strong>${!s.active?(exit?`第 ${exit.month+1} 月，它${exit.reason==='cash'?'连续无法支付开门费用':'连续亏损'}，于是退出了经营。关店后土地释放。`:'已有退出记录，门店停止营业并释放土地。'):s.service<=0?'门店仍然存在，但本月没有提供柜台服务。费用、现款和可用人员都可能构成约束，可以展开实际记录查看。':s.last_profit<0?'实际销售收入和库存价值没覆盖本期成本。客流、运费、店员和开门费都会影响结果，可以看看它的补货路线。':stock===0?'下一期能否补货，要看现款、预期需求和运输条件。可以展开进货记录检查原因。':'居民会比较商品价格与出行便利，决定是否来这里买。'}</div><h3>店里卖什么？</h3><p class="lead">${s.assortment.map(food).join('和')} · 库存 ${stock} 件</p><p class="route-legend">地图橙色是本月实际补货路线。</p>`;
    html+=roundDecision(d.location_decision,s.node)+research(ledger(d.payments,`r${s.id}`)+`<p>本期结果 ${money(s.last_profit)}，现款 ${money(d.cash)}，容量 ${d.capacity}。</p>`,{history:d.history,shipments:d.shipments,events:d.events});
  }
  const pending=pendingAction();if(pending&&!readonly()&&world().civic<pending.cost/100)html+=`<div class="reason"><strong>建设预算不足</strong>当前 ${cashK(world().civic)}，这项操作需要 ${money(pending.cost)}。</div>`;
  $('inspector-body').innerHTML=html;bindInspector();availability();
}

function pendingAction(){
  if(!selection||!detail)return null;
  if(selection.kind==='road'&&detail.cost!==null)return {action:'road',node:{id:detail.a,neighbor:detail.b},cost:detail.cost};
  if(selection.kind==='node'&&tool==='zone'&&zoneChoice!==detail.zone)return {action:'zone',node:{id:detail.id,zone:zoneChoice},cost:250};
  return null;
}
async function mutate(path,data={},message='已完成'){
  if(busy||!state)return false;
  setBusy(true,path==='/api/advance'?'advance':'update');inspectSequence++;
  try{state=await api(path,{revision:state.revision,...data});detail=null;render();toast(message);return true;}
  catch(e){toast(e.message,true);return false;}
  finally{setBusy(false);}
}
async function applyAction(){
  const pending=pendingAction();if(!pending||readonly())return;
  const ok=await mutate('/api/action',{action:pending.action,node:pending.node,note:'玩家在城市地图中确认建设或规划'},planning()?'规划已保存，可继续铺路和划地。':pending.action==='road'?'道路工程已完成。推进后看看真实反应。':'许可已变更，地上不会立即出现新建筑。');
  if(ok){guideRoad=null;impactVisible=false;selection=null;detail=null;tool=planning()?(pending.action==='road'?'road':'zone'):'inspect';$('inspector').hidden=true;$('map-callout').hidden=false;$('map-callout').textContent=planning()?'可继续铺路、划地。准备好后，点击“开放城市”。':pending.action==='road'?'道路工程已完成。下一步：推进 3 个月。':'用地资格已变。下一步：让主体考虑是否进入。';render();}
}
async function advance(count){
  if(planning()){await launchCity();return;}
  const before=state,ok=await mutate('/api/advance',{count},`已度过 ${count} 个月。看看实际发生了什么。`);
  if(!ok)return;
  selection=null;detail=null;guideRoad=null;$('map-callout').hidden=true;
  lastReport={from:before.month,to:state.month,count};buildImpacts(lastReport);impactVisible=false;render();openReport();
}
function buildImpacts(report){
  const city=state.worlds.player,start=report?.from??Math.max(state.fork_month,state.month-3),out=[];
  const planning=state.actions.at(-1);
  if(planning?.action==='zone'&&planning.month>=start){
    const node=city.nodes[planning.node.id],commercial=['shop','mixed','open'].includes(node.zone);
    const title=commercial&&!node.stores.length?'你的许可地还没有营业门店':`你规划的${place(node.id)}`;
    const description=commercial&&!node.stores.length?`这里有${zoneNames[node.zone]}，目前实际门店为 0。其他街区开店，不代表这里已经建成。点开地块看最近考察记录。`:`目前实际住有 ${node.residents} 户，有 ${node.stores.length} 家门店、${node.producers.length} 家工厂。用途许可和实际进入分别统计。`;
    out.push({kind:'node',id:node.id,node:node.id,title,text:description});
  }
  for(const e of city.changes.shops.filter(e=>e.month>=start)){
    out.push({kind:'store',id:e.store,node:e.node,title:`${place(e.node)}的门店${e.event==='entry'?'开业':'退出'}`,text:`第 ${e.month+1} 月，门店 ${e.store+1} ${e.event==='entry'?'自行投资进入':'因经营结果退出'}。`,month:e.month});
  }
  for(const x of state.player_feedback.examples.slice(0,3)){
    const delta=x.player.commute-x.reference.commute;
    if(Math.abs(delta)>1e-6)out.push({kind:'resident',id:x.id,node:x.node,title:`家庭 ${x.id+1} 通勤${delta<0?'更短':'更长'}`,text:`最近 ${state.player_feedback.periods} 个月平均通勤 ${n(x.player.commute)} 小时；未新增规划的对照为 ${n(x.reference.commute)} 小时。${x.used_new_road?'它的实际通勤曾经过新连接。':''}`});
  }
  for(const e of [...city.changes.moves.filter(e=>e.month>=start)].reverse().slice(0,2))out.push({kind:e.type==='home_move'?'resident':'producer',id:e.type==='home_move'?e.resident:e.producer,node:e.to,title:`${e.type==='home_move'?'家庭':'工厂'}搬到了${place(e.to)}`,text:`第 ${e.month+1} 月，从${place(e.from)}搬到${place(e.to)}。可以检查当时的选址理由。`});
  impacts=out.slice(0,7);
}
function openReport(){
  if(!state)return;if(planning()){toast(state.readiness.reason);return;}if(!state.worlds.player.space.length){toast('先推进一个月，再看实际经济变化。');return;}if(!lastReport)buildImpacts(null);
  const player=state.worlds.player,reference=state.worlds.reference,p=player.space.at(-1),r=reference.space.at(-1),f=state.player_feedback;
  $('report-title').textContent=lastReport?`${lastReport.count} 个月后，城市的回信`:'城市目前的变化';
  const windowStart=lastReport?.from??Math.max(state.fork_month,state.month-3);
  const duration=lastReport&&lastReport.count>1?`第 ${lastReport.from+1}–${lastReport.to} 月`:`第 ${state.month} 月`;
  const currentCompare=Number(n(p.commute))-Number(n(r.commute));
  const exposureCompare=Number(n(p.exposure,3))-Number(n(r.exposure,3)),exposureWord=exposureCompare>0?'比对照更强':exposureCompare<0?'比对照减轻':'接近对照';
  let html=`<p class="report-intro">${f.periods?`${duration}。规划没有替居民和企业作决定，下面是它们实际发生的反应。`:'尚未从对照起点推进时间。先试一项改善，再推进三个月，看看实际反应。'}</p><div class="report-cards"><div class="report-card ${currentCompare>0?'worse':''}"><span>本月平均通勤</span><strong>${n(p.commute)} h</strong><small>不新增干预：${n(r.commute)} h</small></div><div class="report-card ${Number(n(player.metrics.shopping_hours/player.residents.length))>Number(n(reference.metrics.shopping_hours/reference.residents.length))?'worse':''}"><span>本月平均购物</span><strong>${n(player.metrics.shopping_hours/player.residents.length)} h</strong><small>不新增干预：${n(reference.metrics.shopping_hours/reference.residents.length)} h</small></div><div class="report-card ${exposureCompare>0?'worse':''}"><span>本月住宅周边生产干扰</span><strong style="font-size:18px">${exposureWord}</strong><small>按实际生产与住宅位置比较</small></div></div>`;
  if(f.periods)html+=`<div class="report-story"><h3>${f.commute_less?`${f.commute_less} 户家庭的通勤比对照更短`:'没有家庭出现更短的平均通勤'}</h3><p>最近 ${f.periods} 个月的实际行程中，${f.commute_more} 户比对照更长，其余相同。${f.commute_more?'改善并没有平均落到每个人身上。':'仍需要继续看谁获得便利。'}${f.new_road_users?`有 ${f.new_road_users} 户家庭的实际通勤记录经过了你新建的连接。`:''}</p></div>`;
  const fiscalPeriods=player.space.filter(x=>x.month>=windowStart),taxTotal=fiscalPeriods.reduce((sum,x)=>sum+(x.tax_income||0),0),maintenanceTotal=fiscalPeriods.reduce((sum,x)=>sum+(x.upkeep_paid||0),0);
  html+=`<div class="report-story"><h3>公共收支与养护</h3><p>${duration}合计实际收税 ${money(taxTotal)}，支付道路养护 ${money(maintenanceTotal)}；现款 ${cashK(player.civic)}。本月养护完成 ${n(player.maintenance_ratio*100,0)}%。税款由家庭真实收入代扣，公共余额增加不代表居民买齐了商品。</p><p>本月食品需求满足 ${n(player.metrics.food_coverage*100,1)}%，日用品 ${n(player.metrics.goods_coverage*100,1)}%（按商品件数）。</p></div>`;
  const shops=player.changes.shops.filter(e=>e.month>=windowStart),entries=shops.filter(e=>e.event==='entry').length,exits=shops.filter(e=>e.event==='exit').length;
  html+=`<div class="report-story"><h3>${entries||exits?`${entries} 家门店进入，${exits} 家门店退出`:'这段时间没有新店开业或退出'}</h3><p>${entries||exits?'这些是实际发生的企业事件，原因请查看门店的经营记录。':'允许商业或改善道路，都不保证会立刻有人投资。已有店仍在与工厂直销竞争。'}</p></div>`;
  if(state.actions.at(-1)?.action==='zone'&&!entries)html+='<div class="reason"><strong>有许可，为什么还是空地？</strong>检查地块时可以看到最近商家有没有考察、是否预计亏损或缺少开店资金。还没有进入记录，就不会在地图上画出虚构商店。</div>';
  html+=impacts.map((item,i)=>`<div class="report-fact"><span class="report-number">${i+1}</span><div class="report-story"><h3>${esc(item.title)}</h3><p>${esc(item.text)}</p><button data-report-impact="${i}">去地图上看看 →</button></div></div>`).join('');
  if(!impacts.length)html+='<p class="lead">还没有可定位的主体差异。可以检查一处住宅、尝试另一项规划，或继续等待。</p>';
  html+=`<details><summary>展开本月对照数字</summary><p class="fineprint">生产干扰指数：你的城市 ${n(p.exposure,5)} / 对照 ${n(r.exposure,5)}。该指数没有现实健康单位；上方按三位小数的显示精度比较。</p></details><div class="report-compare">对照与你从第 ${state.fork_month} 月的相同状态出发，同步运行，但不接收新增规划。家庭比较采用最近 ${f.periods||0} 个月实际记录，可能包含此前规划的影响；本月卡片只显示本月。城市也在自行发展，不能把每个变化都归因于刚才那一条路。</div>`;
  $('report-body').innerHTML=html;$('report-body').querySelectorAll('[data-report-impact]').forEach(b=>b.onclick=()=>{$('report-dialog').close();impactVisible=true;openImpact(Number(b.dataset.reportImpact));});$('report-open').classList.add('has-news');$('report-dialog').showModal();
}
function openImpact(index){const item=impacts[index];if(!item)return;worldKey='player';tool='inspect';if(item.kind==='node')showZones=true;impactVisible=true;render();select({kind:item.kind,id:item.id,node:item.node},true);$('map-callout').hidden=false;$('map-callout').textContent=`${index+1} · ${item.title}`;}
function chooseTool(next){if(busy||readonly()&&next!=='inspect')return;inspectSequence++;impactVisible=false;tool=next;if(next==='zone')showZones=true;guideRoad=next==='road'?recommendRoad():null;selection=null;detail=null;$('map-callout').hidden=next!=='road';$('map-callout').textContent=guideRoad?'点击高亮连接即可修建，花费 12K。':'点击 ＋ 直接修建，每段 12K；点已有道路可查看或改良。';render();}
async function newCity(seed,preset){
  const ok=await mutate('/api/new',{seed,preset},preset==='blank'?'空白场地已准备。先铺路，再沿路划地。':'原有小城已准备。');
  if(ok){lastReport=null;impacts=[];impactVisible=false;worldKey='player';tool='inspect';selection=null;detail=null;guideRoad=null;showZones=false;$('map-callout').hidden=true;$('menu-dialog').close();render();}
}
document.querySelectorAll('[data-tool]').forEach(b=>b.onclick=()=>chooseTool(b.dataset.tool));
document.querySelectorAll('[data-paint-zone]').forEach(b=>b.onclick=()=>{if(busy||readonly())return;zoneChoice=b.dataset.paintZone;selection=null;detail=null;inspectSequence++;render();});
$('advance').onclick=()=>advance(3);$('advance-one').onclick=()=>advance(1);$('advance-twelve').onclick=()=>{$('menu-dialog').close();advance(12);};
$('mission-action').onclick=()=>{if(planning()){if(state.readiness.ready)launchCity();else chooseTool(state.worlds.player.roads.length?'zone':'road');return;}const step=Number($('mission-action').dataset.step);if(step===2)advance(3);else if(step===3)openReport();else{worldKey='player';chooseTool('road');}};
$('zones-toggle').onclick=()=>{showZones=!showZones;render();};$('report-open').onclick=openReport;
$('show-impact').onclick=()=>{$('report-dialog').close();impactVisible=true;render();$('map-callout').hidden=!impacts.length;$('map-callout').textContent='点击地图上带编号的光圈，检查具体家庭或门店。';};
$('compare-view').onclick=()=>{worldKey=readonly()?'player':'reference';selection=null;detail=null;guideRoad=null;tool='inspect';impactVisible=false;render();};
$('inspector-close').onclick=()=>{selection=null;detail=null;inspectSequence++;render();};
$('layers-toggle').onclick=()=>{layersOpen=!layersOpen;renderLayers();};
$('layers-options').onclick=e=>{const button=e.target.closest('[data-map-layer]');if(button)setMapLayer(button.dataset.mapLayer);};
$('layer-clear').onclick=()=>setMapLayer('none');
$('layers-panel').onkeydown=e=>{if(e.key==='Escape'){layersOpen=false;renderLayers();$('layers-toggle').focus();}};
document.addEventListener('click',e=>{if(layersOpen&&!e.target.closest('.layers-control')){layersOpen=false;renderLayers();}});
$('finance-open').onclick=()=>{renderFiscal();availability();$('fiscal-dialog').showModal();};
$('tax-rate').oninput=()=>{$('tax-output').textContent=`${$('tax-rate').value}%`;availability();};
document.querySelectorAll('[data-tax-rate]').forEach(b=>b.onclick=()=>{$('tax-rate').value=b.dataset.taxRate;$('tax-output').textContent=`${b.dataset.taxRate}%`;availability();});
$('tax-apply').onclick=async()=>{if(planning()||readonly())return;const rate=Number($('tax-rate').value);await mutate('/api/action',{action:'tax',node:{rate},note:'玩家在财税面板调整居民收入税'},`收入税已改为 ${rate}%。从后续实际收入代扣，存款没有被扣除。`);};
$('menu-open').onclick=()=>$('menu-dialog').showModal();document.querySelectorAll('[data-close]').forEach(b=>b.onclick=()=>$(b.dataset.close).close());
$('new-tutorial').onclick=()=>newCity(1,'blank');$('new').onclick=()=>newCity(Number($('seed').value),$('preset').value);
$('anchor').onclick=async()=>{if(await mutate('/api/anchor',{},'已从当前状态建立新的同城对照，预算与已有规划保留。')){lastReport=null;impacts=[];impactVisible=false;$('menu-dialog').close();render();}};
$('save').onclick=async()=>{try{const r=await fetch('/api/save');if(!r.ok)throw Error('导出失败');const url=URL.createObjectURL(await r.blob()),a=document.createElement('a');a.href=url;a.download=`land-s${state.seed}-t${state.month}.json`;a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);toast('已导出城市、同城对照、道路和规划记录。');}catch(e){toast(e.message,true);}};
$('load').onchange=async e=>{try{const file=e.target.files[0];if(!file)return;if(file.size>64*1024*1024)throw Error('存档超过 64 MiB');const save=JSON.parse(await file.text());if(await mutate('/api/load',{save},'城市已恢复。')){worldKey='player';selection=null;detail=null;lastReport=null;impacts=[];impactVisible=false;guideRoad=null;tool='inspect';$('menu-dialog').close();render();}}catch(error){toast(error.message,true);}finally{e.target.value='';}};
(async()=>{setBusy(true);try{state=await api('/api/state');render();}catch(e){toast(`无法连接城市：${e.message}`,true);}finally{setBusy(false);}})();

function renderPlanningMission(){
 const city=state.worlds.player,ready=state.readiness.ready,hasRoads=city.roads.length>0;
 $('mission-number').textContent=ready?'3':hasRoads?'2':'1';
 $('mission-kicker').textContent='空白规划阶段 / 没有隐藏运行的经济主体';
 $('mission-title').textContent=ready?'基础规划就绪，可以开放城市':hasRoads?'沿路规划住宅与工业用地':'从第一条路开始，城市由你来铺';
 $('mission-copy').textContent=ready?'开放时，首批 64 户和 4 家工厂才会选择合法且连通的位置初始化。':'先连起道路，再划住宅与工业。两块住宅地、两块工业地连在一起，就能容纳首批主体。';
 $('mission-action').textContent=ready?'开放城市 →':hasRoads?'开始规划土地 →':'选择修路工具 →';
}
async function launchCity(){
 if(!state.readiness.ready){toast(state.readiness.reason,true);return;}
 if(await mutate('/api/launch',{},'城市已开放：首批主体已按规划初始化。现在推进时间，开始真实生产和交易。')){
  selection=null;detail=null;tool='inspect';guideRoad=null;showZones=false;lastReport=null;impacts=[];impactVisible=false;render();
 }
}
