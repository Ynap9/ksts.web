import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { AppMenuitem } from './app.menuitem';
import { SharedService } from '@/app/service/shared.service';
import { PermissionConstants } from '@/app/shared/constants/permission.constants';

@Component({
    selector: 'app-menu',
    standalone: true,
    imports: [CommonModule, AppMenuitem, RouterModule],
    template: `<ul class="layout-menu">
        @for (item of model; track $index) {
            @if (!item.separator) {
                <li app-menuitem [item]="item" [root]="true"></li>
            } @else {
                <li class="menu-separator"></li>
            }
        }
    </ul> `,
})
export class AppMenu {
    _sharedService = inject(SharedService);
    model: MenuItem[] = [];

    ngOnInit() {
        this.model = [
            
             
            {
                items: [
                    {
                        label: 'Chứng thư số',
                        icon: 'pi pi-fw pi-id-card',
                        routerLink: ['/chung-thu-so'],
                        visible: this._sharedService.isGranted(PermissionConstants.MenuChungThuSo),
                    },
                    {
                        label: 'Cấu hình template',
                        icon: 'pi pi-fw pi-file-edit',
                        routerLink: ['/template'],
                        visible: this._sharedService.isGranted(PermissionConstants.MenuTemplate),
                    },
                    {
                        label: 'Import data tuyển sinh',
                        icon: 'pi pi-fw pi-file-excel',
                        routerLink: ['/import-tuyen-sinh'],
                        visible: this._sharedService.isGranted(PermissionConstants.MenuImportTuyenSinh),
                    }
                ],
                // Nhóm hiện khi có quyền vào BẤT KỲ mục nào trong nhóm: gán quyền riêng cho từng màn rồi mà
                // vẫn khoá cả nhóm theo một quyền thì người chỉ được cấp Import sẽ không thấy menu nào cả.
                visible:
                    this._sharedService.isGranted(PermissionConstants.MenuChungThuSo) ||
                    this._sharedService.isGranted(PermissionConstants.MenuTemplate) ||
                    this._sharedService.isGranted(PermissionConstants.MenuImportTuyenSinh),
            },
            {
                items: [
                    {
                        label: 'Ký số',
                        icon: 'pi pi-fw pi-verified',
                        routerLink: ['/ky-so'],
                    }
                ],
                visible: this._sharedService.isGranted(PermissionConstants.MenuKySo),
            },
            {
                items: [
                    {
                        label: 'QL Tài khoản',
                        icon: 'pi pi-fw pi-users',
                        path: '/user-management',
                        visible: this._sharedService.isGranted(PermissionConstants.MenuUserManagement),
                        items: [
                            {
                                label: 'Người dùng',
                                visible: this._sharedService.isGranted(PermissionConstants.MenuUserManagementUser),
                                icon: 'pi pi-fw pi-user',
                                routerLink: ['/user-management/user']
                            },
                            {
                                label: 'Vai trò',
                                visible: this._sharedService.isGranted(PermissionConstants.MenuUserManagementRole),
                                icon: 'pi pi-fw pi-shield',
                                routerLink: ['/user-management/role']
                            }
                        ]
                    }
                ],
                visible: this._sharedService.isGranted(PermissionConstants.MenuUserManagement),
            },
        ];
    }
}
