# KSTS Frontend

Giao diện quản trị của hệ thống ký số giấy báo trúng tuyển. Đọc [README tổng](../README.md) để nắm bối cảnh
trước.

Angular 21 (standalone component, signals), PrimeNG 21 trên khuôn giao diện Sakai, Tailwind CSS 4, pdf.js.

## Mục lục

- [Các màn hình](#các-màn-hình)
- [Cấu trúc thư mục](#cấu-trúc-thư-mục)
- [Xác thực và phân quyền](#xác-thực-và-phân-quyền)
- [Tầng gọi API](#tầng-gọi-api)
- [Hai luồng phức tạp nhất](#hai-luồng-phức-tạp-nhất)
- [Cấu hình môi trường](#cấu-hình-môi-trường)
- [Chạy dự án](#chạy-dự-án)
- [Coding convention](#coding-convention)

## Các màn hình

| Đường dẫn | Màn hình | Việc chính |
|---|---|---|
| `/chung-thu-so` | Chứng thư số | Liệt kê chứng thư trên token qua plugin, xác thực token, tải bộ cài plugin |
| `/template` | Cấu hình template | Danh sách mẫu chữ ký |
| `/template/config-template/:id` | Cấu hình chi tiết | Tải ảnh dấu và chữ ký tươi, kéo thả vị trí trên PDF mẫu, chỉnh độ đậm và độ dày nét |
| `/import-tuyen-sinh` | Import dữ liệu | Đọc Excel, dựng giấy báo hàng loạt, tải zip hoặc đẩy lên MinIO |
| `/ky-so` | Ký số hàng loạt | Chọn nguồn file, chọn template và chứng thư, chạy lô ký, theo dõi tiến độ |
| `/user-management` | Quản trị | Người dùng, vai trò, phân quyền |

## Cấu trúc thư mục

```
src/app/
├── layout/          Khung giao diện: topbar, menu, footer, configurator
├── models/          Interface dữ liệu trao đổi với API
├── pages/           Mỗi màn hình một thư mục, kèm file routes riêng
├── service/         Tầng gọi API, mỗi nghiệp vụ một service
│   └── auth/        Đăng nhập, phiên làm việc, quyền
└── shared/
    ├── components/  Component dùng chung, gồm BaseComponent
    ├── constants/   Hằng số dùng chung, phải khớp với hằng số bên backend
    ├── guard/       authGuard, permissionGuard
    ├── models/      Kiểu dữ liệu dùng chung: phân trang, envelope, JWT payload
    └── utils.ts     Tiện ích localStorage, sessionStorage, giải mã JWT
```

Mỗi màn hình khai route riêng trong `pages/{tên}/{tên}.routes.ts` rồi nối vào `pages.routes.ts`. Component
mới **sinh bằng Angular CLI** chứ không tạo tay từng file:

```bash
ng generate component pages/ten-man-hinh
```

## Xác thực và phân quyền

Đăng nhập theo luồng OAuth2 password grant tới OpenIddict của backend. Token lưu ở localStorage;
`AppSessionService` giải mã JWT để lấy thông tin người dùng, `auth.interceptor.ts` gắn Bearer vào mọi request.

Phân quyền theo **khoá quyền dạng chuỗi**, khai trong `shared/constants/permission.constants.ts` và phải
trùng khớp với `PermissionKeys` bên backend:

```typescript
static MenuKySo = this.Menu + "KySo";
static MenuImportTuyenSinh = this.Menu + "ImportTuyenSinh";
```

Gắn quyền vào route qua `data.permission` và `permissionGuard`:

```typescript
{
  path: '',
  data: { breadcrumb: 'ky-so', permission: PermissionConstants.MenuKySo },
  component: KySo,
  canActivate: [permissionGuard]
}
```

Trong menu, mỗi mục kiểm quyền riêng bằng `isGranted`; nhóm chỉ hiện khi có quyền vào ít nhất một mục con.

Thêm một quyền mới cần làm đủ ba chỗ: hằng số bên backend, hằng số bên frontend, rồi gắn vào route và menu.

## Tầng gọi API

Mỗi nghiệp vụ một service trong `service/`, chỉ làm đúng việc gọi HTTP và không chứa logic màn hình.

Backend trả **HTTP 200 cho mọi trường hợp**, trạng thái thật nằm trong envelope:

```jsonc
{ "status": 1, "data": {}, "code": 200, "message": "Ok" }
```

`BaseComponent.isResponseSucceed(res)` là chỗ duy nhất đọc envelope này; nó trả về `boolean` và tự hiện thông
báo lỗi. Đừng tự kiểm `res.status` rải rác trong component.

Ngoại lệ: các endpoint trả file thô (zip, PDF mẫu, bộ cài plugin) không bọc envelope.

## Hai luồng phức tạp nhất

### Cấu hình template

Màn `config-template` vẽ trang đầu file PDF mẫu lên canvas bằng pdf.js, rồi phủ lên đó các khối kéo thả được.
Toạ độ lưu theo **tỉ lệ 0..1** của khổ trang chứ không theo pixel, nên một lần cấu hình áp được cho mọi khổ
giấy và không phụ thuộc mức phóng của trình duyệt.

Bản xem trước phải dùng **đúng công thức backend áp lúc ký** thì kéo thanh trượt mới thấy trước được kết quả
in ra:

- Độ đậm: `contrast(f) brightness(b)` với `f` là phần trăm chia 100 và `b = 1 - (f - 1) * 0.5`, chặn dưới 0.5.
- Độ dày nét: hai lớp `drop-shadow` bán kính sát 0, bán kính suy từ bề rộng khối đang hiển thị.

Đổi công thức một bên mà quên bên kia thì bản xem trước nói dối.

### Ký số hàng loạt

Màn `ky-so` có hai chế độ nguồn file:

- **File từ máy**: tạo lô rỗng rồi đẩy file theo từng đợt khoảng 50 file. Đợt nào hỏng chỉ gửi lại đúng đợt
  đó, không bắt chọn lại cả thư mục. Vài GB trong một request sẽ đụng giới hạn proxy và timeout.
- **Thư mục trên MinIO**: dán đường dẫn thư mục, backend đọc thẳng từ kho. Không có byte nào được tải lên.

Tiến độ hỏi theo nhịp cố định 2 giây. Lô chạy nền ở backend nên đóng tab rồi mở lại vẫn nạp lại được đúng
tiến độ qua endpoint `lo-ky/dang-chay`.

Frontend **chỉ nói chuyện với plugin đúng một chỗ**: liệt kê chứng thư số. Bước đó không bao giờ hỏi mã PIN;
PIN chỉ bật khi bấm nút Xác thực, và hộp nhập PIN là của Windows chứ không phải của ứng dụng. Lời gọi cần
người dùng thao tác thì **không đặt timeout**.

## Cấu hình môi trường

`src/environments/environment.ts` cho môi trường phát triển, `environment.development.ts` cho bản build tương
ứng:

| Trường | Nội dung |
|---|---|
| `apiUrl` | Địa chỉ backend |
| `appUrl` | Địa chỉ chính ứng dụng, dùng khi chuyển hướng đăng nhập |
| `authGrantType`, `authClientId`, `authClientSecret`, `authScope` | Tham số OAuth2 khớp với OpenIddict bên backend |
| `pluginUrl` | Địa chỉ plugin trên máy người dùng, mặc định `http://127.0.0.1:17739` |

## Chạy dự án

Yêu cầu Node.js 20.x. Khuyến nghị dùng [nvm-windows](https://github.com/coreybutler/nvm-windows) để quản lý
nhiều phiên bản Node.

```bash
nvm install 20.9.0
nvm use 20.9.0
npm i -g @angular/cli

npm install
npm start
```

Ứng dụng chạy tại `http://localhost:4200`.

| Lệnh | Việc |
|---|---|
| `npm start` | Chạy môi trường phát triển |
| `npm run build` | Build bản phát hành |
| `npm run watch` | Build lại mỗi khi có thay đổi |
| `npm run format` | Định dạng mã bằng Prettier |

Thư mục `src/assets` được khai là git submodule trỏ tới bộ asset của Sakai. Clone mới cần chạy thêm:

```bash
git submodule update --init --recursive
```

## Coding convention

### Tổng quan

- Component mới **sinh bằng `ng generate component`**, không tạo tay từng file.
- Dùng **standalone component** và **signals**; không thêm NgModule mới.
- Không lặp lại code giữa các màn hình; phần dùng chung đưa về `shared/components`.
- Component kế thừa `BaseComponent` để có sẵn `loading`, `isResponseSucceed`, các hàm hiện thông báo và hộp
  xác nhận.

### Đặt tên

- Interface mở đầu bằng `I`: `IViewLoKy`, `IConfigTemplate`.
- Tên biến, hàm và nhãn giao diện viết **tiếng Việt không dấu** theo nghiệp vụ (`dangKy`, `phanTramUpload`,
  `onDayLenKho`) — đồng bộ với cách đặt tên bên backend.
- Service của nghiệp vụ nào đặt đúng tên nghiệp vụ đó: `lo-ky.service.ts`, `giay-bao.service.ts`.

### Giao diện

- Theo khuôn Sakai: breadcrumb nằm **trong** trang, tiêu đề ngay dưới, toàn bộ nội dung bọc trong một
  `<div class="card">`.
- Bảng dài thì cuộn **trong bảng**, không để cuộn cả trang.
- Việc chạy lâu phải có thanh tiến độ riêng cho từng việc. Gộp hai việc khác nhau vào một thanh là người dùng
  hiểu nhầm.

### Hằng số

Hằng số nào backend cũng dùng thì phải khớp giá trị hai bên: khoá quyền, khoảng giá trị thanh trượt, hệ số
công thức xử lý ảnh. Sửa một bên mà quên bên kia sẽ sai lặng lẽ, không có lỗi biên dịch nào báo.

### Comment

Comment viết **tiếng Việt**, đặt ở đầu hàm hoặc đầu khối, giải thích *vì sao* chứ không nhắc lại code.
