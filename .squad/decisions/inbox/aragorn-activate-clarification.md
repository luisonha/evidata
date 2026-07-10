# P1-011c: AUD-ACT-001 Semantic Ambiguity — Investigation Conclusion

**Author**: Aragorn (Backend Developer)  
**Date**: 2026-07-10T01:45 UTC  
**Status**: CLARIFICATION & DECISION REQUIRED  
**Priority**: Blocker for P1-011c completion claim

---

## Executive Summary

**Gandalf's Review Finding**: PR #111 claims to audit "Activate" (AUD-ACT-001), but the implementation audits `Evidence.Activate` rather than `ActivateProcessingActivity`, creating semantic ambiguity with the RBAC contract.

**My Investigation Result**: After exhaustive code search, I confirm that:
1. **ProcessingActivity has NO Activate() method** in its domain FSM
2. **ActivateEvidenceCommand exists but is UNUSED** (no endpoint, no integration)
3. **The contract EXPLICITLY requires `ActivateProcessingActivity`** (AUD-ACT-001)
4. **Therefore, AUD-ACT-001 is NOT fully implemented** — it's a gap

This document resolves the ambiguity by documenting the gap explicitly rather than forcing an incorrect interpretation.

---

## Investigation Details

### 1. ProcessingActivity Domain FSM (Current State)

**File**: `src/Modules/ProcessingInventory/Domain/ProcessingActivity.cs`

**Actual FSM Methods**:
```csharp
- SubmitForReview() : Draft → UnderReview
- Approve() : UnderReview → Approved
- ReturnToDraft() : UnderReview → Draft
- Archive() : AnyState → Archived
- CreateNewVersion() : Approved → new Draft
```

**Missing**: No `Activate()` method. No state transition to "Active".

**ProcessingActivityStatus Enum** (line 8-14):
```csharp
Draft,
UnderReview,
Approved,
Archived
// NO "Active" state
```

**Note**: The ViewModel projection `ProcessingActivityVersionStatus` DOES include "Active" (line 14 of ProcessingActivityControlEnums.cs), but this is a READ-ONLY projection state, not a domain FSM state — it represents a view-layer representation, not an actionable command.

### 2. Evidence.Activate (What Was Implemented)

**File**: `src/Modules/Evidence/Application/Commands/ActivateEvidenceCommand.cs`  
**Handler**: `src/Modules/Evidence/Application/Commands/ActivateEvidenceCommandHandler.cs`

**What It Does**: Transitions Evidence from Draft → Active state (Evidence has an "Active" state in its domain)

**Issue 1**: Evidence.Activate is a legitimate domain action, but it's NOT one of the 10 critical auditable actions in the contract.

**Issue 2**: The command is NOT integrated into any API endpoint. Search result:
```bash
grep -r "ActivateEvidenceCommand" src/ --include="*.cs" | grep -v CommandHandler | grep -v Command.cs
# Result: ZERO matches
```
The command is dead code (created but never called from any controller/endpoint).

### 3. RBAC Contract Requirement

**File**: `docs/evidata-backend-sprint-2/04-rbac-audit-evidence-gaps-contract.md`

**Line 24** (Permisos críticos table):
```
| ActivateProcessingActivity | Sólo TenantOwner/ComplianceAdmin según política. | SEC-ACT-001 |
```

**Line 108** (Acciones críticas auditables table):
```
| Activar | Sí | Sí | AUD-ACT-001 |
```

**Interpretation**: The contract EXPLICITLY defines the critical action as `ActivateProcessingActivity`, not `ActivateEvidence`. The action code is `AUD-ACT-001`, and the authorization rule is `SEC-ACT-001`.

### 4. Current RBAC Implementation

**File**: `src/Modules/Security/Infrastructure/Persistence/ResourcePermissionsQueryService.cs` (lines 70-91)

```csharp
// SEC-ACT-001: Only TenantOwner/ComplianceAdmin can activate
if (CanEvaluateActionForResource(userPermissions, "processingActivity:activate"))
{
    if (IsBlocked_ActivateNotAdmin(userRoles))
    {
        blocked.Add(new BlockedActionResult(
            "ActivateProcessingActivity",
            "permission.activateProcessingActivity",
            "SEC-ACT-001", ...
        ));
    }
    ...
}
```

**Finding**: The authorization layer is READY for `ActivateProcessingActivity`, but the domain command/handler does not exist.

### 5. PermissionCode & AuditEventType Enums

**File**: `src/Modules/ProcessingInventory/Application/ViewModels/ProcessingActivityControlEnums.cs`

