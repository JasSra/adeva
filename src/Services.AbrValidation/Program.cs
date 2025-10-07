

using BusinessLookupService.ABRXMLSearchRPC;
using DebtManager.Contracts.External;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks;
using Activity = System.Diagnostics.Activity;

/// <summary>
/// Base class for search filters.
/// </summary>
public abstract class BaseSearchFilter
{
    /// <summary>
    /// Optional postcode filter.
    /// </summary>
    [StringLength(4)]
    public string? Postcode { get; set; }

    /// <summary>
    /// Optional entity type code filter.
    /// </summary>
    [StringLength(10)]
    public string? EntityTypeCode { get; set; }
}

/// <summary>
/// Filter for ABN status searches.
/// </summary>
public class AbnStatusSearchFilter : BaseSearchFilter
{
    /// <summary>
    /// Filter for active ABNs only (Y/N).
    /// </summary>
    [RegularExpression("^[YN]$", ErrorMessage = "Must be 'Y' or 'N'")]
    public string? ActiveABNsOnly { get; set; }

    /// <summary>
    /// Filter for current GST registration only (Y/N).
    /// </summary>
    [RegularExpression("^[YN]$", ErrorMessage = "Must be 'Y' or 'N'")]
    public string? CurrentGSTRegistrationOnly { get; set; }
}

/// <summary>
/// Filter for registration event searches.
/// </summary>
public class RegistrationEventSearchFilter : BaseSearchFilter
{
    /// <summary>
    /// State code for the search.
    /// </summary>
    [StringLength(3)]
    public string? State { get; set; }

    /// <summary>
    /// Month for the registration event (1-12).
    /// </summary>
    [Range(1, 12)]
    public string Month { get; set; } = string.Empty;

    /// <summary>
    /// Year for the registration event.
    /// </summary>
    [Range(1990, 2030)]
    public string Year { get; set; } = string.Empty;
}

/// <summary>
/// Filter for update event searches.
/// </summary>
public class UpdateEventSearchFilter : BaseSearchFilter
{
    /// <summary>
    /// State code for the search.
    /// </summary>
    [StringLength(3)]
    public string? State { get; set; }

    /// <summary>
    /// Update date for the search.
    /// </summary>
    [Required]
    public string UpdateDate { get; set; } = string.Empty;
}

/// <summary>
/// Filter for charity searches.
/// </summary>
public class CharitySearchFilter : BaseSearchFilter
{
    /// <summary>
    /// State code for the search.
    /// </summary>
    [StringLength(3)]
    public string? State { get; set; }

    /// <summary>
    /// Charity type code.
    /// </summary>
    [StringLength(10)]
    public string? CharityTypeCode { get; set; }

    /// <summary>
    /// Concession type code.
    /// </summary>
    [StringLength(10)]
    public string? ConcessionTypeCode { get; set; }
}

/// <summary>
/// Postcode search filter.
/// </summary>
public class PostcodeSearchFilter
{
    /// <summary>
    /// Postcode to search for.
    /// </summary>
    [Required]
    [StringLength(4, MinimumLength = 4)]
    public string Postcode { get; set; } = string.Empty;

}

/// <summary>
/// Represents a simple protocol request for name-based business lookup.
/// </summary>
public class NameSimpleProtocolRequest
{
    /// <summary>
    /// The business name to search for.
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional postcode filter for the search.
    /// </summary>
    [StringLength(4)]
    public string? Postcode { get; set; }

    /// <summary>
    /// Optional legal name filter.
    /// </summary>
    [StringLength(200)]
    public string? LegalName { get; set; }

    /// <summary>
    /// Optional trading name filter.
    /// </summary>
    [StringLength(200)]
    public string? TradingName { get; set; }

    /// <summary>
    /// Optional business name filter (for newer API versions).
    /// </summary>
    [StringLength(200)]
    public string? BusinessName { get; set; }

    /// <summary>
    /// Filter for active ABNs only.
    /// </summary>
    public string? ActiveABNsOnly { get; set; }

    /// <summary>
    /// State filters for business location.
    /// </summary>
    public StateFilters StateFilters { get; set; } = new();

    /// <summary>
    /// Search width parameter for fuzzy matching.
    /// </summary>
    [Range(1, 5)]
    public int SearchWidth { get; set; } = 1;

    /// <summary>
    /// Minimum score for search results.
    /// </summary>
    [Range(1, 100)]
    public int MinimumScore { get; set; } = 50;

    /// <summary>
    /// Maximum number of search results to return.
    /// </summary>
    [Range(1, 200)]
    public int MaxSearchResults { get; set; } = 50;
}

/// <summary>
/// Represents state filters for business searches.
/// </summary>
public class StateFilters
{
    public string? NSW { get; set; }
    public string? SA { get; set; }
    public string? ACT { get; set; }
    public string? VIC { get; set; }
    public string? WA { get; set; }
    public string? NT { get; set; }
    public string? QLD { get; set; }
    public string? TAS { get; set; }
}

