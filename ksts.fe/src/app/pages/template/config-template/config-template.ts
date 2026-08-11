
import { IConfigTemplate, ITemplatePosition, IViewTemplate } from '@/app/models/template.models';
import { ChungThuSoDaChonService } from '@/app/service/chung-thu-so-da-chon.service';
import { TemplateService } from '@/app/service/template.service';
import { Breadcrumb } from '@/app/shared/components/breadcrumb/breadcrumb';
import { BaseComponent } from '@/app/shared/components/base/base-component';
import {
    ANH_ALLOWED_TYPES,
    CHU_KY_BLOCK_HEIGHT_PT,
    CHU_KY_BLOCK_WIDTH_PT,
    DO_DAM_HE_SO_GIAM_SANG,
    DO_DAM_MAC_DINH,
    DO_DAM_MAX,
    DO_DAM_MIN,
    DO_DAM_SANG_TOI_THIEU,
    DO_DAY_NET_BAN_KINH_TI_LE,
    DO_DAY_NET_MAC_DINH,
    DO_DAY_NET_MAU_MUC,
    DO_DAY_NET_MAX,
    DO_DAY_NET_MIN,
    ETemplatePositionKind,
    KHOI_MIN_RATIO,
    PDF_PREVIEW_SCALE,
    TEMPLATE_POSITION_LABELS
} from '@/app/shared/constants/template.constants';
import { SharedImports } from '@/app/shared/import.shared';
import { Component, computed, ElementRef, inject, signal, ViewChild } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { SliderModule } from 'primeng/slider';
import * as pdfjs from 'pdfjs-dist';
import type { PDFDocumentLoadingTask } from 'pdfjs-dist';

// pdf.js giải mã PDF trong worker riêng; không trỏ workerSrc thì thư viện tự chặn với lỗi
// "No GlobalWorkerOptions.workerSrc specified". File worker được angular.json copy ra gốc thư mục build.
pdfjs.GlobalWorkerOptions.workerSrc = new URL('pdf.worker.min.mjs', document.baseURI).toString();

interface IKhoi extends ITemplatePosition {
    label: string;
    anhUrl: string | null;
    choPhepResize: boolean;
}

@Component({
    selector: 'app-config-template',
    imports: [SharedImports, Breadcrumb, SliderModule],
    templateUrl: './config-template.html',
    styleUrl: './config-template.scss'
})
export class ConfigTemplate extends BaseComponent {
    private _templateService = inject(TemplateService);

    @ViewChild('pdfCanvas') pdfCanvas!: ElementRef<HTMLCanvasElement>;
    @ViewChild('pdfLop') pdfLop!: ElementRef<HTMLDivElement>;

    breadcrumbHome: MenuItem = { icon: 'pi pi-home', routerLink: '/' };
    breadcrumbItems: MenuItem[] = [{ label: 'Template', routerLink: '/template' }, { label: 'Cấu hình template' }];

    id = Number(this._activatedRoute.snapshot.paramMap.get('id'));

    /** Chứng thư đã chọn ở màn Chứng thư số, giữ trong RAM của phiên - hết phiên là phải chọn lại. */
    chungThuSo = inject(ChungThuSoDaChonService).chungThuSo;

    kind = ETemplatePositionKind;
    anhAllowedTypes = ANH_ALLOWED_TYPES;

    template = signal<IViewTemplate>({});
    khungCao = signal<number>(0);
    khungRong = signal<number>(0);
    khois = signal<IKhoi[]>([]);
    hienThiChuKySo = signal<boolean>(true);
    nhoiChuKySoVaoAnh = signal<boolean>(false);
    doDamDauDo = signal<number>(DO_DAM_MAC_DINH);
    doDamChuKyTuoi = signal<number>(DO_DAM_MAC_DINH);
    doDayNetChuKyTuoi = signal<number>(DO_DAY_NET_MAC_DINH);

    readonly doDamMin = DO_DAM_MIN;
    readonly doDamMax = DO_DAM_MAX;
    readonly doDayNetMin = DO_DAY_NET_MIN;
    readonly doDayNetMax = DO_DAY_NET_MAX;

    /**
     * Khối kèm sẵn bộ lọc xem trước. Là computed chứ không phải hàm gọi trong template để bản xem trước bám
     * đúng thanh trượt: phép lọc dùng đúng công thức BE áp lúc ký (tăng tương phản quanh mức xám giữa) nên
     * kéo tới đâu là thấy gần đúng bản in ra tới đó.
     */
    khoisXemTruoc = computed(() =>
        this.khois().map((khoi) => ({ ...khoi, boLoc: this.boLocCuaKhoi(khoi) }))
    );

