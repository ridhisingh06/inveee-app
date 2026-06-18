namespace invmgmt.web.DTOs
{
    /// <summary>
    /// DTO for increasing or decreasing stock quantity
    /// </summary>
    public class StockChangeDto
    {
        /// <summary>
        /// Quantity to increase or decrease
        /// </summary>
        public int Quantity { get; set; }
    }
}
