
import { ICreateTemplate, IUpdateTemplate } from '@/app/models/template.models';
import { TemplateService } from '@/app/service/template.service';
import { BaseComponent } from '@/app/shared/components/base/base-component';
import { SharedImports } from '@/app/shared/import.shared';
import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';

@Component({
    selector: 'app-create',
    imports: [SharedImports],
    templateUrl: './create.html',
    styleUrl: './create.scss'
})
export class Create extends BaseComponent {
    private _ref = inject(DynamicDialogRef);
    private _config = inject(DynamicDialogConfig);
    private _templateService = inject(TemplateService);

    override form: FormGroup = new FormGroup(
        {
            tenTemplate: new FormControl('', [Validators.required])
        }
    );

    override ValidationMessages: Record<string, Record<string, string>> = {
        tenTemplate: {
            required: 'Không được bỏ trống'
        }
    };

    get isUpdate() {
        return this._config.data?.id;
    }

    override ngOnInit(): void {
        if (this.isUpdate) {
            this.initOnUpdate();
        }
    }

    initOnUpdate() {
        this._templateService.getById(this._config.data.id).subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res)) {
                    this.form.setValue({ tenTemplate: res.data.tenTemplate });
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
        const body: ICreateTemplate = {
            ...this.form.value
        };
        this.loading.set(true);
        this._templateService.create(body).subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res, true, 'Đã tạo template')) {
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
        const body: IUpdateTemplate = {
            id: this._config.data?.id,
            ...this.form.value
        };
        this.loading.set(true);
        this._templateService.update(body).subscribe({
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
