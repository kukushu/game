# MS-006 employer funding invalidation

Frozen baseline791942f0dcead4b657be0152f1a430b83bfc87db, independent execution branch. Implementation/target tests are committed first; complete regression/maintenance results will be appended separately. Production resident polling remains unchanged. This is a notification prerequisite, not an FPS improvement.

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

Factory cash uses the existing CitySavedFact mechanism; shop Cash and its DTO retain their names/types. Both store exact bits before comparing old/new `>0`. Only threshold changes mark an employer; mode changes are detected at flush. CityModel.JobFunded delegates to the same extracted EmployerFunded predicate, including the legacy/development branches.

CityEntities owns account bindings, the last observed eligibility, reusable dirty IDs and notification scratch space. First subscriber seeds current facts; last unsubscribe clears pending changes. With no subscriber, cash writes do not accumulate dirty work. Cash setters do not enumerate employees or notify listeners.

FlushFundingChanges runs before resident iteration and after RequestCommute returns. It compares final current eligibility, deduplicates affected employees via MS005, sorts by Members.Ordinal, and emits internal FundingEligibilityChanged invalidations only. Net-restored balances and failed payments emit no false transition. A mode change may scan employers once; ordinary cash changes inspect only dirty employers and their actual employees. Listeners must only mark derived scheduling state dirty, never dispatch or settle. Runtime scheduling/filtering is not activated.

Targeted tests first passed in Temp/MillionScaleExecution/ms006-target3: manual independent JobFunded oracle, mode changes, transient writes, pending shop replacement, reassignment/removal, added/retired accounts, cold ownership, same-model handover, precise double bits, failed freight unchanged state and synchronous later-member request order. A deliberate private backing-field write is detected by the oracle; production flush does not scan/heal all employers to conceal a missing notification.

The first fixture failed the original household ledger assertion because Create() starts legacy mode without openingSavings initialized for development mode. The fixture now establishes its opening balance before changing modes; the assertion remains intact. Saved-state replay also includes a checkpoint while development mode is active.

Complete regression and cost data: PENDING; MS006 remains IN_PROGRESS until the follow-up report records actual results. No Player performance claim or formal integration.
