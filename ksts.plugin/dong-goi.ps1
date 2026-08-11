# Đóng gói plugin ký số thành bộ cài mà BE phát cho người dùng.
#
# Sinh ra ksts.be/ksts.be.api/Plugins/KstsPlugin.exe — endpoint api/core/plugin/bo-cai/noi-dung đọc đúng file
# này. Chạy lại mỗi khi sửa mã nguồn plugin hoặc đổi bộ cài middleware.
#
# Bản ra là MỘT file exe: self-contained (máy người dùng không cần .NET runtime) và nhúng sẵn bộ cài
# middleware bit4id. Người dùng tải một file, chạy một file; chính file đó tự cài middleware, tự chép mình
# vào %LocalAppData% rồi chạy nền.

$ErrorActionPreference = "Stop"

$goc = $PSScriptRoot
$tam = Join-Path $env:TEMP ("ksts-plugin-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
$dich = Join-Path $goc "..\ksts.be\ksts.be.api\Plugins"

Write-Host "1/3 Kiem tra middleware bit4id..." -ForegroundColor Cyan
# Middleware là phần mềm của hãng token, không nằm trong repo. Có thì được nhúng vào exe lúc build; không có
# thì vẫn đóng gói được, chỉ là người dùng phải tự cài middleware trước.
$nguonVendor = Join-Path $goc "vendor\bit4id"
$boCaiVendor = if (Test-Path $nguonVendor) {
    Get-ChildItem $nguonVendor -File | Where-Object { $_.Extension -in ".exe", ".msi" } | Select-Object -First 1
} else { $null }

if ($null -eq $boCaiVendor) {
    Write-Host "    KHONG THAY bo cai bit4id trong vendor\bit4id." -ForegroundColor Yellow
    Write-Host "    Ban ra se KHONG tu cai duoc middleware. Xem vendor\bit4id\*.md." -ForegroundColor Yellow
}
else {
    Write-Host "    Se nhung $($boCaiVendor.Name) ($([math]::Round($boCaiVendor.Length/1MB,1)) MB)." -ForegroundColor Green
}

Write-Host "2/3 Publish plugin..." -ForegroundColor Cyan
dotnet publish (Join-Path $goc "ksts.plugin.api\ksts.plugin.api.csproj") `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true -p:DebugType=none `
    -o $tam --nologo
if ($LASTEXITCODE -ne 0) { throw "Publish plugin that bai." }

Write-Host "3/3 Chep sang thu muc phat hanh..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $dich | Out-Null
$exe = Join-Path $dich "KstsPlugin.exe"
Copy-Item (Join-Path $tam "KstsPlugin.exe") $exe -Force
Remove-Item $tam -Recurse -Force -ErrorAction SilentlyContinue

$mb = [math]::Round((Get-Item $exe).Length / 1MB, 1)
Write-Host ""
Write-Host "Xong: $exe ($mb MB)" -ForegroundColor Green
Write-Host "Chay khi phat trien thi build thuong; file exe nay chi tu cai khi la ban publish single-file." -ForegroundColor DarkGray
