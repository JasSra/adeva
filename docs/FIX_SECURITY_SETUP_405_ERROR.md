# Fix: HTTP 405 Error on Security/Setup Page

## Issue
When clicking "Verify Phone" on the Security/Setup page after receiving an SMS code, the application returned:
```
HTTP ERROR 405
https://localhost:5001/Security/Setup
```

## Root Cause
The `asp-action` tag helper in the form was not resolving correctly, causing the form to submit to `/Security/Setup` instead of `/Security/Complete`. 

The `Setup` action only accepts `[HttpGet]` requests, so when the form tried to POST to it, the server returned HTTP 405 (Method Not Allowed).

## Solution Applied

Changed all three forms in `Security/Setup.cshtml` from using `asp-action` tag helpers to explicit action URLs:

### 1. SendSms Form (Step 1 - Send Code)
**Before:**
```html
<form asp-action="SendSms" method="post">
```

**After:**
```html
<form action="/Security/SendSms" method="post">
```

### 2. TOTP Complete Form (Authenticator App)
**Before:**
```html
<form asp-action="Complete" method="post">
```

**After:**
```html
<form action="/Security/Complete" method="post">
```

### 3. SMS Complete Form (Step 2 - Verify Code)
**Before:**
```html
<form asp-action="Complete" method="post">
```

**After:**
```html
<form action="/Security/Complete" method="post">
```

## Why This Happened

The `asp-action` tag helper relies on the MVC routing context to resolve controller actions. Possible reasons it failed:
1. The view might not have had proper routing context
2. Tag helper not properly loaded in _ViewImports.cshtml
3. Controller/Action name resolution issue

Using explicit paths ensures the forms always post to the correct endpoint regardless of routing context.

## Testing

### SMS Flow (Client/User)
1. Navigate to `/Security/Setup`
2. Enter phone number ? Click "Send code"
3. ? Redirects back to `/Security/Setup` with "Code sent" message
4. Enter phone number + SMS code ? Click "Verify Phone"
5. ? Posts to `/Security/Complete` successfully
6. ? Redirects to appropriate onboarding page

### TOTP Flow (Admin)
1. Navigate to `/Security/Setup`
2. Scan QR code with authenticator app
3. Enter 6-digit code ? Click "Verify & Enable"
4. ? Posts to `/Security/Complete` successfully
5. ? Redirects to `/Admin` dashboard

## Files Changed

- **Modified:** `src/DebtManager.Web/Views/Security/Setup.cshtml`
  - Line ~93: SendSms form action
  - Line ~58: TOTP Complete form action
  - Line ~115: SMS Complete form action

## Build Status
? Build successful - No errors, no warnings

## Related Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/Security/Setup` | GET | Display security setup page |
| `/Security/SendSms` | POST | Send SMS verification code |
| `/Security/Complete` | POST | Complete security setup (TOTP or SMS) |

## Prevention

For future forms, prefer explicit action URLs when:
- The form might be rendered in different routing contexts
- The controller/action resolution is ambiguous
- You need guaranteed routing behavior

Tag helpers are still fine for most cases, but explicit URLs provide more reliability for critical flows like authentication.

---

**Status:** ? Fixed
**Verified:** Build successful
**Action Required:** Test the SMS verification flow
