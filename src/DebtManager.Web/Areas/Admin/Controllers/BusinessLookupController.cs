using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "RequireAdminScope")]
public class BusinessLookupController : Controller
{
    private readonly IBusinessLookupService _lookup;

    public BusinessLookupController(IBusinessLookupService lookup)
    {
        _lookup = lookup;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new BusinessLookupVm());
    }

    // Safety net: if something posts to Index, try dispatching appropriately to avoid 405
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(BusinessLookupVm vm, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(vm.Abn))
        {
            vm.Result = await _lookup.SearchByAbnAsync(vm.Abn);
            vm.ActiveTab = "abn";
            return View(vm);
        }
        if (!string.IsNullOrWhiteSpace(vm.Acn))
        {
            vm.Result = await _lookup.SearchByAcnAsync(vm.Acn);
            vm.ActiveTab = "acn";
            return View(vm);
        }
        if (!string.IsNullOrWhiteSpace(vm.Name))
        {
            var req = new NameSimpleProtocolRequest { Name = vm.Name, Postcode = vm.Postcode };
            vm.Result = await _lookup.SearchByNameSimpleProtocolAsync(req);
            vm.ActiveTab = "name";
            return View(vm);
        }

        ModelState.AddModelError(string.Empty, "Enter ABN, ACN, or Name to search.");
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LookupByAbn(BusinessLookupVm vm, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vm.Abn))
        {
            ModelState.AddModelError(nameof(vm.Abn), "ABN is required");
            return View("Index", vm);
        }
        vm.Result = await _lookup.SearchByAbnAsync(vm.Abn);
        vm.ActiveTab = "abn";
        return View("Index", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LookupByAcn(BusinessLookupVm vm, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vm.Acn))
        {
            ModelState.AddModelError(nameof(vm.Acn), "ACN is required");
            return View("Index", vm);
        }
        vm.Result = await _lookup.SearchByAcnAsync(vm.Acn);
        vm.ActiveTab = "acn";
        return View("Index", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LookupByName(BusinessLookupVm vm, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vm.Name))
        {
            ModelState.AddModelError(nameof(vm.Name), "Name is required");
            return View("Index", vm);
        }
        var req = new NameSimpleProtocolRequest
        {
            Name = vm.Name,
            Postcode = vm.Postcode
        };
        vm.Result = await _lookup.SearchByNameSimpleProtocolAsync(req);
        vm.ActiveTab = "name";
        return View("Index", vm);
    }
}

public class BusinessLookupVm
{
    [Display(Name = "ABN")] public string? Abn { get; set; }
    [Display(Name = "ACN")] public string? Acn { get; set; }
    [Display(Name = "Name")] public string? Name { get; set; }
    [Display(Name = "Postcode")] public string? Postcode { get; set; }

    public string ActiveTab { get; set; } = "abn";

    public BusinessLookupResult? Result { get; set; }
}
