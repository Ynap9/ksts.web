# Lưu trữ ảnh trên MinIO

Ảnh dấu đỏ và chữ ký tươi của template được đẩy lên **MinIO** (S3-compatible); DB chỉ lưu **URL công khai** và
**object key**.

## Vì sao không lưu đường dẫn ổ đĩa như SIPPACK

SIPPACK là desktop nên copy ảnh về `%AppData%` rồi lưu đường dẫn. KSTS là web: người dùng upload qua HTTP,
server có thể chạy nhiều instance hoặc trong container không có ổ đĩa bền vững. MinIO là chỗ duy nhất mọi
instance nhìn thấy chung.

## Cấu hình

```jsonc
"S3": {
  "S3_URL":        "https://api.minio.yna.io.vn",
  "S3_REGION":     "us-east-1",
  "S3_BUCKET":     "ksts",
  "S3_ACCESS_KEY": "…",
  "S3_SECRET_KEY": "…"
},
"FileConfig": {
  "File": {
    "LimitUpload":    5242880,
    "AllowExtension": ".doc,.docx,.pdf,.repx,.png,.jpg,.svg,.jpeg,.webp,.xlsx,.xls"
  }
}
```

Bind vào `S3Settings` / `FileSettings` ở `ksts.be.shared/Settings`, đăng ký bằng `Configure<T>` trong
`Program.cs` — cùng kiểu với `AuthServerSettings` đã có. Tên khoá trong JSON dùng `SNAKE_CASE` nên các
property phải gắn `[ConfigurationKeyName]`.

> ⚠️ Access key / secret key đang nằm thẳng trong `appsettings.json`. Chấp nhận được ở môi trường dev; trước
> khi lên production nên chuyển sang biến môi trường hoặc user-secrets, vì file này đi theo mọi bản build.

`AmazonS3Config` bắt buộc `ForcePathStyle = true` và dùng `AuthenticationRegion` (không phải `RegionEndpoint`)
— MinIO không hỗ trợ virtual-host style. Pattern lấy từ `5sdb/nhaplieu.be` (`S3_Client`, `UploadFileService`).

## Đặt object key

```
AnhDauVaChuKyTuoi/{templateId}/dau-do{ext}
AnhDauVaChuKyTuoi/{templateId}/chu-ky-tuoi{ext}
```

**Không dùng tên file gốc người dùng upload.** Hai người cùng upload `dau-do.png` sẽ ghi đè ảnh của nhau —
`nhaplieu.be` đang mắc đúng lỗi này (`Key = filename`). Tách theo `templateId` thì xoá template là xoá gọn cả
thư mục ảnh.

Đổi ảnh từ `.png` sang `.jpg` thì key đổi theo đuôi, nên **phải xoá object cũ** trước khi ghi cái mới, nếu
không sẽ để lại file mồ côi. Ghi đè lên đúng key cũ cũng **xoá trước rồi mới đẩy**, không ghi đè trần: bản cũ
còn nằm trong lịch sử phiên bản của kho và trong bộ đệm tầng phát tán, tải ảnh mới xong vẫn thấy ảnh cũ.

## URL lưu vào DB

Object được ghi với `CannedACL = PublicRead`, URL công khai dựng theo:

```
{S3_URL}/{bucket}/{objectKey}
```

DB lưu **cả hai**: `AnhDauDoUrl` để FE hiển thị, `AnhDauDoObjectKey` để xoá/thay ảnh. Chỉ lưu URL thì muốn xoá
lại phải parse ngược chuỗi — hỏng ngay khi đổi endpoint MinIO.

## Kiểm tra trước khi upload

Thứ tự: đuôi file nằm trong `TemplateConstants.AllowedImageExtensions` (`.png/.jpg/.jpeg`) → dung lượng
≤ `TemplateConstants.MaxImageBytes` (5 MB) → mới upload. Ảnh dấu/chữ ký chỉ vài chục KB; to hơn mức này là
người dùng chọn nhầm file.

`FileSettings.AllowExtension` rộng hơn (có cả `.pdf`, `.xlsx`) vì đó là cấu hình dùng chung toàn hệ thống —
riêng luồng template dùng danh sách hẹp của `TemplateConstants`.
