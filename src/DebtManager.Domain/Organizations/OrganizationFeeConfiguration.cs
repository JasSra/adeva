using DebtManager.Domain.Common;

namespace DebtManager.Domain.Organizations;

public class OrganizationFeeConfiguration : Entity
{
    public Guid OrganizationId { get; private set; }
    
    // Discount percentages for different payment plan types
    public decimal FullPaymentDiscountPercentage { get; private set; }
    public decimal SystemPlanDiscountPercentage { get; private set; }
    
    // Admin fees for custom payment plans
    public decimal CustomPlanAdminFeeFlat { get; private set; }
    public decimal CustomPlanAdminFeePercentage { get; private set; }
    
    // Payment processing fees
    public decimal PaymentProcessingFeePercentage { get; private set; }
    public decimal LateFeeFlat { get; private set; }
    public decimal LateFeePercentage { get; private set; }
    
    // Remittance schedule configuration
    public RemittanceFrequency RemittanceFrequency { get; private set; }
    public decimal MinimumPayoutThreshold { get; private set; }
    public bool AutomaticPayoutsEnabled { get; private set; }
    
    // Smart installment rules
    public decimal MinimumInstallmentAmount { get; private set; }
    public int DefaultInstallmentPeriodWeeks { get; private set; }
    public int MaximumInstallmentCount { get; private set; }
    
    public Organization? Organization { get; private set; }
    
    private OrganizationFeeConfiguration()
    {
        // Default values
        FullPaymentDiscountPercentage = 10.0m;
        SystemPlanDiscountPercentage = 5.0m;
        CustomPlanAdminFeeFlat = 25.0m;
        CustomPlanAdminFeePercentage = 2.0m;
        PaymentProcessingFeePercentage = 2.5m;
        LateFeeFlat = 10.0m;
        LateFeePercentage = 5.0m;
        RemittanceFrequency = RemittanceFrequency.Weekly;
        MinimumPayoutThreshold = 100.0m;
        AutomaticPayoutsEnabled = true;
        MinimumInstallmentAmount = 50.0m;
        DefaultInstallmentPeriodWeeks = 12;
        MaximumInstallmentCount = 52;
    }
    
    public OrganizationFeeConfiguration(Guid organizationId) : this()
    {
        OrganizationId = organizationId;
    }
    
    public void SetDiscounts(decimal fullPaymentDiscount, decimal systemPlanDiscount)
    {
        if (fullPaymentDiscount < 0 || fullPaymentDiscount > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(fullPaymentDiscount), "Discount must be between 0 and 100 percent.");
        }
        
        if (systemPlanDiscount < 0 || systemPlanDiscount > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(systemPlanDiscount), "Discount must be between 0 and 100 percent.");
        }
        
        FullPaymentDiscountPercentage = fullPaymentDiscount;
        SystemPlanDiscountPercentage = systemPlanDiscount;
        UpdatedAtUtc = DateTime.UtcNow;
    }
    
    public void SetCustomPlanFees(decimal flatFee, decimal percentageFee)
    {
        if (flatFee < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(flatFee), "Fee cannot be negative.");
        }
        
        if (percentageFee < 0 || percentageFee > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percentageFee), "Percentage must be between 0 and 100.");
        }
        
        CustomPlanAdminFeeFlat = flatFee;
        CustomPlanAdminFeePercentage = percentageFee;
        UpdatedAtUtc = DateTime.UtcNow;
    }
    
    public void SetRemittanceSchedule(RemittanceFrequency frequency, decimal minimumThreshold, bool automaticPayouts)
    {
        if (minimumThreshold < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumThreshold), "Threshold cannot be negative.");
        }
        
        RemittanceFrequency = frequency;
        MinimumPayoutThreshold = minimumThreshold;
        AutomaticPayoutsEnabled = automaticPayouts;
        UpdatedAtUtc = DateTime.UtcNow;
    }
    
    public void SetInstallmentRules(decimal minimumAmount, int defaultPeriodWeeks, int maxCount)
    {
        if (minimumAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumAmount), "Minimum amount must be positive.");
        }
        
        if (defaultPeriodWeeks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(defaultPeriodWeeks), "Period must be positive.");
        }
        
        if (maxCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCount), "Max count must be positive.");
        }
        
        MinimumInstallmentAmount = minimumAmount;
        DefaultInstallmentPeriodWeeks = defaultPeriodWeeks;
        MaximumInstallmentCount = maxCount;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public enum RemittanceFrequency
{
    Weekly,
    Fortnightly,
    Monthly
}
