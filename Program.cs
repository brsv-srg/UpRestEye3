using Google.Api;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using OpenAI;

using UpRestEye3.Components;
using UpRestEye3.Components.Account;
using UpRestEye3.Data;
using UpRestEye3.Models.Account;
using UpRestEye3.Models.BLO;
using UpRestEye3.Services;
using UpRestEye3.Services.Account;
using UpRestEye3.Services.BusinessLogic;
using UpRestEye3.Services.Controllers;
using UpRestEye3.Services.DataLayer;
using UpRestEye3.Services.Integration;
using UpRestEye3.Services.MLServices;
using UpRestEye3.Services.Recognition;
using static UpRestEye3.Services.BusinessLogic.NetworkHelper;


// TODO добавить логирование

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

//// новая аутентификация и авторизация
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddIdentityCookies();

builder.Services.AddIdentityCore<AppUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAuthenticatedUser", policy => policy.RequireAuthenticatedUser());
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options => 
{ 
    options.UseSqlite(connectionString)
        .UseLoggerFactory(LoggerFactory.Create(builder => { builder.AddConsole(); }))
        .EnableSensitiveDataLogging();
});

builder.Services.AddSingleton<IEmailSender<AppUser>, IdentityNoOpEmailSender>();
builder.Services.AddSingleton<IProcessingLockService, InMemoryProcessingLockService>();

// OpenAI client как Singleton (потокобезопасен)
builder.Services.AddSingleton(sp =>
{
    var apiKey = builder.Configuration["OpenAI:ApiKey"];
    var client = new OpenAIClient(apiKey);
    return client;
});



// Сервисы логирования
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// HTTP контроллеры и HTTP клиент
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddSignalR();

builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Сервисы приложения
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<ILocalMLService, LocalMLService>();
builder.Services.AddScoped<IInvoiceFileProcessor, InvoiceFileProcessor>();
builder.Services.AddScoped<IConsumerService, ConsumerService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IImageRecognitionService, ImageRecognitionService>();
builder.Services.AddScoped<IQRProcessing, QRProcessingOpenCV>();
builder.Services.AddScoped<IQRRecognition, QRRecognitionOpenCV>();
builder.Services.AddScoped<IQRRecognition, QRRecognitionZXing>();
builder.Services.AddScoped<ITextRecognition, TextRecognitionGoogleVision>();

builder.Services.AddScoped<IGPTRecognitionService, GPTRecognitionService>();
builder.Services.AddScoped<IGPTMappingService, GPTMappingService>();

builder.Services.AddScoped<IImagePipelineHelper, ImagePipelineHelper>();
builder.Services.AddScoped<IProductMappingService, ProductMappingService>();
builder.Services.AddScoped<IConnectionParameterService, ConnectionParameterService>();
builder.Services.AddScoped<IRagDataService, RagDataService>();

builder.Services.AddScoped<IRMSProductService, RMSProductService>();

builder.Services.AddScoped<IIntegrationRMSProductsService, IntegrationRMSProductsService>();
builder.Services.AddScoped<IIntegrationRMSEntitiesService, IntegrationRMSEntitiesService>();
builder.Services.AddScoped<IIntegrationSupplierService, IntegrationSupplierService>();
builder.Services.AddScoped<IIntegrationInvoiceService, IntegrationInvoiceService>();

builder.Services.AddScoped<IRMSMeasureUnitService, RMSMeasureUnitService>();
builder.Services.AddScoped<IRMSAccountsService, RMSAccountsService>();

builder.Services.AddScoped<IRAGFileService, RAGFileService>();

var apiKey = builder.Configuration["OpenAI:ApiKey"];
builder.Services.AddScoped<IRagManager>(provider =>
    new RagManager(new HttpClient(), apiKey));




//// Configure AppConfig
//var appConfig = new AppConfig
//{
//    IpAddress = NetworkHelper.GetLocalIpAddress()
//};
//builder.Services.AddSingleton(appConfig);


//// Настройка Kestrel для использования сертификата
//builder.WebHost.ConfigureKestrel(serverOptions =>
//{
//    serverOptions.Listen(IPAddress.Parse("192.168.1.124"), 7124, listenOptions =>
//    {
//        // Путь к PFX файлу и пароль, который вы указали при экспорте
//        listenOptions.UseHttps("c:\\certs\\ipcert.pfx", "certPwd1!");
//    });
//});

var app = builder.Build();

//----------------------------------------------------------------------------------------
// Configire services in the app

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();


