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
using UpRestEye3.Services.BusinessLogic;
using System.Xml;

namespace UpRestEye3.Services.Integration
{
    public interface IIntegrationInvoiceService
    {
        Task<InvoiceDTO> PostInvoiceAsync(InvoiceDTO invoiceDTO);
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


        public async Task<InvoiceDTO> PostInvoiceAsync(InvoiceDTO invoiceDTO)
        {
            try
            {
                await InitConnectionParams((int)invoiceDTO.Consumer.Id);
                await AuthenticateAsync();

                var integrationInvoiceDTO = InvoiceHelper.MapToIncomingInvoiceDto(invoiceDTO);


                var serializer = new XmlSerializer(typeof(IncomingInvoiceDto));
                var stringBuilder = new StringBuilder();

                var settings = new XmlWriterSettings
                {
                    OmitXmlDeclaration = true,
                    Indent = true
                };
                var ns = new XmlSerializerNamespaces();
                ns.Add("", ""); // Убираем пространства имен

                using (var writer = XmlWriter.Create(stringBuilder, settings))
                {
                    serializer.Serialize(writer, integrationInvoiceDTO, ns);
                }

                var content = new StringContent(stringBuilder.ToString(), Encoding.UTF8, "application/xml");
                var url = $"{_apiUrl}api/documents/import/incomingInvoice?key={_token}";
                
                _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-GB"));

                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error response: {errorContent}");
                    response.EnsureSuccessStatusCode();
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var deserializer = new XmlSerializer(typeof(DocumentValidationResult));
                using (var reader = new StringReader(responseContent))
                {
                    var responseValue = (DocumentValidationResult)deserializer.Deserialize(reader);
                    if (responseValue != null)
                    {
                        invoiceDTO.Stage = InvoiceStageEnum.SavedToSystem;
                        invoiceDTO.StageStatus = InvoiceStatusEnum.Ok;
                        return invoiceDTO;
                    }
                    else
                    {
                        throw new Exception("Error uploading invoice to RMS");
                    }
                }

            }
            catch (Exception ex)
            {
                invoiceDTO.StageStatus = InvoiceStatusEnum.Error;
                invoiceDTO.Comments = ex.Message;
                Console.WriteLine($"Error writing Invoice to RMS: {ex.Message}");
                return invoiceDTO;
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




/*
 
                ///////////////////////////////////////////////////////////
                integrationInvoiceDTO.Invoice = "FT111111111";
                integrationInvoiceDTO.IncomingDocumentNumber = "FT111111111";

                integrationInvoiceDTO.Items = [];

                integrationInvoiceDTO.Items = integrationInvoiceDTO.Items.Append(new IncomingInvoiceItemDto()).ToArray();
                integrationInvoiceDTO.Items = integrationInvoiceDTO.Items.Append(new IncomingInvoiceItemDto()).ToArray();
                var item0 = integrationInvoiceDTO.Items[0];

                ///////////////////////////////////////////////////////////
                item0.Num = 1;

                item0.Product = "cffc849e-207c-40a8-82bb-cb17421f65f0";
                item0.AmountUnit = "7ba81c3a-8de5-8f9d-fb9f-e39efcbc57cc";
                item0.ContainerId = "3dc1fda8-8dca-4f99-88c1-73d9c248b515";
                item0.Store = "1239d270-1bbe-f64f-b7ea-5f00518ef508";

                item0.Amount = 0.750m;
                item0.ActualAmount = 0.750m;
                item0.Price =1.06m;
                item0.PriceWithoutVat = 1.0m;

                item0.Sum = 3.18m;
                item0.VatPercent = 6;
                item0.VatSum = 0.18m;

                ///////////////////////////////////////////////////////////
                var item1 = integrationInvoiceDTO.Items[1];
                item1.Num = 2;

                item1.Product = "944901a7-4bb9-4ece-a0f5-aad56bb557a4";
                item1.AmountUnit = "cd19b5ea-1b32-a6e5-1df7-5d2784a0549a";
                item1.ContainerId = "D6CE1FF4-BB3C-43BD-814D-881B1AD81A18";
                item1.Store = "1239d270-1bbe-f64f-b7ea-5f00518ef508";

                item1.Amount = 360m;
                item1.ActualAmount = 360m;
                item1.Price = 106m;
                item1.PriceWithoutVat = 100m;

                item1.Sum = 212m;
                item1.VatPercent = 6;
                item1.VatSum = 12m;

                ///////////////////////////////////////////////////////////
                ///*/