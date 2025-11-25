using FluentValidation;

namespace Journey.Application.Commands.UpdateJourney;

public sealed class UpdateJourneyCommandValidator : AbstractValidator<UpdateJourneyCommand>
{
    public UpdateJourneyCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Journey ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.StartLocation)
            .NotEmpty().WithMessage("Start location is required")
            .MaximumLength(200).WithMessage("Start location cannot exceed 200 characters");

        RuleFor(x => x.ArrivalLocation)
            .NotEmpty().WithMessage("Arrival location is required")
            .MaximumLength(200).WithMessage("Arrival location cannot exceed 200 characters");

        RuleFor(x => x.ArrivalTime)
            .GreaterThan(x => x.StartTime).WithMessage("Arrival time must be after start time");

        RuleFor(x => x.TransportType)
            .NotEmpty().WithMessage("Transport type is required");

        RuleFor(x => x.DistanceKm)
            .GreaterThanOrEqualTo(0).WithMessage("Distance cannot be negative")
            .LessThanOrEqualTo(2147483647m).WithMessage("Distance cannot exceed 2,147,483,647 km");
    }
}

