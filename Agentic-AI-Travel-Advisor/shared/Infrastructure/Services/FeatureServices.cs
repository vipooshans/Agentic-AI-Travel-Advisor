using System.Globalization;
using System.Security.Cryptography;
using TravelAdvisor.Core.Common;
using TravelAdvisor.Core.DTOs.Payments;
using TravelAdvisor.Core.DTOs.Reviews;
using TravelAdvisor.Core.DTOs.Settings;
using TravelAdvisor.Core.DTOs.Transportation;
using TravelAdvisor.Core.DTOs.Users;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Core.Interfaces.Repositories;
using TravelAdvisor.Core.Interfaces.Services;
using TravelAdvisor.Infrastructure.Repositories;

namespace TravelAdvisor.Infrastructure.Services;

public sealed class ReviewService(
    IReviewRepository reviews,
    IBookingRepository bookings,
    IHotelRepository hotels,
    IPackageRepository packages,
    IUnitOfWork unitOfWork) : IReviewService
{
    public async Task<ReviewDto> CreateAsync(UserContext caller, CreateReviewRequest request, CancellationToken cancellationToken = default)
    {
        var booking = await bookings.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking is null || booking.UserId != caller.UserId)
            throw new NotFoundException("Booking not found.");
        if (booking.Status != BookingStatus.Completed)
            throw new BusinessRuleException("You can review a booking only after it is completed.");
        if (await reviews.ExistsForBookingAsync(booking.Id, cancellationToken))
            throw new ConflictException("You have already reviewed this booking.");

        var review = new Review
        {
            UserId = caller.UserId,
            BookingId = booking.Id,
            HotelId = booking.Room?.HotelId,
            TravelPackageId = booking.Room is null ? booking.TravelPackageId : null,
            Rating = request.Rating,
            Comment = Ownership.Clean(request.Comment),
            Status = ReviewStatus.Visible
        };
        reviews.Add(review);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await reviews.GetByIdAsync(review.Id, cancellationToken))!.ToDto();
    }

    public async Task<ReviewDto> UpdateAsync(UserContext caller, int id, UpdateReviewRequest request, CancellationToken cancellationToken = default)
    {
        var review = await reviews.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Review not found.");
        if (review.UserId != caller.UserId)
            throw new ForbiddenException();

        review.Rating = request.Rating;
        review.Comment = Ownership.Clean(request.Comment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return review.ToDto();
    }

    public async Task DeleteAsync(UserContext caller, int id, CancellationToken cancellationToken = default)
    {
        var review = await reviews.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Review not found.");
        if (review.UserId != caller.UserId && !caller.IsAdmin)
            throw new ForbiddenException();
        reviews.Remove(review);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<ReviewDto>> ListAsync(UserContext caller, ReviewQuery query, CancellationToken cancellationToken = default)
    {
        var filter = caller.Role switch
        {
            RoleNames.Admin => new ReviewFilter { Status = query.Status, HotelId = query.HotelId, TravelPackageId = query.TravelPackageId },
            RoleNames.HotelOwner => new ReviewFilter { HotelOwnerId = caller.UserId, Status = ReviewStatus.Visible, HotelId = query.HotelId },
            RoleNames.TravelAgent => new ReviewFilter { AgentId = caller.UserId, Status = ReviewStatus.Visible, TravelPackageId = query.TravelPackageId },
            _ => new ReviewFilter { UserId = caller.UserId }
        };
        return (await reviews.ListAsync(filter, cancellationToken)).Select(r => r.ToDto()).ToList();
    }

    public async Task<List<ReviewDto>> ListForHotelAsync(UserContext? caller, int hotelId, CancellationToken cancellationToken = default)
    {
        var hotel = await hotels.GetByIdAsync(hotelId, cancellationToken: cancellationToken);
        if (hotel is null || !Ownership.CanView(caller, hotel.ApprovalStatus, hotel.OwnerId))
            throw new NotFoundException("Hotel not found.");
        return (await reviews.ListAsync(new ReviewFilter { HotelId = hotelId, Status = ReviewStatus.Visible }, cancellationToken))
            .Select(r => r.ToDto()).ToList();
    }

    public async Task<List<ReviewDto>> ListForPackageAsync(UserContext? caller, int packageId, CancellationToken cancellationToken = default)
    {
        var package = await packages.GetByIdAsync(packageId, includeDetails: false, cancellationToken);
        if (package is null || !Ownership.CanView(caller, package.ApprovalStatus, package.AgentId))
            throw new NotFoundException("Package not found.");
        return (await reviews.ListAsync(new ReviewFilter { TravelPackageId = packageId, Status = ReviewStatus.Visible }, cancellationToken))
            .Select(r => r.ToDto()).ToList();
    }

    public async Task<ReviewDto> SetStatusAsync(int id, UpdateReviewStatusRequest request, CancellationToken cancellationToken = default)
    {
        var review = await reviews.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Review not found.");
        review.Status = request.Status;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return review.ToDto();
    }
}

public sealed class TransportationService(
    ITransportationRepository transportation,
    IPackageRepository packages,
    IDestinationRepository destinations,
    IUnitOfWork unitOfWork) : ITransportationService
{
    public async Task<List<TransportationDto>> SearchAsync(TransportationSearchQuery query, CancellationToken cancellationToken = default) =>
        (await transportation.SearchAsync(new TransportationCriteria
        {
            From = query.From,
            To = query.To,
            DestinationId = query.DestinationId,
            TravelPackageId = query.TravelPackageId,
            Mode = query.Mode,
            MaxPrice = query.MaxPrice,
            ActiveOnly = true
        }, cancellationToken)).Select(t => t.ToDto()).ToList();

    public async Task<List<TransportationDto>> ListMineAsync(UserContext caller, CancellationToken cancellationToken = default) =>
        (await transportation.SearchAsync(new TransportationCriteria
        {
            ProviderId = caller.IsAdmin ? null : caller.UserId,
            ActiveOnly = false
        }, cancellationToken)).Select(t => t.ToDto()).ToList();

    public async Task<TransportationDto> GetAsync(UserContext? caller, int id, CancellationToken cancellationToken = default)
    {
        var item = await transportation.GetByIdAsync(id, cancellationToken);
        var canSeeInactive = caller is not null && (caller.IsAdmin || caller.UserId == item?.ProviderId);
        if (item is null || !item.IsActive && !canSeeInactive)
            throw new NotFoundException("Transportation option not found.");
        return item.ToDto();
    }

    public async Task<TransportationDto> CreateAsync(UserContext caller, SaveTransportationRequest request, CancellationToken cancellationToken = default)
    {
        var item = new Transportation { ProviderId = caller.UserId };
        await ApplyAsync(caller, item, request, cancellationToken);
        transportation.Add(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await transportation.GetByIdAsync(item.Id, cancellationToken))!.ToDto();
    }

    public async Task<TransportationDto> UpdateAsync(UserContext caller, int id, SaveTransportationRequest request, CancellationToken cancellationToken = default)
    {
        var item = await transportation.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Transportation option not found.");
        Ownership.EnsureOwnerOrAdmin(caller, item.ProviderId);
        await ApplyAsync(caller, item, request, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await transportation.GetByIdAsync(item.Id, cancellationToken))!.ToDto();
    }

    public async Task DeleteAsync(UserContext caller, int id, CancellationToken cancellationToken = default)
    {
        var item = await transportation.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Transportation option not found.");
        Ownership.EnsureOwnerOrAdmin(caller, item.ProviderId);
        transportation.Remove(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyAsync(UserContext caller, Transportation item, SaveTransportationRequest request, CancellationToken cancellationToken)
    {
        int? destinationId = request.DestinationId;
        if (request.TravelPackageId.HasValue)
        {
            var package = await packages.GetByIdAsync(request.TravelPackageId.Value, includeDetails: false, cancellationToken)
                          ?? throw new BusinessRuleException("Travel package not found.");
            if (!caller.IsAdmin && package.AgentId != caller.UserId)
                throw new ForbiddenException("You can only attach transport to your own packages.");
            destinationId ??= package.DestinationId;
        }
        if (destinationId.HasValue && !await destinations.ExistsAsync(destinationId.Value, cancellationToken))
            throw new BusinessRuleException("Destination not found.");

        item.TravelPackageId = request.TravelPackageId;
        item.DestinationId = destinationId;
        item.Mode = request.Mode;
        item.FromLocation = request.FromLocation.Trim();
        item.ToLocation = request.ToLocation.Trim();
        item.DepartureTime = string.IsNullOrWhiteSpace(request.DepartureTime)
            ? null
            : TimeSpan.ParseExact(request.DepartureTime, @"hh\:mm", CultureInfo.InvariantCulture);
        item.DurationMinutes = request.DurationMinutes;
        item.PricePerPerson = request.PricePerPerson;
        item.Capacity = request.Capacity;
        item.Description = Ownership.Clean(request.Description);
        item.IsActive = request.IsActive;
    }
}

public sealed class SystemSettingsService(ISystemSettingRepository settings, IUnitOfWork unitOfWork) : ISystemSettingsService
{
    private sealed record Definition(string Description, Func<string, string?> Normalise, string Rule);

    private static readonly Dictionary<string, Definition> Definitions = new()
    {
        [SystemSettingKeys.DefaultCurrency] = new("Currency shown when a listing does not specify one",
            v => v.Trim().ToUpperInvariant() is { Length: 3 } c && c.All(char.IsAsciiLetterUpper) ? c : null,
            "a 3-letter ISO currency code"),
        [SystemSettingKeys.MaxAdvanceBookingDays] = new("How far in the future a booking may start",
            v => int.TryParse(v, out var n) && n is >= 1 and <= 730 ? n.ToString(CultureInfo.InvariantCulture) : null,
            "a whole number from 1 to 730"),
        [SystemSettingKeys.GuestCancellationCutoffHours] = new("Guests may cancel confirmed bookings up to this many hours before check-in",
            v => int.TryParse(v, out var n) && n is >= 0 and <= 720 ? n.ToString(CultureInfo.InvariantCulture) : null,
            "a whole number from 0 to 720"),
        [SystemSettingKeys.AiAssistantEnabled] = new("Turns the AI travel assistant on or off",
            v => bool.TryParse(v.Trim(), out var b) ? (b ? "true" : "false") : null,
            "true or false"),
        [SystemSettingKeys.MaintenanceMessage] = new("Optional banner shown to all clients",
            v => v.Trim().Length <= 500 ? v.Trim() : null,
            "at most 500 characters")
    };

    public async Task<List<SystemSettingDto>> ListAsync(CancellationToken cancellationToken = default) =>
        (await settings.ListAsync(cancellationToken)).Select(s => s.ToDto()).ToList();

    public async Task<PublicSettingsDto> GetPublicAsync(CancellationToken cancellationToken = default)
    {
        var all = (await settings.ListAsync(cancellationToken)).ToDictionary(s => s.Key, s => s.Value);
        string? Value(string key) => all.TryGetValue(key, out var v) ? v : null;
        int Int(string key, int fallback) => int.TryParse(Value(key), out var n) ? n : fallback;

        return new PublicSettingsDto
        {
            DefaultCurrency = Value(SystemSettingKeys.DefaultCurrency) ?? "LKR",
            AiAssistantEnabled = !bool.TryParse(Value(SystemSettingKeys.AiAssistantEnabled), out var enabled) || enabled,
            MaintenanceMessage = Ownership.Clean(Value(SystemSettingKeys.MaintenanceMessage)),
            MaxAdvanceBookingDays = Int(SystemSettingKeys.MaxAdvanceBookingDays, BookingService.DefaultMaxAdvanceDays),
            GuestCancellationCutoffHours = Int(SystemSettingKeys.GuestCancellationCutoffHours, BookingService.DefaultCancellationCutoffHours)
        };
    }

    public async Task<SystemSettingDto> UpdateAsync(UserContext caller, string key, UpdateSystemSettingRequest request, CancellationToken cancellationToken = default)
    {
        if (!Definitions.TryGetValue(key, out var definition))
            throw new NotFoundException("Unknown setting.");

        var value = definition.Normalise(request.Value ?? string.Empty)
                    ?? throw new BusinessRuleException($"{key} must be {definition.Rule}.",
                        new Dictionary<string, string[]> { ["value"] = [$"Must be {definition.Rule}."] });

        var setting = await settings.GetAsync(key, cancellationToken);
        if (setting is null)
        {
            setting = new SystemSetting { Key = key, Description = definition.Description };
            settings.Add(setting);
        }
        setting.Value = value;
        setting.UpdatedBy = caller.UserId;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return setting.ToDto();
    }

    public async Task<bool> IsAiAssistantEnabledAsync(CancellationToken cancellationToken = default)
    {
        var setting = await settings.GetAsync(SystemSettingKeys.AiAssistantEnabled, cancellationToken);
        return !bool.TryParse(setting?.Value, out var enabled) || enabled;
    }
}

public sealed class PaymentService(
    IPaymentRepository payments,
    IBookingRepository bookings,
    ISystemSettingRepository settings,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : IPaymentService
{
    /// <summary>Simulated gateway: this test card is always declined.</summary>
    public const string DeclinedTestCard = "4000000000000002";

    public async Task<List<PaymentDto>> ListAsync(UserContext caller, int bookingId, CancellationToken cancellationToken = default)
    {
        await GetAccessibleBookingAsync(caller, bookingId, cancellationToken);
        return (await payments.ListByBookingAsync(bookingId, cancellationToken)).Select(p => p.ToDto()).ToList();
    }

    public async Task<PaymentDto> CreateAsync(UserContext caller, int bookingId, CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var booking = await GetAccessibleBookingAsync(caller, bookingId, cancellationToken);
        if (booking.UserId != caller.UserId)
            throw new ForbiddenException("Only the guest who made the booking can pay for it.");
        if (booking.Status is not (BookingStatus.Pending or BookingStatus.Confirmed))
            throw new BusinessRuleException($"A {booking.Status.ToString().ToLowerInvariant()} booking cannot be paid.");

        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await bookings.LockBookingAsync(booking.Id, ct);
            var existing = await payments.ListByBookingAsync(booking.Id, ct);
            var committed = existing.Where(p => p.Status is PaymentStatus.Completed or PaymentStatus.Pending).Sum(p => p.Amount);
            var outstanding = booking.TotalPrice - committed;
            if (outstanding <= 0)
                throw new ConflictException("This booking is already paid or has a payment awaiting confirmation.");

            var now = clock.GetUtcNow().UtcDateTime;
            var payment = new Payment
            {
                BookingId = booking.Id,
                Amount = outstanding,
                Currency = (await settings.GetAsync(SystemSettingKeys.DefaultCurrency, ct))?.Value ?? "LKR",
                Method = request.Method,
                TransactionReference = $"SIM-{Convert.ToHexString(RandomNumberGenerator.GetBytes(10))}"
            };

            if (request.Method == PaymentMethod.Card)
            {
                var digits = new string((request.CardNumber ?? string.Empty).Where(char.IsDigit).ToArray());
                payment.Status = digits == DeclinedTestCard ? PaymentStatus.Failed : PaymentStatus.Completed;
                payment.PaidAt = payment.Status == PaymentStatus.Completed ? now : null;
            }
            else
            {
                payment.Status = PaymentStatus.Pending;
            }

            payments.Add(payment);
            await unitOfWork.SaveChangesAsync(ct);
            return payment.ToDto();
        }, cancellationToken: cancellationToken);
    }

    public async Task<PaymentDto> UpdateStatusAsync(UserContext caller, int bookingId, int paymentId, UpdatePaymentStatusRequest request, CancellationToken cancellationToken = default)
    {
        var booking = await GetAccessibleBookingAsync(caller, bookingId, cancellationToken);
        if (caller.IsUser)
            throw new ForbiddenException("Only the provider or an admin can confirm a payment.");

        var payment = await payments.GetAsync(booking.Id, paymentId, cancellationToken) ?? throw new NotFoundException("Payment not found.");
        if (payment.Status != PaymentStatus.Pending || request.Status is not (PaymentStatus.Completed or PaymentStatus.Failed))
            throw new BusinessRuleException("Only a pending payment can be marked completed or failed.");

        payment.Status = request.Status;
        payment.PaidAt = request.Status == PaymentStatus.Completed ? clock.GetUtcNow().UtcDateTime : null;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return payment.ToDto();
    }

    private async Task<Booking> GetAccessibleBookingAsync(UserContext caller, int bookingId, CancellationToken cancellationToken)
    {
        var booking = await bookings.GetByIdAsync(bookingId, cancellationToken) ?? throw new NotFoundException("Booking not found.");
        if (!BookingService.CanAccess(caller, booking))
            throw new ForbiddenException();
        return booking;
    }
}

internal static class FeatureDtoMapper
{
    public static ReviewDto ToDto(this Review review) => new()
    {
        Id = review.Id,
        BookingId = review.BookingId,
        HotelId = review.HotelId,
        HotelName = review.Hotel?.Name,
        TravelPackageId = review.TravelPackageId,
        PackageTitle = review.TravelPackage?.Title,
        Rating = review.Rating,
        Comment = review.Comment,
        Status = review.Status,
        AuthorName = review.User is null
            ? "Traveler"
            : string.IsNullOrWhiteSpace(review.User.LastName)
                ? review.User.FirstName
                : $"{review.User.FirstName} {review.User.LastName.Trim()[0]}.",
        CreatedAt = review.CreatedAt,
        UpdatedAt = review.UpdatedAt
    };

    public static TransportationDto ToDto(this Transportation item) => new()
    {
        Id = item.Id,
        ProviderId = item.ProviderId,
        TravelPackageId = item.TravelPackageId,
        PackageTitle = item.TravelPackage?.Title,
        DestinationId = item.DestinationId,
        DestinationName = item.Destination?.Name,
        Mode = item.Mode,
        FromLocation = item.FromLocation,
        ToLocation = item.ToLocation,
        DepartureTime = item.DepartureTime?.ToString(@"hh\:mm", CultureInfo.InvariantCulture),
        DurationMinutes = item.DurationMinutes,
        PricePerPerson = item.PricePerPerson,
        Capacity = item.Capacity,
        Description = item.Description,
        IsActive = item.IsActive,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt
    };

    public static PaymentDto ToDto(this Payment payment) => new()
    {
        Id = payment.Id,
        BookingId = payment.BookingId,
        Amount = payment.Amount,
        Currency = payment.Currency,
        Method = payment.Method,
        Status = payment.Status,
        TransactionReference = payment.TransactionReference,
        PaidAt = payment.PaidAt,
        CreatedAt = payment.CreatedAt
    };

    public static SystemSettingDto ToDto(this SystemSetting setting) => new()
    {
        Key = setting.Key,
        Value = setting.Value,
        Description = setting.Description,
        UpdatedBy = setting.UpdatedBy,
        UpdatedAt = setting.UpdatedAt
    };

    public static UserProfileDto ToDto(this UserProfile profile) => new()
    {
        PhoneNumber = profile.PhoneNumber,
        Nationality = profile.Nationality,
        DateOfBirth = profile.DateOfBirth,
        AvatarUrl = profile.AvatarUrl,
        Bio = profile.Bio,
        PreferredCurrency = profile.PreferredCurrency
    };
}
