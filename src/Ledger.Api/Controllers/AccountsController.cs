using MediatR;
using Ledger.Application.Features.OpenAccount;
using Ledger.Application.Features.Deposit;
using Ledger.Application.Features.Withdraw;
using Ledger.Application.Features.Transfer;
using Ledger.Application.Features.Reverse;
using Ledger.Application.Features.GetAllAccounts;
using Ledger.Application.Features.GetAccountBalance;
using Ledger.Application.Features.GetTransactionHistory;
using Ledger.Application.Features.GetStatement;
using Ledger.Application.Features.GetPointInTimeBalance;
using Ledger.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Ledger.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> OpenAccount([FromBody] OpenAccountRequest request, CancellationToken ct)
    {
        var command = new OpenAccountCommand(request.AccountId, request.AccountType, request.IdempotencyKey);
        var result = await _mediator.Send(command, ct);

        if (!result.Success)
            return Conflict(new { error = result.Error });

        return Ok(new { result.AggregateId, result.NewVersion });
    }

    [HttpPost("{accountId:guid}/deposit")]
    public async Task<IActionResult> Deposit(Guid accountId, [FromBody] DepositRequest request, CancellationToken ct)
    {
        var command = new DepositCommand(accountId, request.Amount, request.IdempotencyKey);
        var result = await _mediator.Send(command, ct);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new { result.AggregateId, result.NewVersion });
    }

    [HttpPost("{accountId:guid}/withdraw")]
    public async Task<IActionResult> Withdraw(Guid accountId, [FromBody] WithdrawRequest request, CancellationToken ct)
    {
        var command = new WithdrawCommand(accountId, request.Amount, request.IdempotencyKey);
        var result = await _mediator.Send(command, ct);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new { result.AggregateId, result.NewVersion });
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request, CancellationToken ct)
    {
        var command = new TransferCommand(request.FromAccountId, request.ToAccountId, request.Amount, request.IdempotencyKey);
        var result = await _mediator.Send(command, ct);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new { result.AggregateId, result.NewVersion });
    }

    [HttpPost("reverse")]
    public async Task<IActionResult> ReverseTransaction([FromBody] ReverseRequest request, CancellationToken ct)
    {
        var command = new ReverseTransactionCommand(request.TransactionId, request.IdempotencyKey);
        var result = await _mediator.Send(command, ct);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new { result.AggregateId, result.NewVersion });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var query = new GetAllAccountsQuery();
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpGet("{accountId:guid}")]
    public async Task<IActionResult> GetBalance(Guid accountId, CancellationToken ct)
    {
        var query = new GetAccountBalanceQuery(accountId);
        var result = await _mediator.Send(query, ct);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("{accountId:guid}/history")]
    public async Task<IActionResult> GetHistory(Guid accountId, CancellationToken ct)
    {
        var query = new GetTransactionHistoryQuery(accountId);
        var result = await _mediator.Send(query, ct);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("{accountId:guid}/statement")]
    public async Task<IActionResult> GetStatement(Guid accountId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var query = new GetStatementQuery(accountId, from, to);
        var result = await _mediator.Send(query, ct);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("{accountId:guid}/balance-at")]
    public async Task<IActionResult> GetBalanceAt(Guid accountId, [FromQuery] DateTime asOf, CancellationToken ct)
    {
        var query = new GetPointInTimeBalanceQuery(accountId, asOf);
        var result = await _mediator.Send(query, ct);
        return Ok(new { accountId, asOf, balance = result });
    }
}

public record OpenAccountRequest(Guid AccountId, AccountType AccountType, string IdempotencyKey);
public record DepositRequest(decimal Amount, string IdempotencyKey);
public record WithdrawRequest(decimal Amount, string IdempotencyKey);
public record TransferRequest(Guid FromAccountId, Guid ToAccountId, decimal Amount, string IdempotencyKey);
public record ReverseRequest(Guid TransactionId, string IdempotencyKey);
