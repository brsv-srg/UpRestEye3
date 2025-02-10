using Microsoft.AspNetCore.Components.Authorization;

namespace UpRestEye3.Services.Account
{
    public interface IAuthenticationService
    {
        Task<AuthenticationState> GetAuthenticationStateAsync();
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly AuthenticationStateProvider _authenticationStateProvider;

        public AuthenticationService(AuthenticationStateProvider authenticationStateProvider)
        {
            _authenticationStateProvider = authenticationStateProvider;
        }

        public Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return _authenticationStateProvider.GetAuthenticationStateAsync();
        }
    }
}
