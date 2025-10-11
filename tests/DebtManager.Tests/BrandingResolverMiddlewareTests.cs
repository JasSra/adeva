using DebtManager.Web.Services;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using System.Threading.Tasks;

namespace DebtManager.Tests;

public class BrandingResolverMiddlewareTests
{
    [Test]
    public async Task Sets_Default_Theme_When_No_Subdomain()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Host = new HostString("domain.com");
        var mw = new BrandingResolverMiddleware();
        await mw.InvokeAsync(ctx, _ => Task.CompletedTask);
        Assert.That(ctx.Items.ContainsKey(BrandingResolverMiddleware.ThemeItemKey), Is.True);
        var theme = ctx.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        Assert.That(theme, Is.Not.Null);
        Assert.That(theme!.Name, Is.EqualTo(BrandingTheme.Default.Name));
    }

    [Test]
    public async Task Sets_Client1_Theme_For_Client1_Subdomain()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Host = new HostString("client1.domain.com");
        var mw = new BrandingResolverMiddleware();
        await mw.InvokeAsync(ctx, _ => Task.CompletedTask);
        var theme = ctx.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        Assert.That(theme, Is.Not.Null);
        Assert.That(theme!.Name, Is.EqualTo("Client One"));
    }
}
