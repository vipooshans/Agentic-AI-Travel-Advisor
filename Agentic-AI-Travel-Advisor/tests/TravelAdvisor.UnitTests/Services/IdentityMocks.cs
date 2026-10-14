using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using TravelAdvisor.Core.Entities;

namespace TravelAdvisor.UnitTests.Services;

internal static class IdentityMocks
{
    public static Mock<UserManager<ApplicationUser>> UserManager() =>
        new(Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

    public static Mock<SignInManager<ApplicationUser>> SignInManager(UserManager<ApplicationUser> userManager) =>
        new(userManager, Mock.Of<IHttpContextAccessor>(), Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            null!, null!, null!, null!);

    public static IdentityResult Failed(params (string Code, string Description)[] errors) =>
        IdentityResult.Failed(errors.Select(e => new IdentityError { Code = e.Code, Description = e.Description }).ToArray());
}
