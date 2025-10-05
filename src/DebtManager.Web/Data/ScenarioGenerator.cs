using Bogus;
using DebtManager.Domain.Debtors;
using DebtManager.Domain.Debts;
using DebtManager.Domain.Organizations;
using DebtManager.Domain.Payments;
using DebtManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Data;

public static class ScenarioGenerator
{
    public static async Task<ScenarioResult> GenerateAsync(AppDbContext db, ScenarioRequest req, CancellationToken ct = default)
    {
        var seed = req.RandomSeed ?? Random.Shared.Next();
        Randomizer.Seed = new Random(seed);
        var faker = new Faker("en_AU");

        var packId = req.PackId ?? Guid.NewGuid().ToString("N")[..8];
        var packTag = $"pack:{packId}";

        var result = new ScenarioResult
        {
            PackId = packId,
            PackName = req.PackName,
            RandomSeed = seed
        };

        var orgs = new List<Organization>();

        void tagOrg(Organization o, string scenarioTag)
        {
            var tags = new List<string> { "dummy", packTag, scenarioTag };
            o.SetTags(tags);
        }

        // Helper to create an org
        Organization MakeOrg(string scenarioTag)
        {
            var company = faker.Company.CompanyName();
            var trading = company.Split(' ').First();
            var subdomainBase = faker.Internet.DomainWord().Replace("_", "-").Replace(".", "-");
            var subdomain = $"{subdomainBase}-{packId}".ToLowerInvariant();

            var org = new Organization(
                name: company,
                legalName: $"{company} Pty Ltd",
                abn: faker.Random.ReplaceNumbers("###########"),
                defaultCurrency: req.Currency ?? "AUD",
                primaryColorHex: faker.PickRandom(new[] { "#1e3a8a", "#059669", "#7c3aed", "#dc2626" }),
                secondaryColorHex: faker.PickRandom(new[] { "#3b82f6", "#10b981", "#8b5cf6", "#ef4444" }),
                supportEmail: faker.Internet.Email("support", subdomain + ".example"),
                supportPhone: faker.Phone.PhoneNumber("1300#######"),
                timezone: "Australia/Sydney",
                subdomain: subdomain,
                tradingName: trading
            );
            tagOrg(org, scenarioTag);
            return org;
        }

        // Create orgs per scenario
        for (int i = 0; i < req.PendingOrganizations; i++)
        {
            var o = Organization.CreatePending(
                name: faker.Company.CompanyName(),
                legalName: faker.Company.CompanyName() + " Pty Ltd",
                abn: faker.Random.ReplaceNumbers("###########"),
                defaultCurrency: req.Currency ?? "AUD",
                primaryColorHex: "#1e3a8a",
                secondaryColorHex: "#3b82f6",
                supportEmail: faker.Internet.Email("support"),
                supportPhone: faker.Phone.PhoneNumber("1300#######"),
                timezone: "Australia/Sydney",
                subdomain: ($"{faker.Internet.DomainWord()}-{packId}").ToLowerInvariant(),
                tradingName: faker.Company.CompanyName()
            );
            tagOrg(o, "scenario:pending-approval");
            orgs.Add(o);
        }
        for (int i = 0; i < req.RejectedOrganizations; i++)
        {
            var o = Organization.CreatePending(
                name: faker.Company.CompanyName(),
                legalName: faker.Company.CompanyName() + " Pty Ltd",
                abn: faker.Random.ReplaceNumbers("###########"),
                defaultCurrency: req.Currency ?? "AUD",
                primaryColorHex: "#dc2626",
                secondaryColorHex: "#ef4444",
                supportEmail: faker.Internet.Email("support"),
                supportPhone: faker.Phone.PhoneNumber("1300#######"),
                timezone: "Australia/Melbourne",
                subdomain: ($"{faker.Internet.DomainWord()}-{packId}").ToLowerInvariant(),
                tradingName: faker.Company.CompanyName()
            );
            tagOrg(o, "scenario:rejected");
            orgs.Add(o);
        }
        for (int i = 0; i < req.ActiveOrganizations; i++)
        {
            var o = MakeOrg("scenario:active-established");
            o.Approve(DateTime.UtcNow.AddDays(-faker.Random.Int(15, 90)));
            o.MarkOnboarded(DateTime.UtcNow.AddDays(-faker.Random.Int(7, 14)));
            orgs.Add(o);
        }
        for (int i = 0; i < req.RecentOrganizations; i++)
        {
            var o = MakeOrg("scenario:recently-approved");
            o.Approve(DateTime.UtcNow.AddDays(-faker.Random.Int(1, 5)));
            orgs.Add(o);
        }

        await db.Organizations.AddRangeAsync(orgs, ct);
        await db.SaveChangesAsync(ct);
        result.OrganizationIds.AddRange(orgs.Select(o => o.Id));

        // Debtors + debts per org
        foreach (var org in orgs)
        {
            var refPrefix = (org.Subdomain ?? org.Id.ToString("N")[..8]).ToUpperInvariant();

            async Task<List<Debtor>> CreateDebtors(string scenarioTag, int count, Func<int, Debtor> factory)
            {
                var ds = new List<Debtor>(count);
                for (int i = 0; i < count; i++)
                {
                    var d = factory(i);
                    d.SetTags(new[] { "dummy", packTag, scenarioTag });
                    ds.Add(d);
                }
                await db.Debtors.AddRangeAsync(ds, ct);
                await db.SaveChangesAsync(ct);
                result.DebtorIds.AddRange(ds.Select(x => x.Id));
                return ds;
            }

            var newDebtors = await CreateDebtors("scenario:new-customer", req.NewCustomersPerOrg, i =>
            {
                var person = faker.Person;
                var d = new Debtor(org.Id, $"{refPrefix}-NEW-{i + 1:000}", person.Email, faker.Phone.PhoneNumber("+61#########"), person.FirstName, person.LastName);
                d.UpdatePersonalDetails(person.FirstName, person.LastName, person.FirstName, person.DateOfBirth, null);
                d.UpdateAddress(faker.Address.StreetAddress(), faker.Random.Bool() ? faker.Address.SecondaryAddress() : string.Empty, faker.Address.City(), faker.Address.StateAbbr(), faker.Address.ZipCode(), "AU");
                d.SetStatus(DebtorStatus.New);
                return d;
            });

            var invitedDebtors = await CreateDebtors("scenario:invited-customer", req.InvitedCustomersPerOrg, i =>
            {
                var p = faker.Person;
                var d = new Debtor(org.Id, $"{refPrefix}-INV-{i + 1:000}", p.Email, faker.Phone.PhoneNumber("+61#########"), p.FirstName, p.LastName);
                d.UpdatePersonalDetails(p.FirstName, p.LastName, p.FirstName, p.DateOfBirth, null);
                d.UpdateAddress(faker.Address.StreetAddress(), string.Empty, faker.Address.City(), faker.Address.StateAbbr(), faker.Address.ZipCode(), "AU");
                d.SetStatus(DebtorStatus.Invited);
                return d;
            });

            var activeDebtors = await CreateDebtors("scenario:active-paying", req.ActiveCustomersPerOrg, i =>
            {
                var p = faker.Person;
                var d = new Debtor(org.Id, $"{refPrefix}-ACT-{i + 1:000}", p.Email, faker.Phone.PhoneNumber("+61#########"), p.FirstName, p.LastName);
                d.UpdatePersonalDetails(p.FirstName, p.LastName, p.FirstName, p.DateOfBirth, null);
                d.UpdateAddress(faker.Address.StreetAddress(), faker.Address.SecondaryAddress(), faker.Address.City(), faker.Address.StateAbbr(), faker.Address.ZipCode(), "AU");
                d.UpdateEmployment(faker.Company.CompanyName(), "$" + faker.Finance.Amount(60000, 130000, 0));
                d.SetStatus(DebtorStatus.Active);
                d.EnablePortalAccess();
                d.RecordLogin(DateTime.UtcNow.AddDays(-faker.Random.Int(1, 7)));
                return d;
            });

            var delinquentDebtors = await CreateDebtors("scenario:delinquent-non-responsive", req.DelinquentCustomersPerOrg, i =>
            {
                var p = faker.Person;
                var d = new Debtor(org.Id, $"{refPrefix}-DEL-{i + 1:000}", p.Email, faker.Phone.PhoneNumber("+61#########"), p.FirstName, p.LastName);
                d.UpdatePersonalDetails(p.FirstName, p.LastName, p.FirstName, p.DateOfBirth, null);
                d.UpdateAddress(faker.Address.StreetAddress(), string.Empty, faker.Address.City(), faker.Address.StateAbbr(), faker.Address.ZipCode(), "AU");
                d.SetStatus(DebtorStatus.Delinquent);
                d.RecordContact(DateTime.UtcNow.AddDays(-faker.Random.Int(10, 30)), "Left voicemail - no response");
                return d;
            });

            var settledDebtors = await CreateDebtors("scenario:settled", req.SettledCustomersPerOrg, i =>
            {
                var p = faker.Person;
                var d = new Debtor(org.Id, $"{refPrefix}-SET-{i + 1:000}", p.Email, faker.Phone.PhoneNumber("+61#########"), p.FirstName, p.LastName);
                d.UpdatePersonalDetails(p.FirstName, p.LastName, p.FirstName, p.DateOfBirth, null);
                d.UpdateAddress(faker.Address.StreetAddress(), string.Empty, faker.Address.City(), faker.Address.StateAbbr(), faker.Address.ZipCode(), "AU");
                d.SetStatus(DebtorStatus.Settled);
                return d;
            });

            // Helper to make debt
            Debt NewDebt(Debtor d, decimal amount, string category, string code)
            {
                var debt = new Debt(org.Id, d.Id, amount, req.Currency ?? "AUD", $"EXT-{d.ReferenceId}", $"REF-{d.ReferenceId}");
                debt.SetCategory(category, code);
                return debt;
            }

            // Debts for each debtor group
            async Task CreateDebtsFor(IEnumerable<Debtor> group, Action<Debt> configure, string scenarioTag)
            {
                foreach (var d in group)
                {
                    var debt = NewDebt(d, faker.Finance.Amount(800, 6000, 2), faker.PickRandom(new[] { "Utility", "Telecommunications", "Credit Card", "Personal Loan", "Medical" }), faker.Random.AlphaNumeric(6).ToUpperInvariant());
                    configure(debt);
                    debt.SetTags(new[] { "dummy", packTag, scenarioTag });
                    await db.Debts.AddAsync(debt, ct);
                    await db.SaveChangesAsync(ct);
                    result.DebtIds.Add(debt.Id);

                    if (req.IncludePaymentPlans)
                    {
                        var plans = CreatePlansForDebt(debt);
                        foreach (var p in plans)
                        {
                            p.SetTags(new[] { "dummy", packTag, scenarioTag.Replace("scenario:", "plan:") });
                        }
                        await db.PaymentPlans.AddRangeAsync(plans, ct);
                        await db.SaveChangesAsync(ct);
                        result.PaymentPlanIds.AddRange(plans.Select(p => p.Id));

                        if (req.IncludeTransactions)
                        {
                            var txs = CreateTransactionsForDebt(d, debt, plans);
                            await db.Transactions.AddRangeAsync(txs, ct);
                            await db.SaveChangesAsync(ct);
                            result.TransactionIds.AddRange(txs.Select(t => t.Id));
                        }
                    }
                }
            }

            await CreateDebtsFor(newDebtors, debt =>
            {
                debt.SetDueDate(DateTime.UtcNow.AddDays(faker.Random.Int(7, 30)));
                debt.SetStatus(DebtStatus.PendingAssignment);
            }, "scenario:new-debt");

            await CreateDebtsFor(invitedDebtors, debt =>
            {
                debt.SetDueDate(DateTime.UtcNow.AddDays(30));
                debt.SetStatus(DebtStatus.Active);
            }, "scenario:pending-verification");

            await CreateDebtsFor(activeDebtors, debt =>
            {
                debt.SetInterest(8.5m, InterestCalculationMethod.Simple);
                debt.SetLateFees(25.00m, null);
                debt.SetDueDate(DateTime.UtcNow.AddMonths(-2));
                debt.ApplyPayment(500.00m, DateTime.UtcNow.AddDays(-20));
                debt.ApplyPayment(500.00m, DateTime.UtcNow.AddDays(-10));
                debt.SetStatus(DebtStatus.Active);
            }, "scenario:active-on-plan");

            await CreateDebtsFor(delinquentDebtors, debt =>
            {
                debt.SetInterest(12.0m, InterestCalculationMethod.Compound);
                debt.SetLateFees(50.00m, 2.5m);
                debt.SetDueDate(DateTime.UtcNow.AddMonths(-faker.Random.Int(3, 6)));
                debt.AccrueInterest(160.00m);
                debt.AddFee(75.00m, "Late payment fee - 60 days overdue");
                debt.SetStatus(DebtStatus.InArrears);
                debt.ScheduleNextAction(DateTime.UtcNow.AddDays(7));
                debt.AppendNote("Multiple contact attempts unsuccessful");
            }, "scenario:in-arrears-high-risk");

            await CreateDebtsFor(settledDebtors, debt =>
            {
                debt.SetDueDate(DateTime.UtcNow.AddMonths(-6));
                debt.ApplyPayment(debt.OriginalPrincipal, DateTime.UtcNow.AddDays(-30));
                debt.SetStatus(DebtStatus.Settled);
                debt.AppendNote("Settled in full on " + DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd"));
            }, "scenario:settled");

            // Disputed debt for first active debtor
            if (activeDebtors.Any())
            {
                var d = activeDebtors.First();
                var disputed = NewDebt(d, 800m, "Retail", "RET-2024");
                disputed.SetDueDate(DateTime.UtcNow.AddDays(-45));
                disputed.FlagDispute("Customer claims service was never provided");
                disputed.ScheduleNextAction(DateTime.UtcNow.AddDays(14));
                disputed.AppendNote("Awaiting documentation from merchant");
                disputed.SetTags(new[] { "dummy", packTag, "scenario:disputed" });
                await db.Debts.AddAsync(disputed, ct);
                await db.SaveChangesAsync(ct);
                result.DebtIds.Add(disputed.Id);
            }

            // Additional segmented scenarios
            Debtor PickDebtor() => activeDebtors.Concat(invitedDebtors).Concat(newDebtors).OrderBy(_ => Guid.NewGuid()).First();

            // Upcoming payment
            for (int i = 0; i < req.UpcomingPaymentsPerOrg; i++)
            {
                var d = PickDebtor();
                var debt = NewDebt(d, faker.Finance.Amount(1200, 4000, 2), "Credit Card", "CC-UPC");
                debt.SetStatus(DebtStatus.Active);
                debt.SetDueDate(DateTime.UtcNow.AddMonths(1));
                debt.SetTags(new[] { "dummy", packTag, "scenario:upcoming-payment" });
                await db.Debts.AddAsync(debt, ct);
                await db.SaveChangesAsync(ct);
                result.DebtIds.Add(debt.Id);

                var plan = new PaymentPlan(debt.Id, $"PP-{debt.ClientReferenceNumber}", PaymentPlanType.Custom, PaymentFrequency.Weekly, DateTime.UtcNow.AddDays(-28), 150m, 6);
                plan.Activate("admin-generator", DateTime.UtcNow.AddDays(-28));
                // 4 installments; next due within 3 days
                for (int s = 0; s < 4; s++)
                {
                    var due = DateTime.UtcNow.AddDays(-21 + (s * 7));
                    var inst = plan.ScheduleInstallment(s + 1, due, 150m);
                    if (s < 3)
                    {
                        inst.RegisterPayment(150m, due);
                    }
                }
                // Upcoming due soon
                var upcoming = plan.ScheduleInstallment(5, DateTime.UtcNow.AddDays(faker.Random.Int(1, 3)), 150m);
                await db.PaymentPlans.AddAsync(plan, ct);
                await db.SaveChangesAsync(ct);
                result.PaymentPlanIds.Add(plan.Id);
            }

            // Payment failed
            for (int i = 0; i < req.FailedPaymentsPerOrg; i++)
            {
                var d = PickDebtor();
                var debt = NewDebt(d, faker.Finance.Amount(900, 2500, 2), "Telecommunications", "TEL-FAIL");
                debt.SetStatus(DebtStatus.Active);
                debt.SetTags(new[] { "dummy", packTag, "scenario:payment-failed" });
                await db.Debts.AddAsync(debt, ct);
                await db.SaveChangesAsync(ct);
                result.DebtIds.Add(debt.Id);

                var plan = new PaymentPlan(debt.Id, $"PP-{debt.ClientReferenceNumber}", PaymentPlanType.Custom, PaymentFrequency.Fortnightly, DateTime.UtcNow.AddDays(-30), 200m, 5);
                plan.Activate("admin-generator", DateTime.UtcNow.AddDays(-30));
                var inst1 = plan.ScheduleInstallment(1, DateTime.UtcNow.AddDays(-30), 200m);
                inst1.RegisterPayment(200m, DateTime.UtcNow.AddDays(-30));
                var inst2 = plan.ScheduleInstallment(2, DateTime.UtcNow.AddDays(-14), 200m);
                // Failed attempt for installment 2
                inst2.MarkFailed("Insufficient funds");
                await db.PaymentPlans.AddAsync(plan, ct);
                await db.SaveChangesAsync(ct);
                result.PaymentPlanIds.Add(plan.Id);

                var failedTx = new Transaction(debt.Id, d.Id, plan.Id, null, 200m, debt.Currency, TransactionDirection.Inbound, PaymentMethod.DirectDebit, "DD", $"TXN-{Guid.NewGuid().ToString()[..8]}");
                failedTx.MarkFailed("Bank returned payment - insufficient funds");
                await db.Transactions.AddAsync(failedTx, ct);
                await db.SaveChangesAsync(ct);
                result.TransactionIds.Add(failedTx.Id);
            }

            // Missed payments (multiple)
            for (int i = 0; i < req.MissedPaymentsPerOrg; i++)
            {
                var d = PickDebtor();
                var debt = NewDebt(d, faker.Finance.Amount(1500, 5000, 2), "Personal Loan", "LN-MISS");
                debt.SetStatus(DebtStatus.InArrears);
                debt.SetTags(new[] { "dummy", packTag, "scenario:missed-payments" });
                await db.Debts.AddAsync(debt, ct);
                await db.SaveChangesAsync(ct);
                result.DebtIds.Add(debt.Id);

                var plan = new PaymentPlan(debt.Id, $"PP-{debt.ClientReferenceNumber}", PaymentPlanType.Custom, PaymentFrequency.Monthly, DateTime.UtcNow.AddMonths(-3), 350m, 6);
                plan.Activate("admin-generator", DateTime.UtcNow.AddMonths(-3));
                var m1 = plan.ScheduleInstallment(1, DateTime.UtcNow.AddMonths(-3), 350m); m1.MarkFailed("Payment missed");
                var m2 = plan.ScheduleInstallment(2, DateTime.UtcNow.AddMonths(-2), 350m); m2.MarkFailed("Payment missed");
                var m3 = plan.ScheduleInstallment(3, DateTime.UtcNow.AddMonths(-1), 350m);
                await db.PaymentPlans.AddAsync(plan, ct);
                await db.SaveChangesAsync(ct);
                result.PaymentPlanIds.Add(plan.Id);
            }

            // Remittance (outbound payment to client)
            for (int i = 0; i < req.RemittancesPerOrg; i++)
            {
                var d = PickDebtor();
                var debt = NewDebt(d, faker.Finance.Amount(1000, 3000, 2), "Retail", "RET-REM");
                debt.SetStatus(DebtStatus.Active);
                debt.SetTags(new[] { "dummy", packTag, "scenario:remittance" });
                await db.Debts.AddAsync(debt, ct);
                await db.SaveChangesAsync(ct);
                result.DebtIds.Add(debt.Id);

                var plan = new PaymentPlan(debt.Id, $"PP-{debt.ClientReferenceNumber}", PaymentPlanType.Custom, PaymentFrequency.Weekly, DateTime.UtcNow.AddDays(-21), 180m, 8);
                plan.Activate("admin-generator", DateTime.UtcNow.AddDays(-21));
                var p1 = plan.ScheduleInstallment(1, DateTime.UtcNow.AddDays(-21), 180m); p1.RegisterPayment(180m, DateTime.UtcNow.AddDays(-21));
                var p2 = plan.ScheduleInstallment(2, DateTime.UtcNow.AddDays(-14), 180m); p2.RegisterPayment(180m, DateTime.UtcNow.AddDays(-14));
                await db.PaymentPlans.AddAsync(plan, ct);
                await db.SaveChangesAsync(ct);
                result.PaymentPlanIds.Add(plan.Id);

                var inbound1 = new Transaction(debt.Id, d.Id, plan.Id, null, 180m, debt.Currency, TransactionDirection.Inbound, PaymentMethod.Card, "Stripe", $"TXN-{Guid.NewGuid().ToString()[..8]}");
                inbound1.MarkSettled(DateTime.UtcNow.AddDays(-21));
                var inbound2 = new Transaction(debt.Id, d.Id, plan.Id, null, 180m, debt.Currency, TransactionDirection.Inbound, PaymentMethod.Card, "Stripe", $"TXN-{Guid.NewGuid().ToString()[..8]}");
                inbound2.MarkSettled(DateTime.UtcNow.AddDays(-14));
                var remittance = new Transaction(debt.Id, d.Id, null, null, 350m, debt.Currency, TransactionDirection.Outbound, PaymentMethod.BankTransfer, "Bank", $"TXN-{Guid.NewGuid().ToString()[..8]}");
                remittance.MarkSettled(DateTime.UtcNow.AddDays(-7));
                await db.Transactions.AddRangeAsync(new[] { inbound1, inbound2, remittance }, ct);
                await db.SaveChangesAsync(ct);
                result.TransactionIds.AddRange(new[] { inbound1.Id, inbound2.Id, remittance.Id });
            }
        }

        return result;
    }

    private static List<PaymentPlan> CreatePlansForDebt(Debt debt)
    {
        var plans = new List<PaymentPlan>();
        if (debt.TagsCsv.Contains("active-on-plan") || debt.Status == DebtStatus.Active)
        {
            var plan = new PaymentPlan(
                debt.Id,
                $"PP-{debt.ClientReferenceNumber}",
                PaymentPlanType.Custom,
                PaymentFrequency.Fortnightly,
                DateTime.UtcNow.AddDays(-30),
                500.00m,
                8
            );
            plan.Activate("admin-generator", DateTime.UtcNow.AddDays(-30));
            // schedule a few installments for realism
            for (int i = 0; i < 4; i++)
            {
                var due = DateTime.UtcNow.AddDays(-30 + (i * 14));
                var inst = plan.ScheduleInstallment(i + 1, due, 500m);
                if (i < 2)
                {
                    inst.RegisterPayment(500m, due);
                }
            }
            plans.Add(plan);
        }
        else if (debt.TagsCsv.Contains("in-arrears-high-risk") || debt.Status == DebtStatus.InArrears)
        {
            var plan = new PaymentPlan(
                debt.Id,
                $"PP-{debt.ClientReferenceNumber}",
                PaymentPlanType.Custom,
                PaymentFrequency.Monthly,
                DateTime.UtcNow.AddMonths(-3),
                400.00m,
                10
            );
            plan.Activate("admin-generator", DateTime.UtcNow.AddMonths(-3));
            plan.MarkDefaulted(DateTime.UtcNow.AddDays(-45), "Missed 2 consecutive payments");
            plans.Add(plan);
        }
        else if (debt.Status == DebtStatus.Settled)
        {
            var plan = new PaymentPlan(
                debt.Id,
                $"PP-{debt.ClientReferenceNumber}",
                PaymentPlanType.FullSettlement,
                PaymentFrequency.OneOff,
                DateTime.UtcNow.AddDays(-35),
                debt.OriginalPrincipal,
                1
            );
            plan.Activate("admin-generator", DateTime.UtcNow.AddDays(-35));
            plan.Complete(DateTime.UtcNow.AddDays(-30));
            plans.Add(plan);
        }
        return plans;
    }

    private static List<Transaction> CreateTransactionsForDebt(Debtor debtor, Debt debt, List<PaymentPlan> plans)
    {
        var txs = new List<Transaction>();
        if (debt.Status == DebtStatus.Active && plans.Count > 0)
        {
            var plan = plans.First();
            var tx1 = new Transaction(debt.Id, debtor.Id, plan.Id, null, 500.00m, debt.Currency, TransactionDirection.Inbound, PaymentMethod.BankTransfer, "Bank", $"TXN-{Guid.NewGuid().ToString()[..8]}");
            tx1.MarkSettled(DateTime.UtcNow.AddDays(-20));
            txs.Add(tx1);
            var tx2 = new Transaction(debt.Id, debtor.Id, plan.Id, null, 500.00m, debt.Currency, TransactionDirection.Inbound, PaymentMethod.Card, "Stripe", $"TXN-{Guid.NewGuid().ToString()[..8]}");
            tx2.MarkSettled(DateTime.UtcNow.AddDays(-10));
            txs.Add(tx2);
        }
        else if (debt.Status == DebtStatus.Settled)
        {
            var tx = new Transaction(debt.Id, debtor.Id, null, null, debt.OriginalPrincipal, debt.Currency, TransactionDirection.Inbound, PaymentMethod.BankTransfer, "Bank", $"TXN-{Guid.NewGuid().ToString()[..8]}");
            tx.MarkSettled(DateTime.UtcNow.AddDays(-30));
            txs.Add(tx);
        }
        return txs;
    }
}

public class ScenarioRequest
{
    public string PackName { get; set; } = "Scenario Pack";
    public string? PackId { get; set; }
    public string? Currency { get; set; } = "AUD";

