
import { IViewRowRole } from '@/app/models/role.models';
import { ICreateUser, IUpdateUser } from '@/app/models/user.models';
import { RoleService } from '@/app/service/auth/role.service';
import { UserService } from '@/app/service/user.service';
import { BaseComponent } from '@/app/shared/components/base/base-component';
import { SharedImports } from '@/app/shared/import.shared';
import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';

import { CheckboxChangeEvent } from 'primeng/checkbox';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { PickListModule } from 'primeng/picklist';
import { concatMap } from 'rxjs';

@Component({
    selector: 'app-create',
    imports: [SharedImports, PickListModule],
    templateUrl: './create.html',
    styleUrl: './create.scss'
})
export class Create extends BaseComponent {
    private _ref = inject(DynamicDialogRef);
    private _config = inject(DynamicDialogConfig);
    private _userService = inject(UserService);
    private _roleService = inject(RoleService);

    listRole: IViewRowRole[] = [];

    override form: FormGroup = new FormGroup(
        {
            userName: new FormControl('', [Validators.required]),
            fullName: new FormControl('', [Validators.required]),
            email: new FormControl('', [Validators.required]),
            roleNames: new FormControl([], [Validators.required]),
            phoneNumber: new FormControl('', [Validators.required]),
            password: new FormControl(''),
            msAccount: new FormControl(''),
            isRandomPassword: new FormControl(true)
        }
    );

    override ValidationMessages: Record<string, Record<string, string>> = {
        userName: {
            required: 'Không được bỏ trống'
        },
        fullName: {
            required: 'Không được bỏ trống'
        },
        email: {
            required: 'Không được bỏ trống'
        },
        phoneNumber: {
            required: 'Không được bỏ trống'
        },
        roleNames: {
            required: 'Không được bỏ trống'
        },
    };

    get isUpdate() {
        return this._config.data?.id;
    }


    override ngOnInit(): void {
        if (this.isUpdate) {
            this.initOnUpdate();
        } else {
            this.initOnCreate();
        }
    }

    initOnCreate() {
        this._roleService.getList()
            .subscribe({
                next: (res) => {
                    if (this.isResponseSucceed(res)) {
                        this.listRole = res.data;
                    }
                }
            });
    }

    initOnUpdate() {
        this._roleService.getList()
            .pipe(
                concatMap(res => {
                    if (this.isResponseSucceed(res)) {
                        this.listRole = res.data;
                    }
                    return this._userService.getById(this._config.data.id);
                })
            )
            .subscribe({
                next: (res) => {
                    if (this.isResponseSucceed(res)) {
                        this.form.setValue({
                            userName: res.data.userName,
                            fullName: res.data.fullName,
                            email: res.data.email,
                            phoneNumber: res.data.phoneNumber,
                            msAccount: res.data?.msAccount,
                            roleNames: res.data.roles,
                            password: '',
                            isRandomPassword: false,
                        });
                    }
                }
            });
    }

    onChangeIsRandomPassword(event: CheckboxChangeEvent) {
        if (event.checked) {
            this.form.patchValue({ password: null });
        }
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
        const body: ICreateUser = {
            ...this.form.value
        };
        this.loading.set(true);
        this._userService.create(body).subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res, true, 'Đã tạo tài khoản')) {
                    if (!!res.data.passwordRandom) {
                        this.form.patchValue({ password: res.data.passwordRandom })
                    }
                    // this._ref?.close(true);
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
        const body: IUpdateUser = {
            id: this._config.data?.id,
            ...this.form.value
        };
        this.loading.set(true);
        this._userService.update(body).subscribe({
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
