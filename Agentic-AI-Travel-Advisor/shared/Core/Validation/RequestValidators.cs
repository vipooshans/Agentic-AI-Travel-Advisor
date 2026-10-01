using FluentValidation;
using TravelAdvisor.Core.DTOs.AI;
using TravelAdvisor.Core.DTOs.Auth;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.DTOs.Destinations;
using TravelAdvisor.Core.DTOs.Hotels;
using TravelAdvisor.Core.DTOs.Itineraries;
using TravelAdvisor.Core.DTOs.Packages;
using TravelAdvisor.Core.DTOs.Users;
using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.Validation;

internal static class Rules
{
    public static IRuleBuilderOptions<T, string> Email<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Email is required.")
            .MaximumLength(256)
            .EmailAddress().WithMessage("Email must be a valid email address.");

    public static IRuleBuilderOptions<T, string> PersonName<T>(this IRuleBuilder<T, string> rule, string label) =>
        rule.NotEmpty().WithMessage($"{label} is required.")
            .MaximumLength(100);

    public static IRuleBuilderOptions<T, string?> OptionalHttpUrl<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(500)
            .Must(BeHttpUrlOrEmpty).WithMessage("Image URL must be an http or https address.");

    private static bool BeHttpUrlOrEmpty(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;
        return Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
    }
}

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).Email();
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).MaximumLength(128);
        RuleFor(x => x.FirstName).PersonName("First name");
        RuleFor(x => x.LastName).PersonName("Last name");
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).Email();
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.").MaximumLength(128);
    }
}

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FirstName).PersonName("First name");
        RuleFor(x => x.LastName).PersonName("Last name");
    }
}

public sealed class CreateStaffUserRequestValidator : AbstractValidator<CreateStaffUserRequest>
{
    public CreateStaffUserRequestValidator()
    {
        RuleFor(x => x.Email).Email();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(128);
        RuleFor(x => x.FirstName).PersonName("First name");
        RuleFor(x => x.LastName).PersonName("Last name");
        RuleFor(x => x.Role).Must(r => r is RoleNames.HotelOwner or RoleNames.TravelAgent)
            .WithMessage("Role must be HOTEL_OWNER or TRAVEL_AGENT.");
    }
}

public sealed class TravelPreferencesValidator : AbstractValidator<TravelPreferencesDto>
{
    public TravelPreferencesValidator()
    {
        RuleFor(x => x.BudgetMin).GreaterThanOrEqualTo(0).When(x => x.BudgetMin.HasValue);
        RuleFor(x => x.BudgetMax).GreaterThanOrEqualTo(0).When(x => x.BudgetMax.HasValue);
        RuleFor(x => x.BudgetMax).GreaterThanOrEqualTo(x => x.BudgetMin!.Value)
            .When(x => x.BudgetMin.HasValue && x.BudgetMax.HasValue)
            .WithMessage("Maximum budget must be greater than or equal to the minimum budget.");
        RuleFor(x => x.PreferredClimate).MaximumLength(64);
        RuleFor(x => x.Interests).MaximumLength(500);
    }
}

public sealed class CreateHotelRequestValidator : AbstractValidator<CreateHotelRequest>
{
    public CreateHotelRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(250);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ImageUrl).OptionalHttpUrl();
    }
}

public sealed class UpdateHotelRequestValidator : AbstractValidator<UpdateHotelRequest>
{
    public UpdateHotelRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(250);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ImageUrl).OptionalHttpUrl();
    }
}

public sealed class CreateRoomRequestValidator : AbstractValidator<CreateRoomRequest>
{
    public CreateRoomRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.RoomType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PricePerNight).GreaterThan(0).LessThanOrEqualTo(10_000_000);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 20);
    }
}

public sealed class UpdateRoomRequestValidator : AbstractValidator<UpdateRoomRequest>
{
    public UpdateRoomRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.RoomType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PricePerNight).GreaterThan(0).LessThanOrEqualTo(10_000_000);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 20);
    }
}

