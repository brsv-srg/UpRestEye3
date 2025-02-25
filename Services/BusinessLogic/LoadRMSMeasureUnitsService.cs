using System.Text.Json;
using UpRestEye3.Data;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.DataLayer;

namespace UpRestEye3.Services.BusinessLogic
{
    public interface ILoadRMSMeasureUnitsService
    {
        Task LoadMeasureUnitsAsync(int consumerId);
    }

    public class LoadRMSMeasureUnitsService : ILoadRMSMeasureUnitsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRMSMeasureUnitService _measureUnitService;
        private readonly IConnectionParameterService _connectionParameterService;
        private readonly HttpClient _httpClient;
        private string _token;
        private string _login;
        private string _password;
        private string _apiUrl;

        public LoadRMSMeasureUnitsService(
            ApplicationDbContext context,
            IRMSMeasureUnitService measureUnitService,
            IConnectionParameterService connectionParameterService,
            HttpClient httpClient)
        {
            _context = context;
            _measureUnitService = measureUnitService;
            _connectionParameterService = connectionParameterService;
            _httpClient = httpClient;
        }

        public async Task LoadMeasureUnitsAsync(int consumerId)
        {
            await InitConnectionParams(consumerId);
            await AuthenticateAsync();
            var integrationUnits = await GetMeasureUnitsAsync();

            foreach (var integrationUnit in integrationUnits)
            {
                var measureUnitDTO = new RMSMeasureUnitDTO
                {
                    MeasureUnitExtGuid = integrationUnit.id,
                    ConsumerId = consumerId,
                    RootType = integrationUnit.rootType,
                    Deleted = integrationUnit.deleted,
                    Code = integrationUnit.code,
                    Name = integrationUnit.name,
                };
                await _measureUnitService.SaveMeasureUnitAsync(measureUnitDTO);
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

        private async Task<List<IntegrationUnitDTO>> GetMeasureUnitsAsync()
        {
            var measureUnitsUrl = $"{_apiUrl}api/v2/entities/list?rootType=MeasureUnit&key={_token}";

            var response = await _httpClient.GetAsync(measureUnitsUrl);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to fetch measure units");
            }

            var json = await response.Content.ReadAsStringAsync();

            var measureUnits = JsonSerializer.Deserialize<List<IntegrationUnitDTO>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return measureUnits ?? new List<IntegrationUnitDTO>();
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
