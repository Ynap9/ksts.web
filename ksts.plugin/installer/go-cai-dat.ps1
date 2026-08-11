# Gỡ plugin ký số KSTS khỏi máy.
#
# KHÔNG đụng tới middleware bit4id: nó là phần mềm dùng chung cho mọi ứng dụng chữ ký số trên máy, gỡ nó đi
# là làm hỏng cả những phần mềm khác. Muốn gỡ thì gỡ riêng trong Apps & Features.

$ErrorActionPreference = "Stop"

$thuMucCai = Join-Path $env:LOCALAPPDATA "KstsPlugin"
$khoaAutostart = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$tenAutostart = "KstsPlugin"

Write-Host "Dung plugin..." -ForegroundColor Cyan
Get-Process "KstsPlugin" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

Write-Host "Xoa autostart..." -ForegroundColor Cyan
Remove-ItemProperty -Path $khoaAutostart -Name $tenAutostart -ErrorAction SilentlyContinue

Write-Host "Xoa thu muc cai dat..." -ForegroundColor Cyan
if (Test-Path $thuMucCai) {
    Remove-Item $thuMucCai -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "Da go plugin. Middleware bit4id duoc giu nguyen." -ForegroundColor Green
