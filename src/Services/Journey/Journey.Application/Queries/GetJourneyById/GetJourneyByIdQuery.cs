using Journey.Application.DTOs;
using MediatR;
using Shared.Common.Result;

namespace Journey.Application.Queries.GetJourneyById;

public sealed record GetJourneyByIdQuery : IRequest<Result<JourneyDto>>
{
    public Guid Id { get; init; }
    public string UserId { get; init; } = string.Empty;
    public bool IsAdmin { get; init; } = false;
}

