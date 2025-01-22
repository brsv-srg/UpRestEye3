using Blazored.LocalStorage;
using System.Net.Http.Headers;
using UpRestEye3.Models.DTO;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;


namespace UpRestEye3.Services.BusinessLogic
{
    public interface IClientAuthService
    {
        Task<LoginResponseDTO> LoginAsync(string baseUri, LoginRequestDTO request);
        Task<LoginResponseDTO> GetCurrentUserAsync();
        Task<bool> IsAuthenticatedAsync();
        Task SetAuthenticationCookie(LoginResponseDTO user, HttpContext httpContext);

    }


    public class ClientAuthService : IClientAuthService
    {
       

        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;


        public ClientAuthService(HttpClient httpClient, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
        }

        public async Task<LoginResponseDTO> LoginAsync(string baseUri,LoginRequestDTO request)
        {
            
            var response = await _httpClient.PostAsJsonAsync($"{baseUri}/api/user/authenticate", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
                await _localStorage.SetItemAsync("authToken", result.Token);
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", result.Token);
                return result;
            }

            var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            throw new ApplicationException(error?.GetValueOrDefault("message") ?? "Login failed");
        }

        public async Task<LoginResponseDTO> GetCurrentUserAsync()
        {
            var response = await _httpClient.GetAsync("api/auth/user");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
            }
            return null;
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            return !string.IsNullOrEmpty(token);
        }

        public async Task SetAuthenticationCookie(LoginResponseDTO user, HttpContext httpContext)
        {
            try
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Login),
                    new Claim(ClaimTypes.Name, user.Login),
                    new Claim("ConsumerId", user.ConsumerId.ToString()),
                    new Claim("ConsumerTaxNumber", user.ConsumerTaxNumber)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1)
                };

                await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(claimsIdentity));

                if (httpContext != null)
                {
                    await httpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);
                }
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                Console.WriteLine($"Error generating JWT token: {ex.Message}");
                throw;
            }
        }
    }
}
