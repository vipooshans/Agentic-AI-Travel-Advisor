using System.Text.RegularExpressions;
using FluentValidation;
using TravelAdvisor.Core.DTOs.Payments;
using TravelAdvisor.Core.DTOs.Reports;
using TravelAdvisor.Core.DTOs.Reviews;
using TravelAdvisor.Core.DTOs.Settings;
using TravelAdvisor.Core.DTOs.Transportation;
using TravelAdvisor.Core.DTOs.Users;
using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.Validation;

public sealed class CreateReviewRequestValidator : AbstractValidator<CreateReviewRequest>
{
    public CreateReviewRequestValidator()
    {
        RuleFor(x => x.BookingId).GreaterThan(0).WithMessage("BookingId is required.");
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");
        RuleFor(x => x.Comment).MaximumLength(2000);
    }
}

public sealed class UpdateReviewRequestValidator : AbstractValidator<UpdateReviewRequest>
{
    public UpdateReviewRequestValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");
        RuleFor(x => x.Comment).MaximumLength(2000);
    }
}

public sealed class UpdateReviewStatusRequestValidator : AbstractValidator<UpdateReviewStatusRequest>
{
    public UpdateReviewStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithMessage("Unknown review status.");
    }
}

public sealed partial class SaveTransportationRequestValidator : AbstractValidator<SaveTransportationRequest>
{
    public SaveTransportationRequestValidator()
    {
        RuleFor(x => x.Mode).IsInEnum().WithMessage("Unknown transport mode.");
        RuleFor(x => x.FromLocation).NotEmpty().WithMessage("From location is required.").MaximumLength(150);
        RuleFor(x => x.ToLocation).NotEmpty().WithMessage("To location is required.").MaximumLength(150);
        RuleFor(x => x.DepartureTime)
            .Must(t => string.IsNullOrWhiteSpace(t) || TimeOfDay().IsMatch(t))
            .WithMessage("Departure time must be HH:mm (24-hour).");
        RuleFor(x => x.DurationMinutes).InclusiveBetween(1, 4320).WithMessage("Duration must be between 1 and 4320 minutes.");
        RuleFor(x => x.PricePerPerson).GreaterThanOrEqualTo(0).LessThanOrEqualTo(10_000_000);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 500).WithMessage("Capacity must be between 1 and 500.");
        RuleFor(x => x.Description).MaximumLength(1000);
    }

    [GeneratedRegex(@"^([01]\d|2[0-3]):[0-5]\d$")]
    private static partial Regex TimeOfDay();
}

public sealed class UpdateSystemSettingRequestValidator : AbstractValidator<UpdateSystemSettingRequest>
{
    public UpdateSystemSettingRequestValidator()
    {
        RuleFor(x => x.Value).NotNull().MaximumLength(1000);
    }
}

public sealed class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.Method).IsInEnum().WithMessage("Unknown payment method.");
        RuleFor(x => x.CardNumber)
            .Must(BeValidCardNumber).When(x => x.Method == PaymentMethod.Card)
            .WithMessage("Card number must be 12-19 digits and pass the checksum.");
        RuleFor(x => x.CardNumber)
            .Empty().When(x => x.Method != PaymentMethod.Card)
            .WithMessage("Card number is only accepted for card payments.");
    }

    /// <summary>Shape and Luhn checksum only; this is a simulated payment and nothing is charged.</summary>
    internal static bool BeValidCardNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        var compact = value.Replace(" ", string.Empty).Replace("-", string.Empty);
        if (compact.Length is < 12 or > 19 || !compact.All(char.IsAsciiDigit))
            return false;

        var sum = 0;
        for (var i = 0; i < compact.Length; i++)
        {
            var digit = compact[compact.Length - 1 - i] - '0';
            if (i % 2 == 1)
            {
                digit *= 2;
                if (digit > 9)
                    digit -= 9;
            }
            sum += digit;
        }
        return sum % 10 == 0;
    }
}

public sealed class UpdatePaymentStatusRequestValidator : AbstractValidator<UpdatePaymentStatusRequest>
{
    public UpdatePaymentStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithMessage("Unknown payment status.");
    }
}

public sealed partial class UserProfileDtoValidator : AbstractValidator<UserProfileDto>
{
    public UserProfileDtoValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .Must(p => string.IsNullOrWhiteSpace(p) || Phone().IsMatch(p.Trim()))
            .WithMessage("Phone number may contain digits, spaces, (), - and a leading +, 6-32 characters.");
        RuleFor(x => x.Nationality).MaximumLength(64);
        RuleFor(x => x.DateOfBirth)
            .Must(d => d is null || (d.Value.Year >= 1900 && d.Value < DateOnly.FromDateTime(DateTime.UtcNow)))
            .WithMessage("Date of birth must be in the past.");
        RuleFor(x => x.AvatarUrl).OptionalHttpUrl();
        RuleFor(x => x.Bio).MaximumLength(1000);
        RuleFor(x => x.PreferredCurrency)
            .Must(c => string.IsNullOrWhiteSpace(c) || Currency().IsMatch(c.Trim().ToUpperInvariant()))
            .WithMessage("Preferred currency must be a 3-letter code.");
    }

    [GeneratedRegex(@"^\+?[0-9 ()\-]{6,32}$")]
    private static partial Regex Phone();

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex Currency();
}

public sealed class StatisticsQueryValidator : AbstractValidator<StatisticsQuery>
{
    public StatisticsQueryValidator()
    {
        RuleFor(x => x.Top).InclusiveBetween(1, 20).WithMessage("Top must be between 1 and 20.");
    }
}
