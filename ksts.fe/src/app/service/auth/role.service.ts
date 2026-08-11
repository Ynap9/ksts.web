
import { IFindPagingRole, IViewRowRole, ICreateRole, IUpdateRole } from '@/app/models/role.models';
import { IBaseResponsePaging, IBaseResponseWithData, IBaseResponse } from '@/app/shared/models/request-paging.base.models';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})
export class RoleService {
    api = '/api/app/roles';
    http = inject(HttpClient);

    findPaging(query: IFindPagingRole) {
        return this.http.get<IBaseResponsePaging<IViewRowRole>>(this.api, {
            params: { ...query }
        });
    }

    getById(id: string) {
        return this.http.get<IBaseResponseWithData<IViewRowRole>>(`${this.api}/${id}`);
    }

    getList() {
        return this.http.get<IBaseResponseWithData<IViewRowRole[]>>(`${this.api}/list`);
    }

    create(body: ICreateRole) {
        return this.http.post<IBaseResponse>(`${this.api}`, body);
    }

    update(body: IUpdateRole) {
        return this.http.put<IBaseResponse>(`${this.api}`, body);
    }
}
