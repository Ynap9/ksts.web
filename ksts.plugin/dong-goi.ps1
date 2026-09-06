# Đóng gói plugin ký số thành bộ cài mà BE phát cho người dùng.
#
# Sinh ra "Ký số plugin.exe" vào Plugins/ của MỌI backend dùng plugin (ksts.be và kssm.be) — endpoint
# api/core/plugin/bo-cai/noi-dung của từng bên đọc đúng file này. Chạy lại mỗi khi sửa mã nguồn plugin hoặc
# đổi bộ cài middleware.
#
# Bản ra là MỘT file exe: self-contained (máy người dùng không cần .NET runtime) và nhúng sẵn bộ cài
# middleware bit4id. Người dùng tải một file, chạy một file; chính file đó tự cài middleware, tự chép mình
# vào %LocalAppData% rồi chạy nền.

$ErrorActionPreference = "Stop"

$goc = $PSScriptRoot
$tam = Join-Path $env:TEMP ("ksts-plugin-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
# Mỗi backend tự phát bộ cài của mình, không bên nào đi mượn file của bên kia: máy chủ triển khai
# riêng từng dịch vụ.
$dich = @(
    (Join-Path $goc "..\ksts.be\ksts.be.api\Plugins"),
    (Join-Path $goc "..\kssm.be\kssm.be.api\Plugins")
)

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
# Lay ten exe tu chinh ban publish thay vi ghi cung: ten co dau, ma PowerShell 5.1 doc .ps1 khong BOM theo
# bang ma ANSI nen chuoi co dau viet thang trong script se ra sai ten file.
$nguon = Get-ChildItem $tam -Filter *.exe -File | Select-Object -First 1
if ($null -eq $nguon) { throw "Khong thay file exe nao trong ban publish." }

$daChep = @()
foreach ($d in $dich) {
    New-Item -ItemType Directory -Force -Path $d | Out-Null
    $exe = Join-Path $d $nguon.Name
    Copy-Item $nguon.FullName $exe -Force
    $daChep += $exe
}
Remove-Item $tam -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
foreach ($exe in $daChep) {
    $mb = [math]::Round((Get-Item $exe).Length / 1MB, 1)
    Write-Host "Xong: $exe ($mb MB)" -ForegroundColor Green
}
Write-Host "Chay khi phat trien thi build thuong; file exe nay chi tu cai khi la ban publish single-file." -ForegroundColor DarkGray
