using MediatR;
using Ledger.Domain.Models;

namespace Ledger.Application.Features.GetStatement;

public record GetStatementQuery(Guid AccountId, DateTime? PeriodStart = null, DateTime? PeriodEnd = null) : IRequest<StatementDto?>;
