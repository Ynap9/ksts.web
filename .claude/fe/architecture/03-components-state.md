# Component, trạng thái và bảng dữ liệu

> **Phần 3/5** · trước: [02-http-services.md](02-http-services.md) · mục lục: [README.md](README.md)

## BaseComponent — mọi màn đều kế thừa

`shared/components/base/base-component.ts`, khai `@Directive()` abstract. Cho sẵn:

| Thành viên | Dùng khi |
|---|---|
| `isResponseSucceed(res, isShowErrorMsg = true, successMsg = '')` | **Cổng bắt buộc** trước khi đọc `res.data`; tự hiện toast lỗi/thành công |
| `messageSuccess/Warning/Error` | Toast, đã gắn `life` và `closable` |
| `confirmDelete` · `confirmAction` | Hộp xác nhận, nhãn nút tiếng Việt sẵn |
| `form` · `ValidationMessages` · `getError(field)` · `isFormInvalid()` | Reactive form + câu lỗi theo từng control |
| `loading` · `totalRecords` (signal) · `page` · `MAX_PAGE_SIZE = 10` · `START_PAGE_NUMBER = 1` | Trạng thái chung của một màn danh sách |
| `getUser()` | Người đang đăng nhập, ưu tiên bản đã lưu, không có thì suy từ JWT |
| `onRouteActivated()` | Hook gọi lại khi điều hướng về **đúng** route của component này |

`ngOnInit` của lớp con phải khai `override`. Ba service PrimeNG (`MessageService`, `DialogService`,
`ConfirmationService`) inject sẵn ở lớp cha — đừng inject lại ở màn con.

## Trạng thái — ba tầng, không có store

Không NgRx, không Redux. Trạng thái chia theo tuổi thọ:

| Tầng | Ở đâu | Ví dụ |
|---|---|---|
| Một màn | `signal()` / `computed()` trong component | `files`, `tienDo`, `khois`, `coTheBatDau` |
| Một phiên, **chỉ RAM** | service `providedIn: 'root'` | `SharedService` (vai trò + quyền), `AppSessionService` (người dùng), `ChungThuSoDaChonService` (chứng thư đã chọn) |
| Qua nhiều phiên | `localStorage` qua `Utils` | cặp token, thông tin người dùng, bề rộng cột bảng, cờ "không nhắc cài plugin" |

⚠️ **Chứng thư đã chọn chỉ được giữ trong RAM** (`ChungThuSoDaChonService`): không localStorage, không cookie.
Cache xuống đĩa vừa trái quy ước ở [../../docs/bao-mat-agent-ky-so.md](../../docs/bao-mat-agent-ky-so.md) §7,
vừa để lại dấu vết đọc được bởi bất kỳ script chạy trên trang. Mất khi F5 là **đúng** — đọc lại từ token nhanh
và luôn phản ánh token đang cắm.

Đọc/ghi storage **luôn** qua `Utils` (`shared/utils.ts`), không gọi `localStorage` trực tiếp trong component:
`Utils.getLocalStorage` đã nuốt lỗi JSON hỏng, gọi thẳng thì một giá trị rác làm cả màn hình chết.

Zoneless (`provideZonelessChangeDetection`) ⇒ **signal là đường cập nhật giao diện**. Gán vào field thường
(`this.keoKhoiIndex = …`) chỉ dùng cho trạng thái không cần vẽ lại; dữ liệu template đọc phải nằm trong signal.

## DataTable dùng chung

`shared/components/data-table/` bọc `p-table` + `p-paginator`. Component gọi khai `columns: IColumn[]`:

```ts
columns: IColumn[] = [
    { header: 'STT', cellViewType: CellViewTypes.INDEX, headerContainerStyle: 'width: 6rem' },
    { header: 'Tên template', field: 'tenTemplate', clickable: true, cellClass: 'text-blue-600' },
    { header: 'Ngày tạo', field: 'createdDate', cellViewType: CellViewTypes.DATE, dateFormat: 'dd/MM/yyyy HH:mm' },
    { header: 'Thao tác', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: TblAction }
];
```

- `CellViewTypes`: `INDEX · DATE · CURRENCY · CHECKBOX · CUSTOM_COMP · LINK_BLANK · STATUS · SECRET · BOOL_CHECK`.
- Cột thao tác nhận **một component** (`customComponent`); component đó phát sự kiện qua injection token
  `TBL_CUSTOM_COMP_EMIT`, bảng gom lại rồi bắn ra `(onCustomComp)`. Màn hình xử lý bằng một chỗ duy nhất
  (`onCustomEmit`) theo `type` (`TblActionTypes.view/update/delete` hoặc `'cellClick'`).
- `tableKey` bật lưu **bề rộng cột** vào `localStorage` (`tbl_col_widths_<key>`). ⚠️ Bề rộng đã lưu được áp
  **thẳng vào DOM** trong `ngAfterViewInit`, **không** qua `[style]` binding — binding làm handler resize của
  PrimeNG mất tác dụng ngay sau khi nạp cache.
- Phân trang: `(onPageChanged)` trả `PaginatorState`, màn hình quy về `pageNumber = page + 1` (BE đếm từ 1).

## Khuôn một màn CRUD

Mẫu chuẩn là `pages/template/template.ts`:

1. `searchForm` một control + `columns` + `data = signal<T[]>([])` + `query: IFindPaging…`.
2. `getData()` bật `loading`, gọi service, qua cổng `isResponseSucceed(res, false)`, đặt `data` và
   `totalRecords`, tắt `loading` trong `.add(...)`.
3. Tạo/sửa mở **dialog động**: `_dialogService.open(Create, { header, modal: true, styleClass: 'w-[700px]', data })`
   rồi `ref.onClose.subscribe(result => result && this.getData())`. Dialog tự đóng và trả kết quả; màn danh sách
   chỉ biết "có đổi hay không".
4. Xoá đi qua `confirmDelete(...)`.
5. Màn chi tiết là **route riêng** (`/template/config-template/:id`), không phải dialog — nó có bản xem trước PDF.

## Bố cục và import

- Breadcrumb đặt **trong** page: component `Breadcrumb` + hai field `breadcrumbHome` / `breadcrumbItems`; tiêu đề
  ngay dưới; toàn bộ bọc trong một `<div class="card">`.
- Mọi màn import `SharedImports` (`shared/import.shared.ts`) thay vì liệt kê lại module PrimeNG. Module chỉ một
  màn dùng (`SliderModule`, `ProgressBarModule`, `TagModule`) thì import riêng ở màn đó, **không** nhồi vào
  `SharedImports`.
- Template dùng cú pháp mới: `@if` / `@for (… ; track …)`, không `*ngIf` / `*ngFor`.

> **Tiếp:** [04-man-hinh-dac-thu.md](04-man-hinh-dac-thu.md) — màn ký số và màn cấu hình template.