public sealed class UpdateApprovalRequestValidator : AbstractValidator<UpdateApprovalRequest>
{
    public UpdateApprovalRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum()
            .Must(s => s is ApprovalStatus.Approved or ApprovalStatus.Rejected)
            .WithMessage("Status must be Approved or Rejected.");
    }
}

public sealed class CreatePackageRequestValidator : AbstractValidator<CreatePackageRequest>
{
    public CreatePackageRequestValidator()
    {
        RuleFor(x => x.DestinationId).GreaterThan(0).WithMessage("Destination is required.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Price).GreaterThan(0).LessThanOrEqualTo(100_000_000);
        RuleFor(x => x.DurationDays).InclusiveBetween(1, 60);
        RuleFor(x => x.MaxTravelers).InclusiveBetween(1, 500);
        RuleFor(x => x.ImageUrl).OptionalHttpUrl();
    }
}

public sealed class UpdatePackageRequestValidator : AbstractValidator<UpdatePackageRequest>
{
    public UpdatePackageRequestValidator()
    {
        RuleFor(x => x.DestinationId).GreaterThan(0).WithMessage("Destination is required.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Price).GreaterThan(0).LessThanOrEqualTo(100_000_000);
        RuleFor(x => x.DurationDays).InclusiveBetween(1, 60);
        RuleFor(x => x.MaxTravelers).InclusiveBetween(1, 500);
        RuleFor(x => x.ImageUrl).OptionalHttpUrl();
    }
}

public sealed class CreateActivityRequestValidator : AbstractValidator<CreateActivityRequest>
{
    public CreateActivityRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DayNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).LessThanOrEqualTo(10_000_000);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class SaveDestinationRequestValidator : AbstractValidator<SaveDestinationRequest>
{
    public SaveDestinationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100);
        RuleFor(x => x.Country).NotEmpty().WithMessage("Country is required.").MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ImageUrl).OptionalHttpUrl();
    }
}

public sealed class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => x).Must(x => x.RoomId.HasValue != x.TravelPackageId.HasValue)
            .WithName("roomId")
            .WithMessage("Specify either a room or a travel package, not both.");
        RuleFor(x => x.CheckIn).NotEqual(default(DateTime)).WithMessage("CheckIn is required.");
        RuleFor(x => x.CheckOut).NotNull().When(x => x.RoomId.HasValue)
            .WithMessage("CheckOut is required for room bookings.");
        RuleFor(x => x.CheckOut!.Value).GreaterThan(x => x.CheckIn)
            .When(x => x.RoomId.HasValue && x.CheckOut.HasValue)
            .WithName("checkOut")
            .WithMessage("CheckOut must be after CheckIn.");
    }
}

public sealed class UpdateBookingStatusRequestValidator : AbstractValidator<UpdateBookingStatusRequest>
{
    public UpdateBookingStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithMessage("Unknown booking status.");
    }
}

public sealed class CreateItineraryRequestValidator : AbstractValidator<CreateItineraryRequest>
{
    public CreateItineraryRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.").MaximumLength(200);
        RuleFor(x => x.StartDate).NotEqual(default(DateTime)).WithMessage("Start date is required.");
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date must be on or after the start date.");
        RuleFor(x => x.EstimatedCost).GreaterThanOrEqualTo(0).When(x => x.EstimatedCost.HasValue);
        RuleFor(x => x.Summary).MaximumLength(4000);
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one itinerary item is required.");
        RuleFor(x => x.Items.Count).LessThanOrEqualTo(200).WithName("items");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.DayNumber).GreaterThanOrEqualTo(1);
            item.RuleFor(i => i.Title).NotEmpty().MaximumLength(200);
            item.RuleFor(i => i.Description).MaximumLength(2000);
        });
    }
}

public sealed class ChatRequestValidator : AbstractValidator<ChatRequest>
{
    public const int MaxMessageLength = 2000;

    public ChatRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().WithMessage("Message is required.")
            .MaximumLength(MaxMessageLength).WithMessage($"Message must be {MaxMessageLength} characters or fewer.");
        RuleFor(x => x.ConversationId).GreaterThan(0).When(x => x.ConversationId.HasValue);
    }
}
