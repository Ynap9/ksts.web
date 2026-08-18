# Contract — Lô ký số hàng loạt

Gốc: `api/core/lo-ky`. Yêu cầu Bearer token, **trừ đường tải zip**. Xem luật chung ở [README.md](README.md).

Trang web là **người đưa thư** giữa máy chủ và token: nó vừa gọi các route dưới đây, vừa gọi plugin ở
`127.0.0.1` ([plugin-ky-so.contract.md](plugin-ky-so.contract.md)). Luồng đầy đủ:
[docs/luong-ky-so-hang-loat.md](../docs/luong-ky-so-hang-loat.md).

## Routes

| Method | Route | Body / Query | `data` trả về |
|---|---|---|---|
| POST | `lo-ky` | `{ templateId }` | `ViewLoKy` |
| POST | `lo-ky/{id}/them-file` | `multipart/form-data`, một đợt file | `ViewLoKy` |
| POST | `lo-ky/{id}/them-tu-kho` | `{ duongDan }` | `ViewLoKy` |
| POST | `lo-ky/{id}/mo-phien` | `{ chungThuBase64 }` | `ViewLoKy` |
| POST | `lo-ky/{id}/bat-dau` | `{ thumbprint }` | `ViewLoKy` |
| GET | `lo-ky/{id}/cho-ky` | — | `YeuCauKy[]` — **lời gọi bị GIỮ tới khi có việc** |
| POST | `lo-ky/{id}/chu-ky` | `KetQuaKy[]` | `true` |
| POST | `lo-ky/{id}/dong-phien` | — | `true` |
| GET | `lo-ky/{id}/danh-sach-file` | — | `ViewFileKy[]` |
| GET | `lo-ky/{id}/trang-thai` | — | `ViewTienDo` |
| GET | `lo-ky/dang-chay` | — | `ViewLoKy` hoặc `null` — trả cả lô `DangKy` lẫn `TamDung` |
| POST | `lo-ky/{id}/dung` | — | `null` — **tạm dừng**, ký tiếp được |
| POST | `lo-ky/{id}/huy` | — | `null` — **huỷ hẳn**, không ký tiếp |
| GET | `lo-ky/{id}/zip?token=…` | — | **bytes zip thô, không envelope** |

## Thứ tự gọi bắt buộc

```
1. POST lo-ky                          -> loKyId + taiToken
2. POST lo-ky/{id}/them-file    (lặp)  hoặc  POST lo-ky/{id}/them-tu-kho
3. POST plugin ky-so/mo-phien          -> chungThuBase64   (hộp PIN bật ở đây, ĐÚNG MỘT LẦN)
4. POST lo-ky/{id}/mo-phien {chungThuBase64}
5. POST lo-ky/{id}/bat-dau {thumbprint}
6. lặp tới khi HoanTat:
      GET  lo-ky/{id}/cho-ky           -> [{ yeuCauId, duLieuBase64 }]
      POST plugin ky-so/ky             -> [{ yeuCauId, chuKyBase64, loi }]
      POST lo-ky/{id}/chu-ky
7. POST plugin ky-so/dong-phien  +  POST lo-ky/{id}/dong-phien
```

Bước 3 **phải xong trước** bước 4: máy chủ cần chứng thư phần công khai để dựng chuỗi tin cậy và lắp vào CMS.
Nhảy thẳng sang `bat-dau` thì lô chạy nhưng không ai ký, các lượt ký hết hạn sau 120 giây và file tính lỗi.

## Nạp file — hai đường

**`them-file`** — tải lên từ máy người dùng, chia đợt. FE gửi ~50 file/đợt; trần server là **200 file/đợt**.
Đợt hỏng thì **gửi lại đúng đợt đó** — server khử trùng theo tên file trong phạm vi lô nên gửi lại không nhân
đôi. Chỉ nhận `.pdf`, file rỗng bị bỏ qua lặng lẽ.

**`them-tu-kho`** — trỏ vào **thư mục có sẵn trên kho object**, không tải lên byte nào. `duongDan` nhận cả:

