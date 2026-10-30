# MS-006 employer funding invalidation

Frozen baseline791942f0dcead4b657be0152f1a430b83bfc87db, independent execution branch. Implementation/target tests1d14d05; observer-free short path and active-observer growth test0f331fe. Complete results follow below. Production resident polling remains unchanged. This is a notification prerequisite, not an FPS improvement.

## Writer and lifecycle audit

|Writer|Actual work and successful boundary|
|---|---|
|CityCorePayroll.PayAttendance; CityModel.PayAttendance|Debit cash, credit wage counters/person; no callback inside the cash setter. Preserve original formulas and modes.|
|CityIndustry.ProcessFactoryWork / RecordFactorySale|Processing debit precedes production ledger updates; sale credit precedes revenue/sold counters. Existing Trace calls stay after their original work.|
|CityCoreIndustryBusiness.Depart|Shop freight prepayment occurs only after deterministic funding checks; original CheckAdmission / Depart / CommitAdmission order is unchanged.|
|CityCoreTrade.Depart|Raw-material purchase debits factory and then records costs/outside receipts; export cash is outside-city money and is not an employer account.|
|CityCoreConsumption.BeginVisit|Retail credit is part of inventory, family payment, and retail settlement; no mid-write dispatch.|
|CityCoreFinance.SettleDay|Original preflight computes charges, then updates balances/tax ledgers; no rule or ordering changes.|
|Growth / Session initialization|Capital/cash set before AddBuilding binds the account. Detached writes emit nothing.|
|BuildingSaveData.ToBuilding / CityCoreSave.Read|Factory payload/shop account reconstructed before CityEntities binding; owner, observed eligibility and dirty sets are not saved.|
|Add / Remove / ReplaceBuilding|Fresh unbound accounts required for new buildings; removal detaches; shop replacement reuses the original account and preserves any pending eligibility change. Factory replacement remains disallowed by the existing API.|
|Same-CityModel replacement session|Existing TaskTests uses this handover; account ownership transfers only within the same model. Cross-city sharing is rejected. The old session already cannot continue after member ownership transfers.|

No ref/out or cash GetField consumer was found in runtime. Direct replacement of an account on an already live building bypasses the supported CityEntities lifecycle and is detected by binding validation, not silently healed. No new compatibility layer or save version is added.

## Contract and ownership

Factory cash uses the existing CitySavedFact mechanism; shop Cash and its DTO retain their names/types. Both store exact bits; with no observer the setter returns directly, otherwise it compares old/new `>0`. Only threshold changes mark an employer; mode changes are detected at flush. CityModel.JobFunded delegates to the same extracted EmployerFunded predicate, including the legacy/development branches.

CityEntities owns account bindings, the last observed eligibility, reusable dirty IDs and notification scratch space. First subscriber seeds current facts; last unsubscribe clears pending changes. With no subscriber, cash writes do not accumulate dirty work. Cash setters do not enumerate employees or notify listeners.

FlushFundingChanges runs before resident iteration and after RequestCommute returns. It compares final current eligibility, deduplicates affected employees via MS005, sorts by Members.Ordinal, and emits internal FundingEligibilityChanged invalidations only. Net-restored balances and failed payments emit no false transition. A mode change may scan employers once; ordinary cash changes inspect only dirty employers and their actual employees. Listeners must only mark derived scheduling state dirty, never dispatch or settle. Runtime scheduling/filtering is not activated.

Targeted tests first passed in Temp/MillionScaleExecution/ms006-target3: manual independent JobFunded oracle, mode changes, transient writes, pending shop replacement, reassignment/removal, added/retired accounts, cold ownership, same-model handover, precise double bits, failed freight unchanged state and synchronous later-member request order. A deliberate private backing-field write is detected by the oracle; production flush does not scan/heal all employers to conceal a missing notification.

The first fixture failed the original household ledger assertion because Create() starts legacy mode without openingSavings initialized for development mode. The fixture now establishes its opening balance before changing modes; the assertion remains intact. Saved-state replay also includes a checkpoint while development mode is active.

## Completed verification

Source retained exactly0f331fe (runtime diff checked after discarding an unsuccessful inline-hint trial). Frozen comparison791942f. All tests below actually ran; no Unity Editor/Player FPS result is claimed.