    anhDauDo: File | null = null;
    anhChuKyTuoi: File | null = null;
    anhDauDoUrl = signal<string | null>(null);
    anhChuKyTuoiUrl = signal<string | null>(null);
    xoaAnhDauDo = false;
    xoaAnhChuKyTuoi = false;

    pdfTask: PDFDocumentLoadingTask | null = null;
    keoKindMoi: ETemplatePositionKind | null = null;
    keoKhoiIndex: number | null = null;
    keoLechX = 0;
    keoLechY = 0;
    resizeKhoiIndex: number | null = null;

    override ngOnInit(): void {
        this.getTemplate();
        this.veFileMau();
    }

    getTemplate() {
        this._templateService.getById(this.id).subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res)) {
                    this.template.set(res.data);
                    this.hienThiChuKySo.set(res.data.hienThiChuKySo ?? true);
                    this.nhoiChuKySoVaoAnh.set(res.data.nhoiChuKySoVaoAnh ?? false);
                    this.doDamDauDo.set(res.data.doDamDauDo ?? DO_DAM_MAC_DINH);
                    this.doDamChuKyTuoi.set(res.data.doDamChuKyTuoi ?? DO_DAM_MAC_DINH);
                    this.doDayNetChuKyTuoi.set(res.data.doDayNetChuKyTuoi ?? DO_DAY_NET_MAC_DINH);
                    this.anhDauDoUrl.set(res.data.anhDauDoUrl ?? null);
                    this.anhChuKyTuoiUrl.set(res.data.anhChuKyTuoiUrl ?? null);
                    this.khois.set((res.data.positions ?? []).map((position) => this.taoKhoi(position)));
                }
            }
        });
    }

    /**
     * Vẽ trang đầu file PDF mẫu lên canvas. Chỉ trang 1: mọi khối đều đặt trên trang này, tải cả tài liệu
     * chỉ để xem một trang là phí bộ nhớ.
     */
    veFileMau() {
        this.loading.set(true);
        this._templateService
            .getFileMauNoiDung()
            .subscribe({
                next: async (blob) => {
                    this.pdfTask?.destroy();
                    this.pdfTask = pdfjs.getDocument({ data: await blob.arrayBuffer() });
                    const document = await this.pdfTask.promise;
                    const page = await document.getPage(1);
                    // Phóng theo bề rộng cột đang có: preview luôn lấp đầy cột phải, cân với cột cấu hình
                    // bên trái ở mọi cỡ màn hình, thay vì cố định một mức phóng lúc to lúc bé.
                    const beRongCot = this.pdfLop.nativeElement.parentElement?.clientWidth ?? 0;
                    const khoGoc = page.getViewport({ scale: 1 });
                    const scale = beRongCot > 0 ? beRongCot / khoGoc.width : PDF_PREVIEW_SCALE;
                    const viewport = page.getViewport({ scale });
                    const canvas = this.pdfCanvas.nativeElement;
                    canvas.width = viewport.width;
                    canvas.height = viewport.height;
                    await page.render({
                        canvas,
                        canvasContext: canvas.getContext('2d')!,
                        viewport
                    }).promise;
                    this.khungCao.set(canvas.height);
                    this.khungRong.set(canvas.width);
                },
                error: () => {
                    this.messageError('Không đọc được file PDF mẫu.');
                }
            })
            .add(() => {
                this.loading.set(false);
            });
    }

    onBatDauKeoKhoiMoi(kind: ETemplatePositionKind, event: DragEvent) {
        // Firefox chỉ phát sự kiện drop khi dataTransfer có dữ liệu.
        event.dataTransfer?.setData('text/plain', `${kind}`);
        this.keoKindMoi = kind;
    }

    onThaKhoiMoi(event: DragEvent) {
        event.preventDefault();
        if (this.keoKindMoi === null) {
            return;
        }

        const box = this.pdfLop.nativeElement.getBoundingClientRect();
        const widthRatio = CHU_KY_BLOCK_WIDTH_PT / 595;
        const heightRatio = CHU_KY_BLOCK_HEIGHT_PT / 842;
        const position: ITemplatePosition = {
            kind: this.keoKindMoi,
            pageNumber: 1,
            xRatio: this.kepTrongTrang((event.clientX - box.left) / box.width - widthRatio / 2, widthRatio),
            yRatio: this.kepTrongTrang((event.clientY - box.top) / box.height - heightRatio / 2, heightRatio),
            widthRatio,
            heightRatio
        };

        this.khois.update((khois) => [...khois.filter((khoi) => khoi.kind !== position.kind), this.taoKhoi(position)]);
        this.keoKindMoi = null;
    }

    onXoaKhoi(index: number) {
        this.khois.update((khois) => khois.filter((_, i) => i !== index));
    }

    taoKhoi(position: ITemplatePosition): IKhoi {
        return {
            ...position,
            label: TEMPLATE_POSITION_LABELS[position.kind as ETemplatePositionKind],
            // Con dấu phải giữ đúng kích thước gốc của ảnh nên chỉ cho kéo di chuyển, không cho kéo giãn.
            choPhepResize: position.kind !== ETemplatePositionKind.DauDo,
            anhUrl: position.kind === ETemplatePositionKind.DauDo
                ? this.anhDauDoUrl()
                : position.kind === ETemplatePositionKind.ChuKyTuoi
                    ? this.anhChuKyTuoiUrl()
                    : null
        };
    }

    kepTrongTrang(giaTri: number, kichThuoc: number): number {
        return Math.min(Math.max(giaTri, 0), 1 - kichThuoc);
    }

    coChuKhoi(khoi: IKhoi): number {
        return (khoi.heightRatio * this.khungCao()) / 3;
    }

    /** Độ đậm áp cho khối nào thì lấy theo đúng loại ảnh của khối đó; khối chữ ký số không có ảnh nên giữ nguyên. */
    boLocCuaKhoi(khoi: IKhoi): string {
        const phanTram =
            khoi.kind === ETemplatePositionKind.DauDo
                ? this.doDamDauDo()
                : khoi.kind === ETemplatePositionKind.ChuKyTuoi
                  ? this.doDamChuKyTuoi()
                  : 100;

        // Hai vế đúng như BE ghi vào /Decode: tăng tương phản rồi hạ độ sáng. Chỉ tăng tương phản thì nét
        // tách khỏi nền rõ hơn nhưng không sâu thêm.
        const heSo = phanTram / 100;
        const doSang = Math.max(DO_DAM_SANG_TOI_THIEU, 1 - (heSo - 1) * DO_DAM_HE_SO_GIAM_SANG);
        const doDam = `contrast(${heSo}) brightness(${doSang})`;

        return khoi.kind === ETemplatePositionKind.ChuKyTuoi ? `${doDam} ${this.nongNetCuaKhoi(khoi)}` : doDam;
    }

    /**
     * Vế nong nét: hai lớp đổ bóng bán kính sát 0 cùng màu mực chồng lên nhau, đúng cách bản dựng thiết kế
     * gốc làm cho nét chữ ký quét dày lên mà không nhoè. Bán kính suy từ bề rộng khối đang hiển thị nên kéo
     * khối to nhỏ thì nét vẫn dày tương đương, khớp với cách BE tính lúc ký.
     */
    nongNetCuaKhoi(khoi: IKhoi): string {
        const beRong = khoi.widthRatio * this.khungRong();
        const banKinh = (beRong * DO_DAY_NET_BAN_KINH_TI_LE * this.doDayNetChuKyTuoi()) / 100;
        if (banKinh <= 0) {
            return '';
        }

        const lop = `drop-shadow(0 0 ${banKinh}px ${DO_DAY_NET_MAU_MUC})`;
        return `${lop} ${lop}`;
    }


    onBatDauDiChuyen(event: MouseEvent, index: number) {
        event.preventDefault();
        const khoi = this.khois()[index];
        const box = this.pdfLop.nativeElement.getBoundingClientRect();
        this.keoKhoiIndex = index;
        this.keoLechX = event.clientX - (box.left + khoi.xRatio * box.width);
        this.keoLechY = event.clientY - (box.top + khoi.yRatio * box.height);
    }

    onBatDauResize(event: MouseEvent, index: number) {
        event.preventDefault();
        event.stopPropagation();
        this.resizeKhoiIndex = index;
    }

    /**
     * Toạ độ lưu theo TỈ LỆ khổ trang chứ không theo pixel: người dùng đặt một lần trên file mẫu rồi áp cho
     * hồ sơ thật có khổ giấy khác, và mức phóng của trình duyệt không được ảnh hưởng kết quả.
     */
    onKeo(event: MouseEvent) {
        if (this.keoKhoiIndex === null && this.resizeKhoiIndex === null) {
            return;
        }

        const box = this.pdfLop.nativeElement.getBoundingClientRect();
        this.khois.update((khois) =>
            khois.map((khoi, i) => {
                if (i === this.keoKhoiIndex) {
                    return {
                        ...khoi,
                        xRatio: this.kepTrongTrang((event.clientX - this.keoLechX - box.left) / box.width, khoi.widthRatio),
                        yRatio: this.kepTrongTrang((event.clientY - this.keoLechY - box.top) / box.height, khoi.heightRatio)
                    };
                }

                if (i !== this.resizeKhoiIndex) {
                    return khoi;
                }

                // Kéo góc chỉ đổi MỘT hệ số, không kéo méo: cỡ chữ trong khối suy từ chiều cao, khối méo là
                // chữ tràn ra ngoài hoặc lọt thỏm giữa khung.
                const rongMoi = (event.clientX - box.left) / box.width - khoi.xRatio;
                const heSo = Math.max(rongMoi / khoi.widthRatio, KHOI_MIN_RATIO / khoi.widthRatio);
                const widthRatio = Math.min(khoi.widthRatio * heSo, 1 - khoi.xRatio);
                const heightRatio = Math.min(khoi.heightRatio * heSo, 1 - khoi.yRatio);

                return { ...khoi, widthRatio, heightRatio };
            })
        );
    }

    onKetThucKeo() {
        this.keoKhoiIndex = null;
        this.resizeKhoiIndex = null;
    }

    onChonAnhDauDo(event: Event) {
        const file = (event.target as HTMLInputElement).files?.[0] ?? null;
        if (!file) {
            return;
        }

        this.anhDauDo = file;
        this.xoaAnhDauDo = false;
        this.anhDauDoUrl.set(URL.createObjectURL(file));
        this.dongBoAnhVaoKhoi();
    }

    onChonAnhChuKyTuoi(event: Event) {
        const file = (event.target as HTMLInputElement).files?.[0] ?? null;
        if (!file) {
            return;
        }

        this.anhChuKyTuoi = file;
        this.xoaAnhChuKyTuoi = false;
        this.anhChuKyTuoiUrl.set(URL.createObjectURL(file));
        this.dongBoAnhVaoKhoi();
    }

    onXoaAnhDauDo() {
        this.anhDauDo = null;
        this.anhDauDoUrl.set(null);
        this.xoaAnhDauDo = true;
        this.dongBoAnhVaoKhoi();
    }

    onXoaAnhChuKyTuoi() {
        this.anhChuKyTuoi = null;
        this.anhChuKyTuoiUrl.set(null);
        this.xoaAnhChuKyTuoi = true;
        this.dongBoAnhVaoKhoi();
    }

    dongBoAnhVaoKhoi() {
        this.khois.update((khois) => khois.map((khoi) => this.taoKhoi(khoi)));
    }

    /** Mang theo id template để chọn xong chứng thư là quay lại đúng template đang cấu hình dở. */
    onChonChungThuSo() {
        this.router.navigate(['/chung-thu-so'], { queryParams: { templateId: this.id } });
    }

    onHuy() {
        this.router.navigate(['/template']);
    }

    /** PUT ghi đè toàn bộ cấu hình nên luôn gửi trạng thái đầy đủ đang hiển thị, kể cả danh sách khối. */
    onLuu() {
        const thumbprint = this.chungThuSo()?.thumbprint ?? this.template().thumbprint ?? '';
        if (!thumbprint) {
            this.messageWarning('Chưa chọn chứng thư số. Vào màn Chứng thư số để chọn trước khi lưu.');
            return;
        }

        const body: IConfigTemplate = {
            id: this.id,
            thumbprint,
            tenChungThu: this.chungThuSo()?.commonName ?? this.template().tenChungThu,
            lyDoKy: this.template().lyDoKy,
            noiKy: this.template().noiKy,
            hienThiChuKySo: this.hienThiChuKySo(),
            nhoiChuKySoVaoAnh: this.nhoiChuKySoVaoAnh(),
            doDamDauDo: this.doDamDauDo(),
            doDamChuKyTuoi: this.doDamChuKyTuoi(),
            doDayNetChuKyTuoi: this.doDayNetChuKyTuoi(),
            anhDauDo: this.anhDauDo,
            anhChuKyTuoi: this.anhChuKyTuoi,
            xoaAnhDauDo: this.xoaAnhDauDo,
            xoaAnhChuKyTuoi: this.xoaAnhChuKyTuoi,
            positions: this.khois().map((khoi) => ({
                kind: khoi.kind,
                pageNumber: khoi.pageNumber,
                xRatio: khoi.xRatio,
                yRatio: khoi.yRatio,
                widthRatio: khoi.widthRatio,
                heightRatio: khoi.heightRatio
            }))
        };

        this.loading.set(true);
        this._templateService
            .updateConfig(body)
            .subscribe({
                next: (res) => {
                    if (this.isResponseSucceed(res, true, 'Đã lưu cấu hình template')) {
                        this.router.navigate(['/template']);
                    }
                },
                error: (err) => {
                    this.messageError(err?.message);
                }
            })
            .add(() => {
                this.loading.set(false);
            });
    }

    ngOnDestroy(): void {
        this.pdfTask?.destroy();
    }
}
