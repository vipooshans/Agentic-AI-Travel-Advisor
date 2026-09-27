using TravelAdvisor.Core.Entities;

namespace TravelAdvisor.Core.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(ApplicationUser user, string roleName);
}