- VerifyFundingNotifications.ps1 -Results Temp/MillionScaleExecution/ms006-final:39 focused assertions,18 exact saved-state checkpoints, exact full final.hcity bytes and request sequence;1600-step replay +cold80. Independent manual funding oracle detects deliberate bypass of private cash backing field. The assertions are unchanged after correcting fixture openingSavings.
- Same script: mixed growth6200 steps, repaint/road split4000, original30k4000, plus cold continuations;126 exact old/new full-city checkpoints. New growth build has a temporary live funding observer attached in EVERY session including cold loads, asserts member/job ownership and reports9108 notifications. Old business checkpoints remain identical. Temporary observer instrumentation is not in runtime files or performance binaries.
- VerifyEmploymentIndex.ps1 -Reference791942f -Results .../ms006-final-paired -Save <original30k>:726 employee/order oracles, job replacement/detach and1200-step warm/cold continuation, exact city checkpoints.
- VerifyCityCore.ps1 -Results Temp/RoadCore/ms006-core-final.csv under Unity6000.6.3f1 bundled Mono:23627 integrated industry/resident,10000 jobs,1522 grid/payment/rollback,83 zoning and460474 utility assertions passed. Existing SYSLIB0050 reflection warning under .NET remains, no compile errors.

Runtime modifications are only CityEntities, CityIndustry, CityShopAccount and CityCoreResidents. Existing frozen test scripts now also freeze the cash-owner files to avoid mixing incompatible internal interfaces. CityCoreSave/version, business formulas, traffic/clock and gameplay code outside these hooks are unchanged.

## Actual maintenance and full-step costs

Host .NET10 x64 Release MODEL, no attribution timers. Original read-only30k save SHA3F49BD1A02879BB686C24D5517902CC581C657220CE2B955107D89B6FA870B4E; depot lane routing enabled, flat zoning surface.2400 warm steps before timing. Builds/tests were sequential during timed runs.

MS006-Cost.csv is a controlled maintenance test in that loaded city, with a live observer and4 employees on the selected factory. Balance changes inject matching capital purely for diagnostic control; this is NOT a natural gameplay or FPS workload. Each loop warms10 operations before measurement; figures are single-run observations, not statistically established latency guarantees.

|Case|Measured work|Time total|Managed allocation|Notifications|
|---|---|---|---|---|
|Positive→positive|10000 writes + flush|1.6138ms|0B|0|
|Zero→positive cycles|2000 cycles,4000 crossings|0.9502ms|0B|16000|
|Unchanged RebuildJobs|10 calls|11.119ms|0B|0|

Observer attach retained sample+3208B; account binding/cache memory is already included in the whole-scene sample below. Reusable buffers retain peak capacity, not unbounded pending writes. Normal processing is O(dirty employers + affected employees + member sorting); mode switches can scan employers globally once. RebuildJobs itself remains O(jobs + employed members), not a new local job algorithm.

MS006-Paired.csv preserves ALL three four-process A/B/B/A attempts, including unsuccessful experiments:

|Candidate|Old/new mean ms per fixed step|Observation|
|---|---|---|
|Initial1d14d05|2.95053 /3.05986|+3.71%; no performance gain|
|Retained0f331fe|2.86537 /3.03857|+6.04%; measurable maintenance overhead|
|Uncommitted inline hint trial|2.94101 /3.05218|+3.78%; did not prove stable improvement; discarded|

The trial only added MethodImplOptions.AggressiveInlining to the two cash setters; no other source change. Its data remains available, hints removed. The observer-free short path skips comparisons/dirty tracking, but the pairs DO NOT establish that it improves total CPU. No further micro-tuning is included in MS006.

Each process measures600 actual .05-second steps after2400 warm steps. Timed loop excludes Validate/save/hash. All12 final full-city hashes D753D1EBB796CCCE2B836A15E7F8F492CC864E45F8EEA2E6481AA419F97FE6B3 match. Full600-step allocation17,535,888B in EVERY run (unchanged); retained final-pair mean250,573,516→251,032,784B (~+459268B /0.438MiB). P95/P99/max fixed-step values are in CSV; they are NOT render frame times. Source/build fingerprints and full logs remain in local Temp/MillionScaleExecution.

## Decision and handoff

MS006 completes notification correctness and cost characterization, NOT a speed improvement. Production still polls residents, so the added hooks currently cost CPU/memory without removing polling work. Keep on the independent experiment branch; do not integrate into formal development. MS011..013 must count these costs and prove repeated overall benefit before integration. Real Unity Player FPS/multiplier/long-frame acceptance remains MS013.

MS007 is the next and only IN_PROGRESS task: audit health/ability/death and household/address lifecycle invalidations. No health/housing logic was modified in MS006. MS008 still requires006 AND007. No remaining known uncovered supported cash writer; unsupported direct replacement of a live account is detected by binding validation. Rare mode switches and many simultaneously affected employees still have real fan-out cost. No economy, saving or safety assertion was weakened.
