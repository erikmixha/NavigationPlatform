using Journey.Application.DTOs;
using MediatR;
using Shared.Common.Result;

namespace Journey.Application.Queries.GetJourneyByPublicLink;

public sealed record GetJourneyByPublicLinkQuery(string Token) : IRequest<Result<JourneyDto>>;

