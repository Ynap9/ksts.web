import { Component, computed, inject, signal } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { ProgressBarModule } from 'primeng/progressbar';

import { Breadcrumb } from '@/app/shared/components/breadcrumb/breadcrumb';
import { BaseComponent } from '@/app/shared/components/base/base-component';
import { GiayBaoService } from '@/app/service/giay-bao.service';
import { IExcelSheet, IViewThiSinh, IZipJob } from '@/app/models/giay-bao.models';
import { SharedImports } from '@/app/shared/import.shared';

/** Nhịp hỏi tiến độ. Lô 5000 file chạy khoảng nửa tiếng nên hỏi dày hơn chỉ tốn request. */
const NHIP_HOI_TIEN_DO = 2000;

/**
 * Số nhịp hỏi hỏng LIÊN TIẾP trước khi buông theo dõi. Lô chạy nền ở MÁY CHỦ nên một nhịp hỏng chỉ là chập
 * mạng, không phải lô hỏng — buông ngay chỉ làm màn hình báo lỗi trong khi lô vẫn đang chạy tốt.
 */
const SO_NHIP_HONG_TOI_DA = 5;

@Component({
    selector: 'app-import-tuyen-sinh',
    imports: [SharedImports, ProgressBarModule, Breadcrumb],
    templateUrl: './import-tuyen-sinh.html',
    styleUrl: './import-tuyen-sinh.scss'
})
export class ImportTuyenSinh extends BaseComponent {
    _giayBaoService = inject(GiayBaoService);

    breadcrumbHome: MenuItem = { icon: 'pi pi-home', routerLink: '/' };
    breadcrumbItems: MenuItem[] = [{ label: 'Import data tuyển sinh' }];

    file = signal<File | null>(null);
    sheets = signal<IExcelSheet[]>([]);
    sheetName = signal<string>('');
    dongBatDau = signal<number>(1);
    rows = signal<IViewThiSinh[]>([]);
    soNhipHong = 0;

    dangChay = signal<boolean>(false);
    keoVao = signal<boolean>(false);
    job = signal<IZipJob | null>(null);

    daXong = computed(() => this.job()?.daXong ?? 0);
    soLoi = computed(() => this.job()?.soLoi ?? 0);
    phanTram = computed(() => {
        const tong = this.rows().length;
        return tong === 0 ? 0 : Math.round(((this.daXong() + this.soLoi()) / tong) * 100);
    });
    taiDuoc = computed(() => !!this.job()?.hoanTat && !this.job()?.loiChung);

    /** Kho chứa giấy báo của lô. Có giá trị ngay từ khi mở lô vì file được đẩy lên trong lúc dựng. */
    tienToKho = computed(() => this.job()?.tienToKho ?? '');

    /** Tra nguyên nhân theo thứ tự dòng; chỉ dòng lỗi mới có mặt ở đây. */
    lyDoTheoThuTu = computed(() => {
        const bang = new Map<number, string>();
        for (const dong of this.job()?.dongLoi ?? []) {
            bang.set(dong.thuTu, dong.lyDo);
        }
        return bang;
    });

    coTheBatDau = computed(() => !!this.file() && !!this.sheetName() && this.rows().length > 0 && !this.dangChay());

    coTheDocDanhSach = computed(() => !!this.file() && !!this.sheetName() && !this.dangChay());

    onChonFile(event: Event) {
        const input = event.target as HTMLInputElement;
        const file = input.files?.[0];
        input.value = '';
        this.napFile(file);
    }

    onDragOver(event: DragEvent) {
        event.preventDefault();
        if (!this.dangChay()) {
            this.keoVao.set(true);
        }
    }

    onDragLeave(event: DragEvent) {
        event.preventDefault();
        this.keoVao.set(false);
    }

    onDrop(event: DragEvent) {
        event.preventDefault();
        this.keoVao.set(false);
        if (!this.dangChay()) {
            this.napFile(event.dataTransfer?.files?.[0]);
        }
    }

