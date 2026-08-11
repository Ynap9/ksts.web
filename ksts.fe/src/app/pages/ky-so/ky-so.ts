import { Component, computed, inject, signal } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { ProgressBarModule } from 'primeng/progressbar';
import { TagModule } from 'primeng/tag';
import { firstValueFrom } from 'rxjs';

import { IViewFileKy, IViewLoKy, IViewTienDoLoKy } from '@/app/models/lo-ky.models';
import { IViewSignCert } from '@/app/models/plugin.models';
import { IViewRowTemplate } from '@/app/models/template.models';
import { LoKyService } from '@/app/service/lo-ky.service';
import { PluginService } from '@/app/service/plugin.service';
import { TemplateService } from '@/app/service/template.service';
import { BaseComponent } from '@/app/shared/components/base/base-component';
import { Breadcrumb } from '@/app/shared/components/breadcrumb/breadcrumb';
import { SharedImports } from '@/app/shared/import.shared';
import { CaiPlugin } from '../chung-thu-so/cai-plugin/cai-plugin';

/** Số file mỗi đợt upload. Đợt quá lớn thì đụng giới hạn proxy, quá nhỏ thì tốn số vòng request. */
const SO_FILE_MOI_DOT = 50;

/** Nhịp hỏi tiến độ. Lô vài nghìn file chạy hàng chục phút nên hỏi dày hơn chỉ tốn request. */
const NHIP_HOI_TIEN_DO = 2000;

@Component({
    selector: 'app-ky-so',
    imports: [SharedImports, ProgressBarModule, TagModule, Breadcrumb],
    templateUrl: './ky-so.html',
    styleUrl: './ky-so.scss'
})
export class KySo extends BaseComponent {
    private _loKyService = inject(LoKyService);
    private _templateService = inject(TemplateService);
    private _pluginService = inject(PluginService);

    breadcrumbHome: MenuItem = { icon: 'pi pi-home', routerLink: '/' };
    breadcrumbItems: MenuItem[] = [{ label: 'Ký số' }];

    /**
     * Nguồn file cần ký. Lấy từ kho thì KHÔNG có bước tải lên nào: lô chỉ ghi lại object key đang có trên
     * MinIO, nên lô 5000 giấy báo vừa dựng xong vào thẳng lô ký thay vì tải về máy rồi đẩy ngược lên.
     */
    nguon = signal<'may' | 'kho'>('may');
    duongDanKho = signal<string>('');
    dangLayTuKho = signal<boolean>(false);

    files = signal<File[]>([]);
    keoVao = signal<boolean>(false);

    templates = signal<IViewRowTemplate[]>([]);
    templateId = signal<number | null>(null);

    certs = signal<IViewSignCert[]>([]);
    thumbprint = signal<string | null>(null);
    dangXacThuc = signal<boolean>(false);
    tokenHopLe = signal<boolean>(false);

    lo = signal<IViewLoKy | null>(null);
    tienDo = signal<IViewTienDoLoKy | null>(null);
    dangUpload = signal<boolean>(false);
    phanTramUpload = signal<number>(0);
    dangKy = signal<boolean>(false);

    /** Lô đã lập thì lấy danh sách file từ server; chưa lập thì dựng tạm từ file người dùng vừa chọn. */
    rows = computed<IViewFileKy[]>(() => {
        const files = this.tienDo()?.files;
        if (files?.length) {
            return files;
        }

        return this.files().map((file, i) => ({
            id: 0,
            thuTu: i + 1,
            tenFile: file.name,
            trangThai: 'cho' as const
        }));
    });

    tongSo = computed(() => this.tienDo()?.tongSo ?? this.files().length);
    daXong = computed(() => this.tienDo()?.daXong ?? 0);
    soLoi = computed(() => this.tienDo()?.soLoi ?? 0);

    phanTram = computed(() => {
        const tong = this.tongSo();
        return tong === 0 ? 0 : Math.round(((this.daXong() + this.soLoi()) / tong) * 100);
    });

    certDaChon = computed<IViewSignCert | null>(
        () => this.certs().find((cert) => cert.thumbprint === this.thumbprint()) ?? null
    );

