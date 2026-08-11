# Plugin ký số KSTS

Plugin chạy nền trên máy bạn, làm cầu nối giữa trang web ký số và **USB token** cắm ở máy.

## Cài đặt

Bấm đúp **`CAI-DAT.cmd`**. Bộ cài tự làm ba việc:

1. Kiểm tra máy đã có middleware đọc USB token chưa. **Chưa có thì cài ngầm**, không hiện cửa sổ nào —
   bước này xin quyền quản trị vì middleware đăng ký cho cả máy.
2. Chép plugin vào `%LocalAppData%\KstsPlugin`.
3. Bật tự khởi động cùng Windows rồi chạy plugin luôn.

Xong là dùng được ngay, không cần khởi động lại máy.

## Kiểm tra đã chạy chưa

Mở trình duyệt vào `http://127.0.0.1:17739/api/plugin/trang-thai`. Thấy dòng chữ có
`"sanSang":true` là đạt.

Cắm token vào rồi mở `certmgr.msc` → **Personal** → **Certificates**: thấy chứng thư của mình ở đó nghĩa là
middleware đã nhận token. Không thấy thì token chưa được nhận, xem mục Xử lý sự cố.

## Gỡ cài đặt

Bấm chuột phải `go-cai-dat.ps1` → **Run with PowerShell**.

Thao tác này **không** gỡ middleware bit4id, vì nó là phần mềm dùng chung cho mọi ứng dụng chữ ký số trên
máy — gỡ đi là làm hỏng cả những phần mềm khác. Muốn gỡ thì gỡ riêng trong Apps & Features.

## Xử lý sự cố

**Windows chặn khi chạy lần đầu (SmartScreen)** — bấm *More info* → *Run anyway*. Phần mềm chưa mua chứng
thư ký mã nguồn nên Windows cảnh báo với mọi ứng dụng lạ.

**Không thấy chứng thư nào** — theo thứ tự: token đã cắm chưa · rút ra cắm lại · mở `certmgr.msc` xem
Windows có nhận không · nếu vẫn không thấy thì middleware chưa cài được, liên hệ đơn vị cấp chứng thư số.

**Trang web báo chưa cài plugin** — plugin có thể đã tắt. Mở lại `%LocalAppData%\KstsPlugin\KstsPlugin.exe`.

## Plugin làm gì với máy của bạn

- Chỉ nghe ở `127.0.0.1` — **máy khác trong mạng không gọi được**.
- Chỉ đọc danh sách chứng thư và ra lệnh ký; **không đọc file, không gửi dữ liệu đi đâu**.
- **Không bao giờ chạm vào mã PIN.** PIN đi thẳng từ bàn phím vào middleware của token qua Windows, plugin
  không nhìn thấy một ký tự nào. Hộp nhập PIN bạn thấy là của Windows, không phải của phần mềm này.
- Khoá bí mật nằm trong chip của token và **không trích xuất ra được**, kể cả bởi chính plugin.
