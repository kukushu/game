# MS-007 resident/household invalidation (in progress)

Frozen baseline934e064. Formal polling remains authoritative. Runtime changes are CityResidents, CityHouseholds and CityEntities; no medical/death/relocation gameplay or save format change.

## Audit and contract

CanAttendWork reads canWork and sick; AdvanceRequests separately reads dead, location, attendance and actual member.Family resident/home. Numeric health, age and skill are not directly used by this commute predicate. No active fixed-step medical/death writer was found: the current core initializes these facts and permits explicit writes/tests. Notifications do not invent a disease, death, age or corpse lifecycle. Missing future business systems are not declared implemented.

canWork/sick/dead/location and Household.home/resident now use existing explicit saved-fact properties, preserving fact name/type/default/order. atWork keeps its existing attendance update and then invalidates state. Public direct writes are caught; same-value boolean/int writes produce no invalidation. A setter only marks a member dirty through the internal ResidentStateChanged contract. Subscribers must not evaluate/dispatch within partial departure/arrival or household mutations.

Family owner is attached during construction/AddFamily and same-model handover. MoveResident keeps physical location and original Members ordinal, updates both membership sides first, then notifies that member. Add/remove still emit the existing structural invalidation; removed residents detach. Direct manipulation of family.people/householdId is not a supported substitute for these lifecycle methods and is not silently repaired.

A minimal home→households derived index supports residential SetAvailability/ReplaceBuilding invalidation without a city-wide household scan. Family home changes maintain it even with no observer. Household notices are sorted by Members.Ordinal, not family.people or HashSet order. SetAvailability with unchanged flags produces no state notice; original transport/job operations are retained. Occupied housing closure still rejects under the original rule. MS017 may reuse this index; full housing-unit allocation optimization is not claimed.

Per-person changes are O(1); family/home notifications visit only related members and sort their original ordinals. Home index is O(households), never a second housing/resident authority. Its cold validation checks, not repairs, bindings.

## Confirmed pre-existing cold-order defect

In frozen934e064 AND this candidate: move resident1 to a new family then back. Warm Members order1;2;3;4 becomes cold2;3;4;1 because save retains family people order while runtime MoveResident retains the original Members order. After20 fixed steps full states differ. Raw counterexample is in ms007-target/{old,new}/state/existing-order-defect.csv. This is NOT a passing cold-equivalence test; no assertion is relaxed to hide it.

Notification order is correct in each actual runtime and remains equivalent to frozen baseline. Ordinary unmoved cold80 passes. Reordered-family general cold equivalence is NOT established. Keep MS007 IN_PROGRESS pending this correctness issue; do not start MS008 or activate sparse execution on this evidence. Any save-order fix needs an explicit persisted-order design and separate frozen semantic comparison; do not silently sort by citizen ID or add a blanket legacy adapter.

Targeted candidate currently passes direct sickness/ability/death/location/attendance, family activity/home, move without teleport, deliberately reordered family notice order, housing availability/replacement, failed occupied closure, add/remove/detach and cold owner tests. Independent oracle exposes a deliberately bypassed private state write. Full growth/30k regression and maintenance results are pending in this commit.