    coNguon = computed(() =>
        this.nguon() === 'kho' ? this.duongDanKho().trim().length > 0 : this.files().length > 0
    );

    coTheBatDau = computed(
        () =>
            this.coNguon() &&
            !!this.templateId() &&
            this.tokenHopLe() &&
            !this.dangKy() &&
            !this.dangUpload() &&
            !this.dangLayTuKho()
    );

    taiDuoc = computed(() => !!this.tienDo()?.hoanTat && this.daXong() > 0);

    dangDayKho = computed(() => !!this.tienDo()?.dangDayLenKho);
    daDayLenKho = computed(() => this.tienDo()?.daDayLenKho ?? 0);
    soLoiDayKho = computed(() => this.tienDo()?.soLoiDayLenKho ?? 0);
    phanTramDayKho = computed(() => {
        const tong = this.daXong();
        return tong === 0 ? 0 : Math.round(((this.daDayLenKho() + this.soLoiDayKho()) / tong) * 100);
    });

    /** Đẩy lên kho và tải zip là hai lựa chọn ngang hàng, cùng mở ra khi lô đã ký xong. */
    dayKhoDuoc = computed(() => this.taiDuoc() && !this.dangDayKho());

    override ngOnInit(): void {
        this.getTemplates();
        this.getChungThuSo();
        this.getLoDangChay();
    }

    getTemplates() {
        this._templateService
            .findPaging({ pageNumber: this.START_PAGE_NUMBER, pageSize: 1000 })
            .subscribe({
                next: (res) => {
                    if (this.isResponseSucceed(res)) {
                        this.templates.set(res.data?.items ?? []);
                    }
                }
            });
    }

