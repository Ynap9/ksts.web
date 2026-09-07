# Bộ cài plugin ký số

Backend phát file trong thư mục này qua `GET api/core/plugin/bo-cai/noi-dung` cho người dùng tải về. Đặt đúng
một file, đúng tên — `PluginConstants.SetupFileName` khớp cứng chứ không dò theo đuôi:

```
Ký số plugin.exe
```

Đây là bản publish self-contained single-file của `ksts.plugin`, **đã nhúng sẵn bộ cài middleware bit4id**.
Người dùng tải một file, bấm đúp một lần: nó tự cài middleware nếu máy chưa có, tự chép mình vào
`%LocalAppData%\KySoPlugin`, bật tự khởi động rồi chạy nền. Sinh ra bằng `ksts.plugin/dong-goi.ps1` — script
đó chép cùng một exe sang `Plugins/` của **cả** `ksts.be` lẫn `kssm.be`.

Khi chạy ở máy phát triển, `.csproj` chép file sang thư mục output nên chỉ cần build lại là API thấy.

## Trên máy chủ

File là sản phẩm build (~95 MB) nên **không nằm trong git** (`.gitignore` bỏ qua `*.exe`). Máy chủ dựng image
từ bản clone của repo, do đó bộ cài không tự đi theo image — thiếu bước chép tay này thì FE báo *"Máy chủ chưa
có bộ cài plugin"*.

```bash
scp "kssm.be/kssm.be.api/Plugins/Ký số plugin.exe" <user>@<may-chu>:<repo>/kssm.be/kssm.be.api/Plugins/
```

⚠️ `kssm.be` **chưa có mount volume** cho thư mục này như `ksts.be` — `deploy/docker-compose.yml` chỉ khai
`ksts.be`. Chạy `kssm.be` trong container thì phải mount `Plugins/` vào `/app/Plugins`, nếu không exe chép lên
máy chủ nằm ngoài container và endpoint vẫn trả `exists = false`.

Thư mục rỗng **không phải lỗi**: API trả `exists = false`, FE khoá nút tải kèm lời nhắn liên hệ quản trị
hệ thống. Chỉ khi gọi `bo-cai/noi-dung` mà thiếu file mới ném `1080 PluginSetupMissing`.
