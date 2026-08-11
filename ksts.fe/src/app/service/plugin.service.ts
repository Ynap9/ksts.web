
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { timeout } from 'rxjs';
import { environment } from '@/environments/environment';
import { IViewBoCaiPlugin, IViewCertScanResult, IViewTokenVerify, IViewTrangThaiPlugin } from '../models/plugin.models';
import { PLUGIN_PROBE_TIMEOUT_MS } from '../shared/constants/chung-thu-so.constants';
import { IKetQuaKy, IMoPhienKetQua, IYeuCauKy } from '../models/lo-ky.models';
import { IBaseResponse, IBaseResponseWithData } from '../shared/models/request-paging.base.models';

@Injectable({
    providedIn: 'root'
})
export class PluginService {
    /** Plugin chạy ở máy người dùng - địa chỉ tuyệt đối nên interceptor không gắn tiền tố apiUrl. */
    api = `${environment.pluginUrl}/api/plugin`;

    /** Bộ cài plugin do BE phát - đường dẫn tương đối, đi qua interceptor như mọi API khác. */
    apiBoCai = '/api/core/plugin';

    http = inject(HttpClient);

    /**
     * Phép dò plugin đã cài chưa: gọi không tới hoặc quá hạn nghĩa là máy chưa có plugin.
     * Đặt timeout ngắn vì máy chưa cài thì kết nối tới cổng đóng có thể treo lâu hơn người dùng chờ được.
     */
    getTrangThai() {
        return this.http
            .get<IBaseResponseWithData<IViewTrangThaiPlugin>>(`${this.api}/trang-thai`)
            .pipe(timeout(PLUGIN_PROBE_TIMEOUT_MS));
    }

    getListChungThuSo(onlySignable = false) {
        return this.http.get<IBaseResponseWithData<IViewCertScanResult>>(`${this.api}/chung-thu-so`, {
            params: { onlySignable }
        });
    }

    /**
     * Ký thử để kiểm chứng thư đã chọn - hộp thoại nhập PIN của bit4id bật lên ở đây, nên chỉ gọi khi người
     * dùng thực sự chọn một chứng thư. Không đặt timeout: người dùng cần thời gian nhập PIN.
     */
    kiemTraToken(thumbprint: string) {
        return this.http.post<IBaseResponseWithData<IViewTokenVerify>>(`${this.api}/chung-thu-so/kiem-tra-token`, {
            thumbprint
        });
    }

    /**
     * Mở phiên ký trên token. Hộp nhập PIN bật ở đây và chỉ ở đây, nên không đặt timeout: người dùng cần
     * thời gian gõ PIN. Trả về chứng thư phần công khai để nộp lên máy chủ.
     */
    moPhienKy(thumbprint: string) {
        return this.http.post<IBaseResponseWithData<IMoPhienKetQua>>(`${this.api}/ky-so/mo-phien`, {
            thumbprint
        });
    }

    /** Ký cả một đợt yêu cầu trong một lời gọi; token ký tuần tự nên gom đợt là cách duy nhất bớt vòng đi-về. */
    kyLo(yeuCau: IYeuCauKy[]) {
        return this.http.post<IBaseResponseWithData<IKetQuaKy[]>>(`${this.api}/ky-so/ky`, { yeuCau });
    }

    dongPhienKy() {
        return this.http.post<IBaseResponse>(`${this.api}/ky-so/dong-phien`, {});
    }

    getBoCai() {
        return this.http.get<IBaseResponseWithData<IViewBoCaiPlugin>>(`${this.apiBoCai}/bo-cai`);
    }

    /** Nội dung bộ cài dạng nhị phân: BE trả file nén thô, không bọc envelope. */
    taiBoCai() {
        return this.http.get(`${this.apiBoCai}/bo-cai/noi-dung`, { responseType: 'blob' });
    }
}
