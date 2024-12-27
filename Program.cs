using UpRestEye3.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features; 
using UpRestEye3.Data;
using UpRestEye3.Services;


var builder = WebApplication.CreateBuilder(args);

//----------------------------------------------------------------------------------------
// Add services to the container
builder.Services.AddControllers();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient();
builder.Services.AddSignalR();


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<ILocalMLService, LocalMLService>();
builder.Services.AddScoped<IInvoiceFileService, InvoiceFileService>();
builder.Services.AddScoped<IImageFileProcessor, ImageFileProcessor>();
builder.Services.AddScoped<IQRProcessing, QRProcessingOpenCV>();
builder.Services.AddScoped<IQRRecognition, QRRecognitionOpenCV>();
builder.Services.AddScoped<IQRRecognition, QRRecognitionZXing>();
builder.Services.AddScoped<IGPTService, GPTService>();

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

}



app.Run();

