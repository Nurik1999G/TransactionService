using System.ComponentModel.DataAnnotations;

namespace TransactionService.DTOs;

public class TransactionRequest
{
    [Required]
    public Guid Id { get; set; }
    
    [Required]
    public Guid ClientId { get; set; }
    
    [Required]
    public DateTime DateTime { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Сумма должна быть положительной")]
    public decimal Amount { get; set; }
}

public class TransactionResponse
{
    public DateTime InsertDateTime { get; set; }
    public decimal ClientBalance { get; set; }
}

public class RevertResponse
{
    public DateTime RevertDateTime { get; set; }
    public decimal ClientBalance { get; set; }
}

public class BalanceResponse
{
    public DateTime BalanceDateTime { get; set; }
    public decimal ClientBalance { get; set; }
}

public class ErrorResponse
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Detail { get; set; } = string.Empty;
    public string Instance { get; set; } = string.Empty;
}