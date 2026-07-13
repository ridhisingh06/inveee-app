namespace invmgmt.web.DTOs;

public class ReorderSuggestion
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int SuggestedQuantity { get; set; }
}
