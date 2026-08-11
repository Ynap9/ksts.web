# Bộ cài plugin ký số

Backend phát file trong thư mục này qua `GET api/core/plugin/bo-cai/noi-dung` cho người dùng tải về. Đặt đúng
một file, đúng tên — `PluginConstants.SetupFileName` khớp cứng chứ không dò theo đuôi:

```
KstsPlugin.exe
```

Đây là bản publish self-contained single-file của `ksts.plugin`, **đã nhúng sẵn bộ cài middleware bit4id**.
Người dùng tải một file, bấm đúp một lần: nó tự cài middleware nếu máy chưa có, tự chép mình vào
`%LocalAppData%\KstsPlugin`, bật tự khởi động rồi chạy nền. Sinh ra bằng `ksts.plugin/dong-goi.ps1`.

Khi chạy ở máy phát triển, `.csproj` chép file sang thư mục output nên chỉ cần build lại là API thấy.

## Trên máy chủ

File là sản phẩm build (~95 MB) nên **không nằm trong git**. Máy chủ dựng image từ bản clone của repo, do đó
bộ cài không tự đi theo image — thiếu bước chép tay này thì màn Ký số báo *"Máy chủ chưa có bộ cài plugin"*.

`deploy/docker-compose.yml` mount thẳng thư mục này vào `/app/Plugins` (chỉ đọc), nên chép file lên là xong:

```bash
scp ksts.be/ksts.be.api/Plugins/KstsPlugin.exe <user>@<may-chu>:<repo>/ksts.be/ksts.be.api/Plugins/
```

**Không phải build lại image cũng không phải khởi động lại container** — backend mở file theo từng lần tải.

Thư mục rỗng **không phải lỗi**: API trả `exists = false`, giao diện khoá nút tải kèm lời nhắn liên hệ quản
trị hệ thống.

⚠️ Volume mount **che** thư mục cùng đường dẫn trong image. Nếu đổi mount sang chỗ khác thì file nằm ở đây
cũng vô nghĩa, vì thư mục được mount sẽ đè lên.
