namespace invmgmt.web.DTOs;

public class ReorderSuggestion
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int SuggestedQuantity { get; set; }
}