/// <summary>
/// Represents an advanced name search request with filters.
/// </summary>
public class NameSearchRequest
{
    /// <summary>
    /// The business name to search for.
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional filters to apply to the search.
    /// </summary>
    public NameSearchFilters? Filters { get; set; }

    /// <summary>
    /// Search width for fuzzy matching (1-5).
    /// </summary>
    [Range(1, 5)]
    public int SearchWidth { get; set; } = 1;

    /// <summary>
    /// Minimum score threshold (1-100).
    /// </summary>
    [Range(1, 100)]
    public int MinimumScore { get; set; } = 50;

    /// <summary>
    /// Maximum number of results to return.
    /// </summary>
    [Range(1, 200)]
    public int MaxSearchResults { get; set; } = 50;
}

/// <summary>
/// Filters for name-based searches.
/// </summary>
public class NameSearchFilters
{
    public NameTypeFilters? NameType { get; set; }
    public string? Postcode { get; set; }
    public StateFilters? StateCode { get; set; }
    public string? ActiveABNsOnly { get; set; }
}

/// <summary>
/// Name type filters for specific name categories.
/// </summary>
public class NameTypeFilters
{
    public string? TradingName { get; set; }
    public string? LegalName { get; set; }
    public string? BusinessName { get; set; }
}

/// <summary>
/// Represents the main name of the business entity.
/// </summary>
public record MainNameResult(string OrganisationName, DateTime? EffectiveFrom);

/// <summary>
/// Represents the main business physical address.
/// </summary>
public record MainBusinessPhysicalAddressResult(
    string StateCode,
    string Postcode,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo);

/// <summary>
/// Represents the GST registration period.
/// </summary>
public record GoodsAndServicesTaxResult(DateTime? EffectiveFrom, DateTime? EffectiveTo);

/// <summary>
/// Represents the main trading name of the business entity.
/// </summary>
public record MainTradingNameResult(
    string OrganisationName,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo);

/// <summary>
/// Represents an entity status with dates.
/// </summary>
public record EntityStatusResult(
    string StatusCode,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo);

/// <summary>
/// Represents a business name search result.
/// </summary>
public record BusinessSearchResult(
    string? Abn,
    string? Name,
    string? NameType,
    string? StateCode,
    string? Postcode,
    int? Score,
    bool? IsCurrentIndicator);

/// <summary>
/// Represents a comprehensive business lookup result supporting multiple values.
/// </summary>
public class BusinessLookupResult
{
    /// <summary>
    /// List of ABNs associated with the entity.
    /// </summary>
    public List<string> Abns { get; set; } = new();

    /// <summary>
    /// Primary ACN/ASIC number (only one per entity).
    /// </summary>
    public string? Acn { get; set; }

    /// <summary>
    /// List of ASIC numbers.
    /// </summary>
    public List<string> Asics { get; set; } = new();

    /// <summary>
    /// List of entity names.
    /// </summary>
    public List<string> EntityNames { get; set; } = new();

    /// <summary>
    /// List of trading names.
    /// </summary>
    public List<string> TradingNames { get; set; } = new();

    /// <summary>
    /// List of entity types.
    /// </summary>
    public List<string> EntityTypes { get; set; } = new();

    /// <summary>
    /// List of entity statuses.
    /// </summary>
    public List<string> Statuses { get; set; } = new();

    /// <summary>
    /// List of entity statuses with date information.
    /// </summary>
    public List<EntityStatusResult> EntityStatuses { get; set; } = new();

    /// <summary>
    /// List of addresses (postcodes for backward compatibility).
    /// </summary>
    public List<string> Addresses { get; set; } = new();

    /// <summary>
    /// List of main names with date information.
    /// </summary>
    public List<MainNameResult> MainNames { get; set; } = new();

    /// <summary>
    /// List of main trading names with date information.
    /// </summary>
    public List<MainTradingNameResult> MainTradingNames { get; set; } = new();

    /// <summary>
    /// List of main business physical addresses.
    /// </summary>
    public List<MainBusinessPhysicalAddressResult> MainBusinessPhysicalAddresses { get; set; } = new();

    /// <summary>
    /// List of GST registrations.
    /// </summary>
    public List<GoodsAndServicesTaxResult> GoodsAndServicesTaxes { get; set; } = new();

    /// <summary>
    /// List of business search results (for name-based searches).
    /// </summary>
    public List<BusinessSearchResult> SearchResults { get; set; } = new();

    /// <summary>
    /// Exception details if any error occurred.
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// Total number of records found.
    /// </summary>
    public int NumberOfRecords { get; set; }

    /// <summary>
    /// Indicates if the search exceeded the maximum number of results.
    /// </summary>
    public bool ExceedsMaximum { get; set; }

    /// <summary>
    /// Date and time when the data was retrieved.
    /// </summary>
    public DateTime? DateTimeRetrieved { get; set; }

