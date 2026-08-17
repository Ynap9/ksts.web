# Tầng HTTP và service

> **Phần 2/5** · trước: [01-routing-guards.md](01-routing-guards.md) · mục lục: [README.md](README.md)

## authInterceptor — một chỗ lo ba việc

`src/config/auth.interceptor.ts` là interceptor duy nhất, đăng ký trong `app.config.ts`:

1. **Gắn tiền tố `apiUrl`** cho mọi URL tương đối ⇒ service chỉ khai `'/api/core/lo-ky'`, không ghi host.
2. **Gắn `Authorization: Bearer`** — chỉ khi *có* token **và** URL thuộc API của chính hệ thống.
3. **Gặp 401 thì tự làm mới token**: đổi `refresh_token` ở `/connect/token` (OpenIddict, `client_id` +
   `client_secret` lấy từ `environment`), lưu lại cặp token mới rồi **phát lại đúng request đó**. Không có
   refresh token hoặc làm mới thất bại ⇒ dọn storage và về `/auth/login`.

Hai điều kiện ở bước 2 đều có lý do đã sập một lần:

- ⚠️ **Không có token thì không gắn header.** Gắn `Bearer undefined` làm chính endpoint lấy token bị từ chối.
- ⚠️ **Không gắn token cho địa chỉ ngoài `apiUrl`.** Plugin ký số là **tiến trình khác trên máy người dùng**;
  gửi access token sang đó là phát token ra ngoài phạm vi cần thiết. Đây là lý do `PluginService` dùng URL
  tuyệt đối.

## Khuôn một service

Một service cho một nhóm API, đặt ở `src/app/service/`, `providedIn: 'root'`:

```ts
@Injectable({ providedIn: 'root' })
export class LoKyService {
    api = '/api/core/lo-ky';          // tương đối -> interceptor gắn apiUrl
    http = inject(HttpClient);

    taoLo(templateId: number) {
        return this.http.post<IBaseResponseWithData<IViewLoKy>>(this.api, { templateId });
    }
}
```

- Trả **Observable thô**, không `subscribe` trong service — component quyết định vòng đời.
- Kiểu trả về luôn là envelope: `IBaseResponse` · `IBaseResponseWithData<T>` · `IBaseResponsePaging<T>`
  (`shared/models/request-paging.base.models.ts`).
- Service **không** hiện toast, không điều hướng, không giữ trạng thái màn hình.
- `multipart` dựng ngay trong service (`TemplateService.updateConfig` gói cả ảnh và `positions[i].*`) — component
  chỉ đưa object nghiệp vụ.

| Service | Nhóm API |
|---|---|
| `lo-ky.service` · `giay-bao.service` · `template.service` · `chung-thu-so.service` · `user.service` | `api/core/*` của BE |
| `plugin.service` | **Plugin ở máy người dùng** + đường phát bộ cài của BE |
| `auth/auth.service` · `auth/permission.service` · `auth/role.service` | Đăng nhập, quyền, vai trò |
| `shared.service` · `auth/app-session.service` · `chung-thu-so-da-chon.service` · `nhac-cai-plugin.service` | Giữ trạng thái phiên, không gọi API |

## Gọi plugin — khác mọi service còn lại

`PluginService` giữ **hai** gốc URL vì hai đích khác nhau:

```ts
api      = `${environment.pluginUrl}/api/plugin`;   // tuyệt đối -> không tiền tố, KHÔNG Bearer
apiBoCai = '/api/core/plugin';                      // tương đối -> qua interceptor như mọi API
```

Quy tắc timeout, đừng đảo:

| Lời gọi | Timeout |
|---|---|
| `trang-thai` (dò plugin đã cài chưa) | **2 s** (`PLUGIN_PROBE_TIMEOUT_MS`) — cổng đóng có thể treo lâu hơn người dùng chờ được |
| `chung-thu-so/kiem-tra-token`, `ky-so/mo-phien` | **KHÔNG timeout** — hộp PIN bật ở đây, người dùng cần thời gian gõ |
| `lo-ky/{id}/cho-ky` | **KHÔNG timeout** — máy chủ giữ lời gọi tới 25 s |

Hợp đồng đầy đủ: [../../contracts/plugin-ky-so.contract.md](../../contracts/plugin-ky-so.contract.md).

## Envelope — cổng bắt buộc

BE luôn trả HTTP 200, trạng thái thật nằm ở `status` (`1` ok, `0` lỗi). FE **không được** đọc `res.data` trước
khi qua cổng `BaseComponent.isResponseSucceed(res)` — xem [03-components-state.md](03-components-state.md). Bỏ
cổng thì lỗi nghiệp vụ trôi vào màn hình thành dữ liệu rỗng, không ai thấy.

Ngoại lệ: `getTrangThai()` của plugin được kiểm tay (`res?.status !== 1 || !res.data?.sanSang`) vì nó chạy
trong nhánh dò, không nên hiện toast lỗi khi máy chưa cài plugin.

## Ba đường không phải JSON

| Việc | Cách |
|---|---|
| File PDF mẫu, bộ cài plugin | `responseType: 'blob'` rồi tạo object URL / tải xuống |
| Tải zip lô ký, zip giấy báo | Service **chỉ dựng chuỗi URL** (`duongDanTaiZip`), component gán `window.location.href` |
| Đẩy file lên lô | `FormData`, chia đợt ~50 file (xem [04-man-hinh-dac-thu.md](04-man-hinh-dac-thu.md)) |

⚠️ **Zip tuyệt đối không tải bằng `HttpClient` rồi tạo blob**: lô 5000 giấy báo là vài GB, gói vào bộ nhớ trang
là hết bộ nhớ. Điều hướng thẳng thì không gắn được header `Authorization`, nên đường zip mang `taiToken` phát
riêng cho lô — đó là lý do nó là endpoint duy nhất không đòi Bearer.

> **Tiếp:** [03-components-state.md](03-components-state.md) — `BaseComponent`, signals và bảng dữ liệu dùng chung.
