// Browser validation of the actual self-contained file, with networking disabled.
// Uses bundled Playwright or PLAYWRIGHT_PACKAGE and a local Chrome installation.
const fs=require('fs'),path=require('path'),assert=require('assert');
const {execFileSync}=require('child_process');
const {pathToFileURL}=require('url');
const packagePath=process.env.PLAYWRIGHT_PACKAGE||'C:/Users/16934/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright';
const {chromium}=require(packagePath);
(async()=>{
 const root=path.resolve(__dirname,'..'),dir=path.join(root,'Temp/ArchitectureReview');fs.mkdirSync(dir,{recursive:true});
 const browser=await chromium.launch({headless:true,executablePath:process.env.CHROME_EXE||'C:/Program Files/Google/Chrome/Application/chrome.exe'});
 try{
 const context=await browser.newContext({viewport:{width:1500,height:1000},offline:true});
 const page=await context.newPage(),errors=[],requests=[];page.on('pageerror',e=>errors.push(e.message));page.on('request',r=>requests.push(r.url()));
 await page.goto(pathToFileURL(path.join(root,'Docs/Architecture/HarborCityArchitecture.html')).href);
 await page.locator('#ownergraph svg .node').first().waitFor();
 assert.equal(await page.locator('section').count(),13);
 assert.equal(await page.locator('#classgraph .node').count(),30);
 assert.equal(await page.locator('#ownergraph .node').count(),22);
 await page.locator('#ownergraph [data-id="data"]').click();
 assert.match(await page.locator('#ownergraph aside').innerText(),/WorldData/);
 await page.locator('#ownergraph [data-zoom="+"]').click();
 assert.equal(await page.locator('#ownergraph .zoomtext').innerText(),'125%');
 await page.locator('#ownergraph [data-zoom="0"]').click();
 await page.locator('#class-search').fill('Vehicle');
 await page.locator('#class-results [data-class="vehicle"]').click();
 assert.match(await page.locator('#classgraph aside').innerText(),/Position只读投影/);
 assert.match(await page.locator('#classgraph aside').innerText(),/谁可以修改/);
 await page.locator('#class-search').fill('TripTask');
 await page.locator('#class-results [data-class="task"]').click();
 assert.match(await page.locator('#classgraph aside').innerText(),/Task ID独立于Vehicle ID/);
 await page.locator('#ownergraph [data-id="shipment"]').click();
 assert.match(await page.locator('#ownergraph aside').innerText(),/remaining/);
 await page.locator('#module-filter').selectOption({label:'Roads'});
 await page.locator('#class-search').fill('');
 assert(await page.locator('#class-results button').count()>0);
 await page.locator('#simulation-mode').selectOption('target');
 assert.match(await page.locator('#simulation-note').innerText(),/提案/);
 await page.locator('#simulation-flow [data-index="7"]').click();
 assert.match(await page.locator('#simulation-detail').innerText(),/业务原子/);
 await page.locator('#simulation-mode').selectOption('legacy');
 assert.match(await page.locator('#simulation-note').innerText(),/未运行/);
 await page.locator('#simulation-mode').selectOption('current');
 assert.equal(await page.locator('#simulation-flow .stage').count(),11);
 for(const value of ['construction','commute','freight','save']){
 await page.locator('#business-mode').selectOption(value);assert(await page.locator('#business-flow .stage').count()>=6);
 await page.locator('#business-flow .stage').last().click();assert(await page.locator('#business-detail pre').count()>0);
 }
 await page.locator('#scale').selectOption('10000');assert.match(await page.locator('#quadratic').innerText(),/100,000,000/);
 await page.locator('[data-scene="2"]').click();assert.match(await page.locator('#scene-detail').innerText(),/Shipment/);
 await page.locator('[data-object="factory"]').click();assert.match(await page.locator('#scene-object').innerText(),/FactoryState/);
 const data=await page.evaluate(()=>JSON.parse(document.getElementById('architecture-data').textContent));
 const sources=[...data.cards.map(c=>c.source),...data.readings.map(r=>r.source),...data.evidence.map(e=>e.source),...Object.values(data.flows).flatMap(f=>f.stages.map(s=>s.source))];
 const pinned=new Map();
 for(const s of sources){assert(fs.existsSync(path.join(root,s.file)));assert(s.url.includes(data.commit));assert(s.url.endsWith('#L'+s.line));
 if(!pinned.has(s.file))pinned.set(s.file,execFileSync('git',['show',`${data.commit}:${s.file}`],{cwd:root,encoding:'utf8'}).split(/\r?\n/));
 assert(pinned.get(s.file)[s.line-1].includes(s.token),'Pinned source mismatch: '+s.file+':'+s.line);
 }
 const localMissing=await page.evaluate(()=>Array.from(document.querySelectorAll('a[href]')).map(a=>a.getAttribute('href')).filter(h=>!h.startsWith('#')&&!h.startsWith('https:')));
 for(const href of localMissing)assert(fs.existsSync(path.resolve(root,'Docs/Architecture',href)),'Missing '+href);
 await page.locator('#ownership').scrollIntoViewIfNeeded();await page.locator('#ownergraph').screenshot({path:path.join(dir,'ownership.png')});
 await page.locator('#classes').scrollIntoViewIfNeeded();await page.locator('#classgraph').screenshot({path:path.join(dir,'classes.png')});
 await page.locator('#simulation').scrollIntoViewIfNeeded();await page.locator('#simulation').screenshot({path:path.join(dir,'simulation.png')});
 await page.setViewportSize({width:700,height:1000});await page.locator('#ownership').scrollIntoViewIfNeeded();
 assert(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth+1),'Horizontal page overflow at 700px');
 assert.deepEqual(errors,[]);assert(requests.every(u=>u.startsWith('file:')),'External dependency requested');
 const result={passed:true,offline:true,sections:13,classCards:30,ownershipNodes:22,graphs:4,flows:7,sourceReferences:sources.length,pageErrors:errors,externalRequests:requests.filter(u=>!u.startsWith('file:')),screenshots:['ownership.png','classes.png','simulation.png']};
 fs.writeFileSync(path.join(dir,'browser-validation.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result));
 } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
