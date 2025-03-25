using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using UpRestEye3.Components;
using UpRestEye3.Data;
using UpRestEye3.Models.Account;
using UpRestEye3.Components.Account;
using UpRestEye3.Services;
using UpRestEye3.Services.BusinessLogic;
using UpRestEye3.Services.DataLayer;
using UpRestEye3.Services.MLServices;
using UpRestEye3.Services.Recognition;
using UpRestEye3.Services.Account;
using UpRestEye3.Controllers;
using UpRestEye3.Services.Integration;


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
builder.Services.AddScoped<IImageFileProcessor, ImageFileProcessor>();
builder.Services.AddScoped<IConsumerService, ConsumerService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IImageRecognitionService, ImageRecognitionService>();
builder.Services.AddScoped<IQRProcessing, QRProcessingOpenCV>();
builder.Services.AddScoped<IQRRecognition, QRRecognitionOpenCV>();
builder.Services.AddScoped<IQRRecognition, QRRecognitionZXing>();
builder.Services.AddScoped<ITextRecognition, TextRecognitionGoogleVision>();

builder.Services.AddScoped<IGPTLayoutService, GPTLayoutService>();
builder.Services.AddScoped<IGPTSemanticService, GPTSemanticService>();
builder.Services.AddScoped<IGPTMappingService, GPTMappingService>();

builder.Services.AddScoped<IImagePipelineHelper, ImagePipelineHelper>();
builder.Services.AddScoped<IProductMappingService, ProductMappingService>();
builder.Services.AddScoped<IConnectionParameterService, ConnectionParameterService>();
builder.Services.AddScoped<IRMSProductService, RMSProductService>();



builder.Services.AddScoped<IIntegrationRMSProductsService, IntegrationRMSProductsService>();
builder.Services.AddScoped<IIntegrationRMSEntitiesService, IntegrationRMSEntitiesService>();
builder.Services.AddScoped<IIntegrationSupplierService, IntegrationSupplierService>();
builder.Services.AddScoped<IIntegrationInvoiceService, IntegrationInvoiceService>();

builder.Services.AddScoped<IRMSMeasureUnitService, RMSMeasureUnitService>();
builder.Services.AddScoped<IRMSAccountsService, RMSAccountsService>();




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
}



app.Run();

