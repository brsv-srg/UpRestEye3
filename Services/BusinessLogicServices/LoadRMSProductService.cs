using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using UpRestEye3.Components.Pages;
using UpRestEye3.Data;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.DataLayer;

namespace UpRestEye3.Services.BusinessLogic
{
    public interface ILoadRMSProductsService
    {
        Task LoadProductsAsync(int consumerId);
    }

    public class LoadRMSProductsService : ILoadRMSProductsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRMSProductService _productService;
        private readonly IConnectionParameterService _connectionParameterService;
        private readonly HttpClient _httpClient;
        private string _token;
        private string _login;
        private string _password;
        private string _apiUrl;


        public LoadRMSProductsService(
            ApplicationDbContext context,
            IRMSProductService productService,
            IConnectionParameterService connectionParameterService,
            HttpClient httpClient)
        {
            _context = context;
            _productService = productService;
            _connectionParameterService = connectionParameterService;
            _httpClient = httpClient;
        }

        public async Task LoadProductsAsync(int consumerId)
        {
            await InitConnectionParams(consumerId);
            await AuthenticateAsync();
            var products = await GetProductsAsync();

            foreach (var product in products)
            {
                await _productService.SaveProductAsync(product);
            }
        }

        private async Task AuthenticateAsync()
        {
            var authUrl = $"{_apiUrl}api/auth?login={_login}&pass={_password}";

            var response = await _httpClient.GetStringAsync(authUrl);
            if (string.IsNullOrEmpty(response))
            {
                throw new Exception("Authentication failed");
            }

            _token = response;
        }

        private async Task<List<RMSProductDTO>> GetProductsAsync()
        {
            
            var productsUrl = $"{_apiUrl}api/v2/entities/products/list?includeDeleted=false&key={_token}";

            var response = await _httpClient.GetAsync(productsUrl);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to fetch products");
            }

            var products = await response.Content.ReadFromJsonAsync<List<RMSProductDTO>>();
            return products ?? new List<RMSProductDTO>();
        }


        private static string ComputeSha1Hash(string input)
        {
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }

       private async Task InitConnectionParams(int consumerId)
       {
            var connectionParams = await _connectionParameterService.GetConnectionParameterDTOByCustomerIdAsync(consumerId);
            if (connectionParams == null)
            {
                throw new Exception("Connection parameters not found");
            }

            _login = connectionParams.ApiLogin;
            _password = connectionParams.ApiPassword;
            _apiUrl = connectionParams.ApiUrl;

            if (!_apiUrl.EndsWith('/'))
                _apiUrl = _apiUrl + "/";
       }
    }
}
