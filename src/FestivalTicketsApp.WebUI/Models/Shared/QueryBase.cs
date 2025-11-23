namespace FestivalTicketsApp.WebUI.Models.Shared;

public abstract class QueryBase
{
    public int PageNum { get; set; } = RequestDefaults.PageNum;

    public int PageSize { get; set; } = RequestDefaults.PageSize;
}
