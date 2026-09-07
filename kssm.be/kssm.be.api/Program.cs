
using kssm.be.api.Middlewares;
using kssm.be.applications.Base;
using kssm.be.applications.Plugin.Implements;
using kssm.be.applications.Plugin.Interfaces;
using kssm.be.applications.Signing.Implements;
using kssm.be.applications.Signing.Interfaces;
using kssm.be.applications.Template.Implements;
using kssm.be.applications.Template.Interfaces;
using kssm.be.external.Certificates.Implements;
using kssm.be.external.Certificates.Interfaces;
using kssm.be.external.Colors.Implements;
using kssm.be.external.Colors.Interfaces;
using kssm.be.external.Errors.Implements;
using kssm.be.external.Fonts.Implements;
using kssm.be.external.Fonts.Interfaces;
using kssm.be.external.Mongo.Implements;
using kssm.be.external.Mongo.Interfaces;
using kssm.be.external.Pdf.Implements;
using kssm.be.external.Pdf.Interfaces;
using kssm.be.external.SaoMai.Implements;
using kssm.be.external.SaoMai.Interfaces;
using kssm.be.external.Signing.Implements;
using kssm.be.external.Signing.Interfaces;
using kssm.be.external.Tsa.Implements;
using kssm.be.external.Tsa.Interfaces;
using kssm.be.external.Images.Implements;
using kssm.be.external.Images.Interfaces;
using kssm.be.external.Errors.Interfaces;
using kssm.be.external.S3.Implements;
using kssm.be.external.S3.Interfaces;
using kssm.be.infrastructure.Persistence;
using kssm.be.shared.Constants.Auth;
using kssm.be.shared.Constants.Plugin;
using kssm.be.shared.Requests;
using kssm.be.shared.Requests.ErrorRequest;
using kssm.be.shared.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;
using System.Globalization;
using kssm.be.applications.KySo.LoKy.Implements;
using kssm.be.applications.KySo.LoKy.Interfaces;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Info("Starting application...");
var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("EnvironmentName => " + builder.Environment.EnvironmentName);


builder.Logging.ClearProviders();
builder.Host.UseNLog();


#region db
string connectionString = builder.Configuration.GetConnectionString("KY_SO_SAO_MAI")
    ?? throw new InvalidOperationException("Không tìm thấy connection string \"KY_SO_SAO_MAI\" trong appsettings.json");



builder.Services.AddDbContext<KssmDbContext>(options =>
{
    options.UseSqlServer(connectionString, options =>
    {
        //options.MigrationsAssembly(typeof(Program).Namespace);
        //options.MigrationsHistoryTable(DbSchemas.TableMigrationsHistory, DbSchemas.Core);
        options.CommandTimeout(600);
    });
    //options.UseOpenIddict(); // Register OpenIddict entities
}, ServiceLifetime.Scoped);
#endregion
#region cors
string allowOrigins = builder.Configuration.GetSection("AllowedHosts")!.Value!;
Console.WriteLine($"CORS: {allowOrigins}");
var origins = allowOrigins
    .Split(';')
    .Where(s => !string.IsNullOrWhiteSpace(s))
    .ToArray();
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        ProgramExtensions.CorsPolicy,
        builder =>
        {
            builder
                .WithOrigins(origins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .WithExposedHeaders("Content-Disposition");
        }
    );
});
#endregion

// Add services to the container.
#region service
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<IPluginService, PluginService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<ILoKyService, LoKyService>();

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<S3Settings>(builder.Configuration.GetSection("S3"));

// Kết nối MongoDB của sao_mai nằm ở ConnectionStrings chứ không ở section riêng, nên ghép vào cùng phần cấu
// hình callback để tầng external chỉ phải biết một lớp settings.
builder.Services.Configure<SaoMaiSettings>(options =>
{
    builder.Configuration.GetSection("SaoMai").Bind(options);
    options.MongoConnectionString = builder.Configuration.GetConnectionString("MongoDb") ?? string.Empty;
});

