# MS-007 resident/household invalidation (in progress)

Frozen baseline934e064. Formal polling remains authoritative. Runtime changes are CityResidents, CityHouseholds and CityEntities; no medical/death/relocation gameplay or save format change.

## Audit and contract

CanAttendWork reads canWork and sick; AdvanceRequests separately reads dead, location, attendance and actual member.Family resident/home. Numeric health, age and skill are not directly used by this commute predicate. No active fixed-step medical/death writer was found: the current core initializes these facts and permits explicit writes/tests. Notifications do not invent a disease, death, age or corpse lifecycle. Missing future business systems are not declared implemented.

canWork/sick/dead/location and Household.home/resident now use existing explicit saved-fact properties, preserving fact name/type/default/order. atWork keeps its existing attendance update and then invalidates state. Public direct writes are caught; same-value boolean/int writes produce no invalidation. A setter only marks a member dirty through the internal ResidentStateChanged contract. Subscribers must not evaluate/dispatch within partial departure/arrival or household mutations.

Family owner is attached during construction/AddFamily and same-model handover. MoveResident keeps physical location and original Members ordinal, updates both membership sides first, then notifies that member. Add/remove still emit the existing structural invalidation; removed residents detach. Direct manipulation of family.people/householdId is not a supported substitute for these lifecycle methods and is not silently repaired.

A minimal home→households derived index supports residential SetAvailability/ReplaceBuilding invalidation without a city-wide household scan. Family home changes maintain it even with no observer. Household notices are sorted by Members.Ordinal, not family.people or HashSet order. SetAvailability with unchanged flags produces no state notice; original transport/job operations are retained. Occupied housing closure still rejects under the original rule. MS017 may reuse this index; full housing-unit allocation optimization is not claimed.

Per-person changes are O(1); family/home notifications visit only related members and sort their original ordinals. Home index is O(households), never a second housing/resident authority. Its cold validation checks, not repairs, bindings.

## Confirmed pre-existing cold-order defect

In frozen934e064 AND this candidate: move resident1 to a new family then back. Warm Members order1;2;3;4 becomes cold2;3;4;1 because save retains family people order while runtime MoveResident retains the original Members order. After20 fixed steps full states differ. Raw counterexample with final-state SHA256s is in ms007-counterexample/{old,new}/state/existing-order-defect.csv; the small MS007-ExistingColdOrder.csv is committed for independent checking. This is NOT a passing cold-equivalence test; no assertion is relaxed to hide it.

Notification order is correct in each actual runtime and remains equivalent to frozen baseline. Ordinary unmoved cold80 passes. Reordered-family general cold equivalence is NOT established. Keep MS007 IN_PROGRESS pending this correctness issue; do not start MS008 or activate sparse execution on this evidence. Any save-order fix needs an explicit persisted-order design and separate frozen semantic comparison; do not silently sort by citizen ID or add a blanket legacy adapter.

Targeted candidate passes direct sickness/ability/death/location/attendance, family activity/home, move without teleport, deliberately reordered family notice order, housing availability/replacement, failed occupied closure, add/remove/detach and cold owner tests. Independent oracle exposes a deliberately bypassed private state write. ## Executed verification and limits

Runtime586777d, frozen934e064; .NET10 x64 Release model, no attribution hooks. VerifyResidentState.ps1 -Results Temp/MillionScaleExecution/ms007-full passed88 focused notification assertions, same request sequence,16 saved-state marks +ordinary cold80, and identical final .hcity wire. Numeric medical/death business systems were not added. Corrected negative oracle explicitly throws on changed sickness without a notification; bypass is test reflection only.

Mixed growth6200, repaint/road split4000 and original30k4000 steps, plus cold80 continuations:126 exact old/new saved-state checkpoints. New growth sessions INCLUDING cold sessions receive a temporary state observer, validate notified member identity and counted129673 actual state notifications. This diagnostic patch is Temp-only. These successful cold cases did not reorder family membership; they do not override the separate failing counterexample.

VerifyEmploymentIndex.ps1 -Reference934e064 -Results .../ms007-paired -Save <original30k>:726 job/member oracles,1200-step warm/cold and full-state comparisons passed. Unity6000.6.3f1 bundled Mono VerifyCityCore.ps1 -Results Temp/RoadCore/ms007-core-final.csv passed23627 integrated,10000 jobs,1522 grid,83 zoning,460474 utility checks. Existing .NET SYSLIB0050 warning remains. No actual Unity Play/Player FPS test ran for this notification prerequisite.

Original30k save SHA3F49BD1A02879BB686C24D5517902CC581C657220CE2B955107D89B6FA870B4E stays read-only. Four process A/B/B/A, each2400 warm +600 timed .05-second steps, no parallel test builds during timing. MS007-Paired.csv: means2.89934→2.92947ms (~+1.04%); old individual2.80411/2.99458 andnew2.86537/2.99357 show drift, so no stable speed gain is established. All600-step allocations17,535,888B, final hashes D753D1EBB796CCCE2B836A15E7F8F492CC864E45F8EEA2E6481AA419F97FE6B3 identical. Retained mean+728380B (~0.695MiB), mainly the new household index/owner state. Resident polling is still full; nothing here demonstrates scale or FPS improvement.

Controlled notification loops in the loaded30k city (not natural gameplay), after2400 steps and100 warm actions per case:10000 no-op action pairs0 notices,10000 illness cycles20000 notices,1000 family residence cycles6000 notices for3 members. All measured0 managed bytes; raw times2.4154/0.1319/0.2401ms in MS007-Cost.csv. These very small single-run timings can include tiered-JIT/drift and are not latency guarantees or comparative speed wins. Normal model-step statistics above are the broader workload measure.

The existing cold-order defect remains a real FAIL for moved-family warm/cold equivalence, despite passing notification-vs-baseline comparisons. MS007 remains IN_PROGRESS, not DONE; MS008 remains TODO. Next substep must explicitly persist the original member execution sequence and test interleaved families; silently sorting by resident ID would change supported original order. Do not weaken state assertions, silently alter currency/traffic rules, or add a broad old-save adapter to hide this. Any intentional save-schema evolution must be reported and compared separately from notification equivalence. No formal integration.

## Follow-up resolution

The previously recorded cold-order failure is fixed by86145e2 with explicit saved execution order and format18. See [member-order completion](MS007-MemberOrder.md) and its old/new hashes. Earlier format17 evidence above is retained as historical measurement; current game rejects old formats and uses a separate default filename. MS007 can now close; no scheduler execution was enabled.