    /** Không cache danh sách chứng thư: token có thể vừa được cắm hoặc vừa rút giữa hai lần mở màn hình. */
    getChungThuSo() {
        this._pluginService.getListChungThuSo(true).subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res, false)) {
                    this.certs.set(res.data?.certificates ?? []);
                }
            },
            error: () => {
                this.certs.set([]);
                this.onOpenCaiPlugin();
            }
        });
    }

    onOpenCaiPlugin() {
        const ref = this._dialogService.open(CaiPlugin, {
            header: 'Chưa có plugin ký số',
            closable: true,
            modal: true,
            styleClass: 'w-[700px]',
            focusOnShow: false
        });
        ref?.onClose.subscribe((ketQua) => {
            if (ketQua) {
                this.getChungThuSo();
            }
        });
    }

    /** Lô đang chạy dở: mở lại màn hình phải thấy đúng tiến độ chứ không bắt người dùng tạo lô mới. */
    getLoDangChay() {
        this._loKyService.loDangChay().subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res, false) && res.data) {
                    this.lo.set(res.data);
                    this.dangKy.set(true);
                    this.hoiTienDo();
                }
            }
        });
    }

    onChonFile(event: Event) {
        const input = event.target as HTMLInputElement;
        const chon = Array.from(input.files ?? []);
        input.value = '';
        this.napFile(chon);
    }

    onDragOver(event: DragEvent) {
        event.preventDefault();
        if (!this.dangKy()) {
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
        if (!this.dangKy()) {
            this.napFile(Array.from(event.dataTransfer?.files ?? []));
        }
    }

    /** Chỉ nhận .pdf và bỏ file trùng tên: kéo thả cả thư mục thì trong đó thường có sẵn thứ khác. */
    napFile(chon: File[]) {
        const pdf = chon.filter((file) => file.name.toLowerCase().endsWith('.pdf'));
        if (pdf.length === 0) {
            this.messageWarning('Không có file PDF nào trong phần vừa chọn.');
            return;
        }

        const daCo = new Set(this.files().map((file) => file.name));
        const them = pdf.filter((file) => !daCo.has(file.name));

        this.datLaiLo();
        this.files.update((files) => [...files, ...them]);

        if (them.length < pdf.length) {
            this.messageWarning(`Đã bỏ qua ${pdf.length - them.length} file trùng tên.`);
        }
    }

    onXoaDanhSach() {
        this.files.set([]);
        this.datLaiLo();
    }

    /**
     * Xác thực chứng thư = ra lệnh cho plugin ký thử một mẩu dữ liệu. Hộp nhập PIN do middleware token của
     * Windows tự bật ở bước này; PIN không đi qua trình duyệt và không vào hệ thống của mình.
     */
    onXacThucChungThu() {
        const thumbprint = this.thumbprint();
        if (!thumbprint) {
            return;
        }

        this.dangXacThuc.set(true);
        this.tokenHopLe.set(false);
        this._pluginService
            .kiemTraToken(thumbprint)
            .subscribe({
                next: (res) => {
                    if (!this.isResponseSucceed(res)) {
                        return;
                    }

                    this.tokenHopLe.set(!!res.data.valid);
                    if (res.data.valid) {
                        this.messageSuccess('Chứng thư số đã sẵn sàng để ký.');
                    } else {
                        this.messageError(res.data.reason || 'Chứng thư số chưa sẵn sàng để ký.');
                    }
                },
                error: () =>
                    this.messageError('Không xác thực được chứng thư số. Kiểm tra token đã cắm và plugin còn chạy không.')
            })
            .add(() => this.dangXacThuc.set(false));
    }

    onDoiChungThu(thumbprint: string) {
        this.thumbprint.set(thumbprint);
        this.tokenHopLe.set(false);
    }

    async onBatDau() {
        if (!this.coTheBatDau()) {
            return;
        }

        try {
            const lo = await this.taoLoVaDayFile();
            this.lo.set(lo);
            await this.goiBatDau(lo.id);
            this.dangKy.set(true);
            this.hoiTienDo();
        } catch (loi) {
            this.dangUpload.set(false);
            this.dangKy.set(false);
            this.messageError(loi instanceof Error ? loi.message : 'Không mở được lô ký.');
        }
    }

    /** Mở lô rỗng rồi đẩy file theo từng đợt; đợt nào hỏng thì dừng ngay để người dùng gửi lại đúng đợt đó. */
    async taoLoVaDayFile(): Promise<IViewLoKy> {
        this.dangUpload.set(true);
        this.phanTramUpload.set(0);

        const taoLo = await firstValueFrom(this._loKyService.taoLo(this.templateId()!));
        if (taoLo?.status !== 1 || !taoLo.data) {
            throw new Error(taoLo?.message || 'Không tạo được lô ký.');
        }

        const lo = taoLo.data;

        // Nguồn là kho thì không có gì để tải lên: một lời gọi ghi lại object key đang có là xong cả lô.
        if (this.nguon() === 'kho') {
            const res = await firstValueFrom(this._loKyService.themTuKho(lo.id, this.duongDanKho().trim()));
            if (res?.status !== 1 || !res.data) {
                throw new Error(res?.message || 'Không lấy được file từ thư mục trong kho.');
            }

            this.dangUpload.set(false);
            this.phanTramUpload.set(100);
            return res.data;
        }

        const files = this.files();
        for (let i = 0; i < files.length; i += SO_FILE_MOI_DOT) {
            const dot = files.slice(i, i + SO_FILE_MOI_DOT);
            const res = await firstValueFrom(this._loKyService.themFile(lo.id, dot));
            if (res?.status !== 1) {
                throw new Error(res?.message || `Đẩy file hỏng ở đợt bắt đầu từ file thứ ${i + 1}.`);
            }

            this.phanTramUpload.set(Math.round(((i + dot.length) / files.length) * 100));
        }

        this.dangUpload.set(false);
        return lo;
    }

    async goiBatDau(loKyId: number) {
        const res = await firstValueFrom(this._loKyService.batDau(loKyId, this.thumbprint()!));
        if (res?.status !== 1) {
            throw new Error(res?.message || 'Không bắt đầu ký được.');
        }
    }

    /** Hỏi tiến độ theo nhịp cho tới khi lô xong; lô chạy nền nên đóng tab rồi mở lại vẫn hỏi tiếp được. */
    hoiTienDo() {
        const loKyId = this.lo()?.id;
        if (!loKyId) {
            return;
        }

        this._loKyService.trangThai(loKyId).subscribe({
            next: (res) => {
                if (!this.isResponseSucceed(res, false)) {
                    this.dangKy.set(false);
                    this.messageError('Mất dấu lô ký.');
                    return;
                }

                const tienDo = res.data;
                this.tienDo.set(tienDo);

                if (!tienDo.hoanTat && tienDo.dangChay) {
                    setTimeout(() => this.hoiTienDo(), NHIP_HOI_TIEN_DO);
                    return;
                }

                this.dangKy.set(false);
                if (tienDo.loiChung) {
                    this.messageError(`Lô ký dừng: ${tienDo.loiChung}`);
                } else if (tienDo.hoanTat) {
                    this.messageSuccess(`Đã ký xong ${tienDo.daXong}/${tienDo.tongSo} file.`);
                }
            },
            error: () => {
                this.dangKy.set(false);
                this.messageError('Mất kết nối khi theo dõi tiến độ.');
            }
        });
    }

    onHuy() {
        const loKyId = this.lo()?.id;
        if (!loKyId) {
            return;
        }

        this.confirmAction(
            { header: 'Huỷ lô ký', message: 'Dừng lô đang ký? File đã ký xong vẫn giữ nguyên và vẫn hợp lệ.' },
            () => {
                this._loKyService.huy(loKyId).subscribe({
                    next: (res) => {
                        if (this.isResponseSucceed(res, true, 'Đã dừng lô ký.')) {
                            this.dangKy.set(false);
                            this.hoiTienDo();
                        }
                    }
                });
            }
        );
    }

    onDoiNguon(nguon: 'may' | 'kho') {
        this.nguon.set(nguon);
        this.datLaiLo();
    }

    onDayLenKho() {
        const loKyId = this.lo()?.id;
        if (!loKyId || !this.dayKhoDuoc()) {
            return;
        }

        this._loKyService.dayLenKho(loKyId).subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res)) {
                    this.hoiTienDoDayKho();
                }
            },
            error: () => this.messageError('Không mở được việc đẩy lô lên kho.')
        });
    }

    /** Hỏi tiến độ đẩy theo nhịp riêng: việc đẩy chạy nền nên đóng tab rồi mở lại vẫn hỏi tiếp được. */
    hoiTienDoDayKho() {
        const loKyId = this.lo()?.id;
        if (!loKyId) {
            return;
        }

        this._loKyService.trangThai(loKyId).subscribe({
            next: (res) => {
                if (!this.isResponseSucceed(res, false)) {
                    this.messageError('Mất dấu lô khi đang đẩy lên kho.');
                    return;
                }

                const tienDo = res.data;
                this.tienDo.set(tienDo);

                if (!tienDo.hoanTatDayLenKho) {
                    setTimeout(() => this.hoiTienDoDayKho(), NHIP_HOI_TIEN_DO);
                    return;
                }

                if (tienDo.loiDayLenKho) {
                    this.messageError(`Đẩy lên kho hỏng: ${tienDo.loiDayLenKho}`);
                } else if (tienDo.soLoiDayLenKho) {
                    this.messageWarning(
                        `Đã đẩy ${tienDo.daDayLenKho}/${tienDo.daXong} file, lỗi ${tienDo.soLoiDayLenKho} file.`
                    );
                } else {
                    this.messageSuccess(`Đã đẩy ${tienDo.daDayLenKho} file đã ký lên kho.`);
                }
            },
            error: () => this.messageError('Mất kết nối khi theo dõi tiến độ đẩy.')
        });
    }

    onTaiZip() {
        const loKyId = this.lo()?.id;
        const taiToken = this.tienDo()?.taiToken ?? this.lo()?.taiToken;
        if (!loKyId || !taiToken || !this.taiDuoc()) {
            return;
        }

        window.location.href = this._loKyService.duongDanTaiZip(loKyId, taiToken);
    }

    datLaiLo() {
        this.lo.set(null);
        this.tienDo.set(null);
        this.phanTramUpload.set(0);
    }
}
