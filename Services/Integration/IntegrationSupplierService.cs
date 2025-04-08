using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using UpRestEye3.Data;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;
using UpRestEye3.Models.RMSDTO;
using UpRestEye3.Services.DataLayer;
using System.Xml.Serialization;
using System.Xml;
using Google.Apis;
using Microsoft.ML;

namespace UpRestEye3.Services.BusinessLogic
{
    public interface IIntegrationSupplierService
    {
        Task<bool> SynchronizeSuppliersAsync(int consumerId);
    }

    public class IntegrationSupplierService : IIntegrationSupplierService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISupplierService _supplierService;
        private readonly IConnectionParameterService _connectionParameterService;
        private readonly HttpClient _httpClient;
        private string _token;
        private string _login;
        private string _password;
        private string _apiUrl;


        public IntegrationSupplierService(
            ApplicationDbContext context,
            ISupplierService supplierService,
            IConnectionParameterService connectionParameterService,
            HttpClient httpClient)
        {
            _context = context;
            _supplierService = supplierService;
            _connectionParameterService = connectionParameterService;
            _httpClient = httpClient;
        }

        public async Task<bool> SynchronizeSuppliersAsync(int consumerId)
        {
            try
            {
                await InitConnectionParams(consumerId);
                await AuthenticateAsync();
                var suppliersRMS = await GetSuppliersAsync();

                var suppliersRMSWithNIF = suppliersRMS.EmployeeList.Where(supplierRMS => !string.IsNullOrEmpty(supplierRMS.TaxpayerIdNumber)).ToList();

                var suppliersThis = await _supplierService.GetSuppliersDTOAsync(consumerId);
                

                foreach (var supplierRMS in suppliersRMSWithNIF)
                {
                    var supplierThis = suppliersThis.Where(s => s.RMSSupplierId != null && 
                                                                s.RMSSupplierId != Guid.Empty && 
                                                                s.RMSSupplierId == Guid.Parse(supplierRMS.Id) ||
                                                                
                                                                (s.RMSSupplierId == null || s.RMSSupplierId == Guid.Empty) &&
                                                                s.TaxNumber == supplierRMS.TaxpayerIdNumber)
                                                                .FirstOrDefault();

                    if (supplierThis == null)
                        supplierThis = new SupplierDTO();

                    supplierThis.RMSSupplierId = Guid.Parse(supplierRMS.Id);
                    supplierThis.Status = SupplierStatus.Synchronized;
                    supplierThis.Name = supplierRMS.Name;
                    supplierThis.TaxNumber = supplierRMS.TaxpayerIdNumber;
                    //supplierThis.BankAccount = supplierRMS.Gln;
                    supplierThis.ConsumerId = consumerId;

                    await _supplierService.SaveSupplierAsync(supplierThis);
                
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

        private async Task<EmployeesDTO> GetSuppliersAsync()
        {
            try
            {
                var suppliersUrl = $"{_apiUrl}api/suppliers?key={_token}";

                _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-GB"));


                var response = await _httpClient.GetAsync(suppliersUrl);

                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                XmlSerializer deserializer = new XmlSerializer(typeof(EmployeesDTO));

                using var reader = new StringReader(responseContent);

                
                var responseValue = (EmployeesDTO)deserializer.Deserialize(reader);

                return responseValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting products from RMS: {ex.Message}");
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
