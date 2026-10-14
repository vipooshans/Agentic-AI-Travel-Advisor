using TravelAdvisor.Core.DTOs.Payments;
using TravelAdvisor.Core.DTOs.Transportation;
using TravelAdvisor.Core.DTOs.Users;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Core.Validation;
using Xunit;

namespace TravelAdvisor.UnitTests;

public class FeatureValidatorTests
{
    [Theory]
    [InlineData("4242424242424242", true)]
    [InlineData("4242 4242 4242 4242", true)]
    [InlineData("4000-0000-0000-0002", true)]
    [InlineData("4242424242424241", false)]
    [InlineData("1234", false)]
    [InlineData("42424242424242424242", false)]
    [InlineData("4242abcd42424242", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Card_payments_require_a_luhn_valid_number(string? cardNumber, bool valid)
    {
        var result = new CreatePaymentRequestValidator().Validate(new CreatePaymentRequest { Method = PaymentMethod.Card, CardNumber = cardNumber });
        Assert.Equal(valid, result.IsValid);
    }

    [Theory]
    [InlineData(PaymentMethod.Cash, null, true)]
    [InlineData(PaymentMethod.BankTransfer, "", true)]
    [InlineData(PaymentMethod.Cash, "4242424242424242", false)]
    [InlineData((PaymentMethod)9, null, false)]
    public void Non_card_payments_must_not_carry_card_numbers(PaymentMethod method, string? cardNumber, bool valid)
    {
        var result = new CreatePaymentRequestValidator().Validate(new CreatePaymentRequest { Method = method, CardNumber = cardNumber });
        Assert.Equal(valid, result.IsValid);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("00:00", true)]
    [InlineData("23:59", true)]
    [InlineData("07:30", true)]
    [InlineData("24:00", false)]
    [InlineData("7:30", false)]
    [InlineData("07:60", false)]
    [InlineData("noon", false)]
    public void Transport_departure_time_is_24_hour_hh_mm(string? departure, bool valid)
    {
        var request = new SaveTransportationRequest
        {
            Mode = TransportMode.Train,
            FromLocation = "Kandy",
            ToLocation = "Ella",
            DepartureTime = departure,
            DurationMinutes = 420,
            PricePerPerson = 1500,
            Capacity = 40
        };
        Assert.Equal(valid, new SaveTransportationRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void Transport_rejects_non_positive_duration_and_capacity()
    {
        var result = new SaveTransportationRequestValidator().Validate(new SaveTransportationRequest
        {
            Mode = TransportMode.Bus,
            FromLocation = "Colombo",
            ToLocation = "Galle",
            DurationMinutes = 0,
            PricePerPerson = -1,
            Capacity = 0
        });
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SaveTransportationRequest.DurationMinutes));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SaveTransportationRequest.PricePerPerson));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SaveTransportationRequest.Capacity));
    }

    [Theory]
    [InlineData("+94 77 123 4567", "lkr", null, true)]
    [InlineData("call me", "LKR", null, false)]
    [InlineData(null, "RUPEES", null, false)]
    [InlineData(null, "USD", "javascript:alert(1)", false)]
    [InlineData(null, "USD", "https://cdn.example.com/a.png", true)]
    public void Profile_fields_are_validated(string? phone, string currency, string? avatar, bool valid)
    {
        var result = new UserProfileDtoValidator().Validate(new UserProfileDto
        {
            PhoneNumber = phone,
            PreferredCurrency = currency,
            AvatarUrl = avatar
        });
        Assert.Equal(valid, result.IsValid);
    }
}
