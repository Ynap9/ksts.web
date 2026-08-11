# Đóng gói plugin ký số thành bộ cài mà BE phát cho người dùng.
#
# Sinh ra ksts.be/ksts.be.api/Plugins/ksts-plugin-setup.zip — endpoint api/core/plugin/bo-cai/noi-dung
# đọc đúng file này. Chạy lại mỗi khi sửa mã nguồn plugin hoặc đổi bộ cài middleware.
#
# Bản publish là self-contained single-file: máy người dùng KHÔNG cần cài .NET runtime.

$ErrorActionPreference = "Stop"

$goc = $PSScriptRoot
$tam = Join-Path $env:TEMP ("ksts-plugin-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
$dich = Join-Path $goc "..\ksts.be\ksts.be.api\Plugins"
$goi = Join-Path $tam "goi"

Write-Host "1/4 Publish plugin..." -ForegroundColor Cyan
dotnet publish (Join-Path $goc "ksts.plugin.api\ksts.plugin.api.csproj") `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true -p:DebugType=none `
    -o $tam --nologo
if ($LASTEXITCODE -ne 0) { throw "Publish plugin that bai." }

Write-Host "2/4 Gom plugin va trinh cai dat..." -ForegroundColor Cyan
# Chỉ lấy thứ cần để chạy. Bỏ .pdb (ký hiệu gỡ lỗi) và web.config (chỉ dành cho IIS).
New-Item -ItemType Directory -Force -Path $goi | Out-Null
Copy-Item (Join-Path $tam "KstsPlugin.exe") $goi
Copy-Item (Join-Path $tam "appsettings.json") $goi
Copy-Item (Join-Path $goc "installer\*") $goi -Recurse

Write-Host "3/4 Gom middleware bit4id..." -ForegroundColor Cyan
# Middleware là phần mềm của hãng token, không nằm trong repo. Có thì gói kèm để trình cài đặt tự cài ngầm;
# không có thì vẫn đóng gói được, chỉ là người dùng phải tự cài middleware trước.
$nguonVendor = Join-Path $goc "vendor\bit4id"
$boCaiVendor = if (Test-Path $nguonVendor) {
    Get-ChildItem $nguonVendor -File | Where-Object { $_.Extension -in ".exe", ".msi" } | Select-Object -First 1
} else { $null }

if ($null -eq $boCaiVendor) {
    Write-Host "    KHONG THAY bo cai bit4id trong vendor\bit4id." -ForegroundColor Yellow
    Write-Host "    Bo cai se KHONG tu cai duoc middleware. Xem vendor\bit4id\*.md." -ForegroundColor Yellow
}
else {
    $dichVendor = Join-Path $goi "bit4id"
    New-Item -ItemType Directory -Force -Path $dichVendor | Out-Null
    Copy-Item $boCaiVendor.FullName $dichVendor
    $fileThamSo = Join-Path $nguonVendor "tham-so.txt"
    if (Test-Path $fileThamSo) { Copy-Item $fileThamSo $dichVendor }
    Write-Host "    Da kem $($boCaiVendor.Name) ($([math]::Round($boCaiVendor.Length/1MB,1)) MB)." -ForegroundColor Green
}

# Tài liệu dành cho người đóng gói, không phát cho người dùng cuối.
$thua = Join-Path $goi "DAT-BO-CAI-BIT4ID-VAO-DAY.md"
if (Test-Path $thua) { Remove-Item $thua -Force }

Write-Host "4/4 Nen thanh bo cai..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $dich | Out-Null
$zip = Join-Path $dich "ksts-plugin-setup.zip"
Compress-Archive -Path (Join-Path $goi "*") -DestinationPath $zip -CompressionLevel Optimal -Force

$mb = [math]::Round((Get-Item $zip).Length / 1MB, 1)
Write-Host ""
Write-Host "Xong: $zip ($mb MB)" -ForegroundColor Green
Write-Host "Nho build lai ksts.be.api de file duoc chep sang thu muc output." -ForegroundColor Yellow
