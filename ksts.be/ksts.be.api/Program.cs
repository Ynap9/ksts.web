
using System.Globalization;
using ksts.be.api.Workers;
using ksts.be.application.Auth.Implements;
using ksts.be.application.Auth.Interfaces;
using ksts.be.applications.Base;
using ksts.be.applications.GiayBao.Implements;
using ksts.be.applications.GiayBao.Interfaces;
using ksts.be.applications.Plugin.Implements;
using ksts.be.applications.Plugin.Interfaces;
using ksts.be.applications.Signing.Implements;
using ksts.be.applications.Signing.Interfaces;
using ksts.be.applications.Template.Implements;
using ksts.be.applications.Template.Interfaces;
using ksts.be.external.Certificates.Implements;
using ksts.be.external.Certificates.Interfaces;
using ksts.be.external.Images.Implements;
using ksts.be.external.Images.Interfaces;
using ksts.be.external.Pdf.Implements;
using ksts.be.external.Pdf.Interfaces;
using ksts.be.external.Placement.Implements;
using ksts.be.external.Placement.Interfaces;
using ksts.be.external.Excel.Implements;
using ksts.be.external.Excel.Interfaces;
using ksts.be.external.Gotenberg.Implements;
using ksts.be.external.Gotenberg.Interfaces;
using ksts.be.external.Html.Implements;
using ksts.be.external.Html.Interfaces;
using ksts.be.external.Jobs.Implements;
using ksts.be.external.Jobs.Interfaces;
using ksts.be.external.Qr.Implements;
using ksts.be.external.Qr.Interfaces;
using ksts.be.applications.LoKy.Implements;
using ksts.be.applications.LoKy.Interfaces;
using ksts.be.external.Signing.Implements;
using ksts.be.external.Signing.Interfaces;
using ksts.be.external.Tsa.Implements;
using ksts.be.external.Tsa.Interfaces;
using ksts.be.external.Storage.Implements;
using ksts.be.external.Storage.Interfaces;
using ksts.be.domain.Auth;
using ksts.be.infrastructure.Persistence;
using ksts.be.infrastructure.Persistence.Seeder;
using ksts.be.shared.Constants.Auth;
using ksts.be.shared.Settings;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;
using OpenIddict.Abstractions;
using System.Text;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Info("Starting application...");
var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("EnvironmentName => " + builder.Environment.EnvironmentName);


builder.Logging.ClearProviders();
builder.Host.UseNLog();

#region db
string connectionString = builder.Configuration.GetConnectionString("KY_SO_WEB")
    ?? throw new InvalidOperationException("Kh�ng t�m th?y connection string \"KY_SO_WEB\" trong appsettings.json");



builder.Services.AddDbContext<KstsDbContext>(options =>
{
    options.UseSqlServer(connectionString, options =>
    {
        //options.MigrationsAssembly(typeof(Program).Namespace);
        //options.MigrationsHistoryTable(DbSchemas.TableMigrationsHistory, DbSchemas.Core);
        options.CommandTimeout(600);
    });
    options.UseOpenIddict(); // Register OpenIddict entities
}, ServiceLifetime.Scoped);
#endregion


#region cors
string allowOrigins = builder.Configuration.GetSection("AllowedHosts")!.Value!;
//File.WriteAllText("cors.now.txt", $"CORS: {allowOrigins}");;/'\,
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
                //.AllowCredentials()
                .WithExposedHeaders("Content-Disposition");
        }
    );
    options.AddPolicy("SignalRPolicy", builder =>
    {
        builder
            .SetIsOriginAllowed(_ => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});
#endregion

#region identity
// 2. Add Identity
builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<KstsDbContext>()
    .AddDefaultTokenProviders();
#endregion
#region auth
string secretKey = builder.Configuration.GetSection("AuthServer:SecretKey").Value!;




builder.Services.Configure<AuthServerSettings>(builder.Configuration.GetSection("AuthServer"));
builder.Services.Configure<S3Settings>(builder.Configuration.GetSection("S3"));
builder.Services.Configure<FileSettings>(builder.Configuration.GetSection("FileConfig:File"));
builder.Services.Configure<ConvertFileSettings>(builder.Configuration.GetSection("ConvertFile"));



builder.Services.AddOpenIddict()
    .AddCore(opt =>
    {
        opt.UseEntityFrameworkCore()
           .UseDbContext<KstsDbContext>();
    })
    // Register the OpenIddict server components.
    .AddServer(options =>
    {
        // Enable the token endpoint.
        options.SetTokenEndpointUris("connect/token")
            .SetAuthorizationEndpointUris("/connect/authorize")
        ;

        // Enable the client credentials flow.
        options.AllowClientCredentialsFlow()
                .AllowPasswordFlow()
                .AllowRefreshTokenFlow()
                .AllowAuthorizationCodeFlow()
                .RequireProofKeyForCodeExchange()
                ;

        options.AcceptAnonymousClients();
        options.DisableAccessTokenEncryption();

        options.RegisterScopes(OpenIddictConstants.Scopes.OpenId, OpenIddictConstants.Scopes.OfflineAccess, OpenIddictConstants.Scopes.Profile);

        // Register the signing and encryption credentials.
        //options.AddDevelopmentEncryptionCertificate()
        //       .AddDevelopmentSigningCertificate();

        // Development: ephemeral keys (or dev certs if you want)
        options.AddEphemeralEncryptionKey()
               .AddEphemeralSigningKey();

        // ?? Symmetric signing key
        var secret = Encoding.UTF8.GetBytes(secretKey);
        options.AddEncryptionKey(new SymmetricSecurityKey(secret));
        options.AddSigningKey(new SymmetricSecurityKey(secret));

        // Register the ASP.NET Core host and configure the ASP.NET Core options.
        options.UseAspNetCore()
                .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .DisableTransportSecurityRequirement();

    });

builder.Services.AddAuthentication(options =>
{
    //options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
    .AddJwtBearer(
        options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateLifetime = true,        // ? only check exp & nbf
                ClockSkew = TimeSpan.Zero, // no extra leeway

                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                // Symmetric key (HMAC) example
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(secretKey)
                ),

                // ?? Accept both "JWT" and "at+jwt" as token types
                ValidTypes = new[] { "JWT", "at+jwt" }
            };
            options.RequireHttpsMetadata = false;
        }
    )
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);



