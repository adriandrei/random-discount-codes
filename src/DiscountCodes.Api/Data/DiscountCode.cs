using System.ComponentModel.DataAnnotations;

public class DiscountCode
{
    [Key] 
    public string Code { get; set; } = null!;

    public bool IsUsed { get; set; }
    [Timestamp] 
    public byte[] RowVersion { get; set; } = null!;
}