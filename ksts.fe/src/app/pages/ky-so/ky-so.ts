import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { ProgressBarModule } from 'primeng/progressbar';
import { TagModule } from 'primeng/tag';
import { firstValueFrom } from 'rxjs';

import { IViewFileKy, IViewLoKy, IViewTienDoLoKy } from '@/app/models/lo-ky.models';
import { IViewSignCert } from '@/app/models/plugin.models';
import { IViewRowTemplate } from '@/app/models/template.models';
import { LoKyService } from '@/app/service/lo-ky.service';
import { NhacCaiPluginService } from '@/app/service/nhac-cai-plugin.service';
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
    private _nhacCaiPluginService = inject(NhacCaiPluginService);
    private _destroyRef = inject(DestroyRef);

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

    /**
     * Danh sách file của lô, đặt MỘT lần rồi không đụng tới nữa. Tình trạng từng dòng suy ra từ bộ đếm ngay
     * trong template: dựng lại cả mảng mỗi nhịp hỏi tiến độ là bắt Angular vẽ lại cả nghìn dòng mỗi vài
     * giây, và đó chính là thứ làm trình duyệt cạn tài nguyên giữa lô.
     */
    rows = signal<IViewFileKy[]>([]);

    /**
     * Thời gian ký và dấu thời gian của những file đã xong, gom dần qua từng nhịp hỏi tiến độ. Giữ riêng
     * ngoài `rows` để mỗi nhịp không phải dựng lại cả mảng nghìn phần tử — đó là thứ làm trình duyệt cạn
     * tài nguyên rồi chết giữa lô.
     */
    thoiGianTheoThuTu = signal<Map<number, IViewFileKy>>(new Map());

    /** Tra nguyên nhân theo thứ tự dòng; chỉ dòng lỗi mới có mặt ở đây. */
    lyDoTheoThuTu = computed(() => {
        const bang = new Map<number, string>();
        for (const file of this.tienDo()?.filesLoi ?? []) {
            bang.set(file.thuTu, file.lyDoLoi ?? '');
        }
        return bang;
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

    /** Kho chứa bản đã ký của lô; bản ký được đẩy lên ngay trong lúc ký. */
    tienToKho = computed(() => this.tienDo()?.tienToKho ?? '');

    override ngOnInit(): void {
        this.getTemplates();
        this.getChungThuSo();
        this.getLoDangChay();
        this._destroyRef.onDestroy(() => this.dungLoKhiRoiMan());
    }

    /**
     * Rời màn hình là mất người đưa thư nên lô không ký tiếp được: dừng hẳn thay vì để nó nằm lại trạng thái
     * đang ký. Bỏ mặc thì lần đăng nhập sau lô mồ côi đó hiện lại như đang chạy, trong khi từng lượt ký chỉ
     * đang chờ hết hạn để tính lỗi.
     */
    dungLoKhiRoiMan() {
        const loKyId = this.lo()?.id;
        if (!loKyId || !this.dangKy()) {
            return;
        }

        this.dangKy.set(false);
        this._pluginService.dongPhienKy().subscribe();
        this._loKyService.huy(loKyId).subscribe();
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

    onMoChungThuSo() {
        this._pluginService.getTrangThai().subscribe({
            next: (res) => {
                if (res?.status !== 1 || !res.data?.sanSang) {
                    this.onOpenCaiPlugin(true);
                }
            },
            error: () => {
                this.onOpenCaiPlugin(true);
            }
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
                    this.getDanhSachFile(res.data.id);
                    this.hoiTienDo();
                }
            }
        });
    }

    /** Gom file vừa ký xong vào bảng tra theo thứ tự dòng, không đụng tới danh sách file. */
    gomFileVuaXong(files: IViewFileKy[]) {
        if (!files?.length) {
            return;
        }

        this.thoiGianTheoThuTu.update((bang) => {
            const moi = new Map(bang);
            for (const file of files) {
                moi.set(file.thuTu, file);
            }
            return moi;
        });
    }

    /** Danh sách file của lô, gọi đúng một lần: nhịp hỏi tiến độ sau đó chỉ mang bộ đếm và dòng lỗi. */
    getDanhSachFile(loKyId: number) {
        this._loKyService.danhSachFile(loKyId).subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res, false)) {
                    this.rows.set(res.data);
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
        this.apDanhSachTuFileChon();

        if (them.length < pdf.length) {
            this.messageWarning(`Đã bỏ qua ${pdf.length - them.length} file trùng tên.`);
        }
    }

    onXoaDanhSach() {
        this.files.set([]);
        this.datLaiLo();
        this.apDanhSachTuFileChon();
    }

    /** Dựng tạm danh sách từ file người dùng vừa chọn, để xem trước khi lô được lập trên máy chủ. */
    apDanhSachTuFileChon() {
        this.rows.set(
            this.files().map((file, i) => ({
                id: 0,
                thuTu: i + 1,
                tenFile: file.name,
                trangThai: 'cho' as const
            }))
        );
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
            this.getDanhSachFile(lo.id);
            await this.moPhienKy(lo.id);
            await this.goiBatDau(lo.id);
            this.dangKy.set(true);
            this.hoiTienDo();
            this.vongDuaThu(lo.id);
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

    /**
     * Mở phiên ký trên token rồi nộp chứng thư phần công khai lên máy chủ. Hộp nhập PIN bật ở bước này và
     * chỉ ở bước này; máy chủ không bao giờ nhận khoá bí mật lẫn mã PIN.
     */
    async moPhienKy(loKyId: number) {
        const moPhien = await firstValueFrom(this._pluginService.moPhienKy(this.thumbprint()!));
        if (moPhien?.status !== 1 || !moPhien.data) {
            throw new Error(moPhien?.message || 'Không mở được phiên ký trên token.');
        }

        const nop = await firstValueFrom(this._loKyService.moPhien(loKyId, moPhien.data.chungThuBase64));
        if (nop?.status !== 1) {
            throw new Error(nop?.message || 'Máy chủ không nhận chứng thư số của phiên ký.');
        }
    }

    /**
     * Vòng đưa thư: lấy yêu cầu ký từ máy chủ, nhờ token ký, mang chữ ký trả về. Chạy tới khi lô kết thúc.
     *
     * Máy chủ giữ lời gọi lấy việc tới khi có việc nên vòng này không hỏi theo nhịp; đóng tab là mất người
     * đưa thư nên lô dừng, cắm lại chạy tiếp từ file dở.
     */
    async vongDuaThu(loKyId: number) {
        while (this.dangKy()) {
            try {
                const cho = await firstValueFrom(this._loKyService.choKy(loKyId));
                if (cho?.status !== 1 || !cho.data?.length) {
                    continue;
                }

                const ky = await firstValueFrom(this._pluginService.kyLo(cho.data));
                const ketQua = ky?.status === 1 && ky.data
                    ? ky.data
                    : cho.data.map((yeuCau) => ({
                        yeuCauId: yeuCau.yeuCauId,
                        loi: ky?.message || 'Không gọi được plugin để ký.'
                    }));

                await firstValueFrom(this._loKyService.nopChuKy(loKyId, ketQua));
            } catch {
                // Một vòng hỏng không phải lô hỏng: máy chủ vẫn đang chờ, thử lại vòng sau. Lô chỉ dừng khi
                // tiến độ báo kết thúc, và khi đó vòng lặp tự thoát.
                await new Promise((resolve) => setTimeout(resolve, NHIP_HOI_TIEN_DO));
            }
        }

        this._pluginService.dongPhienKy().subscribe();
        this._loKyService.dongPhien(loKyId).subscribe();
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
                this.gomFileVuaXong(tienDo.filesVuaXong);

                if (!tienDo.hoanTat && tienDo.dangChay) {
                    setTimeout(() => this.hoiTienDo(), NHIP_HOI_TIEN_DO);
                    return;
                }

                this.dangKy.set(false);

                // Nạp lại danh sách MỘT lần khi lô dừng: thời gian ký và dấu thời gian của từng file chỉ có
                // sau khi ký xong, mà nhịp hỏi tiến độ thì không mang theo danh sách nên chúng không tự về.
                this.getDanhSachFile(loKyId);

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
