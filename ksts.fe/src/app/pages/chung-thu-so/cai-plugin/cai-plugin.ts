
import { IViewBoCaiPlugin } from '@/app/models/plugin.models';
import { NhacCaiPluginService } from '@/app/service/nhac-cai-plugin.service';
import { PluginService } from '@/app/service/plugin.service';
import { BaseComponent } from '@/app/shared/components/base/base-component';
import { SharedImports } from '@/app/shared/import.shared';
import { Component, inject, signal } from '@angular/core';
import { DynamicDialogRef } from 'primeng/dynamicdialog';

@Component({
    selector: 'app-cai-plugin',
    imports: [SharedImports],
    templateUrl: './cai-plugin.html',
    styles: ``
})
export class CaiPlugin extends BaseComponent {
    private _ref = inject(DynamicDialogRef);
    private _pluginService = inject(PluginService);
    private _nhacCaiPluginService = inject(NhacCaiPluginService);

    boCai = signal<IViewBoCaiPlugin>({});

    override ngOnInit(): void {
        this._pluginService.getBoCai().subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res)) {
                    this.boCai.set(res.data);
                }
            }
        });
    }

    /**
     * Tải bộ cài từ BE rồi đẩy xuống đĩa. Phải đi qua HttpClient chứ không mở thẳng URL: endpoint yêu cầu
     * Bearer token, mở bằng thẻ a hay window.open thì trình duyệt gửi request trần và nhận 401.
     */
    onDownload() {
        this.loading.set(true);
        this._pluginService
            .taiBoCai()
            .subscribe({
                next: (blob) => {
                    const url = URL.createObjectURL(blob);
                    const link = document.createElement('a');
                    link.href = url;
                    link.download = this.boCai().fileName ?? 'KstsPlugin.exe';
                    link.click();
                    URL.revokeObjectURL(url);
                },
                error: () => {
                    this.messageError('Không tải được bộ cài plugin. Vui lòng liên hệ quản trị hệ thống.');
                }
            })
            .add(() => {
                this.loading.set(false);
            });
    }

    onRetry() {
        this._ref?.close(true);
    }

    onKhongNhacLai() {
        this._nhacCaiPluginService.tat();
        this._ref?.close(false);
    }
}
