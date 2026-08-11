import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '@/environments/environment';
import { IExcelSheet, IViewThiSinh, IZipJob } from '../models/giay-bao.models';
import { IBaseResponseWithData } from '../shared/models/request-paging.base.models';

@Injectable({
    providedIn: 'root'
})
export class GiayBaoService {
    api = '/api/core/giay-bao';

    http = inject(HttpClient);

    danhSachSheet(file: File) {
        return this.http.post<IBaseResponseWithData<IExcelSheet[]>>(`${this.api}/danh-sach-sheet`, this.taoForm(file));
    }

    danhSachThiSinh(file: File, sheetName: string, startRow: number) {
        return this.http.post<IBaseResponseWithData<IViewThiSinh[]>>(`${this.api}/danh-sach-thi-sinh`, this.taoForm(file), {
            params: { sheetName, startRow }
        });
    }

    /** Mở lô dựng chạy nền, trả về JobId ngay chứ không chờ dựng xong. */
    taoZip(file: File, sheetName: string, startRow: number) {
        return this.http.post<IBaseResponseWithData<IZipJob>>(`${this.api}/tao-zip`, this.taoForm(file), {
            params: { sheetName, startRow }
        });
    }

    /** Mở việc đẩy cả lô lên MinIO chạy nền. Song song với tải zip, không thay thế nó. */
    dayLenKho(jobId: string) {
        return this.http.post<IBaseResponseWithData<IZipJob>>(`${this.api}/tao-zip/${jobId}/day-len-kho`, null);
    }

    tienDo(jobId: string) {
        return this.http.get<IBaseResponseWithData<IZipJob>>(`${this.api}/tao-zip/${jobId}`);
    }

    /**
     * Đường dẫn tải zip. Trình duyệt tự tải xuống đĩa qua đường này, không đọc qua JS: file lô lớn tới vài
     * GB, nhận vào bộ nhớ trang rồi mới lưu là hết bộ nhớ.
     */
    duongDanTai(job: IZipJob) {
        return `${environment.apiUrl}${this.api}/tao-zip/${job.jobId}/tai-ve?token=${job.taiToken}`;
    }

    private taoForm(file: File): FormData {
        const form = new FormData();
        form.append('file', file, file.name);
        return form;
    }
}
