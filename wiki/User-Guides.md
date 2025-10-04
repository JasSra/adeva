# User Guides

This document provides workflow documentation for each user role in the Debt Management Platform.

## Table of Contents

- [Debtor (User) Workflows](#debtor-user-workflows)
- [Client (Creditor) Workflows](#client-creditor-workflows)
- [Admin Workflows](#admin-workflows)

---

## Debtor (User) Workflows

Debtors are end users who have outstanding debts and use the platform to manage and pay them.

### 1. Registration & Onboarding

**Process:**

1. **Receive Reference ID** - Debtor receives a unique reference ID via email/SMS from their creditor
2. **Access Platform** - Visit the platform URL (may be white-labeled for specific creditor)
3. **Register Account** - Provide personal information and create account
4. **Verify Identity** - Complete OTP (One-Time Password) verification via email or SMS
5. **View Debt Details** - See debt summary, amount owed, and creditor information

**Requirements:**
- Valid email address
- Phone number for SMS verification
- Reference ID from creditor

### 2. Accepting a Debt

Once registered, debtors must formally accept the debt before payment:

1. Navigate to **My Debts** section
2. Review debt details (amount, origin, creditor)
3. Click **Accept Debt** button
4. Acknowledgment is recorded and creditor is notified

### 3. Choosing a Payment Plan

After accepting a debt, debtors are offered **three payment plan options:**

#### Option A: Full Payment with Discount
- Pay entire debt immediately
- Receive maximum discount (creditor-configured)
- One-time Stripe checkout
- Debt settled immediately

#### Option B: System-Generated Weekly Plan
- Automated weekly installments
- Partial discount applied (creditor-configured)
- Fixed schedule over configured period
- Automatic reminders before each payment

#### Option C: Custom Payment Schedule
- Debtor proposes custom payment dates and amounts
- Subject to admin approval
- Administrative fees apply
- No discount offered

**Selecting a Plan:**
1. Click on preferred payment option
2. Review payment schedule and total amount
3. Confirm selection
4. Proceed to payment setup

### 4. Making Payments

**Stripe Checkout Flow:**

1. Click **Make Payment** on dashboard
2. Redirected to Stripe-hosted checkout
3. Enter payment details securely
4. Complete payment
5. Redirected back to platform
6. Payment confirmation displayed

**Supported Payment Methods:**
- Credit/Debit cards
- Bank transfers (ACH)
- Digital wallets (Apple Pay, Google Pay)

### 5. Viewing Payment History

**Dashboard Features:**
- Current debt balance
- Payment history table
- Upcoming payment schedule
- Receipt downloads
- Progress toward debt clearance

**Access:**
1. Log in to account
2. Navigate to **Dashboard** or **My Payments**
3. View transaction list with dates, amounts, and statuses

### 6. Communications & Reminders

**Automatic Notifications:**
- Email and/or SMS reminders before payment due dates
- Payment confirmation receipts
- Debt settlement confirmation
- Failed payment alerts

**Managing Preferences:**
1. Navigate to **Account Settings**
2. Select **Communication Preferences**
3. Choose notification channels (email/SMS)
4. Set reminder timing

### 7. Requesting Payment Plan Changes

If circumstances change:

1. Navigate to **My Debt**
2. Click **Request Plan Change**
3. Explain circumstances and propose new schedule
4. Submit request
5. Wait for admin review and approval

---

## Client (Creditor) Workflows

Clients are businesses that use the platform to manage debt collection for their customers.

### 1. Client Onboarding

**Initial Registration:**

1. **Apply for Account** - Submit business details via registration form
2. **ABR Validation** - Provide Australian Business Number (ABN) for verification
3. **Pending Approval** - Application enters admin review queue
4. **Admin Review** - Platform admin reviews and approves/rejects application
5. **Activation** - Upon approval, client receives activation email
6. **Setup Account** - Complete profile and configuration

**Required Information:**
- Business name and ABN
- Business address
- Contact person details
- Bank account for remittance
- Branding assets (logo, colors)

### 2. Configuring Organization Settings

**Branding Configuration:**

1. Navigate to **Settings** → **Branding**
2. Upload company logo
3. Set primary brand color
4. Configure custom domain (optional)
5. Preview white-labeled interface
6. Save changes

**Fee Configuration:**

1. Navigate to **Settings** → **Fees**
2. Set discount percentages:
   - Full payment discount
   - Weekly plan discount
   - Custom plan admin fee
3. Configure payment processing fees
4. Set late payment penalties
5. Save configuration

**Remittance Schedule:**

1. Navigate to **Settings** → **Remittance**
2. Choose payment frequency (weekly/monthly)
3. Set minimum payout threshold
4. Configure bank account details
5. Enable/disable automatic payouts

### 3. Adding Debtors & Debts

**Manual Entry:**

1. Navigate to **Debtors** → **Add New**
2. Enter debtor information:
   - Name
   - Email
   - Phone
   - Address
3. Click **Create Debtor**
4. Navigate to **Debts** → **Add New**
5. Select debtor
6. Enter debt details:
   - Amount
   - Description
   - Invoice reference
   - Due date
7. Upload invoice (optional)
8. Click **Create Debt**
9. System sends notification to debtor with reference ID

**Bulk Import:**

1. Navigate to **Debts** → **Import**
2. Download CSV template
3. Fill template with debtor and debt data
4. Upload completed CSV
5. Review import preview
6. Confirm import
7. System processes and sends notifications

### 4. Invoice Upload & Management

**Uploading Invoices:**

1. Navigate to **Invoices** → **Upload**
2. Select file (PDF, JPEG, PNG)
3. Choose associated debt or debtor
4. Add notes/description
5. Click **Upload**
6. Invoice attached to debt record

**Invoice OCR Processing (Future):**
- Automatic data extraction from uploaded invoices
- Azure Form Recognizer integration
- Manual review and correction workflow

### 5. Tracking Debt Status

**Dashboard Overview:**
- Total debts outstanding
- Total amount owed
- Collection rate
- Active payment plans
- Recent payments

**Debt Statuses:**
- **Pending** - Debtor not yet registered
- **Accepted** - Debtor accepted debt
- **In Payment** - Active payment plan
- **Paid** - Fully settled
- **Overdue** - Missed payment(s)
- **Disputed** - Debtor disputes debt

**Filtering & Search:**
1. Navigate to **Debts** section
2. Use filters:
   - Status
   - Date range
   - Amount range
   - Debtor name
3. Export results to CSV/Excel

### 6. Reviewing Messages & Communications

**Message Center:**

1. Navigate to **Communications**
2. View sent messages by:
   - Date
   - Debtor
   - Message type (reminder, receipt, etc.)
3. Review message templates
4. See delivery status

**Template Management:**

1. Navigate to **Settings** → **Templates**
2. Customize email/SMS templates:
   - Payment reminders
   - Receipt confirmations
   - Welcome messages
3. Use placeholder variables
4. Preview templates
5. Save changes

### 7. Viewing Reports & Analytics

**Available Reports:**

1. **Payment Summary** - Collections over time
2. **Remittance Report** - Scheduled payouts
3. **Debtor Report** - Individual debtor status
4. **Aging Report** - Outstanding debt by age
5. **Performance Dashboard** - KPIs and metrics

**Accessing Reports:**

1. Navigate to **Reports** section
2. Select report type
3. Choose date range
4. Apply filters
5. View or download (CSV/Excel/PDF)

### 8. Receiving Remittance Payments

**Payout Process:**

1. System calculates collected payments per schedule
2. Admin approves remittance batch
3. Funds transferred to client bank account
4. Remittance statement generated
5. Client receives notification

**Statement Details:**
- Period covered
- Debts collected
- Platform fees deducted
- Net payout amount
- Individual transaction breakdown

---

## Admin Workflows

Admins manage the entire platform, approve clients, monitor system health, and configure global settings.

### 1. First Admin Setup

**One-Time Process:**

1. Navigate to `/Backoffice`
2. See one-time admin signup link (visible only when no admins exist)
3. Click link to create first admin account
4. Complete Azure AD B2C registration with admin scope
5. Admin account activated
6. Access admin portal at `/Admin`

**Subsequent Admin Creation:**
- Only existing admins can assign admin roles
- No public admin registration available

### 2. Client Application Review

**Approval Workflow:**

1. Navigate to **Admin** → **Applications**
2. View pending client applications
3. Click on application to review:
   - Business details
   - ABN validation status
   - Submitted documents
4. Perform verification:
   - Verify ABN on ABR website
   - Check business legitimacy
   - Review terms acceptance
5. Decision:
   - **Approve** - Activate client account
   - **Reject** - Provide reason for rejection
6. Client receives notification

### 3. Managing Organizations

**Organization Management:**

1. Navigate to **Admin** → **Organizations**
2. View all client organizations
3. Actions available:
   - Edit organization details
   - View associated debtors and debts
   - Suspend/reactivate account
   - Configure special terms
   - View activity logs

**White-Label Configuration:**

1. Select organization
2. Navigate to **Branding** tab
3. Review/edit:
   - Subdomain mapping
   - Custom domain (if configured)
   - Brand theme
4. Test white-label URL

### 4. Fee & Configuration Management

**Global Fee Settings:**

1. Navigate to **Admin** → **Configuration** → **Fees**
2. Set platform-wide defaults:
   - Default discount rates
   - Platform service fees
   - Payment processing fees
3. Override per client if needed
4. Save changes

**System Configuration:**

1. Navigate to **Admin** → **Configuration** → **Integrations**
2. Manage API credentials:
   - Stripe keys
   - Twilio credentials
   - SMTP settings
   - ABR API key
3. Sensitive values encrypted in database
4. Test integrations

### 5. Template Management

**Message Templates:**

1. Navigate to **Admin** → **Configuration** → **Templates**
2. Edit system-wide templates:
   - Debtor welcome email
   - Payment reminder
   - Receipt confirmation
   - Debt settlement notice
3. Use merge fields: `{DebtorName}`, `{Amount}`, `{DueDate}`
4. Preview templates
5. Publish changes

### 6. Monitoring Background Jobs

**Hangfire Dashboard:**

1. Navigate to `/hangfire`
2. View job status:
   - Succeeded
   - Failed
   - Processing
   - Scheduled
3. Review job details:
   - Execution time
   - Error messages
   - Retry history
4. Manually trigger jobs if needed
5. Configure recurring job schedules

**Background Job Types:**
- Nightly payment reminders
- Payment reconciliation
- Remittance generation
- Failed payment retries
- Invoice processing

### 7. Payout Approvals

**Remittance Approval Process:**

1. Navigate to **Admin** → **Payments** → **Remittances**
2. View pending remittance batches
3. Review details:
   - Client organization
   - Period covered
   - Total collected
   - Fees deducted
   - Net payout
4. Verify calculations
5. Approve or hold for investigation
6. System processes approved payouts

### 8. System Monitoring & Audit

**Health Monitoring:**

1. Navigate to **Admin** → **System Health**
2. Check health endpoints:
   - `/health/live` - Application running
   - `/health/ready` - Dependencies available
3. Review metrics:
   - Response times
   - Error rates
   - Active users
   - Background job status

**Audit Trail:**

1. Navigate to **Admin** → **Audit**
2. Search audit logs by:
   - User
   - Action type
   - Date range
   - Entity affected
3. Review sensitive operations:
   - Admin actions
   - Payment approvals
   - Configuration changes
4. Export audit reports

### 9. User & Account Management

**Managing Accounts:**

1. Navigate to **Admin** → **Accounts**
2. View all users (debtors, clients, admins)
3. Actions:
   - Reset passwords
   - Disable/enable accounts
   - Assign/revoke roles
   - View user activity
   - Delete accounts (with audit)

### 10. Troubleshooting & Support

**Common Admin Tasks:**

**Failed Payments:**
1. Navigate to **Transactions**
2. Filter by status: **Failed**
3. Review failure reason
4. Contact debtor if needed
5. Retry payment manually

**Disputed Debts:**
1. Navigate to **Debts**
2. Filter by status: **Disputed**
3. Review dispute details
4. Contact client and debtor
5. Resolve or escalate

**Integration Issues:**
1. Navigate to **Configuration** → **Integrations**
2. Test API connections
3. Review error logs
4. Update credentials if needed
5. Contact support if unresolved

---

## Common Tasks Across Roles

### Changing Password

1. Log in to account
2. Navigate to **Account Settings**
3. Click **Change Password**
4. Follow Azure AD B2C password reset flow
5. Confirm via email

### Updating Contact Information

1. Navigate to **Profile**
2. Edit email/phone/address
3. Verify changes via OTP if required
4. Save updates

### Enabling MFA (Multi-Factor Authentication)

1. Navigate to **Security Settings**
2. Click **Enable MFA**
3. Choose method (SMS, authenticator app)
4. Complete setup wizard
5. MFA enforced on next login

### Viewing Notifications

1. Click notification bell icon (top-right)
2. View recent notifications
3. Mark as read
4. Navigate to related item

---

**See Also:**
- [Getting Started](Getting-Started.md) - Setup instructions
- [Architecture](Architecture.md) - Technical details
- [FAQ](FAQ.md) - Common questions
