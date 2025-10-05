using DebtManager.Domain.Debtors;
using DebtManager.Domain.Debts;
using DebtManager.Domain.Organizations;
using DebtManager.Domain.Payments;
using DebtManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Data;

public static class DummyDataSeeder
{
    public static async Task SeedDummyDataAsync(AppDbContext context)
    {
        if (await context.Organizations.AnyAsync(o => o.TagsCsv.Contains("dummy")))
        {
            return; // Already seeded
        }

        var dummyOrganizations = CreateDummyOrganizations();
        await context.Organizations.AddRangeAsync(dummyOrganizations);
        await context.SaveChangesAsync();

        foreach (var org in dummyOrganizations)
        {
            var debtors = CreateDummyDebtors(org);
            await context.Debtors.AddRangeAsync(debtors);
            await context.SaveChangesAsync();

            foreach (var debtor in debtors)
            {
                var debts = CreateDummyDebts(org, debtor);
                await context.Debts.AddRangeAsync(debts);
                await context.SaveChangesAsync();

                foreach (var debt in debts)
                {
                    var paymentPlans = CreateDummyPaymentPlans(debt);
                    await context.PaymentPlans.AddRangeAsync(paymentPlans);

                    var transactions = CreateDummyTransactions(org, debtor, debt, paymentPlans);
                    await context.Transactions.AddRangeAsync(transactions);
                }
            }
        }

        await context.SaveChangesAsync();
    }

    private static List<Organization> CreateDummyOrganizations()
    {
        var organizations = new List<Organization>();

        // Scenario 1: New organization ready for approval
        var pendingOrg = Organization.CreatePending(
            name: "ABC Collections Ltd",
            legalName: "ABC Collections Pty Ltd",
            abn: "12345678901",
            defaultCurrency: "AUD",
            primaryColorHex: "#1e3a8a",
            secondaryColorHex: "#3b82f6",
            supportEmail: "support@abccollections.example",
            supportPhone: "1300123456",
            timezone: "Australia/Sydney",
            subdomain: "abc-collections",
            tradingName: "ABC Collections"
        );
        pendingOrg.SetTags(new[] { "dummy", "scenario:pending-approval" });
        organizations.Add(pendingOrg);

        // Scenario 2: Rejected organization
        var rejectedOrg = Organization.CreatePending(
            name: "XYZ Debt Recovery",
            legalName: "XYZ Debt Recovery Pty Ltd",
            abn: "98765432109",
            defaultCurrency: "AUD",
            primaryColorHex: "#dc2626",
            secondaryColorHex: "#ef4444",
            supportEmail: "support@xyzdebt.example",
            supportPhone: "1300987654",
            timezone: "Australia/Melbourne",
            subdomain: "xyz-recovery",
            tradingName: "XYZ Recovery"
        );
        rejectedOrg.SetTags(new[] { "dummy", "scenario:rejected" });
        organizations.Add(rejectedOrg);

        // Scenario 3: Approved and onboarded organization
        var activeOrg = new Organization(
            name: "Premier Collections",
            legalName: "Premier Collections Pty Ltd",
            abn: "11223344556",
            defaultCurrency: "AUD",
            primaryColorHex: "#059669",
            secondaryColorHex: "#10b981",
            supportEmail: "support@premier.example",
            supportPhone: "1300555666",
            timezone: "Australia/Brisbane",
            subdomain: "premier",
            tradingName: "Premier"
        );
        activeOrg.Approve(DateTime.UtcNow.AddDays(-30));
        activeOrg.MarkOnboarded(DateTime.UtcNow.AddDays(-29));
        activeOrg.SetTags(new[] { "dummy", "scenario:active-established" });
        organizations.Add(activeOrg);

        // Scenario 4: Recently approved organization
        var recentOrg = new Organization(
            name: "Swift Debt Solutions",
            legalName: "Swift Debt Solutions Pty Ltd",
            abn: "55667788990",
            defaultCurrency: "AUD",
            primaryColorHex: "#7c3aed",
            secondaryColorHex: "#8b5cf6",
            supportEmail: "support@swift.example",
            supportPhone: "1300777888",
            timezone: "Australia/Perth",
            subdomain: "swift",
            tradingName: "Swift"
        );
        recentOrg.Approve(DateTime.UtcNow.AddDays(-3));
        recentOrg.SetTags(new[] { "dummy", "scenario:recently-approved" });
        organizations.Add(recentOrg);

        return organizations;
    }

