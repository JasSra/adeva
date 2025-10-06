using DebtManager.Domain.Communications;
using DebtManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Data;

public static class MessageTemplateSeeder
{
    public static async Task SeedTemplatesAsync(AppDbContext db)
    {
        if (await db.Set<MessageTemplate>().AnyAsync())
        {
            return; // Already seeded
        }

        var templates = new List<MessageTemplate>
        {
            // Client onboarding - welcome email
            new MessageTemplate(
                code: "client-onboarding-welcome",
                name: "Client Onboarding - Welcome Email",
                subject: "Welcome to {{PlatformName}} - Application Received",
                bodyTemplate: @"
<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #1e3a8a;'>Welcome to {{PlatformName}}!</h2>
        
        <p>Dear {{ContactFirstName}} {{ContactLastName}},</p>
        
        <p>Thank you for registering <strong>{{OrganizationName}}</strong> 
        {{#if TradingName}}(trading as {{TradingName}}){{/if}} 
        with our debt management platform.</p>
        
        <h3 style='color: #1e3a8a; margin-top: 30px;'>Application Details</h3>
        <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
            <tr style='background-color: #f3f4f6;'>
                <td style='padding: 10px; border: 1px solid #e5e7eb; font-weight: bold;'>Legal Name</td>
                <td style='padding: 10px; border: 1px solid #e5e7eb;'>{{LegalName}}</td>
            </tr>
            <tr>
                <td style='padding: 10px; border: 1px solid #e5e7eb; font-weight: bold;'>ABN</td>
                <td style='padding: 10px; border: 1px solid #e5e7eb;'>{{Abn}}</td>
            </tr>
            {{#if Subdomain}}
            <tr style='background-color: #f3f4f6;'>
                <td style='padding: 10px; border: 1px solid #e5e7eb; font-weight: bold;'>Portal URL</td>
                <td style='padding: 10px; border: 1px solid #e5e7eb;'>https://{{Subdomain}}.debtmanager.local</td>
            </tr>
            {{/if}}
            <tr>
                <td style='padding: 10px; border: 1px solid #e5e7eb; font-weight: bold;'>Support Email</td>
                <td style='padding: 10px; border: 1px solid #e5e7eb;'>{{SupportEmail}}</td>
            </tr>
        </table>
        
        <h3 style='color: #1e3a8a; margin-top: 30px;'>What Happens Next?</h3>
        <ol style='line-height: 2;'>
            <li><strong>Account Review:</strong> Our team will review your application within 1-2 business days.</li>
            <li><strong>Onboarding Call:</strong> A member of our team will reach out to schedule an onboarding session.</li>
            <li><strong>Account Activation:</strong> Once approved, you'll receive full access to the platform.</li>
        </ol>
        
        <div style='background-color: #dbeafe; border-left: 4px solid #3b82f6; padding: 15px; margin: 30px 0;'>
            <p style='margin: 0;'><strong>Need Help?</strong></p>
            <p style='margin: 10px 0 0 0;'>
                Email: <a href='mailto:{{SupportEmail}}' style='color: #2563eb;'>{{SupportEmail}}</a><br/>
                Phone: {{SupportPhone}}<br/>
                Hours: Monday to Friday, 9:00 AM - 5:00 PM AEST
            </p>
        </div>
        
        <p style='margin-top: 30px;'>We look forward to working with you!</p>
        
        <p style='color: #6b7280; font-size: 12px; margin-top: 40px; padding-top: 20px; border-top: 1px solid #e5e7eb;'>
            This is an automated message. Please do not reply directly to this email.
        </p>
    </div>
</body>
</html>",
                channel: MessageChannel.Email,
                description: "Sent to new clients after completing organization registration"
            ),

            // Client onboarding - admin notification
            new MessageTemplate(
                code: "client-onboarding-admin-notification",
                name: "Client Onboarding - Admin Notification",
                subject: "New Client Registration: {{OrganizationName}}",
                bodyTemplate: @"
<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #dc2626;'>?? New Client Registration Pending Approval</h2>
        
        <p>A new organization has registered and requires admin approval:</p>
        
        <h3 style='color: #1e3a8a; margin-top: 30px;'>Organization Details</h3>
        <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
            <tr style='background-color: #f3f4f6;'>
                <td style='padding: 10px; border: 1px solid #e5e7eb; font-weight: bold;'>Organization Name</td>
                <td style='padding: 10px; border: 1px solid #e5e7eb;'>{{OrganizationName}}</td>
            </tr>
            <tr>
                <td style='padding: 10px; border: 1px solid #e5e7eb; font-weight: bold;'>Legal Name</td>
                <td style='padding: 10px; border: 1px solid #e5e7eb;'>{{LegalName}}</td>
            </tr>
            {{#if TradingName}}
            <tr style='background-color: #f3f4f6;'>
                <td style='padding: 10px; border: 1px solid #e5e7eb; font-weight: bold;'>Trading Name</td>
                <td style='padding: 10px; border: 1px solid #e5e7eb;'>{{TradingName}}</td>
            </tr>
            {{/if}}
            <tr>
                <td style='padding: 10px; border: 1px solid #e5e7eb; font-weight: bold;'>ABN</td>
                <td style='padding: 10px; border: 1px solid #e5e7eb;'>{{Abn}}</td>
            </tr>
            {{#if Acn}}
            <tr style='background-color: #f3f4f6;'>
                <td style='padding: 10px; border: 1px solid #e5e7eb; font-weight: bold;'>ACN</td>
                <td style='padding: 10px; border: 1px solid #e5e7eb;'>{{Acn}}</td>
            </tr>
            {{/if}}
            {{#if Subdomain}}
            <tr>
                <td style='padding: 10px; border: 1px solid #e5e7eb; font-weight: bold;'>Requested Subdomain</td>
                <td style='padding: 10px; border: 1px solid #e5e7eb;'>{{Subdomain}}</td>
            </tr>
            {{/if}}
        </table>
        
        <h3 style='color: #1e3a8a; margin-top: 30px;'>Contact Person</h3>
        <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
            <tr style='background-color: #f3f4f6;'>
                <td style='padding: 10px; border: 1px solid #e5e7eb; font-weight: bold;'>Name</td>
                <td style='padding: 10px; border: 1px solid #e5e7eb;'>{{ContactFirstName}} {{ContactLastName}}</td>
            </tr>
            <tr>
                <td style='padding: 10px; border: 1px solid #e5e7eb; font-weight: bold;'>Email</td>
                <td style='padding: 10px; border: 1px solid #e5e7eb;'>{{ContactEmail}}</td>
            </tr>
            <tr style='background-color: #f3f4f6;'>
                <td style='padding: 10px; border: 1px solid #e5e7eb; font-weight: bold;'>Registered At</td>
                <td style='padding: 10px; border: 1px solid #e5e7eb;'>{{RegisteredAt}}</td>
            </tr>
        </table>
        
        <div style='background-color: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px; margin: 30px 0;'>
            <p style='margin: 0;'><strong>Action Required:</strong></p>
            <p style='margin: 10px 0 0 0;'>
                Please review this application in the admin portal and approve or reject within 1-2 business days.
            </p>
        </div>
        
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{{AdminPortalUrl}}/Admin/Organizations' 
               style='display: inline-block; background-color: #2563eb; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold;'>
                Review in Admin Portal
            </a>
        </div>
        
        <p style='color: #6b7280; font-size: 12px; margin-top: 40px; padding-top: 20px; border-top: 1px solid #e5e7eb;'>
            This is an automated notification from the debt management platform.
        </p>
    </div>
</body>
</html>",
                channel: MessageChannel.Email,
                description: "Sent to admins when a new client organization registers"
            ),

            // User onboarding - welcome email
            new MessageTemplate(
                code: "user-onboarding-welcome",
                name: "User Onboarding - Welcome Email",
                subject: "Welcome to {{PlatformName}} - Your Account is Ready",
                bodyTemplate: @"
<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #1e3a8a;'>Welcome to {{PlatformName}}!</h2>
        
        <p>Dear {{FirstName}} {{LastName}},</p>
        
        <p>Your debtor profile has been successfully created. You can now access your personal debt management portal.</p>
        
        <h3 style='color: #1e3a8a; margin-top: 30px;'>Your Portal Access</h3>
        <div style='background-color: #dbeafe; padding: 20px; border-radius: 8px; margin: 20px 0;'>
            <p style='margin: 0;'><strong>Portal URL:</strong> <a href='{{PortalUrl}}' style='color: #2563eb;'>{{PortalUrl}}</a></p>
            <p style='margin: 10px 0 0 0;'><strong>Email:</strong> {{Email}}</p>
        </div>
        
        <h3 style='color: #1e3a8a; margin-top: 30px;'>What You Can Do</h3>
        <ul style='line-height: 2;'>
            <li>View your current debts and payment status</li>
            <li>Make secure online payments</li>
            <li>Set up payment plans</li>
            <li>View payment history and receipts</li>
            <li>Update your contact information</li>
        </ul>
        
        <div style='background-color: #dbeafe; border-left: 4px solid #3b82f6; padding: 15px; margin: 30px 0;'>
            <p style='margin: 0;'><strong>Need Help?</strong></p>
            <p style='margin: 10px 0 0 0;'>
                Email: <a href='mailto:{{SupportEmail}}' style='color: #2563eb;'>{{SupportEmail}}</a><br/>
                Phone: {{SupportPhone}}
            </p>
        </div>
        
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{{PortalUrl}}' 
               style='display: inline-block; background-color: #2563eb; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold;'>
                Access Your Portal
            </a>
        </div>
        
        <p style='color: #6b7280; font-size: 12px; margin-top: 40px; padding-top: 20px; border-top: 1px solid #e5e7eb;'>
            This is an automated message. Please do not reply directly to this email.
        </p>
    </div>
</body>
</html>",
                channel: MessageChannel.Email,
                description: "Sent to new users after completing debtor profile onboarding"
            )
        };

        await db.Set<MessageTemplate>().AddRangeAsync(templates);
        await db.SaveChangesAsync();
    }
}