//app.MapBlazorHub();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapHub<NotificationHub>("/notificationHub");

app.MapAdditionalIdentityEndpoints();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
    /*
    var seedData = new SeedData(services.GetRequiredService<IInvoiceService>(), services.GetRequiredService<IRMSProductService>());
    seedData.InitializeInvoices(services);
    seedData.InitializeRMSProducts(services);

    //Валидация схемы и класса Invoice
    JsonHelper.ValidateInvoiceSchema();
    JsonHelper.ValidateInvoiceObject();
    JsonHelper.ValidateMappedInvoiceSchema();
    JsonHelper.ValidateInvoiceObject();

    var testInvoiceService = new TestInvoiceService(services);
    await testInvoiceService.RunTests();
    */
}



// Запуск очереди обработки инвойсов
StartInvoiceProcessingQueue(app.Services);

app.Run();



void StartInvoiceProcessingQueue(IServiceProvider services)
{
    int threadCount = 3; // Количество потоков
    var queue = new BlockingCollection<Tuple<int, int>>(); // Очередь для хранения ID накладных и consumerId

    // Запуск потоков
    for (int i = 0; i < threadCount; i++)
    {
        Task.Run(async () =>
        {
            using var scope = services.CreateScope();
            var invoiceProcessor = scope.ServiceProvider.GetRequiredService<IInvoiceFileProcessor>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();
            var processingLockService = scope.ServiceProvider.GetRequiredService<IProcessingLockService>();


            foreach (var (invoiceId, consumerId) in queue.GetConsumingEnumerable())
            {
                try
                {
                    try
                    {
                        await hubContext.Clients.All.SendAsync("ReceiveMessage", $"[Thread {i}] Invoice processing started {invoiceId} for consumerId {consumerId}");
                        Console.WriteLine($"[Thread {i}] Invoice processing started {invoiceId} for consumerId {consumerId}");


                        // Обработка накладной
                        await invoiceProcessor.InvoiceFileProcessAsync(invoiceId, consumerId);


                        await hubContext.Clients.All.SendAsync("ReceiveMessage", $"[Thread {i}] Invoice processing completed {invoiceId}");
                        Console.WriteLine($"[Thread {i}] Invoice processing completed {invoiceId}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Invoice processing error {invoiceId}: {ex.Message}");
                        await hubContext.Clients.All.SendAsync("ReceiveMessage", $"[Thread {i}] Invoice processing error {invoiceId}: {ex.Message}");
                    }
                }
                finally
                {
                    await processingLockService.ReleaseLockAsync(invoiceId);
                }
            }
        });
    }

    // Запуск задачи для выборки накладных из БД
    Task.Run(async () =>
    {
        while (true)
        {
            using var scope = services.CreateScope();
            var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
            var processingLockService = scope.ServiceProvider.GetRequiredService<IProcessingLockService>();


            // Стратегия выборки: порциями по 10 записей
            var invoices = await invoiceService.GetInvoicesDAOAsync(null,null,null);
            var filteredInvoices = invoices
                .Where(i => (
                            (i.Stage == InvoiceStageEnum.New && i.StageStatus == InvoiceStatusEnum.Ok )||
                             
                            (i.Stage == InvoiceStageEnum.QRCodeRecognition ||
                             i.Stage == InvoiceStageEnum.TextRecognition) &&
                            (i.StageStatus == InvoiceStatusEnum.Ok ||
                             i.StageStatus == InvoiceStatusEnum.Processing)))
                .Take(10) // Порция записей
                .ToList();

            Console.WriteLine($"There were added {filteredInvoices.Count} Invoices to queue");

            foreach (var invoice in filteredInvoices)
            {
                if (invoice.Id.HasValue && invoice.ConsumerId.HasValue)
                {
                    // Проверяем, не заблокирована ли накладная
                    if (await processingLockService.TryLockAsync(invoice.Id.Value))
                    {
                        var tuple = new Tuple<int, int>(invoice.Id.Value, invoice.ConsumerId.Value);
                        queue.Add(tuple); // Добавляем ID накладной и consumerId в очередь
                    }
                }
            }

            await Task.Delay(10000); // Задержка перед следующей выборкой
        }
    });


    // Периодический вывод состояния очереди
    Task.Run(async () =>
    {
        while (true)
        {
            Console.WriteLine($"The current number of items in the queue: {queue.Count}");
            await Task.Delay(5000); // Проверяем каждые 2 секунды
        }
    });
}
