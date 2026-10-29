const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const source = fs.readFileSync('landlab/static/app.js', 'utf8');
const context = vm.createContext({});
vm.runInContext(source.slice(source.indexOf('function cashflowPath('), source.indexOf('function ledgerParty(')), context);
const payment = (purpose, cents, source='r0', target='property') => ({purpose,cents,source,target});
const rows = [payment('retail_sale',29184,'h0','r0'),payment('rent_utilities',1086),payment('retail_staff',1400,'r0','h0'),payment('freight',888,'r0','h1'),payment('truck_lease',480),payment('wholesale',23040,'r0','p0'),payment('retail_dividend',2312,'r0','fund')];
const tree = context.buildCashTree(rows,'r0');
assert.equal(tree.income.total,29184);
assert.equal(tree.expense.total,29206);
assert.equal(tree.net,-22);
assert.equal(tree.operating,2290);
function reconcile(node){if(node.children.length){assert.equal(node.total,node.children.reduce((s,c)=>s+c.total,0));node.children.forEach(reconcile);}}
reconcile(tree.income);reconcile(tree.expense);
const operating = tree.expense.children.find(c=>c.name==='经营支出');
assert.equal(operating.children.find(c=>c.name==='运输费用').total,1368);
assert.equal(operating.children.find(c=>c.name==='人工费用').total,1400);
const edge = context.buildCashTree([payment('custom',300,'h0','r0'),payment('custom',100,'r0','h0'),payment('custom',200,'h0','r0'),payment('wholesale',999,'p0','p1'),payment('custom',123,'r0','r0'),payment('custom',0)],'r0');
assert.equal(edge.income.total,500);assert.equal(edge.expense.total,100);assert.equal(edge.net,400);
assert.equal(edge.income.children[0].children[0].children[0].total,500);
reconcile(edge.income);reconcile(edge.expense);
const household=context.buildCashTree(rows,'h0');
assert.equal(household.income.children[0].name,'劳动收入');
assert.equal(household.expense.children[0].name,'生活消费');
console.log('Cashflow reconciliation, classification, counterparties and edge cases passed.');

// Presentation changes must not alter the integer transaction tree.
const formatting = vm.createContext({});
vm.runInContext(source.split('\n').slice(0,3).join('\n'),formatting);
for(const [expression, expected] of [["money(29558)",'296K'],["cashK(295.58)",'296K'],["money(0)",'0K'],["money(2)",'＜1K'],["money(-2)",'−＜1K'],["money(-150)",'−2K'],["money(null)",'—']]){assert.equal(vm.runInContext(expression,formatting),expected);}
console.log('Integer K display, tiny flows, signs and unit consistency passed.');
