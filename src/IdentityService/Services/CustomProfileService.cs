using Duende.IdentityModel;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using IdentityService.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace IdentityService.Services
{
    public class CustomProfileService(UserManager<ApplicationUser> userManager) : IProfileService
    {
        private readonly UserManager<ApplicationUser> userManager = userManager;

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var user = await userManager.GetUserAsync(context.Subject);
            if (user == null) return;
            var exisitngClaims = await userManager.GetClaimsAsync(user);

            var claims = new List<Claim>
            {
                new("username", user.UserName ?? string.Empty),
                new("email", user.Email ?? string.Empty)
            };

            context.IssuedClaims.AddRange(claims);
            var nameClaim = exisitngClaims.FirstOrDefault(x => x.Type == JwtClaimTypes.Name);
            if (nameClaim != null)
                context.IssuedClaims.Add(nameClaim);
        }

        public Task IsActiveAsync(IsActiveContext context)
        {
            return Task.CompletedTask;
        }
    }
}