// Whitelist phiên bản plugin. IOptionsMonitor chứ không IOptions: duyệt một bản plugin mới chỉ cần sửa
// appsettings.json, không phải khởi động lại API - giống hệt cách bộ cài đã làm.
builder.Services.Configure<PluginSettings>(
    builder.Configuration.GetSection(PluginConstants.ConfigSection));

// External: dịch vụ bên thứ ba / dùng chung. Singleton vì không giữ trạng thái theo request.
builder.Services.AddSingleton<IErrorResponseResolver, ErrorResponseResolver>();
builder.Services.AddSingleton<ICertificateTrustValidator, CertificateTrustValidator>();
builder.Services.AddSingleton<ICertificateProvider, CertificateProvider>();

// S3FileStorage giữ pool kết nối của AmazonS3Client nên dựng lại mỗi request là phí kết nối.
builder.Services.AddSingleton<IS3FileStorage, S3FileStorage>();
builder.Services.AddSingleton<ITemplateImageStorage, TemplateImageStorage>();
builder.Services.AddSingleton<IHexColorReader, HexColorReader>();
builder.Services.AddSingleton<IS3ClientFactory, S3ClientFactory>();
builder.Services.AddSingleton<ILoKyFileStorage, LoKyFileStorage>();
builder.Services.AddSingleton<IImageSizeReader, ImageSizeReader>();

// Luồng ký: dựng bản ký, ghép CMS, xin dấu thời gian.
builder.Services.AddSingleton<IAppearanceFontResolver, AppearanceFontResolver>();
builder.Services.AddSingleton<IPdfObjectWriter, PdfObjectWriter>();
builder.Services.AddSingleton<IPdfRevisionReader, PdfRevisionReader>();
builder.Services.AddSingleton<IPdfAppearanceBuilder, PdfAppearanceBuilder>();
builder.Services.AddSingleton<IPdfPreparer, PdfPreparer>();
builder.Services.AddSingleton<IPdfContentWriter, PdfContentWriter>();
builder.Services.AddSingleton<IPdfSignatureInspector, PdfSignatureInspector>();
builder.Services.AddSingleton<ICmsAssembler, CmsAssembler>();
builder.Services.AddSingleton<ISignatureVerifier, SignatureVerifier>();
builder.Services.AddSingleton<ITimestampClient, TimestampClient>();

// Hàng đợi giữ phiên ký của từng lô trong bộ nhớ tiến trình nên BẮT BUỘC là Singleton: dựng lại theo request
// là mỗi lượt gọi nhìn thấy một hàng đợi rỗng khác nhau.
builder.Services.AddSingleton<IHangDoiKy, HangDoiKy>();
builder.Services.AddSingleton<ISigningKey, PluginSigningKey>();

// Tiến trình ký chạy nền, sống lâu hơn request nên cũng phải là Singleton; nó tự mở scope cho từng file.
builder.Services.AddSingleton<IKySoRunner, KySoRunner>();

builder.Services.AddSingleton<IProjectReader, ProjectReader>();
builder.Services.AddSingleton<IProjectNotifier, ProjectNotifier>();

builder.Services.AddHttpClient();
#endregion


#region mapper
// Build mapper configuration
builder.Services.AddAutoMapper(cfg => { }, typeof(MappingProfile));
#endregion

builder.Services.AddControllers();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context => new OkObjectResult(
        new ApiResponse(StatusCodeE.Error, null, ErrorCodes.BadRequest,
            ErrorMessages.GetMessage(ErrorCodes.BadRequest)));
});

builder.Services.AddHealthChecks();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "My API", Version = "v1" });
});

var app = builder.Build();


app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture(CultureInfo.InvariantCulture.Name)
    .AddSupportedCultures(CultureInfo.InvariantCulture.Name)
    .AddSupportedUICultures(CultureInfo.InvariantCulture.Name));

app.UseCors(ProgramExtensions.CorsPolicy);

app.MapControllers();


app.MapHealthChecks("/health");
app.Run();