- object key thuần: `GiayBaoTrungTuyen/K71/GiayBaoTrungTuyen/`
- có tên bucket ở đầu: `ehuce/GiayBaoTrungTuyen/…`
- dán nguyên link bảng điều khiển MinIO: `https://…/browser/ehuce/GiayBaoTrungTuyen%2FK71%2F…`

Server tự cắt host, tham số truy vấn, tên bucket và giải mã `%20`. Không thấy file PDF nào ⇒ `1164`.

Cả hai đường chỉ gọi được khi lô còn ở trạng thái `MoiTao`; lô đã bắt đầu ký ⇒ `1162`.

## ViewLoKy

```jsonc
{
  "id": 12,
  "templateId": 3,
  "thumbprint": "A1B2…",
  "taiToken": "9F3C…",          // dùng cho đường tải zip, phát ngay lúc tạo lô
  "trangThai": "DangKy",        // MoiTao | DangKy | Xong | Huy | Loi | TamDung — CHUỖI, không phải số
  "tongSo": 5000,
  "daXong": 1234,
  "soLoi": 2,
  "createdDate": "2026-08-14T09:12:00"
}
```

## ViewTienDo — hỏi theo nhịp khi lô đang chạy

```jsonc
{
  "id": 12, "trangThai": "DangKy", "taiToken": "9F3C…",
  "tongSo": 5000, "daXong": 1234, "soLoi": 2,
  "dangChay": true,             // tiến trình ký còn sống trong bộ nhớ máy chủ
  "hoanTat": false,             // lô đã chốt: Xong | Huy | Loi — TamDung KHÔNG tính là chốt
  "coTheTaiZip": false,         // BE tính; lô TamDung vẫn tải được dù chưa hoàn tất
  "loiChung": null,             // sự cố làm dừng CẢ lô, khác với lỗi từng file
  "tienToKho": "GiayBaoTrungTuyen/K71/GiayBaoTrungTuyenDaKySo/",
  "filesLoi": [ /* ViewFileKy */ ],
  "filesVuaXong": [ /* ViewFileKy, tối đa 100 dòng mới nhất */ ]
}
```

**Chỉ trả file lỗi và file vừa xong**, không bao giờ cả 5000 dòng: kèm ngần ấy dòng mỗi nhịp là thứ làm trình
duyệt cạn tài nguyên rồi chết giữa lô. Danh sách đầy đủ lấy **một lần** bằng `danh-sach-file` khi mở màn hình,
rồi vá dần bằng `filesVuaXong` và `filesLoi`.

`dangChay` và `hoanTat` là hai câu hỏi khác nhau: lô đóng tab giữa chừng có `hoanTat = false` mà
`dangChay = false`.

**`coTheTaiZip` do BE tính, FE đừng suy lại từ `hoanTat`** — lô `TamDung` chưa hoàn tất mà vẫn tải được.

## ViewFileKy

```jsonc
{
  "id": 501, "thuTu": 12, "tenFile": "001234567890.pdf",
  "trangThai": "xong",          // cho | dangKy | xong | loi — CHUỖI viết thường
  "lyDoLoi": null,
  "thoiGianKy": "2026-08-14T09:15:03",
  "dauThoiGian": "2026-08-14T09:15:04"   // giờ trong token TSA, đã quy về giờ VN
}
```

`trangThai` gửi **chuỗi** chứ không phải số thứ tự enum — bảng trên FE đọc thẳng giá trị này, số trần thì mỗi
lần sửa enum là FE hiển thị sai lặng lẽ.

## YeuCauKy / KetQuaKy — phần đưa thư

```jsonc
// GET cho-ky -> tối đa 8 phần tử một đợt
[ { "yeuCauId": "…", "duLieuBase64": "<SignedAttributes DER>" } ]

// POST chu-ky
[ { "yeuCauId": "…", "chuKyBase64": "<chữ ký thô>", "loi": null } ]
```

