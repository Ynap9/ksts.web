
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { IBaseResponseWithData } from '../shared/models/request-paging.base.models';
import { IViewSignCert } from '../models/chung-thu-so.models';

@Injectable({
    providedIn: 'root'
})
export class ChungThuSoService {
    api = '/api/core/chung-thu-so';
    http = inject(HttpClient);

    getList(onlySignable = false) {
        return this.http.get<IBaseResponseWithData<IViewSignCert[]>>(this.api, {
            params: { onlySignable }
        });
    }

    chon(thumbprint: string) {
        return this.http.post<IBaseResponseWithData<IViewSignCert>>(`${this.api}/chon`, { thumbprint });
    }
}
