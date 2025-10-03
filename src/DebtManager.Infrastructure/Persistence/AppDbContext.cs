using DebtManager.Domain.Debtors;
using DebtManager.Domain.Debts;
using DebtManager.Domain.Organizations;
using DebtManager.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Debtor> Debtors => Set<Debtor>();
    public DbSet<Debt> Debts => Set<Debt>();
    public DbSet<PaymentPlan> PaymentPlans => Set<PaymentPlan>();
    public DbSet<PaymentInstallment> PaymentInstallments => Set<PaymentInstallment>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Organization>(builder =>
        {
            builder.HasIndex(x => x.Subdomain);
            builder.HasIndex(x => x.Abn).IsUnique();
            builder.Property(x => x.PrimaryColorHex).HasMaxLength(16);
            builder.Property(x => x.SecondaryColorHex).HasMaxLength(16);
            builder.Navigation(x => x.Debtors).HasField("_debtors").UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(x => x.Debts).HasField("_debts").UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasMany(x => x.Debtors).WithOne(x => x.Organization).HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            builder.HasMany(x => x.Debts).WithOne(x => x.Organization).HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Debtor>(builder =>
        {
            builder.HasIndex(x => x.ReferenceId).IsUnique();
            builder.HasIndex(x => new { x.OrganizationId, x.Email });
            builder.Navigation(x => x.Debts).HasField("_debts").UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(x => x.Transactions).HasField("_transactions").UsePropertyAccessMode(PropertyAccessMode.Field);
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
    }
}
