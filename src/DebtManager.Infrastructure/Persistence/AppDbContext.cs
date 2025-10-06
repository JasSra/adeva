using DebtManager.Domain.Debtors;
using DebtManager.Domain.Debts;
using DebtManager.Domain.Organizations;
using DebtManager.Domain.Payments;
using DebtManager.Domain.AdminUsers;
using DebtManager.Domain.Articles;
using DebtManager.Domain.Documents;
using DebtManager.Domain.Configuration;
using DebtManager.Domain.Analytics;
using DebtManager.Domain.Communications;
using DebtManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Debtor> Debtors => Set<Debtor>();
    public DbSet<Debt> Debts => Set<Debt>();
    public DbSet<PaymentPlan> PaymentPlans => Set<PaymentPlan>();
    public DbSet<PaymentInstallment> PaymentInstallments => Set<PaymentInstallment>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<InvoiceData> InvoiceData => Set<InvoiceData>();
    public DbSet<Metric> Metrics => Set<Metric>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<AppConfigEntry> AppConfigEntries => Set<AppConfigEntry>();
    public DbSet<MessageTemplate> MessageTemplates => Set<MessageTemplate>();
    public DbSet<QueuedMessage> QueuedMessages => Set<QueuedMessage>();
    public DbSet<InternalMessage> InternalMessages => Set<InternalMessage>();
    public DbSet<InternalMessageRecipient> InternalMessageRecipients => Set<InternalMessageRecipient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Identity tables configuration (optional: rename tables)
        modelBuilder.Entity<ApplicationUser>(builder =>
        {
            builder.Property(u => u.ExternalAuthId).HasMaxLength(256);
            builder.Property(u => u.TotpSecretKey).HasMaxLength(512);
            builder.Property(u => u.TotpRecoveryCodes).HasMaxLength(2000);
            builder.HasIndex(u => u.ExternalAuthId);

            builder.HasOne(u => u.Profile)
                .WithOne(p => p.User!)
                .HasForeignKey<UserProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserProfile>(builder =>
        {
            builder.HasIndex(p => p.OrganizationId);
            builder.HasIndex(p => p.DebtorId);
            builder.HasOne(p => p.Organization)
                .WithMany()
                .HasForeignKey(p => p.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(p => p.Debtor)
                .WithMany()
                .HasForeignKey(p => p.DebtorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppConfigEntry>(builder =>
        {
            builder.HasIndex(x => x.Key).IsUnique();
            builder.Property(x => x.Key).HasMaxLength(300).IsRequired();
            builder.Property(x => x.Value).HasMaxLength(4000);
        });

        modelBuilder.Entity<Organization>(builder =>
        {
            builder.HasIndex(x => x.Subdomain);
            builder.HasIndex(x => x.Abn).IsUnique();
            builder.Property(x => x.PrimaryColorHex).HasMaxLength(16);
            builder.Property(x => x.SecondaryColorHex).HasMaxLength(16);
            builder.Navigation(x => x.Debtors).HasField("_debtors").UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(x => x.Debts).HasField("_debts").UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(x => x.Documents).HasField("_documents").UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasMany(x => x.Debtors).WithOne(x => x.Organization).HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            builder.HasMany(x => x.Debts).WithOne(x => x.Organization).HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Debtor>(builder =>
        {
            builder.HasIndex(x => x.ReferenceId).IsUnique();
            builder.HasIndex(x => new { x.OrganizationId, x.Email });
            builder.Navigation(x => x.Debts).HasField("_debts").UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(x => x.Transactions).HasField("_transactions").UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(x => x.Documents).HasField("_documents").UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasMany(x => x.Debts).WithOne(x => x.Debtor).HasForeignKey(x => x.DebtorId).OnDelete(DeleteBehavior.Restrict);
            builder.HasMany(x => x.Transactions).WithOne(x => x.Debtor).HasForeignKey(x => x.DebtorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Debt>(builder =>
        {
            builder.Property(x => x.OriginalPrincipal).HasPrecision(18, 2);
            builder.Property(x => x.OutstandingPrincipal).HasPrecision(18, 2);
            builder.Property(x => x.AccruedInterest).HasPrecision(18, 2);
            builder.Property(x => x.AccruedFees).HasPrecision(18, 2);
            builder.Property(x => x.InterestRateAnnualPercentage).HasPrecision(9, 6);
            builder.Property(x => x.LateFeeFlat).HasPrecision(18, 2);
            builder.Property(x => x.LateFeePercentage).HasPrecision(9, 6);
            builder.Property(x => x.SettlementOfferAmount).HasPrecision(18, 2);
            builder.HasIndex(x => new { x.OrganizationId, x.ClientReferenceNumber });
            builder.HasIndex(x => x.ExternalAccountId);
            builder.Navigation(x => x.PaymentPlans).HasField("_paymentPlans").UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(x => x.Transactions).HasField("_transactions").UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasOne(x => x.Debtor).WithMany(x => x.Debts).HasForeignKey(x => x.DebtorId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Organization).WithMany(x => x.Debts).HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            builder.HasMany(x => x.PaymentPlans).WithOne(x => x.Debt).HasForeignKey(x => x.DebtId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.Transactions).WithOne(x => x.Debt).HasForeignKey(x => x.DebtId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentPlan>(builder =>
        {
            builder.Property(x => x.InstallmentAmount).HasPrecision(18, 2);
            builder.Property(x => x.TotalPayable).HasPrecision(18, 2);
            builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            builder.Property(x => x.DownPaymentAmount).HasPrecision(18, 2);
            builder.HasIndex(x => x.Reference).IsUnique();
            builder.Navigation(x => x.Installments).HasField("_installments").UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(x => x.Transactions).HasField("_transactions").UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasMany(x => x.Installments).WithOne(x => x.PaymentPlan).HasForeignKey(x => x.PaymentPlanId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.Transactions).WithOne(x => x.PaymentPlan).HasForeignKey(x => x.PaymentPlanId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentInstallment>(builder =>
        {
            builder.Property(x => x.AmountDue).HasPrecision(18, 2);
            builder.Property(x => x.AmountPaid).HasPrecision(18, 2);
            builder.Property(x => x.LateFeeAmount).HasPrecision(18, 2);
            builder.HasIndex(x => new { x.PaymentPlanId, x.Sequence }).IsUnique();
            builder.Navigation(x => x.Transactions).HasField("_transactions").UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasMany(x => x.Transactions).WithOne(x => x.PaymentInstallment).HasForeignKey(x => x.PaymentInstallmentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Transaction>(builder =>
        {
            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.Property(x => x.FeeAmount).HasPrecision(18, 2);
            builder.HasIndex(x => x.ProviderRef);
            builder.HasIndex(x => new { x.DebtId, x.ProcessedAtUtc });
            builder.HasOne(x => x.Debtor).WithMany(x => x.Transactions).HasForeignKey(x => x.DebtorId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Debt).WithMany(x => x.Transactions).HasForeignKey(x => x.DebtId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.PaymentPlan).WithMany(x => x.Transactions).HasForeignKey(x => x.PaymentPlanId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.PaymentInstallment).WithMany(x => x.Transactions).HasForeignKey(x => x.PaymentInstallmentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AdminUser>(builder =>
        {
            builder.HasIndex(x => x.Email).IsUnique();
            builder.HasIndex(x => x.ExternalAuthId).IsUnique();
            builder.Property(x => x.Email).HasMaxLength(256);
            builder.Property(x => x.Name).HasMaxLength(200);
            builder.Property(x => x.ExternalAuthId).HasMaxLength(256);
        });

        modelBuilder.Entity<Article>(builder =>
        {
            builder.HasIndex(x => x.Slug).IsUnique();
            builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
            builder.Property(x => x.Slug).HasMaxLength(300).IsRequired();
            builder.Property(x => x.Content).IsRequired();
            builder.Property(x => x.Excerpt).HasMaxLength(500);
            builder.Property(x => x.AuthorName).HasMaxLength(200);
            builder.Property(x => x.MetaDescription).HasMaxLength(300);
            builder.Property(x => x.MetaKeywords).HasMaxLength(500);
        });

        modelBuilder.Entity<Document>(builder =>
        {
            builder.Property(d => d.FileName).HasMaxLength(400).IsRequired();
            builder.Property(d => d.ContentType).HasMaxLength(200).IsRequired();
            builder.Property(d => d.StoragePath).HasMaxLength(1000).IsRequired();
            builder.Property(d => d.Sha256).HasMaxLength(128);
            builder.HasIndex(d => new { d.OrganizationId, d.DebtorId, d.Type });
            builder.HasOne(d => d.Organization).WithMany(o => o.Documents).HasForeignKey(d => d.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(d => d.Debtor).WithMany(de => de.Documents).HasForeignKey(d => d.DebtorId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InvoiceData>(builder =>
        {
            builder.HasIndex(i => i.DocumentId).IsUnique();
            builder.HasIndex(i => i.Status);
            builder.Property(i => i.InvoiceNumber).HasMaxLength(100);
            builder.Property(i => i.Currency).HasMaxLength(10);
            builder.Property(i => i.VendorName).HasMaxLength(300);
            builder.Property(i => i.VendorAddress).HasMaxLength(500);
            builder.Property(i => i.VendorAbn).HasMaxLength(50);
            builder.Property(i => i.CustomerName).HasMaxLength(300);
            builder.Property(i => i.CustomerAddress).HasMaxLength(500);
            builder.Property(i => i.TotalAmount).HasPrecision(18, 2);
            builder.Property(i => i.ConfidenceScore).HasPrecision(5, 4);
            builder.Property(i => i.ErrorMessage).HasMaxLength(2000);
            builder.HasOne(i => i.Document).WithOne().HasForeignKey<InvoiceData>(i => i.DocumentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Metric>(builder =>
        {
            builder.HasIndex(m => m.Key);
            builder.HasIndex(m => m.RecordedAtUtc);
            builder.HasIndex(m => new { m.OrganizationId, m.RecordedAtUtc });
            builder.Property(m => m.Key).HasMaxLength(200).IsRequired();
            builder.Property(m => m.Value).HasPrecision(18, 4);
            builder.Property(m => m.Tags).HasMaxLength(1000);
        });

        // Communications
        modelBuilder.Entity<MessageTemplate>(builder =>
        {
            builder.HasIndex(x => x.Code).IsUnique();
            builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Subject).HasMaxLength(500);
            builder.Property(x => x.BodyTemplate).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<QueuedMessage>(builder =>
        {
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.QueuedAtUtc);
            builder.HasIndex(x => new { x.RelatedEntityType, x.RelatedEntityId });
            builder.Property(x => x.RecipientEmail).HasMaxLength(256);
            builder.Property(x => x.RecipientPhone).HasMaxLength(50);
            builder.Property(x => x.Subject).HasMaxLength(500);
            builder.Property(x => x.Body).IsRequired();
            builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
            builder.Property(x => x.RelatedEntityType).HasMaxLength(100);
            builder.Property(x => x.ProviderMessageId).HasMaxLength(200);
        });

        modelBuilder.Entity<InternalMessage>(builder =>
        {
            builder.HasIndex(x => x.SentAtUtc);
            builder.HasIndex(x => x.Priority);
            builder.HasIndex(x => new { x.RelatedEntityType, x.RelatedEntityId });
            builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
            builder.Property(x => x.Content).IsRequired();
            builder.Property(x => x.Category).HasMaxLength(100);
            builder.Property(x => x.RelatedEntityType).HasMaxLength(100);
            builder.Navigation(x => x.Recipients).HasField("_recipients").UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasMany(x => x.Recipients).WithOne(x => x.InternalMessage).HasForeignKey(x => x.InternalMessageId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InternalMessageRecipient>(builder =>
        {
            builder.HasIndex(x => new { x.UserId, x.Status });
            builder.HasIndex(x => x.InternalMessageId);
        });
    }
}