    napFile(file?: File | null) {
        if (!file) {
            return;
        }

        if (!file.name.toLowerCase().endsWith('.xlsx')) {
            this.messageWarning('Chỉ nhận file Excel định dạng .xlsx.');
            return;
        }

        this.datLai();
        this.file.set(file);
        this.loading.set(true);
        this._giayBaoService
            .danhSachSheet(file)
            .subscribe({
                next: (res) => {
                    if (this.isResponseSucceed(res)) {
                        this.sheets.set(res.data);
                        const dauTien = res.data.find((s) => s.rowCount > 0) ?? res.data[0];
                        if (dauTien) {
                            this.sheetName.set(dauTien.name);
                            this.docDanhSach();
                        }
                    }
                }
            })
            .add(() => this.loading.set(false));
    }

    onDoiNguon() {
        this.rows.set([]);
        this.job.set(null);
        this.docDanhSach();
    }

    docDanhSach() {
        const file = this.file();
        if (!file || !this.sheetName()) {
            return;
        }

        this.loading.set(true);
        this._giayBaoService
            .danhSachThiSinh(file, this.sheetName(), this.dongBatDau())
            .subscribe({
                next: (res) => {
                    if (this.isResponseSucceed(res)) {
                        this.rows.set(res.data);
                    }
                }
            })
            .add(() => this.loading.set(false));
    }

    onBatDau() {
        const file = this.file();
        if (!file || !this.coTheBatDau()) {
            return;
        }

        this.job.set(null);
        this.dangChay.set(true);
        this.soNhipHong = 0;

        this._giayBaoService.taoZip(file, this.sheetName(), this.dongBatDau()).subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res)) {
                    this.job.set(res.data);
                    this.hoiTienDo();
                } else {
                    this.dungVoiLoi('Không mở được lô dựng giấy báo.');
                }
            },
            error: () => this.dungVoiLoi('Không mở được lô dựng giấy báo.')
        });
    }

    /** Hỏi tiến độ theo nhịp cho tới khi lô xong; lô chạy nền nên rời màn rồi quay lại vẫn hỏi tiếp được. */
    hoiTienDo() {
        const jobId = this.job()?.jobId;
        if (!jobId) {
            return;
        }

        this._giayBaoService.tienDo(jobId).subscribe({
            next: (res) => {
                if (!this.isResponseSucceed(res, false)) {
                    this.hongMotNhip('Mất dấu lô dựng giấy báo.');
                    return;
                }

                this.soNhipHong = 0;
                const job = res.data;
                this.job.set(job);

                if (!job.hoanTat) {
                    setTimeout(() => this.hoiTienDo(), NHIP_HOI_TIEN_DO);
                    return;
                }

                this.dangChay.set(false);
                if (job.loiChung) {
                    this.messageError(`Lô dựng hỏng: ${job.loiChung}`);
                } else {
                    this.messageSuccess(`Đã dựng xong ${job.daXong}/${job.tongSo} giấy báo.`);
                }
            },
            error: () => this.hongMotNhip('Mất kết nối khi theo dõi tiến độ.')
        });
    }

    /** Một nhịp hỏi hỏng chưa phải lô hỏng: lô chạy nền ở máy chủ nên hỏi lại vài lần rồi mới buông. */
    hongMotNhip(thongDiep: string) {
        this.soNhipHong++;

        if (this.soNhipHong < SO_NHIP_HONG_TOI_DA) {
            setTimeout(() => this.hoiTienDo(), NHIP_HOI_TIEN_DO * this.soNhipHong);
            return;
        }

        this.dangChay.set(false);
        this.messageError(`${thongDiep} Lô vẫn chạy ở máy chủ, mở lại màn hình để xem tiếp.`);
    }

    dungVoiLoi(thongDiep: string) {
        this.dangChay.set(false);
        this.messageError(thongDiep);
    }

    onTaiVe() {
        const job = this.job();
        if (!job || !this.taiDuoc()) {
            return;
        }

        window.location.href = this._giayBaoService.duongDanTai(job);
    }

    datLai() {
        this.file.set(null);
        this.sheets.set([]);
        this.sheetName.set('');
        this.dongBatDau.set(1);
        this.rows.set([]);
        this.job.set(null);
    }
}