    /// <summary>
    /// Date when the register was last updated.
    /// </summary>
    public DateTime? DateRegisterLastUpdated { get; set; }

    /// <summary>
    /// Usage statement from the ABR service.
    /// </summary>
    public string? UsageStatement { get; set; }
}

/// <summary>
/// Search API version enumeration.
/// </summary>
public enum SearchApiVersion
{
    /// <summary>
    /// Legacy version.
    /// </summary>
    Legacy,

    /// <summary>
    /// Version 2006.
    /// </summary>
    V2006,

    /// <summary>
    /// Version 2012.
    /// </summary>
    V2012,

    /// <summary>
    /// Version 2017.
    /// </summary>
    V2017,

    /// <summary>
    /// Version 2023 (latest).
    /// </summary>
    V2023
}
/// <summary>
/// Provides business lookup operations for ABN, ASIC, ACN, and name-based searches.
/// </summary>
public interface IBusinessLookupService
{
    /// <summary>
    /// Checks if the specified identifier (ABN/ACN) is active.
    /// </summary>
    /// <param name="identifier">The ABN or ACN to check.</param>
    /// <returns>True if the identifier is active, false otherwise.</returns>
    Task<bool> IsIdentifierActiveAsync(string identifier);

    /// <summary>
    /// Checks if the ABR service is available.
    /// </summary>
    /// <returns>True if the service is available, false otherwise.</returns>
    Task<bool> IsServiceAvailableAsync();

    /// <summary>
    /// Searches for business information by ABN.
    /// </summary>
    /// <param name="abn">The ABN to search for.</param>
    /// <returns>Business lookup result containing entity information.</returns>
    Task<BusinessLookupResult> SearchByAbnAsync(string abn);

    /// <summary>
    /// Searches for business information by ASIC number.
    /// </summary>
    /// <param name="asic">The ASIC number to search for.</param>
    /// <returns>Business lookup result containing entity information.</returns>
    Task<BusinessLookupResult> SearchByAsicAsync(string asic);

    /// <summary>
    /// Searches for business information by ACN.
    /// </summary>
    /// <param name="acn">The ACN to search for.</param>
    /// <returns>Business lookup result containing entity information.</returns>
    Task<BusinessLookupResult> SearchByAcnAsync(string acn);

    /// <summary>
    /// Searches for businesses using a simple name protocol.
    /// </summary>
    /// <param name="request">The name search request parameters.</param>
    /// <param name="apiVersion">The API version to use for the search.</param>
    /// <returns>Business lookup result containing matching entities.</returns>
    Task<BusinessLookupResult> SearchByNameSimpleProtocolAsync(NameSimpleProtocolRequest request, SearchApiVersion apiVersion = SearchApiVersion.V2023);

    /// <summary>
    /// Searches for businesses by name using advanced search with comprehensive filters.
    /// </summary>
    /// <param name="request">The advanced name search request parameters.</param>
    /// <returns>Business lookup result containing matching entities.</returns>
    Task<BusinessLookupResult> SearchByNameAdvancedAsync(NameSearchRequest request);

    /// <summary>
    /// Searches for businesses by ABN status filters.
    /// </summary>
    /// <param name="filter">The ABN status search filter.</param>
    /// <returns>Business lookup result containing matching entities.</returns>
    Task<BusinessLookupResult> SearchByABNStatusAsync(AbnStatusSearchFilter filter);

    /// <summary>
    /// Searches for businesses by registration event filters.
    /// </summary>
    /// <param name="filter">The registration event search filter.</param>
    /// <returns>Business lookup result containing matching entities.</returns>
    Task<BusinessLookupResult> SearchByRegistrationEventAsync(RegistrationEventSearchFilter filter);

    /// <summary>
    /// Searches for businesses by update event filters.
    /// </summary>
    /// <param name="filter">The update event search filter.</param>
    /// <returns>Business lookup result containing matching entities.</returns>
    Task<BusinessLookupResult> SearchByUpdateEventAsync(UpdateEventSearchFilter filter);

    /// <summary>
    /// Searches for businesses by postcode.
    /// </summary>
    /// <param name="filter">The postcode search filter.</param>
    /// <returns>Business lookup result containing matching entities.</returns>
    Task<BusinessLookupResult> SearchByPostcodeAsync(PostcodeSearchFilter filter);

    /// <summary>
    /// Searches for charities by filters.
    /// </summary>
    /// <param name="filter">The charity search filter.</param>
    /// <returns>Business lookup result containing matching entities.</returns>
    Task<BusinessLookupResult> SearchByCharityAsync(CharitySearchFilter filter);
}



