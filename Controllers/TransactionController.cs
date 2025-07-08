using Microsoft.AspNetCore.Mvc;
using TransactionService.DTOs;
using TransactionService.Services;

namespace TransactionService.Controllers;

[ApiController]
[Route("")]
[Produces("application/json")]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ILogger<TransactionController> _logger;

    public TransactionController(ITransactionService transactionService, ILogger<TransactionController> logger)
    {
        _transactionService = transactionService;
        _logger = logger;
    }

    /// <summary>
    /// Зачислить деньги на счет клиента
    /// </summary>
    [HttpPost("credit")]
    [ProducesResponseType(typeof(TransactionResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> Credit([FromBody] TransactionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(CreateErrorResponse("Ошибка валидации", 400, 
                    string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))));
            }

            var result = await _transactionService.ProcessCreditAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Ошибка валидации в кредитной транзакции");
            return BadRequest(CreateErrorResponse("Ошибка валидации", 400, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки кредитной транзакции");
            return StatusCode(500, CreateErrorResponse("Внутренняя ошибка сервера", 500, "Произошла ошибка при обработке запроса"));
        }
    }

    /// <summary>
    /// Списать деньги со счета клиента
    /// </summary>
    [HttpPost("debit")]
    [ProducesResponseType(typeof(TransactionResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> Debit([FromBody] TransactionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(CreateErrorResponse("Ошибка валидации", 400, 
                    string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))));
            }

            var result = await _transactionService.ProcessDebitAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Ошибка валидации в дебетовой транзакции");
            return BadRequest(CreateErrorResponse("Ошибка валидации", 400, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Недостаточно средств для дебетовой транзакции");
            return BadRequest(CreateErrorResponse("Недостаточно средств", 400, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки дебетовой транзакции");
            return StatusCode(500, CreateErrorResponse("Внутренняя ошибка сервера", 500, "Произошла ошибка при обработке запроса"));
        }
    }

    /// <summary>
    /// Отменить транзакцию
    /// </summary>
    [HttpPost("revert")]
    [ProducesResponseType(typeof(RevertResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> Revert([FromQuery] Guid id)
    {
        try
        {
            var result = await _transactionService.RevertTransactionAsync(id);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Транзакция для отмены не найдена");
            return NotFound(CreateErrorResponse("Транзакция не найдена", 404, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отмены транзакции");
            return StatusCode(500, CreateErrorResponse("Внутренняя ошибка сервера", 500, "Произошла ошибка при обработке запроса"));
        }
    }

    /// <summary>
    /// Получить баланс клиента
    /// </summary>
    [HttpGet("balance")]
    [ProducesResponseType(typeof(BalanceResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> GetBalance([FromQuery] Guid id)
    {
        try
        {
            var result = await _transactionService.GetBalanceAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения баланса");
            return StatusCode(500, CreateErrorResponse("Внутренняя ошибка сервера", 500, "Произошла ошибка при обработке запроса"));
        }
    }

    private ErrorResponse CreateErrorResponse(string title, int status, string detail)
    {
        return new ErrorResponse
        {
            Type = "about:blank",
            Title = title,
            Status = status,
            Detail = detail,
            Instance = HttpContext.Request.Path
        };
    }
}