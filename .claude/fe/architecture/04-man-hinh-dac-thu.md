# Hai màn hình đặc thù

> **Phần 4/5** · trước: [03-components-state.md](03-components-state.md) · mục lục: [README.md](README.md)
>
> Hai chỗ FE không còn là "màn CRUD gọi API". Luồng nghiệp vụ:
> [../plans/ky-so-man-hinh.plan.md](../plans/ky-so-man-hinh.plan.md) · [../../docs/luong-ky-so-hang-loat.md](../../docs/luong-ky-so-hang-loat.md).

## Màn ký số (`pages/ky-so`) — trang web là NGƯỜI ĐƯA THƯ

Trang đang mở là đường duy nhất nối máy chủ với token. Nó chạy **hai vòng độc lập**, tuyệt đối không gộp:

```ts
async vongDuaThu(loKyId) {        // vòng 1: mang chữ ký, KHÔNG nghỉ giữa hai lượt
    while (this.dangKy()) {
        const cho = await firstValueFrom(this._loKyService.choKy(loKyId));   // BE giữ tới 25s
        const ky  = await firstValueFrom(this._pluginService.kyLo(cho.data)); // token ký cả đợt
        await firstValueFrom(this._loKyService.nopChuKy(loKyId, ketQua));
    }
}

// vòng 2: setTimeout tự hẹn lại mỗi NHIP_HOI_TIEN_DO = 2000ms
hoiTienDo() { … if (!tienDo.hoanTat && tienDo.dangChay) setTimeout(() => this.hoiTienDo(), NHIP_HOI_TIEN_DO); }
```

⚠️ **Gộp hai vòng là hỏng cả hai**: mỗi nhịp tiến độ sẽ chặn một lượt ký, hoặc ngược lại. Và vòng đưa thư
**không được chờ thêm** giữa hai lượt — token ký tuần tự nên mọi khoảng nghỉ nhân thẳng với số file.

Một lượt hỏng **không phải** lô hỏng: nuốt lỗi, ngủ một nhịp rồi thử lại; chỉ tiến độ báo kết thúc mới cho vòng
lặp thoát. Yêu cầu nào plugin trả lỗi thì nộp lại **đúng phần tử đó** với `loi`, các yêu cầu còn lại vẫn nộp.

### Bảng nghìn dòng — ba signal, không dựng lại mảng

| Signal | Nạp khi nào |
|---|---|
| `rows` | **Một lần** bằng `danh-sach-file` lúc mở lô, và một lần nữa khi lô dừng (thời gian ký + dấu thời gian chỉ có sau khi ký) |
| `thoiGianTheoThuTu` (`Map`) | Vá dần từ `filesVuaXong` mỗi nhịp tiến độ |
| `lyDoTheoThuTu` (`computed`) | Suy từ `filesLoi` của tiến độ |

⚠️ **Đừng dựng lại `rows` mỗi nhịp.** Bắt Angular vẽ lại 5000 dòng mỗi 2 giây chính là thứ làm trình duyệt cạn
tài nguyên rồi chết giữa lô. BE cũng chỉ trả file lỗi + tối đa 100 file vừa xong, không bao giờ cả danh sách.

### Dừng, huỷ và ký tiếp

`dungLoKhiRoiMan` nay gọi `dung` (**tạm dừng**, không phải huỷ) rồi mới đóng phiên plugin — rời màn là mất
người đưa thư, nhưng quay lại vẫn ký tiếp được. `loDangDo()` (có lô, `!hoanTat`, `!dangChay`) là cổng của ba
thứ: nút đổi nhãn thành **Ký tiếp**, `coTheBatDau()` bỏ đòi nguồn và template (lô trên máy chủ đã có, mà mở
lại màn thì hai ô rỗng), và `onBatDau` bỏ qua `taoLoVaDayFile()` để gọi thẳng `moPhienKy` + `goiBatDau`. Tải
zip theo `coTheTaiZip` do BE tính, **không** suy từ `hoanTat`. Bảng Dừng-khác-Huỷ:
[../../contracts/lo-ky.contract.md](../../contracts/lo-ky.contract.md).

⚠️ **`dung`/`huy` XONG rồi mới `dongPhienKy`** — đóng phiên trước thì lượt đang chờ nhận lỗi "phiên đã đóng",
không phải lỗi huỷ, nên file rơi vào nhánh **Lỗi** thay vì quay lại hàng đợi và đúng ngần ấy file không ký
tiếp được.

⚠️ `datLaiLo()` phải dọn **cả** `rows` và `thoiGianTheoThuTu`, bỏ sót là bảng của lô vừa huỷ còn nằm trên màn.

### Nạp file — hai đường

- **Từ máy**: kéo thả cả thư mục, lọc `.pdf`, bỏ file trùng tên, đẩy từng đợt `SO_FILE_MOI_DOT = 50`; đợt hỏng
  thì gửi lại **đúng đợt đó** (server khử trùng theo tên trong phạm vi lô).
- **Từ kho**: một lời gọi `them-tu-kho` với đường dẫn thư mục trên MinIO, **không tải lên byte nào** — đường
  dùng thật sau khi dựng giấy báo.

### Chứng thư và plugin

