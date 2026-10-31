# MS-007 completion: persist the execution sequence

Baseline30ede55, implementation86145e2. This fixes the original moved-household save/load divergence found in MS007-ExistingColdOrder.csv. No economic, resident decision, routing, vehicle, fixed-step or time-budget rule changes.

## Authority and format

CityEntities owns the actual Members execution sequence. Household.people remains the family grouping/selection order; it cannot encode interleaved global execution order. SnapshotBusiness now writes a memberOrder integer ID array from that actual sequence. It is an export of the same authority, not a second population or independently mutable runtime order cache.

Cold construction resolves IDs to the original restored CityMember/CityResident facts and assigns derived attendance ordinals in that sequence. The ID array is not retained as another live authority. Count, unknown IDs and duplicates are validated before resident, family or account ownership transfer. New-city construction keeps the original initial family traversal; same-CityModel replacement sessions preserve the existing live Members order. No sorting by citizen ID.

City save format18 / business schema9 replaces17 /8. Runtime reads ONLY18; the old16 conduit fallback was removed, not extended. Missing execution order is rejected. Current saves must contain every registered member once, including members that are not currently eligible to work. The added payload is an ID per member (~4 bytes each plus the field envelope); no new work is added to the fixed-step loop. Constructor validation/materialization is O(N), saving the sequence O(N), transient order IDs O(N).

Default city save path now derives from CityCoreSave.DefaultFileName:harbor-city-core-v18.hcity. This prevents the experiment from overwriting the old default harbor-city-core-v16.hcity or its backup. The separate traffic-only hcrc path is unchanged. **Old city files are intentionally unsupported by this experimental game version; keep them intact and create a new city for normal play.** Formal development remains unchanged.

## Strict cross-format regression

Native17 and18 bytes intentionally differ. Tools/Diagnostics/CityOrderWire.cs is a TEST-only binary witness: it uses the same private scalar encoder, normalizes exactly the known header/business version metadata, omits exactly the new memberOrder root field, includes every other business/transport fact bit-for-bit, and appends the actual runtime execution sequence. Frozen tests assert its legacy envelope equals the frozen native save exactly. New tests independently require native18 cold roundtrip byte equality AND original execution order AND identical continued full state. Thus new schema metadata is checked separately, not used to waive business assertions.

For existing large inputs, a host compiled with frozen30ede55 reads the original16/17 fixture and exports an18 test copy using the observed baseline order. This preserves that reader's actual utility rules and every loaded fact, does not rebuild the city, does not modify the original file and cannot infer an already-lost historical warm order. No frozen reader/export or witness code is linked into the game. It is not a supported player-save migration layer.

## Actually executed

- VerifyMemberOrder.ps1 -TargetedOnly -Results Temp/MillionScaleExecution/order-path-target:23 current checks /10 frozen baseline checks. Move-away/back and interleaved families now retain1;2;3;4 and identical full states after240 steps; the frozen reader reproduces its old divergence. Explicit non-ID order4;3;2;1 survives unchanged.1100 add/remove generations exercise ordinal compaction. Duplicate/unknown/missing/null/count-mismatched orders reject. Rejected direct constructor leaves the live city unchanged. Interleaved same-model handover preserves order. Native18 read/write bytes are exact. Current default filename and old-format rejection checked.
- MS007-MemberOrder.csv contains old and new order/state hashes. Each new warm trajectory equals the original frozen warm trajectory; cold new equals warm. Old broken cold is intentionally different and remains recorded as failure evidence, not a passing behavior requirement.
- VerifyMemberOrder.ps1 -Results .../order-final: existing88 state-notification assertions, exact request/witness replay, ordinary cold80; mixed growth6200, repaint/split4000, real30k4000 plus cold continuations:126 exact frozen/current full-fact AND runtime-order witness checkpoints.
- VerifyFundingNotifications.ps1 -TargetedOnly -Results .../order-funding:39 funding checks, failed payment, lifecycle, request and cold/full-fact witnesses passed after the schema change.
- VerifyEmploymentIndex.ps1 -Reference30ede55 -Results .../order-employment:726 employee/order oracles and1200-step cold continuation passed.
- VerifyResidentGuards.ps1 -Results .../order-guards: frozen93 exact guard/full-fact/order comparisons,126 growth/30k checkpoints andcold80 passed using separate same-city fixtures.
- Final code86145e2: bundled Unity6000.6.3f1 Mono VerifyCityCore.ps1 passed23627 integrated,10000 jobs,1522 grid,83 zoning,460474 utility checks. The formerly hardcoded17 utility assertion now requires18; optional old16/17 inputs must be rejected without modifying their files, while current-format utility activation/cold checks are retained.
- VerifySimulationBudget.ps1 passed101 checks: state, saved debt, finite batches, wall-time yielding, failed command atomicity, in-flight editing/recovery; no dropped fixed steps.

Existing verification paths are retained. State verification now invokes the stronger member-order suite; older paired suites freeze the save/session definitions together and compare explicit canonical witnesses rather than incorrectly requiring native bytes from different protocols to match. Large original input remains SHA3F49BD1A02879BB686C24D5517902CC581C657220CE2B955107D89B6FA870B4E. Full logs, same-input copies and binaries remain local Temp/MillionScaleExecution/order-*.

No FPS or throughput gain is claimed, no loading optimization attempted. Native Editor/Player UI clicking was not run; the new default path is verified in code and model tests. This is a persistence correctness fix that allows MS007 to close. MS008 is next; actual sparse resident execution and real Player benefit still require MS010..013. No branch integration or player-file cleanup.
