using ksts.plugin.applications.CaiDat.Implements;
using ksts.plugin.applications.CaiDat.Interfaces;
using ksts.plugin.applications.ChungThuSo.Implements;
using ksts.plugin.applications.ChungThuSo.Interfaces;
using ksts.plugin.applications.Plugin.Implements;
using ksts.plugin.applications.Plugin.Interfaces;
using ksts.plugin.external.Certificates.Implements;
using ksts.plugin.applications.KySo.Implements;
using ksts.plugin.applications.KySo.Interfaces;
using ksts.plugin.external.Certificates.Interfaces;
using ksts.plugin.external.Signing.Implements;
using ksts.plugin.external.Signing.Interfaces;
using ksts.plugin.external.Setup.Implements;
using ksts.plugin.shared.Constants;
using System.Text;

// Cửa sổ console mặc định dùng bảng mã cũ, tiếng Việt ra dấu hỏi. Đặt trước mọi dòng in ra.
Console.OutputEncoding = Encoding.UTF8;

// Một file exe đóng hai vai. Dựng tay ba service này thay vì qua DI vì phải phân vai XONG rồi mới biết có
// cần web host hay không.
ICaiDatService caiDat = new CaiDatService(new MiddlewareService(), new TuCaiDatService());

if (args.Contains(CaiDatConstants.ThamSoGoCaiDat))
{
    caiDat.ChayLuotGoCaiDat();
    return;
}

// Tiến trình con chạy quyền quản trị: mã thoát đã nói lên thành hay bại, không dừng chờ ai bấm phím.
if (args.Contains(CaiDatConstants.ThamSoCaiMiddleware))
{
    caiDat.ChayLuotCaiMiddleware();
    return;
}

if (!caiDat.LaLuotChayPlugin())
{
    try
    {
        caiDat.ChayLuotCaiDat();
    }
    catch (Exception ex)
    {
        Console.WriteLine();
        Console.WriteLine($"LỖI: {ex.Message}");
    }

    // Người dùng bấm đúp từ Explorer thì cửa sổ đóng ngay khi tiến trình thoát, không kịp đọc gì.
    if (!Console.IsInputRedirected)
    {
        Console.WriteLine();
        Console.WriteLine("Bấm phím bất kỳ để đóng cửa sổ này.");
        Console.ReadKey(intercept: true);
    }

    return;
}

var builder = WebApplication.CreateBuilder(args);

// Chỉ nghe trên loopback: plugin phục vụ đúng trình duyệt của máy này, không lộ ra mạng LAN.
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(PluginConstants.Port);
});

// Danh sách ghim trong mã là nguồn chính vì bản phát hành không kèm file cấu hình; appsettings.json chỉ để
// bổ sung origin khi phát triển.
var allowedOrigins = PluginConstants.OriginMacDinh
    .Concat(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

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

// Phiên ký là Singleton vì nó GIỮ handle khoá đã mở: mỗi request một phiên mới thì lô nào cũng hỏi PIN từng file.
builder.Services.AddSingleton<ISigningSession, SigningSession>();
builder.Services.AddSingleton<IKySoService, KySoService>();

var app = builder.Build();

app.UseCors();
app.MapControllers();

app.Logger.LogInformation("{Ten} {PhienBan} đang nghe tại http://127.0.0.1:{Port}",
    PluginConstants.Ten, PluginConstants.PhienBan, PluginConstants.Port);

app.Run();
