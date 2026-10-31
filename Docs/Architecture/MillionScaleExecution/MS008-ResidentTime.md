# MS-008 exact temporal predicate planning

Baseline405fd8e. Pure internal helper in CityCoreResidents.cs; production AdvanceRequests remains byte-for-byte unchanged. SimulationClock.MaximumTick names the existing1e12 bound (same value/validation), avoiding another independent runtime limit. No queue, resident cache, persistent fact, route query, dispatch or step mutation is added.

## Temporal contract

Departure receives the already effective commute estimate in minutes, AFTER the original observedHome/work/network and lastCommute maximum. It does not recompute that estimate or read Network. End is computed with original float shift+duration. Minute evaluation uses original double absolute/modulo →float dayElapsed →float division by float daySeconds →float multiplication1440. Guard evaluation directly uses !(clock<retryAt || clock<visitUntil); NaN guards therefore retain the original comparison behavior rather than Math.Max propagation.

Departure, ShiftEnd and AfterGuards return a value-type check with Predicate / Calendar / Conservative / ClockEnd reason. Predicate is only the temporal predicate, NOT proof of business eligibility or permission to dispatch. After an attempted dispatch the caller must pass minimum=tick+1; this prevents same-tick repeats when float(clock+.25) rounds down. Health, funding, household, job, trip and access prerequisites remain outside this helper.

Search only covers the current unambiguous calendar day and always returns a calendar recheck instead of silently waiting across a day reset. Monotone binary search uses exact original comparisons, O(log clock-range), no per-call allocation/delegates. If floor-based day labeling and modulo reconstruction disagree at extreme floating-point dates, return Conservative at the requested minimum; no citizen can be skipped on an assumed monotonic minute. Invalid calendar ranges also recheck immediately. ClockEnd is a prediction-domain marker, not permission to discard clock debt or suppress existing simulation errors.

The helper is currently uncalled by normal game logic. MS009 must wire graph/access/day/setting invalidation; MS010 full shadow and MS011..013 actual execution/Player benefit remain mandatory. No performance benefit or event-driven simulation is claimed.

Targeted independent sequential oracle and real30k diagnostic replay are implemented in VerifyResidentTime.ps1. Diagnostic temporary source captures original effective inputs AFTER existing comparisons; it performs no extra route search and is not compiled into runtime. Probe starts only AFTER2400 warm steps and caps at256 residents. Exact old/new native18 state/request comparison and cold80 are required. Full results will follow after core/budget checks.
