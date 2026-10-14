using Microsoft.AspNetCore.Identity;
using Moq;
using TravelAdvisor.Core.Common;
using TravelAdvisor.Core.DTOs.Auth;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Core.Interfaces;
using TravelAdvisor.Core.Interfaces.Repositories;
using TravelAdvisor.Infrastructure.Services;
using Xunit;

namespace TravelAdvisor.UnitTests.Services;

public class AuthServiceTests
{
    private static readonly Role UserRole = new() { Id = 1, Name = RoleNames.User };
    private static readonly DateTime Expiry = new(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc);

    private readonly Mock<UserManager<ApplicationUser>> _userManager = IdentityMocks.UserManager();
    private readonly Mock<SignInManager<ApplicationUser>> _signIn;
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IJwtTokenService> _jwt = new();

    public AuthServiceTests()
    {
        _signIn = IdentityMocks.SignInManager(_userManager.Object);
        _roles.Setup(r => r.GetByNameAsync(RoleNames.User, It.IsAny<CancellationToken>())).ReturnsAsync(UserRole);
        _jwt.Setup(j => j.GenerateToken(It.IsAny<ApplicationUser>(), It.IsAny<string>())).Returns(("signed.jwt.token", Expiry));
        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
    }

    private AuthService CreateService() => new(_userManager.Object, _signIn.Object, _roles.Object, _users.Object, _jwt.Object);

    private static RegisterRequest Register(string email = "  nimal@example.com ") => new()
    {
        Email = email, Password = "Travel#2026", FirstName = " Nimal ", LastName = " Perera "
    };

