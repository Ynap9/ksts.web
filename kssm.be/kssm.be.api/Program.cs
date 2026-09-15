
using kssm.be.api.Middlewares;
using kssm.be.applications.Base;
using kssm.be.applications.KySo.LoKy.Implements;
using kssm.be.applications.KySo.LoKy.Interfaces;
using kssm.be.applications.KySo.Plugin.Implements;
using kssm.be.applications.KySo.Plugin.Interfaces;
using kssm.be.applications.KySo.Signing.Implements;
using kssm.be.applications.KySo.Signing.Interfaces;
using kssm.be.applications.KySo.Template.Implements;
using kssm.be.applications.KySo.Template.Interfaces;
using kssm.be.applications.DongGoi.Config.Implements;
using kssm.be.applications.DongGoi.Config.Interfaces;
using kssm.be.applications.DongGoi.Package.Implements;
using kssm.be.applications.DongGoi.Package.Interfaces;
using kssm.be.domain.DongGoi;
using kssm.be.external.DongGoi.Implements;
using kssm.be.external.DongGoi.Interfaces;
using kssm.be.external.Drive.Implements;
using kssm.be.external.Drive.Interfaces;
using kssm.be.external.Excel.Implements;
using kssm.be.external.Excel.Interfaces;
using kssm.be.external.Errors.Implements;
using kssm.be.external.Errors.Interfaces;
using kssm.be.external.KySo.Certificates.Implements;
using kssm.be.external.KySo.Certificates.Interfaces;
using kssm.be.external.KySo.Colors.Implements;
using kssm.be.external.KySo.Colors.Interfaces;
using kssm.be.external.KySo.Fonts.Implements;
using kssm.be.external.KySo.Fonts.Interfaces;
using kssm.be.external.KySo.Images.Implements;
using kssm.be.external.KySo.Images.Interfaces;
using kssm.be.external.KySo.Pdf.Implements;
using kssm.be.external.KySo.Pdf.Interfaces;
using kssm.be.external.KySo.SaoMai.Implements;
using kssm.be.external.KySo.SaoMai.Interfaces;
using kssm.be.external.KySo.Signing.Implements;
using kssm.be.external.KySo.Signing.Interfaces;
using kssm.be.external.KySo.Tsa.Implements;
using kssm.be.external.KySo.Tsa.Interfaces;
using kssm.be.external.Mongo.Implements;
using kssm.be.external.Mongo.Interfaces;
using kssm.be.external.S3.Implements;
using kssm.be.external.S3.Interfaces;
using kssm.be.infrastructure.Persistence;
using kssm.be.infrastructure.Persistence.Seeder;
using kssm.be.shared.Constants.Auth;
using kssm.be.shared.Constants.Drive;
using kssm.be.shared.Constants.Plugin;
using kssm.be.shared.Requests;
using kssm.be.shared.Requests.ErrorRequest;
using kssm.be.shared.Settings;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;
using System.Globalization;

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
builder.Services.AddScoped<IConfigService, ConfigService>();
builder.Services.AddScoped<IMetadataSchemaService, MetadataSchemaService>();
builder.Services.AddScoped<IDossierLayoutService, DossierLayoutService>();
builder.Services.AddScoped<IHeaderMatchService, HeaderMatchService>();
builder.Services.AddScoped<IPackageSessionService, PackageSessionService>();
builder.Services.AddScoped<IPackageBuilderService, PackageBuilderService>();

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<S3Settings>(builder.Configuration.GetSection("S3"));

builder.Services.Configure<DongGoiSettings>(builder.Configuration.GetSection("DongGoi"));

builder.Services.Configure<DriveSettings>(
    builder.Configuration.GetSection(DriveConstants.ConfigSection));

// Mặc định Kestrel chặn thân request ở ~30 MB còn FormOptions ở ~128 MB: một đợt tải lên vài chục PDF lưu
// trữ là vượt ngay, mà lỗi hiện ra dưới dạng đứt kết nối chứ không phải một câu báo đọc được.
var dongGoiSettings = builder.Configuration.GetSection("DongGoi").Get<DongGoiSettings>() ?? new DongGoiSettings();
var tranThanRequest = (long)Math.Max(1, dongGoiSettings.MaxRequestBodyMb) * 1024 * 1024;

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = tranThanRequest);
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = tranThanRequest;
    options.ValueCountLimit = int.MaxValue;
});


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

// DriveService giữ khoá đã ký và pool HTTP của Google SDK nên dựng lại mỗi request là tự xin lại token.
builder.Services.AddSingleton<IDriveFileStorage, DriveFileStorage>();
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

builder.Services.AddSingleton<IExcelSheetReader, ExcelSheetReader>();
builder.Services.AddSingleton<ITextSimilarity, TextSimilarity>();
builder.Services.AddSingleton<IPdfFormatInspector, PdfFormatInspector>();
builder.Services.AddSingleton<IPackageFileStorage, PackageFileStorage>();
builder.Services.AddSingleton<IPackageXmlBuilder, PackageXmlBuilder>();
builder.Services.AddSingleton<IPackageArchiveBuilder, PackageArchiveBuilder>();
builder.Services.AddSingleton<IPackageSchemaTemplate, PackageSchemaTemplate>();

builder.Services.AddSingleton<IVerifyRunnerService, VerifyRunnerService>();

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
#region Seed data
// Run seeding inside scope
using (var scope = app.Services.CreateScope())
{
    var dongGoiManager = scope.ServiceProvider.GetRequiredService<KssmDbContext>();

    await SeedDongGoi.SeedAsync(dongGoiManager);

    // Kho tạm của phiên đóng gói nằm trên MinIO: kho có thể không với tới lúc khởi động, mà đó không phải
    // lý do để cả API không lên được.
    try
    {
        await scope.ServiceProvider.GetRequiredService<IPackageSessionService>()
            .DonPhienQuaHanAsync(dongGoiSettings.SoGioGiuPhien);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Khong don duoc phien dong goi qua han luc khoi dong.");
    }
}
#endregion

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