Danh sách chứng thư **không cache**, gọi lại mỗi lần mở màn (token có thể vừa cắm hoặc vừa rút). Gọi hỏng ⇒ mở
popup `CaiPlugin` trừ khi người dùng đã tắt nhắc (`NhacCaiPluginService`); bấm thẳng vào ô chứng thư thì **luôn**
hiện popup (`batBuoc = true`).

⚠️ Người dùng đang nhập PIN **hai lần** một lô: một lần ở `kiem-tra-token` (nút Xác thực), một lần ở
`ky-so/mo-phien`. Đã biết, chưa quyết bỏ bước nào — xem [../../dang-lam.md](../../dang-lam.md).

## Màn cấu hình template (`pages/template/config-template`)

Bản xem trước PDF dựng bằng **pdf.js** (`pdfjs-dist`), người dùng kéo thả khối lên trang.

```ts
pdfjs.GlobalWorkerOptions.workerSrc = new URL('pdf.worker.min.js', document.baseURI).toString();
```

⚠️ Thiếu dòng đó thư viện tự chặn với `No GlobalWorkerOptions.workerSrc specified`. **Phải là đuôi `.js`**:
pdfjs-dist chỉ phát worker `.mjs`, máy chủ không map đuôi đó trả `application/octet-stream` ⇒ trình duyệt từ
chối module script, ô xem trước trắng trơn với `Setting up fake worker failed` — **đã sập trên prod**, trong khi
chunk `.js` của Angular vẫn tải bình thường. Nay `scripts/copy-pdf-worker.mjs` chép worker sang `public/` dưới
đuôi `.js` ở `prebuild`; **đừng chỉ thêm MIME `.mjs` cho nginx** — deploy có thể đứng sau máy chủ khác.

- **Bộ đếm `lanMo`** tăng mỗi lần mở tài liệu và ghép vào khoá từng trang: đổi file là canvas dựng lại kể cả khi
  trùng số trang, và vòng vẽ bất đồng bộ tự bỏ dở khi `lanMo` đã đổi — không thì trang file cũ đè lên file mới.
  `pdfTask.destroy()` trong `ngOnDestroy`.
- **Toạ độ là tỉ lệ 0..1** so với khổ trang, kẹp bằng `kepTrongTrang` — đặt một lần trên file mẫu rồi áp cho hồ
  sơ khổ khác, mức phóng trình duyệt không ảnh hưởng kết quả.
- **Kéo góc đổi đúng MỘT hệ số** cho cả rộng và cao: cỡ chữ suy từ chiều cao, khối méo là chữ tràn hoặc lọt thỏm.
- **Con dấu `choPhepResize = false`** — BE vẽ dấu đúng kích thước gốc của ảnh, cho kéo giãn là làm sai con dấu
  ([../../docs/dat-dau-va-chu-ky-tuoi.md](../../docs/dat-dau-va-chu-ky-tuoi.md)).
- **Bản xem trước phải khớp công thức BE**: độ đậm là `contrast(f) brightness(b)` đúng như mảng `/Decode`, độ dày
  nét là hai lớp `drop-shadow` thay cho 9 lớp nong nét của BE. Hệ số nằm ở `shared/constants/template.constants.ts`
  — đổi bên nào **phải đổi cả hai**, không thì xem trước nói dối về bản in ra.
- **Tự cuộn khi kéo**: `mousemove`/`mouseup` gắn ở **khung cuộn**, không phải ở từng trang — gắn theo trang thì
  kéo qua mép là mất dấu chuột, thao tác đứt đoạn. Trang nhận khối ghi lại từ lúc `mousedown` (`keoLop`), con
  trỏ đi đâu cũng quy về đúng trang đó. Con trỏ vào vùng `CUON_VUNG_MEP_PX` thì một vòng `requestAnimationFrame`
  cuộn tiếp **cả khi chuột đứng yên**, và mỗi nhịp phải **tính lại khối** vì trang đang trượt dưới con trỏ.
- **Màu**: khối chữ ký số đổi bằng `[style.color]`; ảnh chữ ký tươi nhuộm bằng bộ lọc SVG
  `feComponentTransfer type="linear"` — chuỗi filter đúng thứ tự BE áp: **đậm nhạt → nhuộm → nong nét**. Bảng màu
  mực **khởi đầu bằng màu trích từ chính ảnh** (`InkColorService`) chứ không phải đen; chưa chọn thì không nhuộm
  gì, chọn đen là nhuộm đen thật, quầng nong nét cũng lấy màu đó.
- **Ô "ký đè" chỉ bật khi file xem trước có chữ ký thật** (`pdfDoc.getSignatures()`); ô chữ ký trống không tính.
- **Lưu là PUT ghi đè toàn bộ**: luôn gửi trạng thái đầy đủ đang hiển thị, kể cả `positions` — vá từng phần sẽ
  để sót khối người dùng vừa xoá.
- Chứng thư chọn ở màn Chứng thư số, mang qua bằng `ChungThuSoDaChonService`; đi chọn thì gắn
  `queryParams.templateId` để quay lại đúng template đang cấu hình dở.

> **Tiếp:** [05-conventions.md](05-conventions.md) — quy ước bắt buộc và những việc không được làm.
