using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using UpRestEye3.Data;
using UpRestEye3.Models.DTO;
using UpRestEye3.Models.BLO;
using UpRestEye3.Services.DataLayer;
using System.Xml.Serialization;
using UpRestEye3.Models.RMSDTO;

namespace UpRestEye3.Services.Integration
{
    public interface IIntegrationInvoiceService
    {
        Task<bool> PostInvoiceAsync(InvoiceDTO invoiceDTO);
    }

    public class IntegrationInvoiceService : IIntegrationInvoiceService
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IConnectionParameterService _connectionParameterService;
        private readonly HttpClient _httpClient;
        private string _token;
        private string _login;
        private string _password;
        private string _apiUrl;


        public IntegrationInvoiceService(
            IInvoiceService invoiceService,
            IConnectionParameterService connectionParameterService,
            HttpClient httpClient)
        {
            _invoiceService = invoiceService;
            _connectionParameterService = connectionParameterService;
            _httpClient = httpClient;
        }


        public async Task<bool> PostInvoiceAsync(InvoiceDTO invoiceDTO)
        {
            try
            {
                await InitConnectionParams((int)invoiceDTO.Consumer.Id);
                await AuthenticateAsync();

                var serializer = new XmlSerializer(typeof(IncomingInvoiceDto));
                var stringBuilder = new StringBuilder();
                using (var writer = new StringWriter(stringBuilder))
                {
                    serializer.Serialize(writer, invoiceDTO);
                }

                var content = new StringContent(stringBuilder.ToString(), Encoding.UTF8, "application/xml");
                var response = await _httpClient.PostAsync("/documents/import/incomingInvoice", content);

                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var deserializer = new XmlSerializer(typeof(DocumentValidationResult));
                using (var reader = new StringReader(responseContent))
                {
                    var responseValue = (DocumentValidationResult)deserializer.Deserialize(reader);
                    if (responseValue != null)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing Invoice to RMS: {ex.Message}");
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