    private ApplicationUser GivenUser(string email, bool active = true, string role = RoleNames.User)
    {
        var user = new ApplicationUser
        {
            Id = "u-1", Email = email, UserName = email, FirstName = "Nimal", LastName = "Perera",
            IsActive = active, Role = new Role { Name = role }
        };
        _userManager.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync(user);
        _users.Setup(u => u.GetByIdAsync("u-1", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        return user;
    }

    [Fact]
    public async Task Register_creates_a_USER_with_trimmed_fields_and_returns_a_token()
    {
        var response = await CreateService().RegisterAsync(Register());

        Assert.Equal("signed.jwt.token", response.Token);
        Assert.Equal(Expiry, response.ExpiresAt);
        Assert.Equal("nimal@example.com", response.User.Email);
        Assert.Equal("Nimal", response.User.FirstName);
        Assert.Equal(RoleNames.User, response.User.Role);
        _userManager.Verify(m => m.CreateAsync(
            It.Is<ApplicationUser>(u => u.RoleId == UserRole.Id && u.IsActive && u.UserName == "nimal@example.com"),
            "Travel#2026"), Times.Once);
        _jwt.Verify(j => j.GenerateToken(It.IsAny<ApplicationUser>(), RoleNames.User), Times.Once);
    }

    [Fact]
    public async Task Register_with_a_taken_email_is_a_conflict_and_creates_nothing()
    {
        GivenUser("nimal@example.com");

        var ex = await Assert.ThrowsAsync<ConflictException>(() => CreateService().RegisterAsync(Register()));

        Assert.Equal(409, ex.StatusCode);
        _userManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        _jwt.Verify(j => j.GenerateToken(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Identity_password_errors_are_returned_under_the_password_field()
    {
        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityMocks.Failed(
                ("PasswordRequiresDigit", "Passwords must have at least one digit."),
                ("PasswordTooShort", "Passwords must be at least 8 characters."),
                ("DuplicateUserName", "Username is already taken.")));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService().RegisterAsync(Register()));

        Assert.Equal(2, ex.Errors!["password"].Length);
        Assert.Single(ex.Errors["email"]);
    }

    [Fact]
    public async Task Register_fails_loudly_when_roles_were_not_seeded()
    {
        _roles.Setup(r => r.GetByNameAsync(RoleNames.User, It.IsAny<CancellationToken>())).ReturnsAsync((Role?)null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService().RegisterAsync(Register()));
    }

    [Fact]
    public async Task Login_with_valid_credentials_returns_a_token_for_the_users_role()
    {
        var user = GivenUser("owner@example.com", role: RoleNames.HotelOwner);
        _signIn.Setup(s => s.CheckPasswordSignInAsync(user, "Owner@123", true)).ReturnsAsync(SignInResult.Success);

        var response = await CreateService().LoginAsync(new LoginRequest { Email = " owner@example.com ", Password = "Owner@123" });

        Assert.Equal("signed.jwt.token", response.Token);
        Assert.Equal(RoleNames.HotelOwner, response.User.Role);
        _jwt.Verify(j => j.GenerateToken(user, RoleNames.HotelOwner), Times.Once);
    }

    [Fact]
    public async Task Login_uses_lockout_on_failure()
    {
        var user = GivenUser("nimal@example.com");
        _signIn.Setup(s => s.CheckPasswordSignInAsync(user, It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(SignInResult.Failed);

        await Assert.ThrowsAsync<UnauthorizedAppException>(() =>
            CreateService().LoginAsync(new LoginRequest { Email = "nimal@example.com", Password = "wrong" }));

        _signIn.Verify(s => s.CheckPasswordSignInAsync(user, "wrong", true), Times.Once);
    }

    [Fact]
    public async Task Unknown_email_and_wrong_password_give_the_same_401_message()
    {
        var user = GivenUser("nimal@example.com");
        _signIn.Setup(s => s.CheckPasswordSignInAsync(user, It.IsAny<string>(), true)).ReturnsAsync(SignInResult.Failed);

        var unknown = await Assert.ThrowsAsync<UnauthorizedAppException>(() =>
            CreateService().LoginAsync(new LoginRequest { Email = "nobody@example.com", Password = "x" }));
        var wrong = await Assert.ThrowsAsync<UnauthorizedAppException>(() =>
            CreateService().LoginAsync(new LoginRequest { Email = "nimal@example.com", Password = "x" }));

        Assert.Equal(401, unknown.StatusCode);
        Assert.Equal(unknown.Message, wrong.Message);
        _jwt.Verify(j => j.GenerateToken(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Deactivated_user_cannot_sign_in_and_the_password_is_not_checked()
    {
        GivenUser("nimal@example.com", active: false);

        await Assert.ThrowsAsync<UnauthorizedAppException>(() =>
            CreateService().LoginAsync(new LoginRequest { Email = "nimal@example.com", Password = "Travel#2026" }));

        _signIn.Verify(s => s.CheckPasswordSignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Locked_out_user_gets_a_specific_message()
    {
        var user = GivenUser("nimal@example.com");
        _signIn.Setup(s => s.CheckPasswordSignInAsync(user, It.IsAny<string>(), true)).ReturnsAsync(SignInResult.LockedOut);

        var ex = await Assert.ThrowsAsync<UnauthorizedAppException>(() =>
            CreateService().LoginAsync(new LoginRequest { Email = "nimal@example.com", Password = "Travel#2026" }));

        Assert.Contains("Too many failed sign-in attempts", ex.Message);
    }

    [Fact]
    public async Task Current_user_for_an_unknown_id_is_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().GetCurrentUserAsync("missing"));
    }

    [Fact]
    public async Task Profile_update_trims_names_and_surfaces_identity_failures()
    {
        var user = GivenUser("nimal@example.com");
        _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var dto = await CreateService().UpdateProfileAsync("u-1", new UpdateProfileRequest { FirstName = "  Kamal ", LastName = " Silva " });
        Assert.Equal(("Kamal", "Silva"), (dto.FirstName, dto.LastName));

        _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityMocks.Failed(("ConcurrencyFailure", "Optimistic concurrency failure.")));
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateService().UpdateProfileAsync("u-1", new UpdateProfileRequest { FirstName = "A", LastName = "B" }));
        Assert.Contains("general", ex.Errors!.Keys);
    }
}
