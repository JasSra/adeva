# Onboarding Flow Completeness Analysis

## Summary

Analysis of Client and User onboarding flows revealed critical gaps. Both flows have been reviewed and fixed.

---

## Client Onboarding (Organization) ?

**Flow**: `/Client/Onboarding`

### What It Does
1. ? Validates ABN via IAbrValidator
2. ? Optionally validates ACN via IBusinessLookupService
3. ? Creates Organization entity with status `Pending` (awaiting admin approval)
4. ? Links `OrganizationId` to current user's `UserProfile`

### Database Impact
- Creates: `Organization` (with `IsApproved = false`)
- Updates: `UserProfile.OrganizationId = org.Id`

### Post-Onboarding
- User redirects to `/Client` dashboard
- `SecurityEnforcementMiddleware` checks `profile.OrganizationId != null` ?
- User can access Client portal features

### Status: **COMPLETE** ?

---

## User Onboarding (Debtor) ? ? ? (FIXED)

**Flow**: `/User/Onboarding`

### Original Issue (CRITICAL BUG)
- ? Only updated `FirstName` and `LastName` on `UserProfile`
- ? Did NOT create a `Debtor` entity
- ? Did NOT set `UserProfile.DebtorId`
- ? Caused infinite redirect loop: `/Security/Setup` ? `/User/Onboarding` ? `/User/Onboarding`...

### Root Cause
`SecurityEnforcementMiddleware` checks:
```csharp
if (context.User.IsInRole("User") && profile?.DebtorId == null)
{
    context.Response.Redirect("/User/Onboarding");
    return;
}
```

Since `DebtorId` was never set, users were trapped in onboarding.

### Fix Applied ?

**New Implementation**:
1. ? Creates or retrieves a "Platform" organization for self-service debtors
2. ? Creates a `Debtor` entity linked to the platform organization
3. ? Sets debtor details: `FirstName`, `LastName`, `Email`, `Phone`
4. ? Links `UserProfile.DebtorId = debtor.Id`
5. ? Enables portal access for the debtor
6. ? Sets status to `DebtorStatus.New`

### Database Impact
- Creates: `Organization` (platform org, if not exists)
- Creates: `Debtor` (for user)
- Updates: `UserProfile.DebtorId = debtor.Id`

### Platform Organization Strategy
- **Name**: "Platform" / "Adeva Plus Platform"
- **Purpose**: Container for self-service user debtors who sign up before being assigned to a creditor
- **ABN**: `00000000000` (placeholder)
- **Subdomain**: `platform`
- **Auto-approved**: Yes (created as approved and onboarded)

### Post-Onboarding
- User redirects to `/User` dashboard
- `SecurityEnforcementMiddleware` checks `profile.DebtorId != null` ?
- User can access User portal features

### Status: **FIXED** ?

---

## Security Setup Flow

**Flow**: `/Security/Setup` ? `/Security/Complete`

### POST Handler Routing (Already Implemented) ?

After OTP/TOTP verification:
1. **Admin Role** ? `/Admin`
2. **Client Role** ? Checks `OrganizationId`:
   - If `null` ? `/Client/Onboarding`
   - If exists ? `/Client`
3. **User Role** ? Checks `DebtorId`:
   - If `null` ? `/User/Onboarding`
   - If exists ? `/User`

### OTP Bypass (Dev Only) ?
- Config key: `Security:BypassOtpVerification` (default: `true` in Development)
- Managed via: Admin ? Configuration ? Secrets
- When enabled: SMS/TOTP validation is skipped

---

## SecurityEnforcementMiddleware

**Purpose**: Enforce onboarding completion before allowing portal access

### Enforcement Rules

1. **Security Setup** (all users):
   ```csharp
   var needsSecuritySetup = !user.TwoFactorEnabled || !user.PhoneNumberConfirmed;
   if (needsSecuritySetup && !atSecuritySetup) ? Redirect("/Security/Setup")
   ```

2. **Client Onboarding**:
   ```csharp
   if (User.IsInRole("Client") && profile?.OrganizationId == null) ? Redirect("/Client/Onboarding")
   ```

