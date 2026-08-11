# Cài plugin ký số KSTS lên máy người dùng.
#
# Hai phần, làm theo đúng thứ tự này:
#   1. Middleware bit4id — điều kiện để chứng thư trên USB token hiện ra trong Windows certificate store.
#      Máy đã có thì BỎ QUA, chưa có thì cài NGẦM từ file kèm trong gói.
#   2. Plugin — cài per-user vào %LocalAppData%, tự chạy cùng Windows.
#
# Chỉ leo thang quyền quản trị khi thật sự phải cài middleware; riêng phần plugin không cần UAC.

param(
    [switch]$DaLeoQuyen
)

$ErrorActionPreference = "Stop"

$goc = $PSScriptRoot
$thuMucCai = Join-Path $env:LOCALAPPDATA "KstsPlugin"
$tenExe = "KstsPlugin.exe"
$khoaAutostart = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$tenAutostart = "KstsPlugin"

function Ghi($chu, $mau = "Gray") { Write-Host $chu -ForegroundColor $mau }

<#
.SYNOPSIS
Middleware bit4id đã có trên máy hay chưa.

Hỏi thẳng danh sách CSP/KSP đã đăng ký với Windows chứ không dò tên trong Programs and Features: cái quyết
định việc token hiện ra trong certificate store là provider mật mã có được đăng ký hay không, còn mục trong
Programs and Features chỉ nói ai đó từng chạy bộ cài. Có bản gỡ lỗi để lại mục mà mất provider.
#>
function CoMiddleware {
    $duong = @(
        "HKLM:\SOFTWARE\Microsoft\Cryptography\Defaults\Provider",
        "HKLM:\SOFTWARE\Microsoft\Cryptography\Providers"
    )

    foreach ($d in $duong) {
        if (-not (Test-Path $d)) { continue }
        $ten = Get-ChildItem $d -ErrorAction SilentlyContinue | ForEach-Object { $_.PSChildName }
        if ($ten -match "bit4id|bit4xpki") { return $true }
    }

    # Đường dò dự phòng: một số bản bit4id đăng ký provider theo tên khác nhưng luôn đặt thư viện vào System32.
    if (Get-ChildItem "$env:WINDIR\System32\bit4*.dll" -ErrorAction SilentlyContinue) { return $true }

    return $false
}

<#
.SYNOPSIS
Tìm file cài middleware đi kèm gói. Không có thì trả về $null.
#>
function TimBoCaiMiddleware {
    $thuMuc = Join-Path $goc "bit4id"
    if (-not (Test-Path $thuMuc)) { return $null }

    return Get-ChildItem $thuMuc -File |
        Where-Object { $_.Extension -in ".exe", ".msi" } |
        Select-Object -First 1
}

<#
.SYNOPSIS
Cài middleware ở chế độ ngầm, không hiện giao diện nào.

Tham số ngầm khác nhau theo loại bộ cài, nên đọc từ tham-so.txt nếu người đóng gói có đặt: bit4id phát hành
khi thì MSI, khi thì InstallShield, khi thì NSIS, mỗi loại một cờ riêng.
#>
function CaiMiddleware($file) {
    $fileThamSo = Join-Path (Join-Path $goc "bit4id") "tham-so.txt"
    $thamSo = if (Test-Path $fileThamSo) { (Get-Content $fileThamSo -Raw).Trim() } else { $null }

    if ($file.Extension -eq ".msi") {
        if (-not $thamSo) { $thamSo = "/qn /norestart" }
        $args = "/i `"$($file.FullName)`" $thamSo"
        Ghi "    msiexec $args"
        $p = Start-Process "msiexec.exe" -ArgumentList $args -Wait -PassThru
    }
    else {
        if (-not $thamSo) { $thamSo = "/S" }
        Ghi "    $($file.Name) $thamSo"
        $p = Start-Process $file.FullName -ArgumentList $thamSo -Wait -PassThru
    }

    # 3010 = cài xong nhưng cần khởi động lại; vẫn tính là thành công.
    if ($p.ExitCode -ne 0 -and $p.ExitCode -ne 3010) {
        throw "Bo cai middleware tra ve ma loi $($p.ExitCode)."
    }
}

# ===== 1. Middleware =====

Ghi "[1/2] Kiem tra middleware doc USB token..." "Cyan"

if (CoMiddleware) {
    Ghi "    Da co san, bo qua." "Green"
}
else {
    $boCai = TimBoCaiMiddleware
    if ($null -eq $boCai) {
        Ghi "    CHUA CO middleware, va goi nay khong kem file cai." "Yellow"
        Ghi "    Plugin van cai duoc nhung SE KHONG THAY chung thu tren token." "Yellow"
        Ghi "    Lay bo cai bit4id tu don vi cap chung thu so roi cai truoc." "Yellow"
    }
    elseif (-not $DaLeoQuyen -and -not ([Security.Principal.WindowsPrincipal] `
            [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
                [Security.Principal.WindowsBuiltInRole]::Administrator)) {
        # Cài middleware phải có quyền quản trị vì nó đăng ký provider mật mã cho cả máy.
        Ghi "    Can quyen quan tri de cai middleware, dang xin nang quyen..." "Yellow"
        $p = Start-Process "powershell.exe" -Verb RunAs -Wait -PassThru -ArgumentList @(
            "-ExecutionPolicy", "Bypass", "-NoProfile", "-File", "`"$PSCommandPath`"", "-DaLeoQuyen")
        if ($p.ExitCode -ne 0) { throw "Buoc cai co quyen quan tri that bai." }
        exit 0
    }
    else {
        Ghi "    Dang cai $($boCai.Name) o che do ngam..."
        CaiMiddleware $boCai
        Ghi "    Cai middleware xong." "Green"
    }
}

# ===== 2. Plugin =====

Ghi "[2/2] Cai plugin ky so..." "Cyan"

# Plugin đang chạy thì phải dừng, không thì không ghi đè được file exe.
Get-Process "KstsPlugin" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

New-Item -ItemType Directory -Force -Path $thuMucCai | Out-Null
Copy-Item (Join-Path $goc $tenExe) $thuMucCai -Force
$fileCauHinh = Join-Path $goc "appsettings.json"
if (Test-Path $fileCauHinh) { Copy-Item $fileCauHinh $thuMucCai -Force }

$duongExe = Join-Path $thuMucCai $tenExe

# Autostart theo NGƯỜI DÙNG (HKCU) chứ không phải theo máy: cài per-user thì không cần UAC, không cài driver,
# không dựng service SYSTEM.
New-ItemProperty -Path $khoaAutostart -Name $tenAutostart -Value "`"$duongExe`"" `
    -PropertyType String -Force | Out-Null

Start-Process $duongExe
Start-Sleep -Seconds 2

try {
    $tt = Invoke-RestMethod "http://127.0.0.1:17739/api/plugin/trang-thai" -TimeoutSec 8
    Ghi "    Plugin dang chay: $($tt.data.ten) $($tt.data.phienBan)" "Green"
}
catch {
    Ghi "    Da cai xong nhung chua goi duoc plugin. Thu mo lai: $duongExe" "Yellow"
}

Ghi ""
Ghi "HOAN TAT. Plugin nam tai $thuMucCai va tu chay cung Windows." "Green"
