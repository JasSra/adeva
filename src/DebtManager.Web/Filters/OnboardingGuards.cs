using System.Security.Claims;
using DebtManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Filters;

public class RequireOrganizationOnboardedAttribute : TypeFilterAttribute
{
    public RequireOrganizationOnboardedAttribute() : base(typeof(RequireOrganizationOnboardedFilter)) { }

    private class RequireOrganizationOnboardedFilter : IAsyncActionFilter
    {
        private readonly AppDbContext _db;
        public RequireOrganizationOnboardedFilter(AppDbContext db) => _db = db;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (!(user?.Identity?.IsAuthenticated ?? false))
            {
                await next();
                return;
            }

            if (!user.IsInRole("Client"))
            {
                await next();
                return;
            }

            var uid = user.FindFirstValue("oid") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(uid))
            {
                await next();
                return;
            }

            var appUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.ExternalAuthId == uid);
            if (appUser == null)
            {
                await next();
                return;
            }

            var profile = await _db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == appUser.Id);
            if (profile?.OrganizationId == null)
            {
                var factory = context.HttpContext.RequestServices.GetService(typeof(ITempDataDictionaryFactory)) as ITempDataDictionaryFactory;
                var tempData = factory?.GetTempData(context.HttpContext);
                if (tempData != null)
                {
                    tempData["OnboardingInfo"] = "Please complete organization onboarding to access the Client portal.";
                }
                context.Result = new RedirectResult("/Client/Onboarding");
                return;
            }

            await next();
        }
    }
}

public class RequireDebtorOnboardedAttribute : TypeFilterAttribute
{
    public RequireDebtorOnboardedAttribute() : base(typeof(RequireDebtorOnboardedFilter)) { }

    private class RequireDebtorOnboardedFilter : IAsyncActionFilter
    {
        private readonly AppDbContext _db;
        public RequireDebtorOnboardedFilter(AppDbContext db) => _db = db;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (!(user?.Identity?.IsAuthenticated ?? false))
            {
                await next();
                return;
            }

            if (!user.IsInRole("User"))
            {
                await next();
                return;
            }

            var uid = user.FindFirstValue("oid") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(uid))
            {
                await next();
                return;
            }

            var appUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.ExternalAuthId == uid);
            if (appUser == null)
            {
                await next();
                return;
            }

            var profile = await _db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == appUser.Id);
            if (profile?.DebtorId == null)
            {
                var factory = context.HttpContext.RequestServices.GetService(typeof(ITempDataDictionaryFactory)) as ITempDataDictionaryFactory;
                var tempData = factory?.GetTempData(context.HttpContext);
                if (tempData != null)
                {
                    tempData["OnboardingInfo"] = "Please complete your profile to access the User portal.";
                }
                context.Result = new RedirectResult("/User/Onboarding");
                return;
            }

            await next();
        }
    }
}
