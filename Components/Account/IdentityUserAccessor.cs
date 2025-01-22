using Microsoft.AspNetCore.Identity;
using UpRestEye3.Models.DTO;

namespace UpRestEye3.Components.Account
{
    internal sealed class IdentityUserAccessor(UserManager<UserDTO> userManager, IdentityRedirectManager redirectManager)
    {
        public async Task<UserDTO> GetRequiredUserAsync(HttpContext context)
        {
            var user = await userManager.GetUserAsync(context.User);

            if (user is null)
            {
                redirectManager.RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
            }

            return user;
        }
    }
}
