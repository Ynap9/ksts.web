# Plan — Đóng gói TT05, đường local: nhận thư mục và kiểm tra (BE)

> **Trạng thái: 🔶 đang thi công** (2026-09-09). Nền nghiệp vụ:
> [../../docs/tt05-chuan-sip.md](../../docs/tt05-chuan-sip.md) ·
> [../../docs/tt05-pdfa.md](../../docs/tt05-pdfa.md). Kho object:
> [../../docs/luu-tru-minio.md](../../docs/luu-tru-minio.md). Bản tham chiếu là SIPPACK
> (`workspace/Sip`, `CheckService.cs`) — app desktop đã chạy thật luồng này.

## Vì sao

`kssm.be` đã có **nửa đầu**: bộ cấu hình chuẩn metadata TT05 trong DB (36 `MetadataField`, 8
`PackageVariant`, 112 `PackageFieldConfig`, 76 `CodeValue`) và endpoint `cau-hinh/xuat-excel` sinh file
Excel mẫu trắng cho người dùng tải về điền. Thiếu **nửa sau**: điền xong rồi đẩy thư mục lên thì chưa có
gì nhận và kiểm.

Phạm vi lần này dừng ở **nhận + kiểm**. Dựng gói SIP thật (2 tầng METS, EAD_DOC, checksum, ZIP) và đường
**dự án** là task sau, dùng lại chính phần kiểm này.

## Input — cái đã có

- `ConfigService.FindPagingAsync` đã là truy vấn lấy bộ trường theo `PackageType` × `MetadataObjectType`,
  trả cả `Requirement`, `ConditionField`/`ConditionValues`, `DataType`, `Codes`. Đây **là** bộ luật kiểm.
- `MetadataFieldAlias`: có bảng, có index, **0 bản ghi, chưa chỗ nào đọc** — dựng sẵn cho đúng việc đối
  chiếu tên cột người dùng đặt khác chuẩn.
- `IPdfSignatureInspector.HasSignature(byte[])` đã có, quét marker trên `Encoding.Latin1`.
- `IS3FileStorage` đủ dùng, `DeleteByPrefixAsync` đã xoá theo lô 1000 key.
- `ksts.be/ksts.be.external/Excel/` đã giải xong bài đọc Excel theo **tên cột** (không theo thứ tự), trả
  **mọi ô dạng chuỗi** (quy về số là mất số 0 đứng đầu của mã hồ sơ) — port sang.
- ClosedXML 0.105.1 đã có ở `applications`.

## Quyết định đã chốt

1. **Không kiểm cấu trúc thư mục.** Chỉ phân biệt một hồ sơ hay nhiều hồ sơ, suy từ đường dẫn tương đối.
2. **Tên file PDF khớp `docId`** (*Mã định danh tài liệu*), **không** phải `docCode`. Gói mẫu đặt
   `H49.64.33.2001.160.1.pdf`; so với `docCode` (`…160.0000001`) là trượt 100% số file.
3. **Ngưỡng 80% áp lên từng cặp tên cột**, không phải lên độ phủ. Ghép xong phải đủ **100% trường bắt
   buộc**. Header thừa bỏ qua, không tính lỗi.
4. **Excel nhận cả hai chỗ**: bản chung ở thư mục cha, hoặc bản riêng trong từng hồ sơ con (riêng thắng).
5. **Lưu phiên xuống DB, FE poll tiến độ.** Không thêm SignalR — trình duyệt hiện chưa gọi thẳng `kssm.be`
   (`sao_mai_fe` chỉ gọi `sao_mai_be`), thêm hub là mở kênh mới và tăng tải server.
6. **Ký số và PDF/A chỉ cảnh báo, không chặn** — không tài liệu nào cho phép từ chối gói vì PDF chưa ký, và
   `tt05-pdfa.md` chốt PDF/A là tuỳ chọn.
7. **MinIO làm kho tạm**, tiền tố `dong-goi/{sessionId}/`, dọn sạch khi xong / huỷ / quá hạn.

## Steps

1. **Shared** — `KiemTraConstants` (tên trạng thái, trần file mỗi đợt, ngưỡng 0.8, tiền tố object).
   `ErrorCodes` mở dải mới **1200–1219** kèm dòng tương ứng trong `ErrorMessages` (thiếu là ra
   `"Unknown error."`).
2. **Domain** — 3 entity `PackageSession`, `PackageDossier`, `PackageDocument`, theo khuôn 5 entity DongGoi
   đang có: chỉ `ISoftDeleted`, **không navigation**, quan hệ mềm theo Id, 4 cột audit khai tay.
3. **Infrastructure** — 3 `DbSet` + `OnModelCreating` (index `SessionId`), migration mới.
4. **External / Excel** — port `IExcelSheetReader` từ `ksts.be`, đổi namespace, thêm ClosedXML vào
   `kssm.be.external.csproj`.
