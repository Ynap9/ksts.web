
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { IBaseResponse, IBaseResponsePaging, IBaseResponseWithData } from '../shared/models/request-paging.base.models';
import { IConfigTemplate, ICreateTemplate, IFindPagingTemplate, IUpdateTemplate, IViewFileMau, IViewRowTemplate, IViewTemplate } from '../models/template.models';

@Injectable({
    providedIn: 'root'
})
export class TemplateService {
    api = '/api/core/template-chu-ky';
    http = inject(HttpClient);

    findPaging(query: IFindPagingTemplate) {
        return this.http.get<IBaseResponsePaging<IViewRowTemplate>>(`${this.api}/find-paging`, {
            params: { ...query }
        });
    }

    getById(id: number) {
        return this.http.get<IBaseResponseWithData<IViewTemplate>>(`${this.api}/${id}`);
    }

    create(body: ICreateTemplate) {
        return this.http.post<IBaseResponseWithData<IViewTemplate>>(this.api, body);
    }

    update(body: IUpdateTemplate) {
        return this.http.put<IBaseResponseWithData<IViewTemplate>>(this.api, body);
    }

    delete(id: number) {
        return this.http.delete<IBaseResponse>(`${this.api}/${id}`);
    }

    getFileMau() {
        return this.http.get<IBaseResponseWithData<IViewFileMau>>(`${this.api}/file-mau`);
    }

    /** Nội dung file PDF mẫu: BE trả bytes thô, không bọc envelope, để trình xem PDF đọc thẳng. */
    getFileMauNoiDung() {
        return this.http.get(`${this.api}/file-mau/noi-dung`, { responseType: 'blob' });
    }

    /** Ghi đè toàn bộ cấu hình ký. Multipart vì kèm ảnh dấu đỏ và chữ ký tươi. */
    updateConfig(body: IConfigTemplate) {
        const formData = new FormData();
        formData.append('id', `${body.id}`);
        formData.append('thumbprint', body.thumbprint ?? '');
        formData.append('tenChungThu', body.tenChungThu ?? '');
        formData.append('lyDoKy', body.lyDoKy ?? '');
        formData.append('noiKy', body.noiKy ?? '');
        formData.append('hienThiChuKySo', `${body.hienThiChuKySo}`);
        formData.append('nhoiChuKySoVaoAnh', `${body.nhoiChuKySoVaoAnh}`);
        formData.append('kyDe', `${body.kyDe}`);
        formData.append('doDamDauDo', `${body.doDamDauDo}`);
        formData.append('doDamChuKyTuoi', `${body.doDamChuKyTuoi}`);
        formData.append('doDayNetChuKyTuoi', `${body.doDayNetChuKyTuoi}`);
        formData.append('mauChuKySo', body.mauChuKySo);
        formData.append('mauChuKyTuoi', body.mauChuKyTuoi ?? '');
        formData.append('xoaAnhDauDo', `${!!body.xoaAnhDauDo}`);
        formData.append('xoaAnhChuKyTuoi', `${!!body.xoaAnhChuKyTuoi}`);

        if (body.anhDauDo) {
            formData.append('anhDauDo', body.anhDauDo);
        }
        if (body.anhChuKyTuoi) {
            formData.append('anhChuKyTuoi', body.anhChuKyTuoi);
        }

        body.positions.forEach((position, index) => {
            formData.append(`positions[${index}].kind`, `${position.kind}`);
            formData.append(`positions[${index}].pageNumber`, `${position.pageNumber}`);
            formData.append(`positions[${index}].xRatio`, `${position.xRatio}`);
            formData.append(`positions[${index}].yRatio`, `${position.yRatio}`);
            formData.append(`positions[${index}].widthRatio`, `${position.widthRatio}`);
            formData.append(`positions[${index}].heightRatio`, `${position.heightRatio}`);
        });

        return this.http.put<IBaseResponseWithData<IViewTemplate>>(`${this.api}/cau-hinh`, formData);
    }
}
