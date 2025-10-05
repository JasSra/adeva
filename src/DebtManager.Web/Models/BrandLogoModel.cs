namespace DebtManager.Web.Models;

public class BrandLogoModel
{
    public string Variant { get; set; } = "full"; // full | circle | mark
    public string Size { get; set; } = "md"; // xs, sm, md, lg, xl
    public bool AsLink { get; set; } = false;
    public string LinkHref { get; set; } = "/";
    public string? Title { get; set; } = "Adeva Plus";
}