    private static List<Debtor> CreateDummyDebtors(Organization org)
    {
        var debtors = new List<Debtor>();

        // Ensure debtor ReferenceId is unique across organizations
        var refPrefix = (org.Subdomain ?? org.Id.ToString("N").Substring(0, 8)).ToUpperInvariant();

        // Scenario: New debtor
        var newDebtor = new Debtor(org.Id, $"{refPrefix}-NEW-001", "john.smith@example.com", "+61400111222", "John", "Smith");
        newDebtor.UpdatePersonalDetails("John", "Smith", "Johnny", new DateTime(1985, 5, 15), null);
        newDebtor.UpdateAddress("123 Main St", "Unit 4", "Sydney", "NSW", "2000", "AU");
        newDebtor.SetStatus(DebtorStatus.New);
        newDebtor.SetTags(new[] { "dummy", "scenario:new-customer" });
        debtors.Add(newDebtor);

        // Scenario: Invited debtor
        var invitedDebtor = new Debtor(org.Id, $"{refPrefix}-INV-001", "sarah.jones@example.com", "+61400333444", "Sarah", "Jones");
        invitedDebtor.UpdatePersonalDetails("Sarah", "Jones", "Sarah", new DateTime(1990, 8, 22), null);
        invitedDebtor.UpdateAddress("456 High St", "", "Melbourne", "VIC", "3000", "AU");
        invitedDebtor.SetStatus(DebtorStatus.Invited);
        invitedDebtor.SetTags(new[] { "dummy", "scenario:invited-customer" });
        debtors.Add(invitedDebtor);

        // Scenario: Active debtor making payments
        var activeDebtor = new Debtor(org.Id, $"{refPrefix}-ACT-001", "mike.brown@example.com", "+61400555666", "Michael", "Brown");
        activeDebtor.UpdatePersonalDetails("Michael", "Brown", "Mike", new DateTime(1978, 3, 10), null);
        activeDebtor.UpdateAddress("789 Park Ave", "Apt 12", "Brisbane", "QLD", "4000", "AU");
        activeDebtor.UpdateEmployment("Tech Corp Pty Ltd", "$80,000-$100,000");
        activeDebtor.SetStatus(DebtorStatus.Active);
        activeDebtor.EnablePortalAccess();
        activeDebtor.RecordLogin(DateTime.UtcNow.AddDays(-2));
        activeDebtor.RecordContact(DateTime.UtcNow.AddDays(-5), "Discussed payment plan options");
        activeDebtor.SetTags(new[] { "dummy", "scenario:active-paying" });
        debtors.Add(activeDebtor);

        // Scenario: Delinquent debtor
        var delinquentDebtor = new Debtor(org.Id, $"{refPrefix}-DEL-001", "linda.white@example.com", "+61400777888", "Linda", "White");
        delinquentDebtor.UpdatePersonalDetails("Linda", "White", "Lin", new DateTime(1982, 11, 5), null);
        delinquentDebtor.UpdateAddress("321 Ocean Rd", "", "Perth", "WA", "6000", "AU");
        delinquentDebtor.SetStatus(DebtorStatus.Delinquent);
        delinquentDebtor.RecordContact(DateTime.UtcNow.AddDays(-10), "Left voicemail - no response");
        delinquentDebtor.RecordContact(DateTime.UtcNow.AddDays(-15), "Sent email reminder");
        delinquentDebtor.SetTags(new[] { "dummy", "scenario:delinquent-non-responsive" });
        debtors.Add(delinquentDebtor);

        // Scenario: Settled debtor
        var settledDebtor = new Debtor(org.Id, $"{refPrefix}-SET-001", "david.green@example.com", "+61400999000", "David", "Green");
        settledDebtor.UpdatePersonalDetails("David", "Green", "Dave", new DateTime(1975, 7, 18), null);
        settledDebtor.UpdateAddress("555 Lake St", "", "Adelaide", "SA", "5000", "AU");
        settledDebtor.SetStatus(DebtorStatus.Settled);
        settledDebtor.RecordContact(DateTime.UtcNow.AddDays(-30), "Final payment received - account settled");
        settledDebtor.SetTags(new[] { "dummy", "scenario:settled" });
        debtors.Add(settledDebtor);

        return debtors;
    }

