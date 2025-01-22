using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;

using UpRestEye3.Components;
using UpRestEye3.Data;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.DataLayer;
using UpRestEye3.Services.Recognition;
using UpRestEye3.Services.BusinessLogic;
using UpRestEye3.Services.MLServices;
using UpRestEye3.Components.Account;

// TODO добавить логирование

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

//----------------------------------------------------------------------------------------
// Add services to the container
builder.Services.AddControllers();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient(); //??
builder.Services.AddSignalR();  //??


//// новая аутентификация и авторизация
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddIdentityCookies();

builder.Services.AddIdentityCore<UserDTO>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
////
///
/// 
/// 
/// 
//// Старая аутентификация и авторизация
///
///builder.Services.AddIdentity<UserDTO, IdentityRole>()
//.AddEntityFrameworkStores<ApplicationDbContext>()
//.AddDefaultTokenProviders();

//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//.AddJwtBearer(options =>
//{
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateLifetime = true,
//        ValidateIssuerSigningKey = true,
//        ValidIssuer = builder.Configuration["Jwt:Issuer"],
//        ValidAudience = builder.Configuration["Jwt:Audience"],
//        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
//    };
//});

//builder.Services.AddBlazoredLocalStorage();

//builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//    .AddCookie(options =>
//    {
//        options.LoginPath = "/login";
//    });


//builder.Services.AddHttpContextAccessor();

//builder.Services.AddAuthorizationCore();
///
//app.UseAuthentication();
//app.UseAuthorization();
///
///



var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<ILocalMLService, LocalMLService>();
builder.Services.AddScoped<IInvoiceFileService, FileService>();
builder.Services.AddScoped<IConsumerService, ConsumerService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IImageFileProcessor, ImageProcessor>();
builder.Services.AddScoped<IQRProcessing, QRProcessingOpenCV>();
builder.Services.AddScoped<IQRRecognition, QRRecognitionOpenCV>();
builder.Services.AddScoped<IQRRecognition, QRRecognitionZXing>();
builder.Services.AddScoped<ITextRecognition, TextRecognitionGoogleVision>();
builder.Services.AddScoped<IGPTService, GPTService>();
builder.Services.AddScoped<IImageProcessingPipelineHelper, ImagePipelineHelper>();
builder.Services.AddScoped<IConnectionParameterService, ConnectionParameterService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IServerAuthService, ServerAuthService>();
builder.Services.AddScoped<IClientAuthService, ClientAuthService>();


// Настройка параметров формы для обработки больших файлов
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB limit
});


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


//app.MapBlazorHub();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();



using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
   
    var seedData = new SeedData(services.GetRequiredService<IInvoiceService>());
    seedData.Initialize(services);

    // Валидация схемы и класса Invoice
    //InvoiceJsonHelper.ValidateInvoiceSchema();
    //InvoiceJsonHelper.ValidateInvoiceObject();

    //var testInvoiceService = new TestInvoiceService(services);
    //await testInvoiceService.RunTests();
}



app.Run();

