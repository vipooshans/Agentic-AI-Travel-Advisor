using Microsoft.AspNetCore.Identity;
using TravelAdvisor.Core.Common;
using TravelAdvisor.Core.DTOs.Auth;
using TravelAdvisor.Core.DTOs.Users;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Core.Interfaces.Repositories;
using TravelAdvisor.Core.Interfaces.Services;

namespace TravelAdvisor.Infrastructure.Services;

public sealed class UserService(
    UserManager<ApplicationUser> userManager,
    IRoleRepository roles,
    IUserRepository users,
    ITravelPreferencesRepository preferences,
    IUserProfileRepository profiles,
    IUnitOfWork unitOfWork,
    IUserStatusCache statusCache) : IUserService
{
    private static readonly string[] StaffRoles = [RoleNames.HotelOwner, RoleNames.TravelAgent];

    public async Task<UserProfileDto> GetProfileAsync(string userId, CancellationToken cancellationToken = default) =>
        (await profiles.GetByUserIdAsync(userId, cancellationToken))?.ToDto() ?? new UserProfileDto();

    public async Task<UserProfileDto> UpdateProfileAsync(string userId, UserProfileDto request, CancellationToken cancellationToken = default)
    {
        var profile = await profiles.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
        {
            profile = new UserProfile { UserId = userId };
            profiles.Add(profile);
        }

        profile.PhoneNumber = Ownership.Clean(request.PhoneNumber);
        profile.Nationality = Ownership.Clean(request.Nationality);
        profile.DateOfBirth = request.DateOfBirth;
        profile.AvatarUrl = Ownership.Clean(request.AvatarUrl);
        profile.Bio = Ownership.Clean(request.Bio);
        profile.PreferredCurrency = string.IsNullOrWhiteSpace(request.PreferredCurrency) ? "LKR" : request.PreferredCurrency.Trim().ToUpperInvariant();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return profile.ToDto();
    }

    public async Task<TravelPreferencesDto> GetPreferencesAsync(string userId, CancellationToken cancellationToken = default) =>
        (await preferences.GetByUserAsync(userId, cancellationToken)).ToDto();

    public async Task<TravelPreferencesDto> UpdatePreferencesAsync(string userId, TravelPreferencesDto request, CancellationToken cancellationToken = default)
    {
        var prefs = await preferences.GetByUserAsync(userId, cancellationToken);
        if (prefs is null)
        {
            prefs = new TravelPreferences { UserId = userId };
            preferences.Add(prefs);
        }

        prefs.BudgetMin = request.BudgetMin;
        prefs.BudgetMax = request.BudgetMax;
        prefs.PreferredClimate = Clean(request.PreferredClimate);
        prefs.Interests = Clean(request.Interests);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return prefs.ToDto();
    }

    public async Task<List<UserDto>> ListAsync(string? role, CancellationToken cancellationToken = default) =>
        (await users.ListAsync(role, cancellationToken)).Select(u => u.ToDto()).ToList();

    public async Task<UserDto> CreateStaffAsync(CreateStaffUserRequest request, CancellationToken cancellationToken = default)
    {
        if (!StaffRoles.Contains(request.Role))
            throw new BusinessRuleException("Role must be HOTEL_OWNER or TRAVEL_AGENT.");

        var role = await roles.GetByNameAsync(request.Role, cancellationToken)
                   ?? throw new InvalidOperationException($"Role {request.Role} is missing; the database was not seeded.");

        var email = request.Email.Trim();
        if (await userManager.FindByEmailAsync(email) is not null)
            throw new ConflictException("Email is already registered.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            RoleId = role.Id,
            Role = role,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new BusinessRuleException("Could not create user.", IdentityErrors.ToDictionary(result));
        return user.ToDto();
    }

    public async Task<UserDto> SetActiveAsync(UserContext caller, string userId, bool isActive, CancellationToken cancellationToken = default)
    {
        if (caller.UserId == userId && !isActive)
            throw new BusinessRuleException("You cannot deactivate your own account.");

        var user = await users.GetByIdAsync(userId, cancellationToken) ?? throw new NotFoundException("User not found.");
        user.IsActive = isActive;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new BusinessRuleException("Could not update user.", IdentityErrors.ToDictionary(result));

        if (!isActive)
            await userManager.UpdateSecurityStampAsync(user);
        statusCache.Invalidate(userId);
        return user.ToDto();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