builder.Services.AddAuthorization();


builder.Services.AddHostedService<AuthWorker>();
#endregion


#region mapper
// Build mapper configuration
builder.Services.AddAutoMapper(cfg => { }, typeof(MappingProfile));
#endregion



// Add services to the container.
#region service
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPermissionsService, PermissionsService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<IPluginService, PluginService>();
builder.Services.AddScoped<IGiayBaoService, GiayBaoService>();
builder.Services.AddScoped<ILoKyService, LoKyService>();

// External: dịch vụ bên thứ ba / dùng chung. Singleton vì không giữ trạng thái theo request, và
// S3FileStorage giữ pool kết nối của AmazonS3Client nên dựng lại mỗi request là phí kết nối.
builder.Services.AddSingleton<IS3FileStorage, S3FileStorage>();
builder.Services.AddSingleton<ITemplateImageStorage, TemplateImageStorage>();
builder.Services.AddSingleton<ICertificateTrustValidator, CertificateTrustValidator>();
builder.Services.AddSingleton<ICertificateProvider, CertificateProvider>();
builder.Services.AddSingleton<IPdfTextLocator, PdfTextLocator>();
builder.Services.AddSingleton<IImageSizeReader, ImageSizeReader>();
builder.Services.AddSingleton<ISealPlacementResolver, SealPlacementResolver>();
builder.Services.AddSingleton<IQrCodeSvgRenderer, QrCodeSvgRenderer>();
builder.Services.AddSingleton<IExcelSheetReader, ExcelSheetReader>();
builder.Services.AddSingleton<IHtmlDocumentFiller, HtmlDocumentFiller>();
builder.Services.AddSingleton<IGotenbergConverter, GotenbergConverter>();
builder.Services.AddSingleton<ITimestampClient, TimestampClient>();
builder.Services.AddSingleton<IZipJobStore, ZipJobStore>();
builder.Services.AddSingleton<ICmsAssembler, CmsAssembler>();
builder.Services.AddSingleton<IPdfRevisionReader, PdfRevisionReader>();
builder.Services.AddSingleton<IPdfObjectWriter, PdfObjectWriter>();
builder.Services.AddSingleton<IPdfAppearanceBuilder, PdfAppearanceBuilder>();
builder.Services.AddSingleton<IPdfPreparer, PdfPreparer>();
builder.Services.AddSingleton<IPdfContentWriter, PdfContentWriter>();
// Hàng đợi chờ chữ ký giữ trạng thái phiên của từng lô nên phải là Singleton.
builder.Services.AddSingleton<IHangDoiKy, HangDoiKy>();

// Nguồn ký: mặc định là plugin ở máy người dùng - khoá nằm trong token, máy chủ chỉ gửi SignedAttributes đi
// và nhận chữ ký về. Đặt Signing:Nguon = "store" để quay về đọc certificate store của máy chạy API, chỉ dùng
// được khi API và token nằm trên cùng một máy Windows.
if (string.Equals(builder.Configuration["Signing:Nguon"], "store", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<ISigningKey, StoreSigningKey>();
}
else
{
    builder.Services.AddSingleton<ISigningKey, PluginSigningKey>();
}
builder.Services.AddSingleton<ILoKyFileStorage, LoKyFileStorage>();

// Tiến trình ký chạy nền: Singleton vì lô sống lâu hơn request, tự mở scope riêng cho từng file.
builder.Services.AddSingleton<IKySoRunner, KySoRunner>();

#endregion

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "My API", Version = "v1" });

    // ?? Add Bearer JWT Security Definition
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIs...\"",
    });

    // ?? Add Security Requirement (apply globally to all endpoints)
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

#region Seed data
// Run seeding inside scope
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    await SeedUser.SeedAsync(userManager, roleManager);

}
#endregion

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Model binding của form đọc số theo CurrentCulture. Máy đặt vùng dùng dấu "." làm phân nhóm nghìn sẽ nuốt
// mất dấu thập phân: "0.69" thành 69, khiến toạ độ tỉ lệ 0..1 phình lên và bị chặn là nằm ngoài trang.
// Ép invariant cho mọi request để số gửi lên luôn được hiểu như nhau, không phụ thuộc cấu hình máy chủ.
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture(CultureInfo.InvariantCulture.Name)
    .AddSupportedCultures(CultureInfo.InvariantCulture.Name)
    .AddSupportedUICultures(CultureInfo.InvariantCulture.Name));

app.UseCors(ProgramExtensions.CorsPolicy);
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.MapHealthChecks("/health");
app.Run();



