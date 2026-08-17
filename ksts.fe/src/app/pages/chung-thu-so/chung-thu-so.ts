
import { ICertRow, IViewSignCert } from '@/app/models/plugin.models';
import { ChungThuSoDaChonService } from '@/app/service/chung-thu-so-da-chon.service';
import { NhacCaiPluginService } from '@/app/service/nhac-cai-plugin.service';
import { PluginService } from '@/app/service/plugin.service';
import { Breadcrumb } from '@/app/shared/components/breadcrumb/breadcrumb';
import { BaseComponent } from '@/app/shared/components/base/base-component';
import { DataTable } from '@/app/shared/components/data-table/data-table';
import {
    CERT_NOT_SIGNABLE_LABEL,
    CERT_SIGNABLE_LABEL,
    CERT_SOURCE_LABELS,
    CERT_SOURCE_SEVERITIES,
    ECertSource
} from '@/app/shared/constants/chung-thu-so.constants';
import { CellViewTypes } from '@/app/shared/constants/data-table.constants';
import { SharedImports } from '@/app/shared/import.shared';
import { IColumn } from '@/app/shared/models/data-table.models';
import { Component, computed, inject, signal } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { TagModule } from 'primeng/tag';
import { CaiPlugin } from './cai-plugin/cai-plugin';



@Component({
    selector: 'app-chung-thu-so',
    imports: [SharedImports, DataTable, Breadcrumb, TagModule],
    templateUrl: './chung-thu-so.html',
    styleUrl: './chung-thu-so.scss'
})
export class ChungThuSo extends BaseComponent {
    private _pluginService = inject(PluginService);
    private _chungThuSoDaChonService = inject(ChungThuSoDaChonService);
    private _nhacCaiPluginService = inject(NhacCaiPluginService);

    breadcrumbHome: MenuItem = { icon: 'pi pi-home', routerLink: '/' };
    breadcrumbItems: MenuItem[] = [{ label: 'Chứng thư số' }];

    /** Có id nghĩa là người dùng đang cấu hình dở một template và ghé qua đây chỉ để chọn chứng thư. */
    templateId = this._activatedRoute.snapshot.queryParamMap.get('templateId');

    coPlugin = signal<boolean>(false);
    dangDoPlugin = signal<boolean>(false);
    dangKiemTraToken = signal<boolean>(false);
    tokenHopLe = signal<boolean>(false);
    rows = signal<ICertRow[]>([]);
    selectedThumbprint = signal<string | null>(null);

    selectedCert = computed<ICertRow | null>(
        () => this.rows().find((row) => row.thumbprint === this.selectedThumbprint()) ?? null
    );

    hasInvalidSelection = computed<boolean>(() => {
        const cert = this.selectedCert();
        return !!cert && !cert.kyDuoc;
    });

    signableCount = computed<number>(() => this.rows().filter((row) => row.kyDuoc).length);
    tokenCount = computed<number>(() => this.rows().filter((row) => row.source === ECertSource.UsbToken).length);

    columns: IColumn[] = [
        { header: 'STT', cellViewType: CellViewTypes.INDEX, headerContainerStyle: 'width: 6rem' },
        { header: 'Chủ sở hữu', field: 'commonName', headerContainerStyle: 'min-width: 14rem' },
        { header: 'Tổ chức cấp', field: 'issuerCommonName', headerContainerStyle: 'min-width: 14rem' },
        {
            header: 'Nguồn',
            field: 'sourceLabel',
            cellViewType: CellViewTypes.STATUS,
            statusSeverityFunction: (row: ICertRow) => CERT_SOURCE_SEVERITIES[row.source as ECertSource] ?? 'secondary'
        },
        { header: 'Số serial', field: 'serialNumber', headerContainerStyle: 'min-width: 10rem' },
        { header: 'Hiệu lực đến', field: 'validTo', headerContainerStyle: 'min-width: 10rem' },
        {
            header: 'Trạng thái',
            field: 'statusLabel',
            cellViewType: CellViewTypes.STATUS,
            statusSeverityFunction: (row: ICertRow) => (row.kyDuoc ? 'success' : 'danger')
        },
        { header: 'Ghi chú', field: 'reason', headerContainerStyle: 'min-width: 14rem' }
    ];

    override ngOnInit(): void {
        this.kiemTraPlugin();
    }

