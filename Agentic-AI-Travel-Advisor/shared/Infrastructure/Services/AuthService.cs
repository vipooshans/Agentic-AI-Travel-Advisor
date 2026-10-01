using Microsoft.AspNetCore.Identity;
using TravelAdvisor.Core.Common;
using TravelAdvisor.Core.DTOs.Auth;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Core.Interfaces;
using TravelAdvisor.Core.Interfaces.Repositories;
using TravelAdvisor.Core.Interfaces.Services;

namespace TravelAdvisor.Infrastructure.Services;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IRoleRepository roles,
    IUserRepository users,
    IJwtTokenService jwtTokenService) : IAuthService
{
    private const string InvalidCredentials = "Invalid email or password.";

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var userRole = await roles.GetByNameAsync(RoleNames.User, cancellationToken)
                       ?? throw new InvalidOperationException("Default user role is missing; the database was not seeded.");

        var email = request.Email.Trim();
        if (await userManager.FindByEmailAsync(email) is not null)
            throw new ConflictException("Email is already registered.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            RoleId = userRole.Id,
            Role = userRole,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new BusinessRuleException("Registration failed.", IdentityErrors.ToDictionary(result));

        return BuildResponse(user, userRole.Name);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var found = await userManager.FindByEmailAsync(request.Email.Trim());
        if (found is null)
            throw new UnauthorizedAppException(InvalidCredentials);

        var user = await users.GetByIdAsync(found.Id, cancellationToken);
        if (user is null || !user.IsActive)
            throw new UnauthorizedAppException(InvalidCredentials);

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
            throw new UnauthorizedAppException("Too many failed sign-in attempts. Try again in a few minutes.");
        if (!result.Succeeded)
            throw new UnauthorizedAppException(InvalidCredentials);

        return BuildResponse(user, user.Role.Name);
    }

    public async Task<UserDto> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken) ?? throw new NotFoundException("User not found.");
        return user.ToDto();
    }

    public async Task<UserDto> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken) ?? throw new NotFoundException("User not found.");
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new BusinessRuleException("Profile update failed.", IdentityErrors.ToDictionary(result));
        return user.ToDto();
    }

    private AuthResponse BuildResponse(ApplicationUser user, string roleName)
    {
        var (token, expiresAt) = jwtTokenService.GenerateToken(user, roleName);
        return new AuthResponse { Token = token, ExpiresAt = expiresAt, User = user.ToDto() };
    }
}

internal static class IdentityErrors
{
    public static Dictionary<string, string[]> ToDictionary(IdentityResult result) =>
        result.Errors
            .GroupBy(e => e.Code switch
            {
                var c when c.StartsWith("Password", StringComparison.Ordinal) => "password",
                var c when c.Contains("Email", StringComparison.Ordinal) || c.Contains("UserName", StringComparison.Ordinal) => "email",
                _ => "general"
            })
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
}
