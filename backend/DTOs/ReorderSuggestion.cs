namespace invmgmt.web.DTOs;

public class ReorderSuggestion
{
    public string ItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int SuggestedQuantity { get; set; }
}
