
import { IViewPermission } from '@/app/models/permission.models';
import { IBaseResponseWithData } from '@/app/shared/models/request-paging.base.models';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})
export class PermissionService {
    api = '/api/app/permissions';
    http = inject(HttpClient);

    getList() {
        return this.http.get<IBaseResponseWithData<IViewPermission[]>>(this.api);
    }
}
