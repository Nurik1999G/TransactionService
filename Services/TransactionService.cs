using Microsoft.EntityFrameworkCore;
using TransactionService.Data;
using TransactionService.DTOs;
using TransactionService.Models;

namespace TransactionService.Services;

public interface ITransactionService
{
    Task<TransactionResponse> ProcessCreditAsync(TransactionRequest request);
    Task<TransactionResponse> ProcessDebitAsync(TransactionRequest request);
    Task<RevertResponse> RevertTransactionAsync(Guid transactionId);
    Task<BalanceResponse> GetBalanceAsync(Guid clientId);
}

public class TransactionServiceImpl : ITransactionService
{
    private readonly TransactionDbContext _context;
    private readonly ILogger<TransactionServiceImpl> _logger;

    public TransactionServiceImpl(TransactionDbContext context, ILogger<TransactionServiceImpl> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TransactionResponse> ProcessCreditAsync(TransactionRequest request)
    {
        // Валидация
        ValidateTransaction(request);

        try
        {
            // Проверка уникальности глобального ID (в обеих таблицах)
            var existsInCredit = await _context.CreditTransactions
                .AnyAsync(t => t.Id == request.Id);
            var existsInDebit = await _context.DebitTransactions
                .AnyAsync(t => t.Id == request.Id);

            if (existsInCredit)
            {
                // Возврат кешированного результата для существующего кредита
                var existingCredit = await _context.CreditTransactions
                    .FirstAsync(t => t.Id == request.Id);
                var balance = await CalculateClientBalanceAsync(request.ClientId);
                return new TransactionResponse
                {
                    InsertDateTime = existingCredit.InsertDateTime,
                    ClientBalance = balance
                };
            }

            if (existsInDebit)
            {
                throw new InvalidOperationException($"Транзакция с ID {request.Id} уже существует как дебетовая транзакция");
            }

            // Создание новой кредитной транзакции
            var creditTransaction = new CreditTransaction
            {
                Id = request.Id,
                ClientId = request.ClientId,
                DateTime = request.DateTime,
                Amount = request.Amount,
                InsertDateTime = DateTime.UtcNow,
                IsReverted = false
            };

            _context.CreditTransactions.Add(creditTransaction);
            await _context.SaveChangesAsync();

            var newBalance = await CalculateClientBalanceAsync(request.ClientId);

            _logger.LogInformation("Кредитная транзакция {TransactionId} обработана для клиента {ClientId}", 
                request.Id, request.ClientId);

            return new TransactionResponse
            {
                InsertDateTime = creditTransaction.InsertDateTime,
                ClientBalance = newBalance
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки кредитной транзакции {TransactionId}", request.Id);
            throw;
        }
    }

    public async Task<TransactionResponse> ProcessDebitAsync(TransactionRequest request)
    {
        // Валидация
        ValidateTransaction(request);

        try
        {
            // Проверка уникальности глобального ID (в обеих таблицах)
            var existsInCredit = await _context.CreditTransactions
                .AnyAsync(t => t.Id == request.Id);
            var existsInDebit = await _context.DebitTransactions
                .AnyAsync(t => t.Id == request.Id);

            if (existsInDebit)
            {
                // Возврат кешированного результата для существующего дебета
                var existingDebit = await _context.DebitTransactions
                    .FirstAsync(t => t.Id == request.Id);
                var balance = await CalculateClientBalanceAsync(request.ClientId);
                return new TransactionResponse
                {
                    InsertDateTime = existingDebit.InsertDateTime,
                    ClientBalance = balance
                };
            }

            if (existsInCredit)
            {
                throw new InvalidOperationException($"Транзакция с ID {request.Id} уже существует как кредитная транзакция");
            }

            // Проверка достаточности средств у клиента
            var currentBalance = await CalculateClientBalanceAsync(request.ClientId);
            if (currentBalance < request.Amount)
            {
                throw new InvalidOperationException($"Недостаточно средств. Текущий баланс: {currentBalance}, Требуется: {request.Amount}");
            }

            // Создание новой дебетовой транзакции
            var debitTransaction = new DebitTransaction
            {
                Id = request.Id,
                ClientId = request.ClientId,
                DateTime = request.DateTime,
                Amount = request.Amount,
                InsertDateTime = DateTime.UtcNow,
                IsReverted = false
            };

            _context.DebitTransactions.Add(debitTransaction);
            await _context.SaveChangesAsync();

            var newBalance = await CalculateClientBalanceAsync(request.ClientId);

            _logger.LogInformation("Дебетовая транзакция {TransactionId} обработана для клиента {ClientId}", 
                request.Id, request.ClientId);

            return new TransactionResponse
            {
                InsertDateTime = debitTransaction.InsertDateTime,
                ClientBalance = newBalance
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки дебетовой транзакции {TransactionId}", request.Id);
            throw;
        }
    }

    public async Task<RevertResponse> RevertTransactionAsync(Guid transactionId)
    {
        try
        {
            // Попытка найти кредитную транзакцию
            var creditTransaction = await _context.CreditTransactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (creditTransaction != null)
            {
                if (creditTransaction.IsReverted)
                {
                    // Уже отменена, возврат кешированного результата
                    var balance = await CalculateClientBalanceAsync(creditTransaction.ClientId);
                    return new RevertResponse
                    {
                        RevertDateTime = creditTransaction.RevertDateTime!.Value,
                        ClientBalance = balance
                    };
                }

                creditTransaction.IsReverted = true;
                creditTransaction.RevertDateTime = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var newBalance = await CalculateClientBalanceAsync(creditTransaction.ClientId);

                _logger.LogInformation("Кредитная транзакция {TransactionId} отменена", transactionId);

                return new RevertResponse
                {
                    RevertDateTime = creditTransaction.RevertDateTime.Value,
                    ClientBalance = newBalance
                };
            }

            // Попытка найти дебетовую транзакцию
            var debitTransaction = await _context.DebitTransactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (debitTransaction != null)
            {
                if (debitTransaction.IsReverted)
                {
                    // Уже отменена, возврат кешированного результата
                    var balance = await CalculateClientBalanceAsync(debitTransaction.ClientId);
                    return new RevertResponse
                    {
                        RevertDateTime = debitTransaction.RevertDateTime!.Value,
                        ClientBalance = balance
                    };
                }

                debitTransaction.IsReverted = true;
                debitTransaction.RevertDateTime = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var newBalance = await CalculateClientBalanceAsync(debitTransaction.ClientId);

                _logger.LogInformation("Дебетовая транзакция {TransactionId} отменена", transactionId);

                return new RevertResponse
                {
                    RevertDateTime = debitTransaction.RevertDateTime.Value,
                    ClientBalance = newBalance
                };
            }

            throw new ArgumentException($"Транзакция с ID {transactionId} не найдена");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отмены транзакции {TransactionId}", transactionId);
            throw;
        }
    }

    public async Task<BalanceResponse> GetBalanceAsync(Guid clientId)
    {
        var balance = await CalculateClientBalanceAsync(clientId);

        return new BalanceResponse
        {
            BalanceDateTime = DateTime.UtcNow,
            ClientBalance = balance
        };
    }

    private async Task<decimal> CalculateClientBalanceAsync(Guid clientId)
    {
        // Расчет кредитов (не отмененных)
        var totalCredits = await _context.CreditTransactions
            .Where(t => t.ClientId == clientId && !t.IsReverted)
            .SumAsync(t => t.Amount);

        // Расчет дебетов (не отмененных)
        var totalDebits = await _context.DebitTransactions
            .Where(t => t.ClientId == clientId && !t.IsReverted)
            .SumAsync(t => t.Amount);

        return totalCredits - totalDebits;
    }

    private static void ValidateTransaction(TransactionRequest request)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Сумма должна быть положительной");

        if (request.DateTime > DateTime.UtcNow)
            throw new ArgumentException("Дата транзакции не может быть в будущем");
    }
}