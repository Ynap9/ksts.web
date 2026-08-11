using ksts.plugin.applications.ChungThuSo.Implements;
using ksts.plugin.applications.ChungThuSo.Interfaces;
using ksts.plugin.applications.Plugin.Implements;
using ksts.plugin.applications.Plugin.Interfaces;
using ksts.plugin.external.Certificates.Implements;
using ksts.plugin.external.Certificates.Interfaces;
using ksts.plugin.shared.Constants;

var builder = WebApplication.CreateBuilder(args);

// Chỉ nghe trên loopback: plugin phục vụ đúng trình duyệt của máy này, không lộ ra mạng LAN.
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(PluginConstants.Port);
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    // Origin không phải hàng rào bảo mật (curl đặt được tuỳ ý), nhưng thiếu CORS thì trình duyệt không đọc
    // được kết quả - đây là điều kiện để FE chạy, không phải lớp phòng thủ.
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddControllers();

builder.Services.AddSingleton<ICertificateProvider, CertificateProvider>();
builder.Services.AddSingleton<ITokenVerifier, TokenVerifier>();
builder.Services.AddSingleton<IChungThuSoService, ChungThuSoService>();
builder.Services.AddSingleton<IPluginService, PluginService>();

var app = builder.Build();

app.UseCors();
app.MapControllers();

app.Logger.LogInformation("{Ten} {PhienBan} đang nghe tại http://127.0.0.1:{Port}",
    PluginConstants.Ten, PluginConstants.PhienBan, PluginConstants.Port);

app.Run();
