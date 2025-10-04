# User Area Documentation

## Overview

The User area is a self-service portal for debtors to manage their debts, make payments, and communicate with the organization. It supports both individual and organizational debtors with role-based access control.

## Features

### 1. Dashboard (`/User`)
- **Summary Cards**: Total owed, next payment due, active debts, account status
- **Quick Actions**: Make payment, view payment plan, view debts, contact support
- **Recent Activity**: Latest transactions and communications
- **Account Information**: User details and status

### 2. Debts Management (`/User/Debts`)

#### List View (`/User/Debts`)
- View all debts associated with the user
- Filter by status (Active, In Arrears, Settled, Disputed)
- Search by reference or description
- Pagination support (20 items per page by default)

#### Details View (`/User/Debts/Details/{id}`)
- Complete debt information (original amount, outstanding balance, interest, fees)
- Payment history with receipts
- Activity log and audit trail
- Quick actions sidebar
- Current payment plan summary
- Contact information for support

#### Dispute (`/User/Debts/Dispute/{id}`)
- Submit formal dispute for a debt
- Multiple dispute reasons (not mine, already paid, incorrect amount, fraud, identity theft)
- Detailed explanation with minimum character requirements
- File upload support for supporting documentation
- Declaration and consent checkboxes

#### Request Extension (`/User/Debts/RequestExtension/{id}`)
- Request additional time for payment
- Select new due date (minimum 7 days from current date)
- Reason selection (financial hardship, medical, job loss, family emergency)
- Detailed explanation required
- Specify ability to pay in full on new date
- Optional supporting documentation

### 3. Payments (`/User/Payments`)

#### Payment History (`/User/Payments`)
- View all past payments with receipts
- Filter by date, debt reference, or payment method
- Download receipts
- Pagination support

#### Make Payment (`/User/Payments/MakePayment`)
- Select debt to pay (if not specified)
- Enter payment amount with quick options (minimum, full balance)
- Choose payment method (Credit/Debit Card, Bank Transfer)
- Secure payment processing with encryption indicators
- Payment branded with organization's theme if debt-specific

#### Payment Plans (Stubs for future implementation)
- `/User/Payments/ViewPlan/{debtId}` - View current payment plan
- `/User/Payments/ChangePlan/{debtId}` - Modify payment plan
- `/User/Payments/Upcoming` - View upcoming scheduled payments

### 4. Contact Details (`/User/Contact`)
- **Personal Information**: First name, last name, preferred name
- **Contact Methods**: Primary email, primary phone, alternate phone, preferred contact method
- **Address Information**: Full address with city, state, postal code, country
- Update capabilities for all fields
- Validation and required field indicators

### 5. Receipts (`/User/Receipts`)
- List all payment receipts and invoices
- Filter by date range
- View and download receipts
- Linked to specific debts and payment transactions
- Pagination support

### 6. Communications (`/User/Communications`)
- View all communications sent to the user
- Types: Email, SMS, Portal Messages
- Filter by communication type
- Read/unread status indicators
- Chronological display with timestamps
- Full message content and delivery status

### 7. Audit Log (`/User/Audit`)
- Complete activity trail for the user account
- Categorized by activity type:
  - Payments
  - Debt changes
  - Contact updates
  - Communications
  - Security events (logins)
- Chronological grouping (Today, Yesterday, Last Week, etc.)
- Filter by activity type and date range
- Pagination support

## Anonymous Payment Portal (`/Payment/Anonymous`)

### Features
- **No Login Required**: Users can make payments without an account
- **OTP Verification**: Identity verification via email or SMS
- **Branded Experience**: Uses organization's branding based on debt reference
- **Secure Process**: Multi-step verification for security

### Payment Flow
1. **Enter Debt Reference**: User provides their debt reference number
2. **View Debt Information**: System displays debt details
3. **Select Verification Method**: Choose email or SMS for OTP
4. **Verify Identity**: Enter 6-digit verification code
5. **Make Payment**: Process payment securely

