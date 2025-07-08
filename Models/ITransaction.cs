namespace TransactionService.Models;

public interface ITransaction
{
    Guid Id { get; }
    Guid ClientId { get; }
    DateTime DateTime { get; }
    decimal Amount { get; }
}