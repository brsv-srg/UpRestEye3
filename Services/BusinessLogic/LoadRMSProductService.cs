using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UpRestEye3.Data;
using UpRestEye3.Models.DTO;
using UpRestEye3.Models.BLO;
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
                var rmsProductDTO = RMSProductHelper.BuildRMSProductDTO(product);
                rmsProductDTO.ConsumerId = consumerId;
                rmsProductDTO.Status = RMSProductStatusEnum.FromRMS;
                await _productService.SaveProductAsync(rmsProductDTO);
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

        private async Task<List<IntegrationProductDTO>> GetProductsAsync()
        {
            var productsUrl = $"{_apiUrl}api/v2/entities/products/list?includeDeleted=false&type=GOODS&key={_token}";

            var response = await _httpClient.GetAsync(productsUrl);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to fetch products");
            }

            //var products = await response.Content.ReadFromJsonAsync<List<IntegrationProductDTO>>();

            var json = await response.Content.ReadAsStringAsync();

            var products = JsonSerializer.Deserialize<List<IntegrationProductDTO>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true/*,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }*/
            });

            return products?.Where(p => p.type == "GOODS")?.ToList() ?? new List<IntegrationProductDTO>();
        }

        //var productsUrl = $"{_apiUrl}api/v2/entities/products/list";

        //var requestBody = new
        //{
        //    includeDeleted = false,
        //    type = new List<string> { "GOODS" },
        //    key = _token
        //};

        //// Установка заголовка Content-Type
        //_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //var response = await _httpClient.PostAsJsonAsync(productsUrl, requestBody);


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