### Security Features
- One-time password (OTP) verification
- Masked contact information display
- Code expiration (10 minutes)
- Secure HTTPS encryption
- Terms and conditions acceptance

## Authorization

All User area pages require the `RequireUserScope` policy:
```csharp
[Area("User")]
[Authorize(Policy = "RequireUserScope")]
public class HomeController : Controller
```

Anonymous payment pages are publicly accessible (no authorization required).

## Branding

- **Default Branding**: Used throughout the User area
- **Organization Branding**: Applied to payment pages when debt reference is provided
- **Theme Resolution**: Handled by `BrandingResolverMiddleware`
- **Dynamic Colors**: Primary color applied via CSS variables

## UI/UX Patterns

### Consistent Elements
- Tailwind CSS for styling
- Sidebar navigation with active state indicators
- Card-based layouts for content organization
- Form validation with helpful error messages
- Loading states and progress indicators
- Responsive design for mobile and desktop

### Color Coding
- **Red**: Debts, outstanding balances, warnings
- **Green**: Completed payments, successful actions
- **Blue**: Information, primary actions
- **Yellow**: Warnings, pending actions
- **Purple**: Communications, secondary actions

### Icons
- SVG icons from Heroicons (outline style)
- Consistent sizing (w-5 h-5 for navigation, w-6 h-6 for actions)
- Semantic use (dollar sign for payments, envelope for emails, etc.)

## Navigation Structure

```
User Portal
├── Dashboard (Home)
├── My Account
│   ├── My Debts
│   ├── Payments
│   └── Contact Details
└── Activity
    ├── Receipts
    ├── Communications
    └── Audit Log
```

## Controllers

| Controller | Purpose |
|------------|---------|
| `HomeController` | Dashboard and landing page |
| `DebtsController` | Debt management, disputes, extensions |
| `PaymentsController` | Payment processing and history |
| `ContactController` | Contact information management |
| `ReceiptsController` | Receipt viewing and downloads |
| `CommunicationsController` | Message history |
| `AuditController` | Activity log and audit trail |
| `PaymentController` | Anonymous payment portal (non-area) |

## Views

All views follow the layout pattern:
```cshtml
@{
    Layout = "~/Areas/User/Views/Shared/_UserLayout.cshtml";
}
```

### Layout Features (`_UserLayout.cshtml`)
- Branded header with organization name
- Sidebar navigation with icons
- Active page highlighting
- User information in footer
- Sign out link
- Responsive design

## Integration Points (Future)

### Backend Services
- Debt repository for fetching user's debts
- Payment processing service (Stripe integration)
- Communication service for sending OTPs
- Audit service for logging user actions
- File upload service for document attachments

### External APIs
- SMS gateway (Twilio) for OTP delivery
- Email service (SendGrid) for notifications
- Payment gateway (Stripe) for processing payments
- Document storage (Azure Blob) for uploaded files

## Testing Considerations

- Test all authorization policies
- Verify OTP generation and validation
- Test payment flow end-to-end
- Validate file upload restrictions
- Check responsive design on various devices
- Test pagination and filtering
- Verify branding resolution
- Test form validations

## Security Considerations

- All forms use HTTPS
- CSRF protection on all POST requests
- Input validation and sanitization
- Secure file upload handling
- Rate limiting on OTP requests
- Session timeout handling
- Audit trail for all actions
- Sensitive data encryption

## Future Enhancements

1. **Payment Plan Customization**: Allow users to create custom payment plans
2. **Chat Support**: Live chat integration for real-time support
3. **Payment Reminders**: Configurable notification preferences
4. **Mobile App**: Native mobile applications
5. **Document Management**: Upload and manage documents
6. **Multi-factor Authentication**: Enhanced security options
7. **Payment History Export**: Download payment history as PDF/CSV
8. **Debt Consolidation**: Combine multiple debts into one
9. **Auto-pay Setup**: Automatic payment scheduling
10. **Individual vs Organization UI**: Different UX based on debtor type
