using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using UpRestEye3.Data;
using UpRestEye3.Models.BLO;
using UpRestEye3.Services.DataLayer;
using UpRestEye3.Services.BusinessLogic;
using UpRestEye3.Models.RMSDTO;

namespace UpRestEye3.Services.Integration
{
    public interface IIntegrationRMSProductsService
    {
        Task<bool> GetProductsAsync(int consumerId);
        Task<bool> PostProductsAsync(int consumerId, int? productId);
    }

    public class IntegrationRMSProductsService : IIntegrationRMSProductsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRMSProductService _productService;
        private readonly IConnectionParameterService _connectionParameterService;
        private readonly HttpClient _httpClient;
        private string _token;
        private string _login;
        private string _password;
        private string _apiUrl;


        public IntegrationRMSProductsService(
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

        public async Task<bool> GetProductsAsync(int consumerId)
        {
            try
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
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting products from RMS: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PostProductsAsync(int consumerId, int? productId)
        {
            try
            {
                await InitConnectionParams(consumerId);
                await AuthenticateAsync();

                var products = _productService.GetProductsByConsumerIdAsync(consumerId)
                                    .Result.Where(
                                     p => (productId != null && p.Id == productId) || 
                                            (productId == null && (p.Status == RMSProductStatusEnum.NewProduct ||
                                            p.Status == RMSProductStatusEnum.NewContainer)));
                


                if(productId != null && products.Any(p => (p.Status != RMSProductStatusEnum.NewProduct &&
                                                            p.Status != RMSProductStatusEnum.NewContainer)))
                {
                    throw new Exception($"RMSProduct {productId} doesn't contain new information for sending to RMS");
                }


                foreach (var product in products)
                {

                    var intProduct = RMSProductHelper.BuildSaveProductDTO(product);
                    intProduct.type = "GOODS";
                    var resultProduct = await PostProductAsync(intProduct);
                    if (resultProduct != null)
                    {
                        product.Status = RMSProductStatusEnum.FromRMS;
                        product.Num = resultProduct.num;
                        product.RMSProductExtGuid = resultProduct.id;
                        foreach (var container in product.Containers)
                        {
                            container.Num = resultProduct.containers.FirstOrDefault(c => c.name == container.Name)?.num;
                            container.RMSContainerExtGuid = resultProduct.containers.FirstOrDefault(c => c.name == container.Name)?.id;
                        }

                        await _productService.SaveProductAsync(product);
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting products from RMS: {ex.Message}");
                return false;
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

        private async Task<List<GetProductDTO>> GetProductsAsync()
        {
            var productsUrl = $"{_apiUrl}api/v2/entities/products/list?includeDeleted=false&type=GOODS&type=SERVICE&key={_token}";

            _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-GB"));


            var response = await _httpClient.GetAsync(productsUrl);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to fetch products");
            }

            //var products = await response.Content.ReadFromJsonAsync<List<GetProductDTO>>();

            var json = await response.Content.ReadAsStringAsync();

            var products = JsonSerializer.Deserialize<List<GetProductDTO>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true/*,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }*/
            });

            return products?.Where(p => p.type == "GOODS" || p.type == "SERVICE")?.ToList() ?? new List<GetProductDTO>();
            //return products;
        }



        private async Task<GetProductDTO?> PostProductAsync(SaveProductDTO product)
        {

            var saveProductUrl = $"{_apiUrl}api/v2/entities/products/save?generateNomenclatureCode=true&generateFastCode=false&key={_token}";

            var productJson = JsonSerializer.Serialize(product);
            var content = new StringContent(productJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(saveProductUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                // Логирование ошибки
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to save product. Status code: {response.StatusCode}, Error: {errorContent}");
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var saveProductResponse = JsonSerializer.Deserialize<SaveProductResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (saveProductResponse.result == "SUCCESS")
            {
                return saveProductResponse.response;
            }
            else
            {
                // Логирование ошибок
                foreach (var error in saveProductResponse.errors)
                {
                    Console.WriteLine($"Error: {error.code}: {error.value}");
                }
                return null;
            }
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
