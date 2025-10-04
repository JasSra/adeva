using DebtManager.Domain.Articles;
using DebtManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Data;

public static class ArticleSeeder
{
    public static async Task SeedArticlesAsync(AppDbContext context)
    {
        if (await context.Articles.AnyAsync())
        {
            return;
        }

        var articles = GetSeedArticles();

        foreach (var article in articles)
        {
            article.Publish();
        }

        await context.Articles.AddRangeAsync(articles);
        await context.SaveChangesAsync();
    }

    private static List<Article> GetSeedArticles()
    {
        return new List<Article>
        {
            Article.Create(
                "About Adeva Plus",
                "about-us",
                CreateAboutUsContent(),
                "Discover how Adeva Plus is revolutionizing debt management through technology and innovation.",
                "https://images.unsplash.com/photo-1454165804606-c3d57bc86b40?w=1200",
                "Adeva Plus Team"
            ),

            Article.Create(
                "How It Works",
                "how-it-works",
                CreateHowItWorksContent(),
                "Learn how Adeva Plus streamlines debt management for businesses and debtors.",
                "https://images.unsplash.com/photo-1460925895917-afdab827c52f?w=1200",
                "Adeva Plus Team"
            ),

            Article.Create(
                "Frequently Asked Questions",
                "faq",
                CreateFaqContent(),
                "Find answers to common questions about using Adeva Plus for debt management.",
                "https://images.unsplash.com/photo-1516321497487-e288fb19713f?w=1200",
                "Support Team"
            ),

            Article.Create(
                "Privacy Policy",
                "privacy-policy",
                CreatePrivacyPolicyContent(),
                "Learn how Adeva Plus protects your privacy and handles your personal information.",
                "https://images.unsplash.com/photo-1450101499163-c8848c66ca85?w=1200",
                "Legal Team"
            ),

            Article.Create(
                "Terms of Service",
                "terms-of-service",
                CreateTermsContent(),
                "Read our Terms of Service to understand your rights and obligations.",
                "https://images.unsplash.com/photo-1589829545856-d10d557cf95f?w=1200",
                "Legal Team"
            ),

            Article.Create(
                "Getting Started Guide",
                "getting-started",
                CreateGettingStartedContent(),
                "Your quick-start guide to using Adeva Plus for debt management.",
                "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?w=1200",
                "Support Team"
            ),

            Article.Create(
                "Payment Plans Explained",
                "payment-plans",
                CreatePaymentPlansContent(),
                "Learn about the flexible payment plan options available through Adeva Plus.",
                "https://images.unsplash.com/photo-1554224155-6726b3ff858f?w=1200",
                "Financial Team"
            ),

            Article.Create(
                "Security & Data Protection",
                "security",
                CreateSecurityContent(),
                "Learn about the comprehensive security measures we use to protect your data.",
                "https://images.unsplash.com/photo-1563986768609-322da13575f3?w=1200",
                "Security Team"
            ),

            Article.Create(
                "For Organizations: Branding Guide",
                "branding-guide",
                CreateBrandingGuideContent(),
                "Customize Adeva Plus to match your brand identity.",
                "https://images.unsplash.com/photo-1561070791-2526d30994b5?w=1200",
                "Product Team"
            ),

            Article.Create(
                "Reporting & Analytics",
                "reporting-analytics",
                CreateReportingContent(),
                "Gain insights into your debt portfolio with powerful reporting tools.",
                "https://images.unsplash.com/photo-1551288049-bebda4e38f71?w=1200",
                "Analytics Team"
            ),

            Article.Create(
                "API Documentation",
                "api-documentation",
                CreateApiDocsContent(),
                "Integrate Adeva Plus with your existing systems using our RESTful API.",
                "https://images.unsplash.com/photo-1555949963-ff9fe0c870eb?w=1200",
                "Engineering Team"
            ),

            Article.Create(
                "Contact Support",
                "contact-support",
                CreateContactContent(),
                "Multiple ways to get help and support from the Adeva Plus team.",
                "https://images.unsplash.com/photo-1423666639041-f56000c27a9a?w=1200",
                "Support Team"
            ),

            Article.Create(
                "Compliance & Regulations",
                "compliance",
                CreateComplianceContent(),
                "Understanding Adeva Plus's commitment to compliance and regulatory adherence.",
                "https://images.unsplash.com/photo-1589829545856-d10d557cf95f?w=1200",
                "Compliance Team"
            ),

            Article.Create(
                "Integration Options",
                "integrations",
                CreateIntegrationsContent(),
                "Connect Adeva Plus with your existing business systems for maximum efficiency.",
                "https://images.unsplash.com/photo-1558494949-ef010cbdcc31?w=1200",
                "Integration Team"
            ),

            Article.Create(
                "Release Notes & Changelog",
                "changelog",
                CreateChangelogContent(),
                "Track the latest updates, features, and improvements to the Adeva Plus platform.",
                "https://images.unsplash.com/photo-1517694712202-14dd9538aa97?w=1200",
                "Product Team"
            )
        };
    }

    private static string CreateAboutUsContent() => @"<h2>Welcome to Adeva Plus Debt Management</h2>
<p>Adeva Plus is a cutting-edge debt management platform designed to simplify the debt collection process for businesses and debtors alike.</p>
<h3>Our Vision</h3>
<p>We envision a world where debt management is streamlined, automated, and humane.</p>
<h3>What We Offer</h3>
<ul>
<li><strong>Automated Payment Plans</strong></li>
<li><strong>Real-time Tracking</strong></li>
<li><strong>Multi-channel Communications</strong></li>
<li><strong>Secure Payment Processing</strong></li>
</ul>";

