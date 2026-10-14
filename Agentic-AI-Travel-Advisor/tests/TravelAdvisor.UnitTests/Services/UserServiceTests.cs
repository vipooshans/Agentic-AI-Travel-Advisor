using Microsoft.AspNetCore.Identity;
using Moq;
using TravelAdvisor.Core.Common;
using TravelAdvisor.Core.DTOs.Auth;
using TravelAdvisor.Core.DTOs.Users;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Core.Interfaces.Repositories;
using TravelAdvisor.Infrastructure.Services;
using Xunit;

namespace TravelAdvisor.UnitTests.Services;

public class UserServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManager = IdentityMocks.UserManager();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ITravelPreferencesRepository> _preferences = new();
    private readonly Mock<IUserProfileRepository> _profiles = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserStatusCache> _statusCache = new();

    public UserServiceTests()
    {
        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.UpdateSecurityStampAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
    }

    private UserService CreateService() => new(
        _userManager.Object, _roles.Object, _users.Object, _preferences.Object, _profiles.Object, _unitOfWork.Object, _statusCache.Object);

    private static CreateStaffUserRequest Staff(string role) => new()
    {
        Email = " new.owner@example.com ", Password = "Owner#2026", FirstName = "New", LastName = "Owner", Role = role
    };

    [Theory]
    [InlineData(RoleNames.HotelOwner)]
    [InlineData(RoleNames.TravelAgent)]
    public async Task Admin_can_create_provider_accounts(string role)
    {
        _roles.Setup(r => r.GetByNameAsync(role, It.IsAny<CancellationToken>())).ReturnsAsync(new Role { Id = 5, Name = role });

        var dto = await CreateService().CreateStaffAsync(Staff(role));

        Assert.Equal(role, dto.Role);
        Assert.Equal("new.owner@example.com", dto.Email);
        _userManager.Verify(m => m.CreateAsync(It.Is<ApplicationUser>(u => u.RoleId == 5), "Owner#2026"), Times.Once);
    }

    [Theory]
    [InlineData(RoleNames.Admin)]
    [InlineData(RoleNames.User)]
    [InlineData("SUPERUSER")]
    public async Task Staff_creation_cannot_mint_admins_or_other_roles(string role)
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService().CreateStaffAsync(Staff(role)));
        _userManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Staff_creation_rejects_a_duplicate_email()
    {
        _roles.Setup(r => r.GetByNameAsync(RoleNames.HotelOwner, It.IsAny<CancellationToken>())).ReturnsAsync(new Role { Id = 2, Name = RoleNames.HotelOwner });
        _userManager.Setup(m => m.FindByEmailAsync("new.owner@example.com")).ReturnsAsync(new ApplicationUser());

        await Assert.ThrowsAsync<ConflictException>(() => CreateService().CreateStaffAsync(Staff(RoleNames.HotelOwner)));
    }

    [Fact]
    public async Task Admin_cannot_deactivate_themselves()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService().SetActiveAsync(new UserContext("admin-1", RoleNames.Admin), "admin-1", false));
        _userManager.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task Deactivation_rotates_the_security_stamp_and_clears_the_status_cache()
    {
        var target = new ApplicationUser { Id = "u-9", IsActive = true, Role = new Role { Name = RoleNames.User } };
        _users.Setup(u => u.GetByIdAsync("u-9", It.IsAny<CancellationToken>())).ReturnsAsync(target);

        var dto = await CreateService().SetActiveAsync(new UserContext("admin-1", RoleNames.Admin), "u-9", false);

        Assert.False(dto.IsActive);
        _userManager.Verify(m => m.UpdateSecurityStampAsync(target), Times.Once);
        _statusCache.Verify(c => c.Invalidate("u-9"), Times.Once);
    }

    [Fact]
    public async Task Reactivation_does_not_rotate_the_security_stamp()
    {
        var target = new ApplicationUser { Id = "u-9", IsActive = false, Role = new Role { Name = RoleNames.User } };
        _users.Setup(u => u.GetByIdAsync("u-9", It.IsAny<CancellationToken>())).ReturnsAsync(target);

        Assert.True((await CreateService().SetActiveAsync(new UserContext("admin-1", RoleNames.Admin), "u-9", true)).IsActive);
        _userManager.Verify(m => m.UpdateSecurityStampAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _statusCache.Verify(c => c.Invalidate("u-9"), Times.Once);
    }

    [Fact]
    public async Task Activating_an_unknown_user_is_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().SetActiveAsync(new UserContext("admin-1", RoleNames.Admin), "ghost", true));
    }

    [Fact]
    public async Task First_profile_save_creates_the_profile_and_defaults_currency_to_LKR()
    {
        UserProfile? added = null;
        _profiles.Setup(p => p.Add(It.IsAny<UserProfile>())).Callback<UserProfile>(p => added = p);

        var dto = await CreateService().UpdateProfileAsync("u-1", new UserProfileDto { PhoneNumber = "  0771234567 ", Bio = "   " });

        Assert.NotNull(added);
        Assert.Equal("u-1", added!.UserId);
        Assert.Equal("0771234567", dto.PhoneNumber);
        Assert.Null(dto.Bio);
        Assert.Equal("LKR", dto.PreferredCurrency);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Preferences_normalise_transport_and_accommodation()
    {
        var prefs = new TravelPreferences { UserId = "u-1" };
        _preferences.Setup(p => p.GetByUserAsync("u-1", It.IsAny<CancellationToken>())).ReturnsAsync(prefs);

        var dto = await CreateService().UpdatePreferencesAsync("u-1", new TravelPreferencesDto
        {
            BudgetMax = 60_000m, AccommodationPreference = " Boutique ", TransportPreference = "train", Interests = " hiking "
        });

        Assert.Equal("boutique", dto.AccommodationPreference);
        Assert.Equal(nameof(TransportMode.Train), dto.TransportPreference);
        Assert.Equal("hiking", dto.Interests);
        _preferences.Verify(p => p.Add(It.IsAny<TravelPreferences>()), Times.Never);
    }

    [Fact]
    public async Task Unknown_transport_preference_is_cleared()
    {
        var prefs = new TravelPreferences { UserId = "u-1", TransportPreference = "Bus" };
        _preferences.Setup(p => p.GetByUserAsync("u-1", It.IsAny<CancellationToken>())).ReturnsAsync(prefs);

        var dto = await CreateService().UpdatePreferencesAsync("u-1", new TravelPreferencesDto { TransportPreference = "hoverboard" });

        Assert.Null(dto.TransportPreference);
    }
}
