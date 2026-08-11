import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '@/environments/environment';
import { IKetQuaKy, IViewFileKy, IViewLoKy, IViewTienDoLoKy, IYeuCauKy } from '../models/lo-ky.models';
import { IBaseResponse, IBaseResponseWithData } from '../shared/models/request-paging.base.models';

@Injectable({
    providedIn: 'root'
})
export class LoKyService {
    api = '/api/core/lo-ky';

    http = inject(HttpClient);

    taoLo(templateId: number) {
        return this.http.post<IBaseResponseWithData<IViewLoKy>>(this.api, { templateId });
    }

    /**
     * Đẩy MỘT đợt file lên lô. Vài GB trong một request sẽ đụng giới hạn proxy và timeout, hỏng là phải tải
     * lại từ đầu; chia đợt thì đợt nào hỏng chỉ gửi lại đúng đợt đó.
     */
    themFile(loKyId: number, files: File[]) {
        const form = new FormData();
        files.forEach((file) => form.append('files', file, file.name));
        return this.http.post<IBaseResponseWithData<IViewLoKy>>(`${this.api}/${loKyId}/them-file`, form);
    }

    /**
     * Lấy file từ một thư mục có sẵn trên MinIO. Không có gì được tải lên cả — lô chỉ ghi lại object key
     * đang có, nên lô vài GB vào lô trong một request duy nhất.
     */
    themTuKho(loKyId: number, duongDan: string) {
        return this.http.post<IBaseResponseWithData<IViewLoKy>>(`${this.api}/${loKyId}/them-tu-kho`, { duongDan });
    }

    batDau(loKyId: number, thumbprint: string) {
        return this.http.post<IBaseResponseWithData<IViewLoKy>>(`${this.api}/${loKyId}/bat-dau`, { thumbprint });
    }

    /** Nộp chứng thư phần công khai của phiên ký lên máy chủ, gọi trước khi bắt đầu. */
    moPhien(loKyId: number, chungThuBase64: string) {
        return this.http.post<IBaseResponseWithData<IViewLoKy>>(`${this.api}/${loKyId}/mo-phien`, {
            chungThuBase64
        });
    }

    dongPhien(loKyId: number) {
        return this.http.post<IBaseResponse>(`${this.api}/${loKyId}/dong-phien`, {});
    }

    /**
     * Lấy yêu cầu ký đang chờ. Máy chủ GIỮ lời gọi này tới khi có việc nên không đặt timeout ngắn: hỏi theo
     * nhịp sẽ cộng độ trễ vào từng file, mà token ký tuần tự nên khoản đó nhân thẳng với số file.
     */
    choKy(loKyId: number) {
        return this.http.get<IBaseResponseWithData<IYeuCauKy[]>>(`${this.api}/${loKyId}/cho-ky`);
    }

    nopChuKy(loKyId: number, ketQua: IKetQuaKy[]) {
        return this.http.post<IBaseResponse>(`${this.api}/${loKyId}/chu-ky`, ketQua);
    }

    /** Danh sách file của lô, gọi MỘT lần khi mở màn hình - tiến độ hỏi theo nhịp nên không kèm danh sách. */
    danhSachFile(loKyId: number) {
        return this.http.get<IBaseResponseWithData<IViewFileKy[]>>(`${this.api}/${loKyId}/danh-sach-file`);
    }

    trangThai(loKyId: number) {
        return this.http.get<IBaseResponseWithData<IViewTienDoLoKy>>(`${this.api}/${loKyId}/trang-thai`);
    }

    huy(loKyId: number) {
        return this.http.post<IBaseResponse>(`${this.api}/${loKyId}/huy`, {});
    }

    /** Lô đang chạy dở của người dùng, để mở lại màn hình là thấy đúng tiến độ chứ không bắt tạo lô mới. */
    loDangChay() {
        return this.http.get<IBaseResponseWithData<IViewLoKy | null>>(`${this.api}/dang-chay`);
    }

    /**
     * Đường dẫn tải zip. Trình duyệt tự tải thẳng xuống đĩa qua đường này chứ không đọc qua JS: lô 5000 giấy
     * báo lên tới vài GB, nhận vào bộ nhớ trang rồi mới lưu là hết bộ nhớ. Điều hướng thì không gắn được
     * header Authorization nên phải mang theo token phát riêng cho lô.
     */
    duongDanTaiZip(loKyId: number, taiToken: string) {
        return `${environment.apiUrl}${this.api}/${loKyId}/zip?token=${encodeURIComponent(taiToken)}`;
    }
}