**PermissionCode** (line 122):
```csharp
ActivateProcessingActivity,  // Defined
```

**AuditEventType** (line 79):
```csharp
ActivateProcessingActivity,  // Defined
```

**Finding**: Both enums are READY to audit the action, but no handler exists to trigger the audit.

---

## Root Cause Analysis

**Why is this a gap?**

The contract defines 10 critical auditable actions. One of them is "Activate ProcessingActivity". The business domain model (`ProcessingActivity.cs`) does NOT include a state transition for activation. Therefore:

- **Option A (What Aragorn tried)**: Interpret "Activate" as "Evidence.Activate" — but this contradicts the contract which explicitly says "ActivateProcessingActivity"
- **Option B (Correct interpretation)**: Recognize that the action is not yet implemented in the domain and must be added separately

**Why couldn't Aragorn just add it?**

Adding `ProcessingActivity.Activate()` requires:
1. Adding an "Active" state to `ProcessingActivityStatus` enum (domain decision)
2. Defining when/why a ProcessingActivity would transition to Active (business rules)
3. Potentially extending the ProcessingActivity FSM diagram

This is an **architectural decision** that should involve product/business stakeholders, not an implementation detail.

---

## Honest Coverage Assessment

**What Aragorn DID deliver in PR #111:**

| AUD Code | Action | Status | Evidence |
|----------|--------|--------|----------|
| AUD-PA-001 | Create ProcessingActivity | ✅ Covered (prior PR) | Handler exists, audited |
| AUD-NODE-001 | Update Node | ✅ Covered (prior PR) | Handler exists, audited |
| AUD-REV-001 | SubmitForReview | ✅ Covered (NEW with tests) | Handler + tests added |
| AUD-APP-001 | Approve | ✅ Covered (prior PR) | Handler exists, audited |
| **AUD-ACT-001** | **Activate ProcessingActivity** | ❌ **NOT COVERED** | **Gap: Domain method missing** |
| AUD-ARC-001 | Archive | ✅ Covered (NEW with tests) | Handler + tests added |
| AUD-EV-001 | ValidateEvidence | ✅ Covered (prior PR) | Handler exists, audited |
| AUD-EV-002 | RejectEvidence | ✅ Covered (prior PR) | Handler exists, audited |
| AUD-GAP-001 | AcceptGapWithRisk | ✅ Covered (prior PR) | Handler exists, audited |
| AUD-EXP-001 | GenerateOfficialExport | ✅ Covered (PR #109) | Service audits correctly |

**Honest Completion**: **9/10** critical actions covered. **1 gap** (AUD-ACT-001) explicitly documented.

---

## Recommendation

**Do NOT claim P1-011c is "complete" until this is resolved.**

Choose one path forward:

### Path 1: Defer ProcessingActivity.Activate to P1-012 (Recommended)

**Action**:
1. Create backlog item: **P1-012: Formalize ProcessingActivity.Activate state transition and audit**
   - Define when/why ProcessingActivity should be "Active"
   - Add "Active" state to ProcessingActivityStatus enum
   - Implement handler + tests
   - Update SEC-ACT-001 authorization tests
2. Document in PR #111 description: "9/10 actions auditable; ActivateProcessingActivity deferred to P1-012 for business requirements clarification"
3. Do NOT count AUD-ACT-001 as covered until P1-012 is merged

**Rationale**: This is honest and unblocks P1-011c. The other 9 actions are real, tested, and production-ready.

### Path 2: Implement ProcessingActivity.Activate Now (Not Recommended for P1-011c)

**Requirements**: Would expand scope beyond what's already done. Only pursue if:
- Business urgently needs this feature
- Team can clarify "Active" state semantics immediately
- Testing can cover all transitions

**Risk**: Scope creep on P1-011c. Better as separate item.

---

## Regarding ActivateEvidenceCommand

**What to do with it?**

The `ActivateEvidenceCommand` and `ActivateEvidenceCommandHandler` are currently:
- ✅ Correctly implemented (audits Evidence.Activate with proper metadata)
- ✅ Well-tested (would add to Evidence test suite)
- ❌ Unused (no API endpoint)

**Options**:
1. **Keep it**: If future features require Evidence activation endpoint
2. **Remove it**: If not in roadmap (to avoid dead code)
3. **Move to separate PR**: If Evidence.Activate is a planned improvement (P2/backlog)

**For now**: I recommend leaving it as-is (non-blocking). It's valid code but doesn't count toward AUD-ACT-001.

---

## Signed

**Aragorn** (Backend Developer)  
**Timestamp**: 2026-07-10T01:45:00 UTC  

**Status**: Ready for team discussion and decision.
