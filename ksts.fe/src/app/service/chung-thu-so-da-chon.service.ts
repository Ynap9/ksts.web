
import { Injectable, signal } from '@angular/core';
import { IChungThuSoDaChon } from '../models/plugin.models';

/**
 * Giữ chứng thư số người dùng đã chọn và đã ký thử thành công ở màn Chứng thư số, để màn cấu hình template
 * dùng lại mà không phải chọn - và không bật lại hộp PIN - ở mỗi màn.
 *
 * CHỈ giữ trong RAM của phiên làm việc: không localStorage, không sessionStorage, không cookie. Cache chứng
 * thư xuống đĩa vừa trái quy ước ở docs/bao-mat-agent-ky-so.md §7, vừa để lại dấu vết đọc được bởi bất kỳ
 * script nào chạy trên trang. Mất khi F5 là đúng - đọc lại từ token nhanh và luôn phản ánh token đang cắm.
 *
 * Chỉ giữ phần định danh cần cho màn cấu hình; không giữ subject, issuer đầy đủ, số serial hay tên provider.
 */
@Injectable({
    providedIn: 'root'
})
export class ChungThuSoDaChonService {
    chungThuSo = signal<IChungThuSoDaChon | null>(null);

    set(cert: IChungThuSoDaChon) {
        this.chungThuSo.set({
            thumbprint: cert.thumbprint,
            commonName: cert.commonName,
            issuerCommonName: cert.issuerCommonName,
            validTo: cert.validTo
        });
    }

    clear() {
        this.chungThuSo.set(null);
    }
}
