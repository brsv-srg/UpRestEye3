using System.Text.Json;
using System.Net.Http.Headers;
using UpRestEye3.Data;
using UpRestEye3.Models.DTO;
using UpRestEye3.Models.BLO;
using UpRestEye3.Services.DataLayer;
using UpRestEye3.Models.RMSDTO;

namespace UpRestEye3.Services.Integration
{
    public interface IIntegrationRMSEntitiesService
    {
        Task<bool> GetMeasureUnitsAsync(int consumerId);
        Task<bool> GetAccountsAsync(int consumerId);
    }

    public class IntegrationRMSEntitiesService : IIntegrationRMSEntitiesService
    {
        private readonly IRMSAccountsService _accountService;
        private readonly IRMSMeasureUnitService _measureUnitService;
        private readonly IConnectionParameterService _connectionParameterService;
        private readonly HttpClient _httpClient;
        private string _token;
        private string _login;
        private string _password;
        private string _apiUrl;

        public IntegrationRMSEntitiesService(
            IRMSAccountsService accountService,
            IRMSMeasureUnitService measureUnitService,
            IConnectionParameterService connectionParameterService,
            HttpClient httpClient)
        {
            _accountService = accountService;
            _measureUnitService = measureUnitService;
            _connectionParameterService = connectionParameterService;
            _httpClient = httpClient;
        }

        public async Task<bool> GetMeasureUnitsAsync(int consumerId)
        {
            try
            {
                await InitConnectionParams(consumerId);
                await AuthenticateAsync();
                var integrationUnits = await GetEntitiesAsync("MeasureUnit");

                foreach (var integrationUnit in integrationUnits)
                {
                    var measureUnitDTO = new RMSMeasureUnitDTO
                    {
                        EntityExtGuid = integrationUnit.id,
                        ConsumerId = consumerId,
                        RootType = integrationUnit.rootType,
                        Status = integrationUnit.deleted ? EntityStatus.Deleted : EntityStatus.Synchronized,
                        Code = integrationUnit.code,
                        Name = integrationUnit.name
                    };
                    await _measureUnitService.SaveMeasureUnitAsync(measureUnitDTO);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting products from RMS: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> GetAccountsAsync(int consumerId)
        {
            try
            {
                await InitConnectionParams(consumerId);
                await AuthenticateAsync();
                var integrationUnits = (await GetEntitiesAsync("Account"))?.Where(s => s.type == "INVENTORY_ASSETS").ToList();

                foreach (var integrationUnit in integrationUnits)
                {
                    var accountDTO = new RMSAccountDTO
                    {
                        EntityExtGuid = integrationUnit.id,
                        ConsumerId = consumerId,
                        RootType = integrationUnit.rootType,
                        Status = integrationUnit.deleted ? EntityStatus.Deleted : EntityStatus.Synchronized,
                        Code = integrationUnit.code,
                        Name = integrationUnit.name
                    };
                    await _accountService.SaveAccountAsync(accountDTO);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting accounts from RMS: {ex.Message}");
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

        private async Task<List<IntegrationEntitiesDTO>> GetEntitiesAsync(string _entity)
        {
            var url = $"{_apiUrl}api/v2/entities/list?rootType={_entity}&key={_token}";

            _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-GB"));


            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to fetch measure units");
            }

            var json = await response.Content.ReadAsStringAsync();

            var measureUnits = JsonSerializer.Deserialize<List<IntegrationEntitiesDTO>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return measureUnits ?? new List<IntegrationEntitiesDTO>();
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