    private static List<Debt> CreateDummyDebts(Organization org, Debtor debtor)
    {
        var debts = new List<Debt>();

        if (debtor.TagsCsv.Contains("new-customer"))
        {
            // New debt for new customer
            var debt = new Debt(org.Id, debtor.Id, 2500.00m, "AUD", $"EXT-{debtor.ReferenceId}", $"REF-{debtor.ReferenceId}");
            debt.SetCategory("Utility", "UTIL-2024");
            debt.SetDueDate(DateTime.UtcNow.AddDays(14));
            debt.SetStatus(DebtStatus.PendingAssignment);
            debt.SetTags(new[] { "dummy", "scenario:new-debt" });
            debts.Add(debt);
        }
        else if (debtor.TagsCsv.Contains("invited-customer"))
        {
            // Debt pending customer verification
            var debt = new Debt(org.Id, debtor.Id, 1800.00m, "AUD", $"EXT-{debtor.ReferenceId}", $"REF-{debtor.ReferenceId}");
            debt.SetCategory("Telecommunications", "TELCO-2024");
            debt.SetDueDate(DateTime.UtcNow.AddDays(30));
            debt.SetStatus(DebtStatus.Active);
            debt.SetTags(new[] { "dummy", "scenario:pending-verification" });
            debts.Add(debt);
        }
        else if (debtor.TagsCsv.Contains("active-paying"))
        {
            // Active debt with payment plan
            var debt = new Debt(org.Id, debtor.Id, 5000.00m, "AUD", $"EXT-{debtor.ReferenceId}", $"REF-{debtor.ReferenceId}");
            debt.SetCategory("Credit Card", "CC-2024");
            debt.SetInterest(8.5m, InterestCalculationMethod.Simple);
            debt.SetLateFees(25.00m, null);
            debt.SetDueDate(DateTime.UtcNow.AddMonths(-2));
            debt.ApplyPayment(500.00m, DateTime.UtcNow.AddDays(-20));
            debt.ApplyPayment(500.00m, DateTime.UtcNow.AddDays(-10));
            debt.SetStatus(DebtStatus.Active);
            debt.SetTags(new[] { "dummy", "scenario:active-on-plan" });
            debts.Add(debt);
        }
        else if (debtor.TagsCsv.Contains("delinquent-non-responsive"))
        {
            // Overdue debt in arrears
            var debt = new Debt(org.Id, debtor.Id, 3200.00m, "AUD", $"EXT-{debtor.ReferenceId}", $"REF-{debtor.ReferenceId}");
            debt.SetCategory("Personal Loan", "LOAN-2023");
            debt.SetInterest(12.0m, InterestCalculationMethod.Compound);
            debt.SetLateFees(50.00m, 2.5m);
            debt.SetDueDate(DateTime.UtcNow.AddMonths(-4));
            debt.AccrueInterest(160.00m);
            debt.AddFee(75.00m, "Late payment fee - 60 days overdue");
            debt.SetStatus(DebtStatus.InArrears);
            debt.ScheduleNextAction(DateTime.UtcNow.AddDays(7));
            debt.AppendNote("Multiple contact attempts unsuccessful");
            debt.SetTags(new[] { "dummy", "scenario:in-arrears-high-risk" });
            debts.Add(debt);
        }
        else if (debtor.TagsCsv.Contains("settled"))
        {
            // Fully settled debt
            var debt = new Debt(org.Id, debtor.Id, 1500.00m, "AUD", $"EXT-{debtor.ReferenceId}", $"REF-{debtor.ReferenceId}");
            debt.SetCategory("Medical", "MED-2023");
            debt.SetDueDate(DateTime.UtcNow.AddMonths(-6));
            debt.ApplyPayment(1500.00m, DateTime.UtcNow.AddDays(-30));
            debt.SetStatus(DebtStatus.Settled);
            debt.AppendNote("Settled in full on " + DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd"));
            debt.SetTags(new[] { "dummy", "scenario:settled" });
            debts.Add(debt);
        }

        // Add a disputed debt scenario for one of the active debtors
        if (debtor.TagsCsv.Contains("active-paying"))
        {
            var disputedDebt = new Debt(org.Id, debtor.Id, 800.00m, "AUD", $"EXT-DISP-{debtor.ReferenceId}", $"REF-DISP-{debtor.ReferenceId}");
            disputedDebt.SetCategory("Retail", "RET-2024");
            disputedDebt.SetDueDate(DateTime.UtcNow.AddDays(-45));
            disputedDebt.FlagDispute("Customer claims service was never provided");
            disputedDebt.ScheduleNextAction(DateTime.UtcNow.AddDays(14));
            disputedDebt.AppendNote("Awaiting documentation from merchant");
            disputedDebt.SetTags(new[] { "dummy", "scenario:disputed" });
            debts.Add(disputedDebt);
        }

        return debts;
    }

    private static List<PaymentPlan> CreateDummyPaymentPlans(Debt debt)
    {
        var plans = new List<PaymentPlan>();

        if (debt.TagsCsv.Contains("active-on-plan"))
        {
            // Active payment plan
            var plan = new PaymentPlan(
                debt.Id,
                $"PP-{debt.ClientReferenceNumber}",
                PaymentPlanType.Custom,
                PaymentFrequency.Fortnightly,
                DateTime.UtcNow.AddDays(-30),
                500.00m,
                8
            );
            plan.Activate("admin-seeder", DateTime.UtcNow.AddDays(-30));
            plan.SetTags(new[] { "dummy", "scenario:active-plan" });
            plans.Add(plan);

            // Generate installments
            for (int i = 0; i < 8; i++)
            {
                var installment = plan.ScheduleInstallment(
                    i + 1,
                    DateTime.UtcNow.AddDays(-30 + (i * 14)),
                    500.00m
                );

                // Mark first 2 as paid
                if (i < 2)
                {
                    installment.RegisterPayment(500.00m, DateTime.UtcNow.AddDays(-30 + (i * 14)));
                }
            }
        }
        else if (debt.TagsCsv.Contains("in-arrears-high-risk"))
        {
            // Defaulted payment plan
            var plan = new PaymentPlan(
                debt.Id,
                $"PP-{debt.ClientReferenceNumber}",
                PaymentPlanType.Custom,
                PaymentFrequency.Monthly,
                DateTime.UtcNow.AddMonths(-3),
                400.00m,
                10
            );
            plan.Activate("admin-seeder", DateTime.UtcNow.AddMonths(-3));
            plan.MarkDefaulted(DateTime.UtcNow.AddDays(-45), "Missed 2 consecutive payments");
            plan.SetTags(new[] { "dummy", "scenario:defaulted-plan" });
            plans.Add(plan);
        }
        else if (debt.TagsCsv.Contains("settled"))
        {
            // Completed payment plan
            var plan = new PaymentPlan(
                debt.Id,
                $"PP-{debt.ClientReferenceNumber}",
                PaymentPlanType.FullSettlement,
                PaymentFrequency.OneOff,
                DateTime.UtcNow.AddDays(-35),
                1500.00m,
                1
            );
            plan.Activate("admin-seeder", DateTime.UtcNow.AddDays(-35));
            plan.Complete(DateTime.UtcNow.AddDays(-30));
            plan.SetTags(new[] { "dummy", "scenario:completed-plan" });
            plans.Add(plan);
        }
        else if (debt.TagsCsv.Contains("new-debt"))
        {
            // Draft payment plan awaiting approval
            var plan = new PaymentPlan(
                debt.Id,
                $"PP-DRAFT-{debt.ClientReferenceNumber}",
                PaymentPlanType.SystemGenerated,
                PaymentFrequency.Weekly,
                DateTime.UtcNow.AddDays(7),
                250.00m,
                10
            );
            plan.RequireManualReview();
            plan.SetTags(new[] { "dummy", "scenario:draft-plan" });
            plans.Add(plan);
        }

        return plans;
    }

    private static List<Transaction> CreateDummyTransactions(Organization org, Debtor debtor, Debt debt, List<PaymentPlan> paymentPlans)
    {
        var transactions = new List<Transaction>();

        // Create transactions for debts that have received payments
        if (debt.TagsCsv.Contains("active-on-plan"))
        {
            var plan = paymentPlans.FirstOrDefault();
            if (plan != null)
            {
                // First payment
                var tx1 = new Transaction(
                    debtId: debt.Id,
                    debtorId: debtor.Id,
                    paymentPlanId: plan.Id,
                    paymentInstallmentId: null,
                    amount: 500.00m,
                    currency: "AUD",
                    direction: TransactionDirection.Inbound,
                    method: PaymentMethod.BankTransfer,
                    provider: "Bank",
                    providerRef: $"TXN-{Guid.NewGuid().ToString()[..8]}"
                );
                tx1.MarkSettled(DateTime.UtcNow.AddDays(-20));
                transactions.Add(tx1);

                // Second payment
                var tx2 = new Transaction(
                    debtId: debt.Id,
                    debtorId: debtor.Id,
                    paymentPlanId: plan.Id,
                    paymentInstallmentId: null,
                    amount: 500.00m,
                    currency: "AUD",
                    direction: TransactionDirection.Inbound,
                    method: PaymentMethod.Card,
                    provider: "Stripe",
                    providerRef: $"TXN-{Guid.NewGuid().ToString()[..8]}"
                );
                tx2.MarkSettled(DateTime.UtcNow.AddDays(-10));
                transactions.Add(tx2);
            }
        }
        else if (debt.TagsCsv.Contains("settled"))
        {
            // Full settlement payment
            var tx = new Transaction(
                debtId: debt.Id,
                debtorId: debtor.Id,
                paymentPlanId: null,
                paymentInstallmentId: null,
                amount: 1500.00m,
                currency: "AUD",
                direction: TransactionDirection.Inbound,
                method: PaymentMethod.BankTransfer,
                provider: "Bank",
                providerRef: $"TXN-{Guid.NewGuid().ToString()[..8]}"
            );
            tx.MarkSettled(DateTime.UtcNow.AddDays(-30));
            transactions.Add(tx);
        }

        return transactions;
    }
}