    private static string CreateHowItWorksContent() => @"<h2>How Adeva Plus Works</h2>
<p>Our platform makes debt management simple and efficient.</p>
<h3>For Businesses</h3>
<ol>
<li>Create your organization account</li>
<li>Import debts individually or in bulk</li>
<li>Track payments and manage communications</li>
</ol>
<h3>For Debtors</h3>
<ol>
<li>Receive notification about a debt</li>
<li>Log in to view details</li>
<li>Choose a payment plan</li>
<li>Make secure payments</li>
</ol>";

    private static string CreateFaqContent() => @"<h2>Frequently Asked Questions</h2>
<h3>What is Adeva Plus?</h3>
<p>Adeva Plus is a comprehensive debt management platform.</p>
<h3>Is my data secure?</h3>
<p>Yes, we use industry-standard encryption and security measures.</p>
<h3>How do I access my account?</h3>
<p>You'll receive login credentials via email after registration.</p>";

    private static string CreatePrivacyPolicyContent() => @"<h2>Privacy Policy</h2>
<p><em>Last Updated: January 2024</em></p>
<h3>1. Introduction</h3>
<p>Adeva Plus is committed to protecting your privacy.</p>
<h3>2. Information We Collect</h3>
<p>We collect personal information necessary to provide our services.</p>
<h3>3. How We Use Your Information</h3>
<p>We use collected information to provide and improve our services.</p>";

    private static string CreateTermsContent() => @"<h2>Terms of Service</h2>
<p><em>Last Updated: January 2024</em></p>
<h3>1. Acceptance of Terms</h3>
<p>By using Adeva Plus, you agree to these terms.</p>
<h3>2. Service Description</h3>
<p>Adeva Plus provides cloud-based debt management services.</p>";

    private static string CreateGettingStartedContent() => @"<h2>Getting Started with Adeva Plus</h2>
<h3>For Businesses</h3>
<ol>
<li>Set up your account</li>
<li>Configure branding</li>
<li>Import debts</li>
<li>Monitor progress</li>
</ol>
<h3>For Debtors</h3>
<ol>
<li>Create account</li>
<li>Review debt details</li>
<li>Choose payment plan</li>
<li>Make payments</li>
</ol>";

    private static string CreatePaymentPlansContent() => @"<h2>Understanding Payment Plans</h2>
<h3>Types of Plans</h3>
<ul>
<li>Short-term (3-6 months)</li>
<li>Medium-term (6-12 months)</li>
<li>Long-term (12-24 months)</li>
</ul>
<h3>Features</h3>
<p>Flexible payment options with automatic processing.</p>";

    private static string CreateSecurityContent() => @"<h2>Security & Data Protection</h2>
<h3>Infrastructure Security</h3>
<p>Hosted on Microsoft Azure with 99.9% uptime.</p>
<h3>Data Encryption</h3>
<p>AES-256 encryption for data at rest, TLS for data in transit.</p>
<h3>Compliance</h3>
<p>GDPR compliant, PCI DSS Level 1.</p>";

    private static string CreateBrandingGuideContent() => @"<h2>Branding Your Organization</h2>
<h3>What You Can Customize</h3>
<ul>
<li>Logo and colors</li>
<li>Custom subdomain</li>
<li>Contact information</li>
<li>Email templates</li>
</ul>";

    private static string CreateReportingContent() => @"<h2>Reporting & Analytics</h2>
<h3>Available Reports</h3>
<ul>
<li>Debt Portfolio Overview</li>
<li>Payment Performance</li>
<li>Cash Flow Projections</li>
<li>Aging Reports</li>
</ul>";

    private static string CreateApiDocsContent() => @"<h2>API Documentation</h2>
<h3>Getting Started</h3>
<p>Use API keys for authentication.</p>
<h3>Base URL</h3>
<pre>https://api.adevaplus.com/v1/</pre>
<h3>Core Endpoints</h3>
<ul>
<li>GET /debts</li>
<li>POST /debts</li>
<li>GET /payments</li>
</ul>";

    private static string CreateContactContent() => @"<h2>Contact Adeva Plus Support</h2>
<h3>Email</h3>
<p>support@adevaplus.com</p>
<h3>Phone</h3>
<p>1300 ADEVA PLUS</p>
<h3>Office Hours</h3>
<p>Monday-Friday, 9:00 AM - 5:00 PM AEST</p>";

    private static string CreateComplianceContent() => @"<h2>Compliance & Regulations</h2>
<h3>Australian Regulations</h3>
<p>Compliant with Privacy Act 1988 and Australian Consumer Law.</p>
<h3>International Compliance</h3>
<p>GDPR compliant for EU citizens.</p>";

    private static string CreateIntegrationsContent() => @"<h2>Integration Options</h2>
<h3>Available Integrations</h3>
<ul>
<li>Stripe (Payment Processing)</li>
<li>Xero (Accounting)</li>
<li>Salesforce (CRM)</li>
<li>Twilio (SMS)</li>
</ul>";

    private static string CreateChangelogContent() => @"<h2>Release Notes & Changelog</h2>
<h3>Version 2.1.0 (January 2024)</h3>
<h4>New Features</h4>
<ul>
<li>Article Management System</li>
<li>Enhanced Analytics</li>
<li>Payment Plan Templates</li>
</ul>";
}