/// <summary>
/// Implementation of IBusinessLookupService that calls ABRXMLSearchRPCSoapClient.
/// Provides comprehensive business lookup functionality including name-based searches.
/// </summary>
public class AbrBusinessLookupService : IBusinessLookupService, IDisposable
{
    private readonly BusinessLookupService.ABRXMLSearchRPC.ABRXMLSearchRPCSoapClient _client;
    private readonly string _authGuid;
    private readonly ILogger<AbrBusinessLookupService> _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the BusinessLookupService.
    /// </summary>
    /// <param name="authGuid">Authentication GUID for ABR service.</param>
    /// <param name="logger">Logger instance for logging operations.</param>
    public AbrBusinessLookupService(string authGuid, ILogger<AbrBusinessLookupService> logger)
    {
        _authGuid = authGuid ?? throw new ArgumentNullException(nameof(authGuid));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _client = new ABRXMLSearchRPCSoapClient(ABRXMLSearchRPCSoapClient.EndpointConfiguration.ABRXMLSearchRPCSoap);
    }

    /// <summary>
    /// Checks if the specified identifier (ABN/ACN) is active.
    /// </summary>
    public async Task<bool> IsIdentifierActiveAsync(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            _logger.LogWarning("IsIdentifierActiveAsync called with null or empty identifier.");
            return false;
        }

        using var activity = new Activity("IsIdentifierActiveAsync").Start();
        activity?.SetTag("identifier", identifier);