3. **User Onboarding**:
   ```csharp
   if (User.IsInRole("User") && profile?.DebtorId == null) ? Redirect("/User/Onboarding")
   ```

### Bypass Paths
- `/css/*`, `/js/*`, `/images/*`, `/health/*`, `/api/*`
- `/Account/*`, `/Dev/*`, `/Article/*`
- `/Security/Setup` (to prevent redirect loop)

---

## Testing Checklist

### Client Onboarding
- [ ] Sign in as Client (fake or OIDC)
- [ ] Complete OTP verification
- [ ] Redirected to `/Client/Onboarding`
- [ ] Validate ABN ? Enter org details ? Submit
- [ ] Check database: `Organizations` table has new entry
- [ ] Check database: `UserProfiles.OrganizationId` is set
- [ ] Redirected to `/Client` dashboard
- [ ] No further onboarding prompts

### User Onboarding
- [ ] Sign in as User (fake or OIDC)
- [ ] Complete OTP verification
- [ ] Redirected to `/User/Onboarding`
- [ ] Enter First Name + Last Name ? Submit
- [ ] Check database: `Debtors` table has new entry (linked to Platform org)
- [ ] Check database: `UserProfiles.DebtorId` is set
- [ ] Redirected to `/User` dashboard
- [ ] No further onboarding prompts

### Edge Cases
- [ ] User signs in without Client/User scope ? No onboarding required
- [ ] Admin signs in ? No org/debtor onboarding, only TOTP setup
- [ ] User already onboarded ? Skip onboarding, go straight to portal
- [ ] OTP bypass enabled ? Security setup passes without code validation

---

## Configuration Keys (IAppConfigService)

All managed via: **Admin ? Configuration ? Secrets**

| Key | Default | Purpose |
|-----|---------|---------|
| `Security:BypassOtpVerification` | `true` (Dev), `false` (Prod) | Skip OTP/TOTP validation |
| `DevAuth:EnableFakeSignin` | `true` (Dev), `false` (Prod) | Enable fake dev sign-in |
| `Security:AutoElevateFirstAdmin` | `false` | Auto-grant Admin to first OIDC user |
| `System:BootstrapComplete` | `false` | Sentinel to skip bootstrap checks |

---

## Recommendations

### 1. Debtor-Organization Relationship
Current implementation creates a "Platform" organization for self-service debtors. Consider:
- **Option A**: Keep platform org (current fix) - simple, works for self-service users
- **Option B**: Debtors only created when a Client assigns a debt - more strict, requires admin/client workflow first
- **Option C**: Multi-tenancy: allow debtors to be linked to multiple organizations

### 2. Onboarding UX
- Add progress indicators: "Step 1: Security ? Step 2: Profile"
- Show "Welcome" message after successful onboarding
- Email confirmation after profile creation

### 3. Data Validation
- Add phone number format validation (E.164)
- Add email verification step (send confirmation email)
- Consider adding address fields to debtor onboarding

### 4. Admin Approval Workflow
- Client organizations require admin approval (`IsApproved = false`)
- Consider adding approval notification emails
- Add bulk approval feature in Admin ? Organizations

---

## Files Modified

1. `src/DebtManager.Web/Controllers/SecurityController.cs`
   - Added onboarding routing logic to `Complete` action

2. `src/DebtManager.Web/Areas/User/Controllers/OnboardingController.cs`
   - **FIXED**: Now creates Debtor and sets UserProfile.DebtorId

3. `src/DebtManager.Web/Middleware/SecurityEnforcementMiddleware.cs`
   - Added `/Security/Setup` bypass to prevent redirect loops

4. `src/DebtManager.Web/Data/DbInitializer.cs`
   - Seeds `Security:BypassOtpVerification` config key

5. `src/DebtManager.Web/Auth/TokenValidatedHandler.cs`
   - Gated auto-admin elevation behind `Security:AutoElevateFirstAdmin` flag

---

## Build Status

? **All changes compiled successfully**

---

## Next Steps

1. Test onboarding flows in development environment
2. Verify database state after each onboarding step
3. Consider adding automated integration tests for onboarding
4. Review platform organization strategy with stakeholders
5. Add user feedback/help text to onboarding forms
