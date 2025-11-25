using Gateway.Application.DTOs;
using MediatR;
using Shared.Common.Result;

namespace Gateway.Application.Queries.GetUsersWithStatus;

/// <summary>
/// Query to get all users with their account status for admin operations.
/// </summary>
public sealed record GetUsersWithStatusQuery : IRequest<Result<IEnumerable<UserWithStatusDto>>>;