        try
        {
            _logger.LogInformation("Checking if identifier {Identifier} is active.", identifier);

            // Try ABN first
            var payload = await _client.ABRSearchByABNAsync(identifier, "N", _authGuid);
            var result = await ProcessIdentifierActiveResponseAsync(payload, identifier);

            if (result.HasValue)
                return result.Value;

            // Try ACN if ABN search failed
            _logger.LogInformation("ABN search failed for {Identifier}, trying ACN.", identifier);
            payload = await _client.ABRSearchByASICAsync(identifier, "N", _authGuid);
            result = await ProcessIdentifierActiveResponseAsync(payload, identifier);

            return result ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking identifier {Identifier} status.", identifier);
            return false;
        }
    }

    /// <summary>
    /// Checks if the ABRXMLSearchRPC service is available.
    /// </summary>
    public async Task<bool> IsServiceAvailableAsync()
    {
        using var activity = new Activity("IsServiceAvailableAsync").Start();

        try
        {
            _logger.LogInformation("Checking ABRXMLSearchRPC service availability.");
            await _client.OpenAsync();
            _logger.LogInformation("Service is available.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service availability check failed.");
            return false;
        }
    }

    /// <summary>
    /// Searches for business information by ABN.
    /// </summary>
    public async Task<BusinessLookupResult> SearchByAbnAsync(string abn)
    {
        if (string.IsNullOrWhiteSpace(abn))
        {
            _logger.LogWarning("SearchByAbnAsync called with null or empty ABN.");
            return CreateErrorResult("ABN cannot be null or empty.");
        }

        using var activity = new Activity("SearchByAbnAsync").Start();
        activity?.SetTag("abn", abn);

        try
        {
            _logger.LogInformation("Searching by ABN: {Abn}", abn);
            var payload = await _client.ABRSearchByABNAsync(abn, "N", _authGuid);
            return MapToResult(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by ABN: {Abn}", abn);
            return CreateErrorResult($"Error searching by ABN: {ex.Message}");
        }
    }

    /// <summary>
    /// Searches for business information by ASIC number.
    /// </summary>
    public async Task<BusinessLookupResult> SearchByAsicAsync(string asic)
    {
        if (string.IsNullOrWhiteSpace(asic))
        {
            _logger.LogWarning("SearchByAsicAsync called with null or empty ASIC.");
            return CreateErrorResult("ASIC number cannot be null or empty.");
        }

        using var activity = new Activity("SearchByAsicAsync").Start();
        activity?.SetTag("asic", asic);

        try
        {
            _logger.LogInformation("Searching by ASIC: {Asic}", asic);
            var payload = await _client.ABRSearchByASICAsync(asic, "N", _authGuid);
            return MapToResult(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by ASIC: {Asic}", asic);
            return CreateErrorResult($"Error searching by ASIC: {ex.Message}");
        }
    }

    /// <summary>
    /// Searches for business information by ACN.
    /// </summary>
    public async Task<BusinessLookupResult> SearchByAcnAsync(string acn)
    {
        if (string.IsNullOrWhiteSpace(acn))
        {
            _logger.LogWarning("SearchByAcnAsync called with null or empty ACN.");
            return CreateErrorResult("ACN cannot be null or empty.");
        }

        using var activity = new Activity("SearchByAcnAsync").Start();
        activity?.SetTag("acn", acn);

        try
        {
            _logger.LogInformation("Searching by ACN: {Acn}", acn);
            var payload = await _client.ABRSearchByASICAsync(acn, "N", _authGuid);
            return MapToResult(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by ACN: {Acn}", acn);
            return CreateErrorResult($"Error searching by ACN: {ex.Message}");
        }
    }

    /// <summary>
    /// Searches for businesses using a simple name protocol.
    /// </summary>
    public async Task<BusinessLookupResult> SearchByNameSimpleProtocolAsync(NameSimpleProtocolRequest request, SearchApiVersion apiVersion = SearchApiVersion.V2023)
    {
        if (request == null)
        {
            _logger.LogWarning("SearchByNameSimpleProtocolAsync called with null request.");
            return CreateErrorResult("Search request cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            _logger.LogWarning("SearchByNameSimpleProtocolAsync called with null or empty name.");
            return CreateErrorResult("Business name cannot be null or empty.");
        }

        using var activity = new Activity("SearchByNameSimpleProtocolAsync").Start();
        activity?.SetTag("name", request.Name);
        activity?.SetTag("apiVersion", apiVersion.ToString());

        try
        {
            _logger.LogInformation("Searching by name using simple protocol: {Name}, API Version: {ApiVersion}", request.Name, apiVersion);

            var payload = await _client.ABRSearchByNameSimpleProtocolAsync(
                request.Name, request.Postcode, request.LegalName, request.TradingName,
                request.StateFilters.NSW, request.StateFilters.SA, request.StateFilters.ACT,
                request.StateFilters.VIC,
                request.StateFilters.WA, request.StateFilters.NT, request.StateFilters.QLD,
                request.StateFilters.TAS,
                _authGuid);

            return MapToResult(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by name using simple protocol: {Name}", request.Name);
            return CreateErrorResult($"Error searching by name: {ex.Message}");
        }
    }

    /// <summary>
    /// Searches for businesses using advanced name search with filters.
    /// </summary>
    public async Task<BusinessLookupResult> SearchByNameAdvancedAsync(NameSearchRequest request)
    {
        if (request == null)
        {
            _logger.LogWarning("SearchByNameAdvancedAsync called with null request.");
            return CreateErrorResult("Search request cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            _logger.LogWarning("SearchByNameAdvancedAsync called with null or empty name.");
            return CreateErrorResult("Business name cannot be null or empty.");
        }

        using var activity = new Activity("SearchByNameAdvancedAsync").Start();
        activity?.SetTag("name", request.Name);

        try
        {
            _logger.LogInformation("Searching by name using advanced search: {Name}", request.Name);

            var payload = await SearchByNameAdvancedLegacyAsync(request);

            return MapToResult(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by name using advanced search: {Name}", request.Name);
            return CreateErrorResult($"Error searching by name: {ex.Message}");
        }
    }

    /// <summary>
    /// Searches for businesses by ABN status filters.
    /// </summary>
    public async Task<BusinessLookupResult> SearchByABNStatusAsync(AbnStatusSearchFilter filter)
    {
        if (filter == null)
        {
            _logger.LogWarning("SearchByABNStatusAsync called with null filter.");
            return CreateErrorResult("Filter cannot be null.");
        }

        using var activity = new Activity("SearchByABNStatusAsync").Start();

        try
        {
            _logger.LogInformation("Searching by ABN status with filter: {Filter}", filter);
            var payload = await _client.SearchByABNStatusAsync(
                filter.Postcode,
                filter.ActiveABNsOnly,
                filter.CurrentGSTRegistrationOnly,
                filter.EntityTypeCode,
                _authGuid);
            return MapToResult(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by ABN status.");
            return CreateErrorResult($"Error searching by ABN status: {ex.Message}");
        }
    }

    /// <summary>
    /// Searches for businesses by registration event filters.
    /// </summary>
    public async Task<BusinessLookupResult> SearchByRegistrationEventAsync(RegistrationEventSearchFilter filter)
    {
        if (filter == null)
        {
            _logger.LogWarning("SearchByRegistrationEventAsync called with null filter.");
            return CreateErrorResult("Filter cannot be null.");
        }

        using var activity = new Activity("SearchByRegistrationEventAsync").Start();

        try
        {
            _logger.LogInformation("Searching by registration event with filter: {Filter}", filter);
            var payload = await _client.SearchByRegistrationEventAsync(
                filter.Postcode,
                filter.State,
                filter.EntityTypeCode,
                filter.Month,
                filter.Year,
                _authGuid);
            return MapToResult(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by registration event.");
            return CreateErrorResult($"Error searching by registration event: {ex.Message}");
        }
    }

    /// <summary>
    /// Searches for businesses by update event filters.
    /// </summary>
    public async Task<BusinessLookupResult> SearchByUpdateEventAsync(UpdateEventSearchFilter filter)
    {
        if (filter == null)
        {
            _logger.LogWarning("SearchByUpdateEventAsync called with null filter.");
            return CreateErrorResult("Filter cannot be null.");
        }

        using var activity = new Activity("SearchByUpdateEventAsync").Start();

        try
        {
            _logger.LogInformation("Searching by update event with filter: {Filter}", filter);
            var payload = await _client.SearchByUpdateEventAsync(
                filter.Postcode,
                filter.State,
                filter.EntityTypeCode,
                filter.UpdateDate,
                _authGuid);
            return MapToResult(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by update event.");
            return CreateErrorResult($"Error searching by update event: {ex.Message}");
        }
    }

    /// <summary>
    /// Searches for businesses by postcode.
    /// </summary>
    public async Task<BusinessLookupResult> SearchByPostcodeAsync(PostcodeSearchFilter filter)
    {
        if (filter == null)
        {
            _logger.LogWarning("SearchByPostcodeAsync called with null filter.");
            return CreateErrorResult("Filter cannot be null.");
        }

        using var activity = new Activity("SearchByPostcodeAsync").Start();
        activity?.SetTag("postcode", filter.Postcode);

        try
        {
            _logger.LogInformation("Searching by postcode: {Postcode}", filter.Postcode);
            var payload = await _client.SearchByPostcodeAsync(filter.Postcode, _authGuid);
            return MapToResult(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by postcode: {Postcode}", filter.Postcode);
            return CreateErrorResult($"Error searching by postcode: {ex.Message}");
        }
    }

    /// <summary>
    /// Searches for charities by filters.
    /// </summary>
    public async Task<BusinessLookupResult> SearchByCharityAsync(CharitySearchFilter filter)
    {
        if (filter == null)
        {
            _logger.LogWarning("SearchByCharityAsync called with null filter.");
            return CreateErrorResult("Filter cannot be null.");
        }

        using var activity = new Activity("SearchByCharityAsync").Start();

        try
        {
            _logger.LogInformation("Searching by charity with filter: {Filter}", filter);
            var payload = await _client.SearchByCharityAsync(
                filter.Postcode,
                filter.State,
                filter.CharityTypeCode,
                filter.ConcessionTypeCode,
                _authGuid);
            return MapToResult(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by charity.");
            return CreateErrorResult($"Error searching by charity: {ex.Message}");
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Processes the response for identifier active check.
    /// </summary>
    private async Task<bool?> ProcessIdentifierActiveResponseAsync(Payload payload, string identifier)
    {
        if (payload?.Response?.ResponseBody is ResponseException exception)
        {
            _logger.LogWarning("Service returned exception for {Identifier}: {Description} ({Code})",
                identifier, exception.Description, exception.Code);
            return null;
        }

        if (payload?.Response?.ResponseBody is ResponseBusinessEntity entity)
        {
            var isActive = entity.EntityStatus?.Length > 0 &&
                           entity.EntityStatus[0].EntityStatusCode == "Active" &&
                           entity.ABN?.Length > 0 &&
                           entity.ABN[0].IsCurrentIndicator == "Y";

            _logger.LogInformation("Identifier {Identifier} is active: {IsActive}", identifier, isActive);
            return isActive;
        }

        return null;
    }

    /// <summary>
    /// Advanced name search for legacy API version.
    /// </summary>
    private async Task<Payload> SearchByNameAdvancedLegacyAsync(NameSearchRequest request)
    {
        var searchRequest = new ExternalRequestNameSearchAdvanced
        {
            AuthenticationGUID = _authGuid,
            Name = request.Name,
            SearchWidth = request.SearchWidth.ToString(),
            MinimumScore = request.MinimumScore,
            Filters = CreateFiltersLegacy(request.Filters)
        };

        return await _client.ABRSearchByNameAdvancedAsync(searchRequest, _authGuid);
    }

    /// <summary>
    /// Creates filters for legacy API versions.
    /// </summary>
    private ExternalRequestFilters? CreateFiltersLegacy(NameSearchFilters? filters)
    {
        if (filters == null) return null;

        return new ExternalRequestFilters
        {
            Postcode = filters.Postcode,
            NameType = filters.NameType != null ? new ExternalRequestFilterNameType
            {
                LegalName = filters.NameType.LegalName,
                TradingName = filters.NameType.TradingName
            } : null,
            StateCode = CreateStateCodeFilter(filters.StateCode)
        };
    }

    /// <summary>
    /// Creates state code filter from StateFilters.
    /// </summary>
    private ExternalRequestFilterStateCode? CreateStateCodeFilter(StateFilters? stateFilters)
    {
        if (stateFilters == null) return null;

        return new ExternalRequestFilterStateCode
        {
            NSW = stateFilters.NSW,
            SA = stateFilters.SA,
            ACT = stateFilters.ACT,
            VIC = stateFilters.VIC,
            WA = stateFilters.WA,
            NT = stateFilters.NT,
            QLD = stateFilters.QLD,
            TAS = stateFilters.TAS
        };
    }

    /// <summary>
    /// Creates an error result with the specified message.
    /// </summary>
    private BusinessLookupResult CreateErrorResult(string errorMessage)
    {
        return new BusinessLookupResult
        {
            Exception = errorMessage,
            DateTimeRetrieved = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Maps ABR service payload to BusinessLookupResult with comprehensive data extraction.
    /// </summary>
    private BusinessLookupResult MapToResult(Payload? payload)
    {
        using var activity = new Activity("MapToResult").Start();

        try
        {
            var response = payload?.Response;
            if (response == null)
            {
                _logger.LogWarning("No response found in payload.");
                return CreateErrorResult("No response received from ABR service.");
            }

            var result = new BusinessLookupResult
            {
                DateTimeRetrieved = response.DateTimeRetrieved,
                DateRegisterLastUpdated = response.DateRegisterLastUpdated,
                UsageStatement = response.UsageStatement
            };

            // Handle exception response
            if (response.ResponseBody is ResponseException exception)
            {
                _logger.LogWarning("Service returned exception: {Description} ({Code})", exception.Description, exception.Code);
                result.Exception = $"Service Exception: {exception.Description} (Code: {exception.Code})";
                return result;
            }

            // Handle business entity response (single entity)
            if (response.ResponseBody is ResponseBusinessEntity entity)
            {
                MapBusinessEntity(entity, result);
                return result;
            }

            // Handle search results list (multiple records)
            if (response.ResponseBody is ResponseSearchResultsList searchResults)
            {
                MapSearchResultsList(searchResults, result);
                return result;
            }

            // Handle ABN list response
            if (response.ResponseBody is ResponseABNList abnList)
            {
                result.NumberOfRecords = abnList.NumberOfRecords;
                if (abnList.ABN != null)
                {
                    result.Abns.AddRange(abnList.ABN.Where(abn => !string.IsNullOrWhiteSpace(abn)));
                }
                return result;
            }

            _logger.LogWarning("Unknown response body type: {Type}", response.ResponseBody?.GetType().Name);
            return CreateErrorResult($"Unknown response type: {response.ResponseBody?.GetType().Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping payload to result.");
            return CreateErrorResult($"Error processing response: {ex.Message}");
        }
    }

    /// <summary>
    /// Maps business entity data to the result object.
    /// </summary>
    private void MapBusinessEntity(ResponseBusinessEntity entity, BusinessLookupResult result)
    {
        // Map ABNs
        if (entity.ABN != null)
        {
            foreach (var abn in entity.ABN.Where(a => !string.IsNullOrWhiteSpace(a?.IdentifierValue)))
            {
                result.Abns.Add(abn.IdentifierValue);
            }
        }

        // Map ASIC/ACN number
        if (!string.IsNullOrWhiteSpace(entity.ASICNumber))
        {
            result.Acn = entity.ASICNumber;
            result.Asics.Add(entity.ASICNumber);
        }

        // Map entity statuses
        if (entity.EntityStatus != null)
        {
            foreach (var status in entity.EntityStatus.Where(s => !string.IsNullOrWhiteSpace(s?.EntityStatusCode)))
            {
                result.Statuses.Add(status.EntityStatusCode);
                result.EntityStatuses.Add(new EntityStatusResult(status.EntityStatusCode, status.EffectiveFrom, status.EffectiveTo));
            }
        }

        // Map entity type
        if (!string.IsNullOrWhiteSpace(entity.EntityType?.EntityDescription))
        {
            result.EntityTypes.Add(entity.EntityType.EntityDescription);
        }

        // Map GST registrations
        if (entity.GoodsAndServicesTax != null)
        {
            foreach (var gst in entity.GoodsAndServicesTax)
            {
                result.GoodsAndServicesTaxes.Add(new GoodsAndServicesTaxResult(gst.EffectiveFrom, gst.EffectiveTo));
            }
        }

        // Map names and trading names
        MapEntityNames(entity, result);

        // Map addresses
        if (entity.MainBusinessPhysicalAddress != null)
        {
            foreach (var address in entity.MainBusinessPhysicalAddress)
            {
                result.MainBusinessPhysicalAddresses.Add(new MainBusinessPhysicalAddressResult(
                    address.StateCode, address.Postcode, address.EffectiveFrom, address.EffectiveTo));

                if (!string.IsNullOrWhiteSpace(address.Postcode))
                {
                    result.Addresses.Add(address.Postcode);
                }
            }
        }
    }

    /// <summary>
    /// Maps entity names from various sources.
    /// </summary>
    private void MapEntityNames(ResponseBusinessEntity entity, BusinessLookupResult result)
    {
        // Map main names
        if (entity.Name != null)
        {
            foreach (var name in entity.Name)
            {
                var nameString = ExtractNameString(name);
                if (!string.IsNullOrWhiteSpace(nameString))
                {
                    result.EntityNames.Add(nameString);

                    if (name is OrganisationName orgName)
                    {
                        result.MainNames.Add(new MainNameResult(orgName.OrganisationName, orgName.EffectiveFrom));
                    }
                    else if (name is IndividualName indName)
                    {
                        result.MainNames.Add(new MainNameResult(indName.FullName ?? $"{indName.GivenName} {indName.FamilyName}".Trim(), indName.EffectiveFrom));
                    }
                }
            }
        }

        // Map main trading names
        if (entity.MainTradingName != null)
        {
            foreach (var tradingName in entity.MainTradingName.Where(t => !string.IsNullOrWhiteSpace(t?.OrganisationName)))
            {
                result.TradingNames.Add(tradingName.OrganisationName);
                result.MainTradingNames.Add(new MainTradingNameResult(
                    tradingName.OrganisationName,
                    tradingName.EffectiveFrom,
                    tradingName.EffectiveTo as DateTime?));
            }
        }

        // Map other trading names
        if (entity.OtherTradingName != null)
        {
            foreach (var tradingName in entity.OtherTradingName.Where(t => !string.IsNullOrWhiteSpace(t?.OrganisationName)))
            {
                result.TradingNames.Add(tradingName.OrganisationName);
            }
        }

        // Map business names (if available)
        if (entity is ResponseBusinessEntity201205 entity2012 && entity2012.BusinessName != null)
        {
            foreach (var businessName in entity2012.BusinessName.Where(b => !string.IsNullOrWhiteSpace(b?.OrganisationName)))
            {
                result.EntityNames.Add(businessName.OrganisationName);
            }
        }
    }

    /// <summary>
    /// Maps search results list to the result object.
    /// </summary>
    private void MapSearchResultsList(ResponseSearchResultsList searchResults, BusinessLookupResult result)
    {
        result.NumberOfRecords = searchResults.NumberOfRecords;
        result.ExceedsMaximum = !string.IsNullOrWhiteSpace(searchResults.ExceedsMaximum) &&
                                searchResults.ExceedsMaximum.Equals("Y", StringComparison.OrdinalIgnoreCase);

        if (searchResults.SearchResultsRecord == null) return;

        foreach (var record in searchResults.SearchResultsRecord)
        {
            // Map ABNs
            if (record.ABN != null)
            {
                foreach (var abn in record.ABN.Where(a => !string.IsNullOrWhiteSpace(a?.IdentifierValue)))
                {
                    result.Abns.Add(abn.IdentifierValue);
                }
            }

            // Map addresses
            if (record.MainBusinessPhysicalAddress != null)
            {
                foreach (var address in record.MainBusinessPhysicalAddress)
                {
                    result.MainBusinessPhysicalAddresses.Add(new MainBusinessPhysicalAddressResult(
                        address.StateCode, address.Postcode, null, null));

                    if (!string.IsNullOrWhiteSpace(address.Postcode))
                    {
                        result.Addresses.Add(address.Postcode);
                    }
                }
            }

            // Map names and create search results
            if (record.Name != null)
            {
                foreach (var name in record.Name)
                {
                    var nameString = ExtractNameString(name);
                    if (!string.IsNullOrWhiteSpace(nameString))
                    {
                        result.EntityNames.Add(nameString);

                        // Create business search result with proper NameType handling
                        var nameType = record.NameType?.FirstOrDefault().ToString() ?? "Unknown";
                        var searchResult = new BusinessSearchResult(
                            record.ABN?.FirstOrDefault()?.IdentifierValue,
                            nameString,
                            nameType,
                            record.MainBusinessPhysicalAddress?.FirstOrDefault()?.StateCode,
                            record.MainBusinessPhysicalAddress?.FirstOrDefault()?.Postcode,
                            ExtractScore(name),
                            ExtractCurrentIndicator(name)
                        );
                        result.SearchResults.Add(searchResult);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Extracts name string from various name types.
    /// </summary>
    private string? ExtractNameString(object name)
    {
        return name switch
        {
            OrganisationName orgName => orgName.OrganisationName,
            OrganisationSimpleName orgSimpleName => orgSimpleName.OrganisationName,
            IndividualName indName => indName.FullName ?? $"{indName.GivenName} {indName.FamilyName}".Trim(),
            IndividualSimpleName indSimpleName => indSimpleName.FullName ?? $"{indSimpleName.GivenName} {indSimpleName.FamilyName}".Trim(),
            Organisation org => org.OrganisationName,
            Individual ind => ind.FullName ?? $"{ind.GivenName} {ind.FamilyName}".Trim(),
            _ => name?.ToString()
        };
    }

    /// <summary>
    /// Extracts score from name objects that have scoring.
    /// </summary>
    private int? ExtractScore(object name)
    {
        return name switch
        {
            OrganisationSimpleName orgSimpleName => orgSimpleName.Score,
            IndividualSimpleName indSimpleName => indSimpleName.Score,
            _ => null
        };
    }

    /// <summary>
    /// Extracts current indicator from name objects.
    /// </summary>
    private bool? ExtractCurrentIndicator(object name)
    {
        var indicator = name switch
        {
            OrganisationSimpleName orgSimpleName => orgSimpleName.IsCurrentIndicator,
            IndividualSimpleName indSimpleName => indSimpleName.IsCurrentIndicator,
            _ => null
        };

        return indicator?.Equals("Y", StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Disposes the service and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose method.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                _client?.Close();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing ABR client during dispose.");
            }
            finally
            {
                _client?.Abort();
            }
            _disposed = true;
        }
    }

    #endregion
}

