using System.ComponentModel.DataAnnotations;

namespace TransactionService.Models;

public class CreditTransaction : ITransaction
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid ClientId { get; set; }
    
    [Required]
    public DateTime DateTime { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Сумма должна быть положительной")]
    public decimal Amount { get; set; }
    
    public DateTime InsertDateTime { get; set; }
    
    public bool IsReverted { get; set; }
    
    public DateTime? RevertDateTime { get; set; }
}