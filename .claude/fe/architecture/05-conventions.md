# Quy ước code (FE)

> **Phần 5/5** · trước: [04-man-hinh-dac-thu.md](04-man-hinh-dac-thu.md) · mục lục: [README.md](README.md)

## Sinh màn hình mới bằng CLI, không tạo tay

```bash
cd ksts.fe
ng generate component pages/<ten-man>          # đủ .ts .html .scss .spec.ts, khai báo standalone đúng khuôn
```

Tạo tay từng file là nguồn của lệch khuôn: thiếu `styleUrl`, sai `selector`, quên `standalone`. Sinh xong mới sửa
nội dung.

## Đặt tên

| Thứ | Quy ước | Ví dụ |
|---|---|---|
| File | kebab-case, đuôi nói rõ vai | `lo-ky.service.ts` · `template.models.ts` · `chung-thu-so.constants.ts` · `ky-so.routes.ts` |
| Class component | PascalCase, **không** hậu tố `Component` | `KySo` · `ConfigTemplate` · `DataTable` |
| Selector | `app-<kebab>` | `app-ky-so` |
| Interface | `I` + PascalCase, khớp tên DTO của BE | `IViewLoKy` · `IViewFileKy` · `IConfigTemplate` |
| Enum | `E` + PascalCase | `ETemplatePositionKind` · `ECertSource` |
| Hằng số module | SCREAMING_SNAKE_CASE | `PLUGIN_PROBE_TIMEOUT_MS` · `DO_DAM_MAC_DINH` |
| Method, biến, tham số | **ưu tiên tiếng Anh** | `createBatch` · `openSigningSession` · `buildDownloadUrl` |

### Tiếng Anh trước, tiếng Việt khi không có từ sát nghĩa

Tên **mới** đặt bằng tiếng Anh. Chỉ giữ tiếng Việt khi khái niệm nghiệp vụ **không có từ tiếng Anh sát nghĩa** —
dịch ép ra một từ gần đúng còn tệ hơn, vì mỗi người sẽ dịch một kiểu.

```ts
// ✅ createBatch, openSigningSession, buildDownloadUrl, uploadInChunks
// ✅ giayBaoTrungTuyen, chuKyTuoi, dauDo, trungTuyen — không có từ tiếng Anh nào sát nghĩa
// ❌ taoLo, moPhienKy, duongDanTaiZip — batch / session / download url đều có từ rõ nghĩa
```

Bốn chỗ **vẫn là tiếng Việt**, không đổi: trường của interface trong `models/` (khoá JSON do BE quyết, đổi là
sai payload); đường dẫn API (`'/api/core/lo-ky'`); giá trị enum khớp BE; và **mọi câu hiển thị cho người dùng**
— tiếng Việt có dấu, đủ câu, không thay `message` của BE bằng câu chung chung.

⚠️ Tên cũ trong repo đang là tiếng Việt (`taoLo`, `vongDuaThu`, `khois`…). **Không đổi hàng loạt** — đổi tên
một method của service kéo theo mọi màn đang gọi nó, mà lợi ích chỉ là cái tên. Đặt đúng từ lần sau.

## Comment — ít, tiếng Anh, chỉ khi có lý do

- Comment và JSDoc viết **tiếng Anh**, ngắn, trả lời **vì sao**, không thuật lại code.
- Đặt ở **đầu method** hoặc đầu field có ràng buộc nghiệp vụ. Không comment từng dòng trong thân hàm.
- Cấm: comment kể lại việc code đang làm; comment lan man nhiều dòng; ghi lịch sử sửa đổi hay tên người sửa
  (git giữ rồi); comment code chết — xoá hẳn đoạn đó; `// TODO` trống nghĩa — việc còn dở ghi vào
  [../../dang-lam.md](../../dang-lam.md).
- **Interface trong `models/` không comment** — túi dữ liệu khớp payload BE; ý nghĩa từng trường nằm ở
  [../../contracts/](../../contracts/).
- **Hằng số có nghiệp vụ thì PHẢI comment**: con số ở đâu ra, khớp hằng số nào của BE, vì sao không được đổi một
  mình (xem `template.constants.ts`).

⚠️ Comment cũ đang là tiếng Việt. Đụng vào file nào thì đổi comment của **phần mình sửa**, không dịch cả file —
diff phải còn đọc được.

## Hằng số đặt ở đâu

| Loại | Chỗ |
|---|---|
| Dùng nhiều màn, hoặc phải khớp BE | `shared/constants/<nhom>.constants.ts` |
| Chỉ một màn, là số điều chỉnh của riêng màn đó | `const` ở đầu file component, **kèm comment nêu lý do** — `SO_FILE_MOI_DOT`, `NHIP_HOI_TIEN_DO` |
| Khoá `localStorage` | Hằng số trong `constants/`, không rải chuỗi trần |

Enum khớp BE (`ETemplatePositionKind`, `ECertSource`) **cấm đổi số, cấm chèn vào giữa** — số đã nằm trong DB.

## Định dạng

`.prettierrc.json`: 4 space, nháy đơn, có dấu chấm phẩy, `trailingComma: none`, `printWidth: 250`. Chạy
`npm run format` trước khi chốt.

⚠️ `eslint.config.js` là cấu hình còn lại của sakai, đang đòi selector prefix **`p`** trong khi KSTS dùng `app-`,
và `package.json` **không có script `lint`**. Nghĩa là nó không chạy và không phải nguồn chân lý — đừng đổi
selector của KSTS theo nó.

## Không được làm

- **Không đọc `res.data` mà chưa qua `isResponseSucceed`** — envelope luôn HTTP 200, bỏ cổng là lỗi nghiệp vụ
  trôi vào màn hình thành dữ liệu rỗng.
- **Không ghi `environment.apiUrl` trong component** — service khai URL tương đối, interceptor gắn tiền tố.
- **Không gửi Bearer sang plugin** ở `127.0.0.1` — tiến trình khác trên máy người dùng.
- **Không đặt timeout** cho lời gọi cần người dùng thao tác (nhập PIN) hoặc lời gọi bị máy chủ giữ (`cho-ky`).
- **Không cache danh sách chứng thư**, không lưu chứng thư xuống đĩa.
- **Không tải zip bằng `HttpClient` + blob** — điều hướng thẳng kèm `taiToken`.
- **Không dựng lại mảng nghìn dòng theo nhịp** hỏi tiến độ.
- **Không `subscribe` trong service**, không hiện toast trong service.
- **Không gọi `localStorage` trực tiếp** — qua `Utils`.
- **Không lấy `pages/uikit`, `pages/crud`, `pages/service`, `pages/landing` làm mẫu** — phần demo của sakai.
- **Không dùng `*ngIf` / `*ngFor`** — dùng `@if` / `@for (… ; track …)`.

## Chốt công việc

`npm run build` sạch trong `ksts.fe`. Sửa gì lệch khỏi tài liệu thì cập nhật tài liệu **trong cùng task** — xem
[../../skills/write-markdown/SKILL.md](../../skills/write-markdown/SKILL.md).

> **Hết.** Quay lại mục lục: [README.md](README.md) · quy ước phía BE:
> [../../be/architecture/08-conventions.md](../../be/architecture/08-conventions.md)
