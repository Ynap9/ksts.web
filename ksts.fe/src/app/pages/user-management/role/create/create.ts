
import { IViewPermission } from '@/app/models/permission.models';
import { ICreateRole, IUpdateRole } from '@/app/models/role.models';
import { PermissionService } from '@/app/service/auth/permission.service';
import { RoleService } from '@/app/service/auth/role.service';
import { BaseComponent } from '@/app/shared/components/base/base-component';
import { SharedImports } from '@/app/shared/import.shared';
import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';

import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { PickListModule } from 'primeng/picklist';

@Component({
    selector: 'app-create',
    imports: [SharedImports, PickListModule],
    templateUrl: './create.html',
    styleUrl: './create.scss'
})
export class Create extends BaseComponent {
    private _ref = inject(DynamicDialogRef);
    private _config = inject(DynamicDialogConfig);
    private _permissionService = inject(PermissionService);
    private _roleService = inject(RoleService);

    listPermission: IViewPermission[] = [];
    selectedPermissions: IViewPermission[] = [];

    override form: FormGroup = new FormGroup({
        name: new FormControl('', [Validators.required])
    });

    override ValidationMessages: Record<string, Record<string, string>> = {
        name: {
            required: 'Không được bỏ trống'
        }
    };

    override ngOnInit(): void {
        this.getPermission();
    }

    get isUpdate() {
        return this._config.data?.id;
    }

    getPermission() {
        this._permissionService.getList().subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res)) {
                    this.listPermission = res.data;

                    if (this.isUpdate) {
                        this.form.patchValue({ name: this._config.data?.name });
                        this.listPermission = res.data.filter((x) => !(this._config.data?.permissionKey || []).includes(x.key));
                        this.selectedPermissions = res.data.filter((x) => (this._config.data?.permissionKey || []).includes(x.key));
                    }
                }
            }
        });
    }

    onSubmit() {
        if (this.isFormInvalid()) {
            return;
        }

        if (this.isUpdate) {
            this.onSubmitUpdate();
        } else {
            this.onSubmitCreate();
        }
    }

    onSubmitCreate() {
        const body: ICreateRole = {
            name: this.form.value['name'],
            permissionKey: this.selectedPermissions.map((x) => x.key!)
        };
        this.loading.set(true);
        this._roleService.create(body).subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res, true, 'Đã thêm vai trò')) {
                    this._ref?.close(true);
                }
            },
            error: (err) => {
                this.messageError(err?.message);
            },
            complete: () => {
                this.loading.set(false);
            }
        });
    }

    onSubmitUpdate() {
        const body: IUpdateRole = {
            id: this._config.data?.id,
            name: this.form.value['name'],
            permissionKey: this.selectedPermissions.map((x) => x.key!)
        };
        this.loading.set(true);
        this._roleService.update(body).subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res, true, 'Đã cập nhật')) {
                    this._ref?.close(true);
                }
            },
            error: (err) => {
                this.messageError(err?.message);
            },
            complete: () => {
                this.loading.set(false);
            }
        });
    }
}