    /**
     * Dò plugin ở máy người dùng. Gọi không tới hoặc quá hạn đều coi là CHƯA CÀI - máy chưa cài thì không có
     * gì trả lời, không phân biệt được lỗi mạng với lỗi chưa cài, mà cách xử lý cho người dùng thì như nhau.
     */
    kiemTraPlugin() {
        this.dangDoPlugin.set(true);
        this._pluginService
            .getTrangThai()
            .subscribe({
                next: (res) => {
                    const sanSang = res?.status === 1 && !!res.data?.sanSang;
                    this.coPlugin.set(sanSang);
                    if (sanSang) {
                        this.getChungThuSo();
                    } else {
                        this.onOpenCaiPlugin();
                    }
                },
                error: () => {
                    this.coPlugin.set(false);
                    this.onOpenCaiPlugin();
                }
            })
            .add(() => {
                this.dangDoPlugin.set(false);
            });
    }

    onOpenCaiPlugin(batBuoc = false) {
        if (!batBuoc && this._nhacCaiPluginService.daTat()) {
            return;
        }

        const ref = this._dialogService.open(CaiPlugin, {
            header: 'Chưa có plugin ký số',
            closable: true,
            modal: true,
            draggable: false,
            styleClass: 'w-[700px]',
            focusOnShow: false
        });
        ref?.onClose.subscribe((result) => {
            if (result) {
                this.kiemTraPlugin();
            }
        });
    }

    /** Không cache danh sách: token có thể vừa được cắm hoặc vừa rút giữa hai lần mở màn hình. */
    getChungThuSo() {
        this.loading.set(true);
        this.tokenHopLe.set(false);
        this._pluginService
            .getListChungThuSo()
            .subscribe({
                next: (res) => {
                    if (this.isResponseSucceed(res)) {
                        const data: ICertRow[] = (res.data.certificates ?? []).map((cert) => ({
                            ...cert,
                            kyDuoc: !cert.reason,
                            sourceLabel: CERT_SOURCE_LABELS[cert.source as ECertSource] ?? 'Không xác định',
                            statusLabel: !cert.reason ? CERT_SIGNABLE_LABEL : CERT_NOT_SIGNABLE_LABEL
                        }));
                        this.rows.set(data);
                        this.totalRecords.set(data.length);

                        // Không chọn sẵn chứng thư nào: chọn là ký thử, mà ký thử là bật hộp PIN - không
                        // được tự bật khi người dùng mới chỉ mở màn hình.
                        this.selectedThumbprint.set(null);
                    }
                },
                error: () => {
                    this.rows.set([]);
                    this.selectedThumbprint.set(null);
                    this.coPlugin.set(false);
                    this.onOpenCaiPlugin();
                }
            })
            .add(() => {
                this.loading.set(false);
            });
    }

    /**
     * Chọn chứng thư là ra lệnh cho plugin ký thử: bit4id bật hộp nhập PIN của Windows ở bước này. Cert
     * không ký được thì không gọi, tránh làm phiền người dùng bằng một hộp PIN chắc chắn vô ích.
     */
    onSelectCert(thumbprint: string) {
        this.selectedThumbprint.set(thumbprint);
        const cert = this.selectedCert();
        if (!cert) {
            return;
        }

        if (!cert.kyDuoc) {
            this.messageWarning(cert.reason || 'Chứng thư số này không dùng để ký được.');
            this.tokenHopLe.set(false);
            return;
        }

        this.dangKiemTraToken.set(true);
        this._pluginService
            .kiemTraToken(thumbprint)
            .subscribe({
                next: (res) => {
                    if (!this.isResponseSucceed(res)) {
                        this.tokenHopLe.set(false);
                        return;
                    }

                    this.tokenHopLe.set(!!res.data.valid);
                    if (res.data.valid) {
                        this._chungThuSoDaChonService.set(cert);
                        this.messageSuccess('Chứng thư số đã sẵn sàng để ký.');
                        if (this.templateId) {
                            this.router.navigate(['/template/config-template', this.templateId]);
                        }
                    } else {
                        this._chungThuSoDaChonService.clear();
                        this.messageError(res.data.reason || 'Chứng thư số chưa sẵn sàng để ký.');
                    }
                },
                error: () => {
                    this.tokenHopLe.set(false);
                    this.messageError('Không kiểm tra được chứng thư số. Kiểm tra token đã cắm và plugin còn chạy không.');
                }
            })
            .add(() => {
                this.dangKiemTraToken.set(false);
            });
    }
}