5. **External / DongGoi** — `ITextSimilarity` (chuẩn hoá + điểm giống), `IPdfFormatInspector` (PDF/A part +
   conformance từ XMP, lớp chữ vô hình `3 Tr`, đếm trang), `IPackageFileStorage` (dựng object key + dọn
   theo tiền tố, bọc trên `IS3FileStorage` đúng khuôn `ITemplateImageStorage`).
6. **Applications** — `IMetadataSchemaProvider` (bộ trường + alias + bảng mã), `IDossierLayoutResolver`
   (một hay nhiều hồ sơ), `IHeaderMatcher` (ghép cặp + báo cáo), `IPackageSessionService`, `IVerifyRunner`.
7. **API** — `KiemTraController` tại `api/core/dong-goi/kiem-tra`: `tao-phien` · `{id}/them-file` ·
   `{id}/bat-dau` · `{id}/tien-do` · `{id}/ket-qua` · `{id}/huy`.
8. **Program.cs** — đăng ký DI từng dòng (applications Scoped, external Singleton, runner Singleton nhận
   `IServiceScopeFactory`), **nâng `MaxRequestBodySize` + `FormOptions`**, dọn phiên quá hạn lúc khởi động,
   bỏ dòng `AddScoped<IConfigService>` đang bị lặp hai lần.
9. **Tài liệu** — thêm tiền tố `dong-goi/` vào bảng bản đồ ở
   [../../docs/luu-tru-minio.md](../../docs/luu-tru-minio.md); cập nhật
   [../../dang-lam.md](../../dang-lam.md) và bảng trạng thái ở [../../README.md](../../README.md).
10. `dotnet build` sạch (người dùng tự chạy build và tự áp migration).

## Expected output

- Thư mục tên `H49.64.33.2001.160` chứa Excel + 3 PDF `…160.1.pdf`…`.3.pdf` ⇒ **đạt**, ba file báo chưa ký
  số và không phải PDF/A dưới dạng cảnh báo.
- Đổi header `Tiêu đề hồ sơ` → `Tiêu đề của hồ sơ` vẫn đạt; đổi thành `Tên gọi` thì trượt, báo thiếu trường
  bắt buộc. Thêm cột lạ vẫn đạt, cột đó liệt kê là không dùng.
- Đổi tên thư mục ⇒ báo mã hồ sơ không khớp. Đổi một PDF sang `…160.0000001.pdf` ⇒ báo file mồ côi.
- Thư mục cha nhiều hồ sơ con chạy được với **cả hai** kiểu đặt Excel.
- Huỷ phiên ⇒ tiền tố `dong-goi/{id}/` trên kho sạch không còn object.

## Điểm cần chú ý

- **Soi PDF ngay trong lượt upload**, lúc bytes còn trong tay, rồi mới đẩy lên kho. Để bước kiểm tự tải về
  thì lô 10.000 file kéo thêm ~8,7 GB chiều ngược — theo số đo ở
  [../../docs/luong-ky-so-hang-loat.md](../../docs/luong-ky-so-hang-loat.md) là thêm 9–14 phút chỉ để đọc
  lại thứ vừa đi qua. Phần kiểm chéo còn lại chỉ cần **tên file và dòng Excel**, không đụng byte PDF nào.
- **Điểm giống lấy `max(Levenshtein, Dice theo từ)`.** Chỉ Levenshtein là hỏng: `tieudehoso` với
  `tieudecuahoso` ra 0,77 — trượt oan đúng ca người dùng hay gặp nhất. Dice theo từ ra 0,89.
- **Chuẩn hoá phải bỏ phần trong ngoặc.** Hai nguồn ngay trong repo đã lệch: Excel mẫu ghi
  `Tình trạng vật lý`, `EadFieldMapping` của SIPPACK ghi `Tình trạng vật lý (nếu có)`.
- **Đường dẫn tương đối là khoảng trống thật.** `LoKyService.ThemFileAsync` làm phẳng hoàn toàn và FE chỉ
  gửi `file.name`; nhiều hồ sơ thì bắt buộc phải có `webkitRelativePath` gửi kèm.
- ⚠️ `kssm.be` **chưa cấu hình giới hạn upload ở đâu cả** nên đang chạy mặc định Kestrel ~30 MB/request. Lô
  Ký sống được nhờ FE chia đợt 50 file và giấy báo rất nhẹ; PDF lưu trữ nặng hơn nhiều. Không nâng là vỡ
  ngay đợt đầu.
- ⚠️ Section `FileConfig` trong `appsettings.json` là **code chết** — `FileSettings` chưa bao giờ được
  `Configure<>`. Đừng dựa vào `LimitUpload`/`AllowExtension` ở đó; khai section `DongGoi` riêng.
- ClosedXML **không đọc `.xls`**, chỉ `.xlsx` — phải báo lỗi rõ cho định dạng cũ.
