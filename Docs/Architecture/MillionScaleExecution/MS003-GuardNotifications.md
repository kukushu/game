# MS-003: resident request guard notifications

Baseline 93d97f2004b3893d3b76bf765e972e21405d19fa; implementation commit is recorded in Progress after committing this report. Two runtime files changed: CityResidents.cs and CityEntities.cs. Existing Entity owner now also owns trip/retry/visit invalidation. Setters store original bits, then emit a typed member notification when an observer exists. No observer means no membership lookup or allocation. No queue or request execution is enabled here.

The existing explicit CitySavedFact mechanism preserves fact names/types and ordinal binary order. CityCoreSave and its version are unchanged; no old-save adapter was added. These notifications are not business transaction events: observers can mark a derived candidate dirty, never dispatch or settle inside a setter.

Actual commands: Tools/VerifyResidentGuards.ps1; Tools/VerifyCityCore.ps1 -Results Temp/RoadCore/ms003-core.csv. Fresh outputs: Temp/MillionScaleExecution/guard-checks, guards-run.log, core-run.log. Host: .NET 10 x64 Release for frozen comparisons; Unity 6000.6.3f1 bundled Mono for core checks. These are model tests, not Unity Player performance tests.

Results: frozen 93d vs new exact guards.csv and final .hcity bytes; 126 exact growth/occupied/pending/repaint/split/30k state checkpoints per version plus cold80. Direct guard fixture completed 2000 fixed steps on warm and cold sessions with 20 exact continuation checkpoints; 33 baseline / 133 notification assertions. Observed 60 writes (17 trip,28 retry,15 visit); includes direct writes, equal values, earlier/later deadlines, signed zero/NaN payload/infinity bit retention, member move/remove and detached/cold ownership. Negative floating values are storage tests only, not claims that invalid city inputs pass Validate.

Core Mono: 23627 integrated industry/resident,10000 jobs,1522 grid/rollback/payment/save,83 zoning and460474 utility checks passed. Existing .NET SYSLIB0050 reflection warning remains; no compile errors. git diff --check passed.

No performance benefit claimed: authoritative full polling still runs, no real Player comparison yet. MS-004 tests notification-driven shadow coverage before any execution replacement. Job/funding/health/day/network scheduling remains unimplemented and conservatively polled.
