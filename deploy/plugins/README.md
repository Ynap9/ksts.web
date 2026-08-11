# Bộ cài plugin ký số trên máy chủ

Thư mục này được mount vào container backend tại `/app/Plugins` (chỉ đọc). Backend phát file trong đây qua
`GET api/core/plugin/bo-cai/noi-dung` cho người dùng tải về.

Đặt đúng một file, đúng tên:

```
deploy/plugins/ksts-plugin-setup.zip
```

## Vì sao phải chép tay

File là sản phẩm build của `ksts.plugin` (~43 MB, gần hết là `KstsPlugin.exe` self-contained) nên **không nằm
trong git**. Máy chủ dựng image từ bản clone của repo, do đó bộ cài không bao giờ tự đi theo image — thiếu
bước chép này thì màn Ký số báo *"Máy chủ chưa có bộ cài plugin"*.

## Cập nhật bộ cài

Trên máy phát triển, đóng gói rồi đẩy file lên máy chủ:

```powershell
cd ksts.plugin
./dong-goi.ps1
```

```bash
scp ksts.be/ksts.be.api/Plugins/ksts-plugin-setup.zip <user>@<may-chu>:<duong-dan-repo>/deploy/plugins/
```

**Không cần build lại image cũng không cần khởi động lại container**: backend mở file theo từng lần tải, thư
mục lại mount trực tiếp nên bản mới có hiệu lực ngay.

Thư mục rỗng **không phải lỗi** — API trả `exists = false`, giao diện khoá nút tải kèm lời nhắn liên hệ quản
trị hệ thống.