- `duLieuBase64` đưa **nguyên xi** xuống `plugin ky-so/ky`, không đụng vào.
- `cho-ky` là **lời gọi bị giữ**: chưa có việc thì máy chủ ngâm tối đa 25 giây rồi trả mảng rỗng. FE **không
  đặt timeout ngắn hơn** và gọi lại ngay khi nó trả về — đừng chờ thêm giữa hai lượt.
- Một yêu cầu hỏng thì nộp lại phần tử đó với `loi` khác `null`; các yêu cầu còn lại vẫn nộp bình thường.
- Yêu cầu không được nộp trong **120 giây** thì chết và file đó tính lỗi.

## Tải zip

`GET lo-ky/{id}/zip?token={taiToken}` — **không** bọc envelope, **không** đòi Bearer: trình duyệt điều hướng
tới đây thì không gắn được header `Authorization`, nên `taiToken` phát lúc tạo lô là thứ duy nhất chặn đường
này. FE điều hướng thẳng (`window.location`), **không** tải qua `HttpClient` rồi tạo blob — gói lô vài GB vào
bộ nhớ trang là hết bộ nhớ.

Sai token, lô đang ký dở, hoặc chưa có file nào ký xong ⇒ **404 trơn**, không nêu lý do (đường này mở cho
request chưa đăng nhập; phân biệt "sai token" với "lô chưa xong" là chỉ điểm cho người dò).

## Mã lỗi

| Code | Ý nghĩa |
|---|---|
| `1160` | Không tìm thấy lô ký |
| `1161` | Lô của người dùng khác |
| `1162` | Lô đang chạy (bắt đầu lại, hoặc thêm file khi đã bắt đầu) |
| `1163` | Lô rỗng — chưa có file để ký, hoặc chưa file nào ký xong khi tải zip |
| `1164` | Thư mục trên kho không có file PDF nào |
| `1020` | Chưa chọn chứng thư / không đọc được chứng thư máy người dùng gửi lên |
| `1021` | Chứng thư không dựng được chuỗi tin cậy về CA đã ghim |
| `1003` | Không tải được ảnh chữ ký tươi của template (dừng cả lô) |
| `1148` | File nguồn đã có chữ ký mà template chưa bật `kyDe` — **lỗi của riêng file đó**, lô vẫn chạy tiếp |

## Dừng khác Huỷ

| | `dung` | `huy` |
|---|---|---|
| Trạng thái lô | `TamDung` | `Huy` |
| File đang ký dở | trả về `Cho` | để nguyên |
| `bat-dau` lại được | ✅ chạy tiếp từ file kế tiếp | ❌ |
| Bản đã ký trên kho | giữ | **giữ** |
| File nguồn `lo-ky/{id}/nguon/` | giữ, còn cần | dọn |
| Hiện ở `lo-ky/dang-chay` | ✅ | ❌ |
| `coTheTaiZip` | ✅ | bản ghi còn nhưng FE không còn đường bấm |

## FE phải nắm

- **`bat-dau` không kiểm trạng thái lô** nên gọi lại trên lô `TamDung` là chạy tiếp ngay; lấy việc luôn lọc
  `TrangThai = Cho` nên không bao giờ ký đè file đã `Xong`.

- **Đóng tab là lô dừng.** File đã ký giữ nguyên; bấm bắt đầu lại thì chạy tiếp từ file kế tiếp.
- **Mở lại màn hình giữa lô**: `lo-ky/dang-chay` cho thấy đúng tiến độ, nhưng vòng đưa thư **chưa nối lại
  được** — phải hỏi PIN lần nữa, chưa có giao diện cho việc đó.
- **Gọi `dung`/`huy` XONG rồi mới `dong-phien`.** Ngược lại là các lượt đang chờ nhận lỗi "phiên đã đóng" và
  file rơi vào nhánh lỗi thay vì được trả về hàng đợi — đúng ngần ấy file không ký tiếp được.
- Bản đã ký nằm sẵn trên kho tại `tienToKho`; zip chỉ là đường tải cho người dùng, không phải nơi lưu.