    // Org counts
    public int PendingOrganizations { get; set; } = 0;
    public int RejectedOrganizations { get; set; } = 0;
    public int ActiveOrganizations { get; set; } = 1;
    public int RecentOrganizations { get; set; } = 1;

    // Debtors per org
    public int NewCustomersPerOrg { get; set; } = 2;
    public int InvitedCustomersPerOrg { get; set; } = 2;
    public int ActiveCustomersPerOrg { get; set; } = 3;
    public int DelinquentCustomersPerOrg { get; set; } = 2;
    public int SettledCustomersPerOrg { get; set; } = 1;

    // Segmented payment scenarios per org
    public int UpcomingPaymentsPerOrg { get; set; } = 1;
    public int FailedPaymentsPerOrg { get; set; } = 1;
    public int MissedPaymentsPerOrg { get; set; } = 1;
    public int RemittancesPerOrg { get; set; } = 1;

    public bool IncludePaymentPlans { get; set; } = true;
    public bool IncludeTransactions { get; set; } = true;

    public int? RandomSeed { get; set; }
}

public class ScenarioResult
{
    public string PackId { get; set; } = string.Empty;
    public string PackName { get; set; } = string.Empty;
    public int RandomSeed { get; set; }

    public List<Guid> OrganizationIds { get; } = new();
    public List<Guid> DebtorIds { get; } = new();
    public List<Guid> DebtIds { get; } = new();
    public List<Guid> PaymentPlanIds { get; } = new();
    public List<Guid> TransactionIds { get; } = new();
}
