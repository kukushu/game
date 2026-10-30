# Resident request invalidation contract (MS-002)

Baseline: 93d97f2004b3893d3b76bf765e972e21405d19fa. This is an audit and a staged prerequisite, not activation of event-driven requests.

| Fact / lifecycle | Authoritative writes | Consumer / required invalidation |
|---|---|---|
| tripId | CityCoreResidents.Depart / Arrive; initialization, explicit public writes | AdvanceRequests first guard; both departure and return-to-zero invalidate the member |
| retryAt (float) | AdvanceRequests +0.25; CityCoreConsumption.Step +0.5; BeginVisit; public writes | Both earlier and later deadlines; preserve float rounding and all bits |
| visitUntil (double) | Depart(Home) clears; Consumption.BeginVisit sets arrival + VisitSeconds; public writes | Both earlier and later deadlines; preserve exact double value |
| owner binding | CityEntities constructor / AddResident; RemoveResident detaches | Deserialization/object initialization has no owner; listeners attach to the new session, never persist |
| membership | AddResident / RemoveResident; MoveResident preserves Members position | Separate structural invalidation required in MS-004, not a global building Revision subscription |

Repository-wide C# search found no ref/out accesses to these three resident facts and no GetField lookup of them. Other types have their own tripId and remain unchanged. CityCoreSave already explicitly supports [CitySavedFact] properties; ordinal wire name/type/order remain identical, private backing fields and the owner stay outside the save. No migration/version adapter is needed or added.

A setter must store the original value before notifying. Floating equality must not discard negative zero or NaN payload writes. Notifications only invalidate derived scheduling state; subscribers must not dispatch, settle, or evaluate incomplete departure/arrival transactions inside a setter. Candidate evaluation stays at the existing AdvanceRequests stage with original Members order. No per-person delegate/closure is necessary: the unique CityEntities owner exposes an internal observer for one member and fact kind.

Guard-only filtering cannot sleep an otherwise eligible member until a shift time: job assignment/release, employer funding, illness, ability, household changes, day and network/access dependencies still require MS-005..009. Until then they remain conservatively polled. Synchronous request/log callbacks and writes to later members during a pass require explicit order tests before activation. A successful shadow run without these contracts is not proof that arbitrary state changes are safe.

MS-003 acceptance: direct writes (including earlier deadlines), detach/new owner, actual dispatch/arrival/shopping, float/double bits, frozen-baseline wire and full-state replay. MS-004 must prove the earlier-deadline positive case and retain a deliberately bypassed-notification negative control. Runtime resident polling is unchanged in both stages.
