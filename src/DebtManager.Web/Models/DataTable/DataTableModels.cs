namespace DebtManager.Web.Models.DataTable;

public class DataTableColumn
{
    public string Id { get; set; } = string.Empty; // key used in rows
    public string Title { get; set; } = string.Empty;
    public bool Visible { get; set; } = true;
    public bool Sortable { get; set; } = true;
    public string? Width { get; set; }
    public string? Align { get; set; } // left, right, center
    public bool HideOnMobile { get; set; }

    public DataTableColumn() { }
    public DataTableColumn(string id, string title, bool visible = true)
    {
        Id = id; Title = title; Visible = visible;
    }
}

public class DataTableRow
{
    public string Key { get; set; } = string.Empty; // e.g., id for selection
    public Dictionary<string, string> Cells { get; set; } = new(); // columnId -> html/text
    public string? ActionsHtml { get; set; }
}

public class DataTableViewModel
{
    public string TableKey { get; set; } = Guid.NewGuid().ToString("N"); // used for localStorage
    public string Title { get; set; } = string.Empty;
    public List<DataTableColumn> Columns { get; set; } = new();
    public List<DataTableRow> Rows { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public bool ShowSearch { get; set; } = true;
    public bool ShowStatusFilter { get; set; }
    public string? StatusFilterLabel { get; set; }
    public List<(string Value, string Label)> StatusOptions { get; set; } = new();
    public bool ShowDensityToggle { get; set; } = true; // compact vs normal

    // Optional remote source for rows (JSON)
    public string? DataUrl { get; set; }

    public DataTableViewModel() { }
    public DataTableViewModel(string title)
    {
        Title = title;
    }
}
