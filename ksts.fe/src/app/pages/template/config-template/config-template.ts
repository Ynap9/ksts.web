
import { IConfigTemplate, ITemplatePosition, IViewTemplate } from '@/app/models/template.models';
import { ChungThuSoDaChonService } from '@/app/service/chung-thu-so-da-chon.service';
import { InkColorService } from '@/app/service/ink-color.service';
import { TemplateService } from '@/app/service/template.service';
import { Breadcrumb } from '@/app/shared/components/breadcrumb/breadcrumb';
import { BaseComponent } from '@/app/shared/components/base/base-component';
import {
    ANH_ALLOWED_TYPES,
    CHU_KY_BLOCK_HEIGHT_PT,
    CHU_KY_BLOCK_WIDTH_PT,
    CUON_TOC_DO_TOI_DA_PX,
    CUON_VUNG_MEP_PX,
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
    MAU_CHU_KY_TUOI_FILTER_ID,
    MAU_MAC_DINH,
    PDF_PHAN_MO_RONG,
    PDF_PREVIEW_SCALE,
    TEMPLATE_POSITION_LABELS
} from '@/app/shared/constants/template.constants';
import { SharedImports } from '@/app/shared/import.shared';
import { AfterViewInit, Component, computed, ElementRef, inject, QueryList, signal, ViewChild, ViewChildren } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { ColorPickerModule } from 'primeng/colorpicker';
import { SliderModule } from 'primeng/slider';
import * as pdfjs from 'pdfjs-dist';
import type { PDFDocumentLoadingTask, PDFDocumentProxy } from 'pdfjs-dist';

// pdf.js giải mã PDF trong worker riêng; không trỏ workerSrc thì thư viện tự chặn với lỗi
// "No GlobalWorkerOptions.workerSrc specified". File worker do scripts/copy-pdf-worker.mjs chép sang public/
// dưới đuôi .js - KHÔNG dùng .mjs của pdfjs-dist, xem bẫy MIME ở fe/architecture/04-man-hinh-dac-thu.md.
pdfjs.GlobalWorkerOptions.workerSrc = new URL('pdf.worker.min.js', document.baseURI).toString();

interface IKhoi extends ITemplatePosition {
    label: string;
    anhUrl: string | null;
    choPhepResize: boolean;
}

interface ITrangXemTruoc {
    khoa: string;
    soTrang: number;
}

@Component({
    selector: 'app-config-template',
    imports: [SharedImports, Breadcrumb, SliderModule, ColorPickerModule],
    templateUrl: './config-template.html',
    styleUrl: './config-template.scss'
})
export class ConfigTemplate extends BaseComponent implements AfterViewInit {
    private _templateService = inject(TemplateService);
    private _inkColorService = inject(InkColorService);

    @ViewChild('khungCuon') khungCuon!: ElementRef<HTMLDivElement>;
    @ViewChild('pdfKhung') pdfKhung!: ElementRef<HTMLDivElement>;
    @ViewChildren('pdfCanvas') pdfCanvases!: QueryList<ElementRef<HTMLCanvasElement>>;

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
    kyDe = signal<boolean>(false);

    /**
     * File đang xem trước đã có chữ ký số hay chưa. Chỉ mở ô "ký đè" khi đúng là có: bật nó cho một file
     * chưa ký thì người dùng không có cách nào thấy được cờ đó thực sự làm gì.
     */
    fileCoChuKy = signal<boolean>(false);
    doDamDauDo = signal<number>(DO_DAM_MAC_DINH);
    doDamChuKyTuoi = signal<number>(DO_DAM_MAC_DINH);
    doDayNetChuKyTuoi = signal<number>(DO_DAY_NET_MAC_DINH);
    mauChuKySo = signal<string>(MAU_MAC_DINH);

    /** Để trống nghĩa là chưa chọn màu: ảnh chữ ký tươi giữ nguyên mực gốc, kể cả khi mực đó không phải đen. */
    mauChuKyTuoi = signal<string | null>(null);

    /** Màu mực đọc được từ chính ảnh chữ ký tươi, chỉ để hiển thị - không gửi lên BE nếu người dùng không chọn. */
    originalInkColor = signal<string | null>(null);

    readonly mauFilterId = MAU_CHU_KY_TUOI_FILTER_ID;

    /**
     * Màu bảng màu đang chỉ tới: màu đã chọn, không có thì màu mực thật của ảnh. Hiện đen mặc định là nói dối
     * về một chữ ký mực xanh, và người dùng chọn đen sẽ tưởng mình vừa đổi màu trong khi không đổi gì.
     */
    pickerInkColor = computed(() => this.mauChuKyTuoi() ?? this.originalInkColor() ?? MAU_MAC_DINH);

    /** Chưa chọn màu thì không dựng bộ lọc nhuộm; chọn rồi thì nhuộm đúng màu đó, đen cũng là một màu. */
    coNhuomMau = computed(() => this.mauChuKyTuoi() !== null);

    /**
     * Hệ số của phép nhuộm cho từng kênh màu, khớp đúng phép BE ghi vào /Decode: điểm tối kéo về màu đã chọn,
     * điểm sáng vẫn là trắng nên nền của ảnh quét không bị nhuộm theo. out = c + (1 - c) * in.
     */
    heSoNhuom = computed(() =>
        this.kenhMau(this.mauChuKyTuoi() ?? MAU_MAC_DINH).map((c) => ({ doDoc: 1 - c, chan: c }))
    );

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

    filesXemTruoc = signal<File[]>([]);
    fileIndex = signal<number>(0);
    duongDan = signal<string>('');
    trangs = signal<ITrangXemTruoc[]>([]);

    coFileTruoc = computed(() => this.fileIndex() > 0);
    coFileSau = computed(() => this.fileIndex() < this.filesXemTruoc().length - 1);

    pdfTask: PDFDocumentLoadingTask | null = null;
    pdfDoc: PDFDocumentProxy | null = null;
    lanMo = 0;
    keoKindMoi: ETemplatePositionKind | null = null;
    keoKhoiIndex: number | null = null;
    keoLechX = 0;
    keoLechY = 0;
    resizeKhoiIndex: number | null = null;

    /**
     * Trang đang nhận khối kéo, ghi lại từ lúc bấm chuột. Con trỏ có thể đi ra khỏi trang giữa chừng - lúc
     * đó vẫn phải quy toạ độ về đúng trang đã bắt đầu kéo chứ không phải trang con trỏ đang lướt qua.
     */
    keoLop: HTMLElement | null = null;

    conTroX = 0;
    conTroY = 0;
    maCuon: number | null = null;

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
                    this.kyDe.set(res.data.kyDe ?? false);
                    this.doDamDauDo.set(res.data.doDamDauDo ?? DO_DAM_MAC_DINH);
                    this.doDamChuKyTuoi.set(res.data.doDamChuKyTuoi ?? DO_DAM_MAC_DINH);
                    this.doDayNetChuKyTuoi.set(res.data.doDayNetChuKyTuoi ?? DO_DAY_NET_MAC_DINH);
                    this.mauChuKySo.set(res.data.mauChuKySo || MAU_MAC_DINH);
                    this.mauChuKyTuoi.set(res.data.mauChuKyTuoi || null);
                    this.anhDauDoUrl.set(res.data.anhDauDoUrl ?? null);
                    this.anhChuKyTuoiUrl.set(res.data.anhChuKyTuoiUrl ?? null);
                    this.readOriginalInkColor();
                    this.khois.set((res.data.positions ?? []).map((position) => this.taoKhoi(position)));
                }
            }
        });
    }

    ngAfterViewInit(): void {
        this.pdfCanvases.changes.subscribe(() => this.veCacTrang());
        this.veCacTrang();
    }

    veFileMau() {
        this.loading.set(true);
        this._templateService
            .getFileMauNoiDung()
            .subscribe({
                next: async (blob) => {
                    this.duongDan.set('');
                    await this.moTaiLieu(await blob.arrayBuffer());
                },
                error: () => {
                    this.messageError('Không đọc được file PDF mẫu.');
                }
            })
            .add(() => {
                this.loading.set(false);
            });
    }

    onChonFilePdf(event: Event) {
        const file = (event.target as HTMLInputElement).files?.[0] ?? null;
        this.datDanhSachFile(file ? [file] : []);
    }

    onChonThuMucPdf(event: Event) {
        const files = Array.from((event.target as HTMLInputElement).files ?? [])
            .filter((file) => file.name.toLowerCase().endsWith(PDF_PHAN_MO_RONG))
            .sort((a, b) => a.name.localeCompare(b.name));

        this.datDanhSachFile(files);
    }

    datDanhSachFile(files: File[]) {
        if (files.length === 0) {
            this.messageWarning('Không có file PDF nào trong lựa chọn.');
            return;
        }

        this.filesXemTruoc.set(files);
        this.fileIndex.set(0);
        this.moFileDangChon();
    }

    onFileTruoc() {
        if (!this.coFileTruoc()) {
            return;
        }

        this.fileIndex.update((index) => index - 1);
        this.moFileDangChon();
    }

    onFileSau() {
        if (!this.coFileSau()) {
            return;
        }

        this.fileIndex.update((index) => index + 1);
        this.moFileDangChon();
    }

    async moFileDangChon() {
        const file = this.filesXemTruoc()[this.fileIndex()];
        if (!file) {
            return;
        }

        this.loading.set(true);
        this.duongDan.set(file.webkitRelativePath || file.name);

        try {
            await this.moTaiLieu(await file.arrayBuffer());
        } catch {
            this.messageError('Không đọc được file PDF đã chọn.');
        } finally {
            this.loading.set(false);
        }
    }

    /**
     * Khoá của trang mang theo số lần mở tài liệu nên đổi file là toàn bộ canvas được dựng lại, kể cả khi
     * file mới trùng số trang với file cũ — nhờ đó vòng vẽ luôn chạy trên canvas mới thay vì giữ ảnh trang cũ.
     */
    async moTaiLieu(data: ArrayBuffer) {
        this.pdfTask?.destroy();
        this.lanMo++;
        this.pdfTask = pdfjs.getDocument({ data });
        this.pdfDoc = await this.pdfTask.promise;

        // getSignatures trả null khi tài liệu chưa có chữ ký nào; ô chữ ký để trống không tính là đã ký.
        const chuKy = await this.pdfDoc.getSignatures();
        this.fileCoChuKy.set((chuKy?.length ?? 0) > 0);

        this.trangs.set(
            Array.from({ length: this.pdfDoc.numPages }, (_, i) => ({
                khoa: `${this.lanMo}-${i + 1}`,
                soTrang: i + 1
            }))
        );
    }

    /**
     * Vẽ mọi trang của tài liệu đang xem. Phóng theo bề rộng khung preview để trang lấp đầy cột phải ở mọi
     * cỡ màn hình; đổi file giữa chừng thì bỏ dở vòng vẽ, tránh trang của file cũ đè lên file mới.
     */
    async veCacTrang() {
        const canvases = this.pdfCanvases?.toArray() ?? [];
        const tailieu = this.pdfDoc;
        if (!tailieu || canvases.length === 0) {
            return;
        }

        const lan = this.lanMo;
        const beRongKhung = this.pdfKhung?.nativeElement.clientWidth ?? 0;

        try {
            for (let i = 0; i < canvases.length; i++) {
                if (lan !== this.lanMo) {
                    return;
                }

                const page = await tailieu.getPage(i + 1);
                const khoGoc = page.getViewport({ scale: 1 });
                const scale = beRongKhung > 0 ? beRongKhung / khoGoc.width : PDF_PREVIEW_SCALE;
                const viewport = page.getViewport({ scale });
                const canvas = canvases[i].nativeElement;
                canvas.width = viewport.width;
                canvas.height = viewport.height;
                await page.render({
                    canvas,
                    canvasContext: canvas.getContext('2d')!,
                    viewport
                }).promise;

                if (i === 0) {
                    this.khungCao.set(canvas.height);
                    this.khungRong.set(canvas.width);
                }
            }
        } catch {
            if (lan === this.lanMo) {
                this.messageError('Không vẽ được nội dung file PDF.');
            }
        }
    }

    onBatDauKeoKhoiMoi(kind: ETemplatePositionKind, event: DragEvent) {
        // Firefox chỉ phát sự kiện drop khi dataTransfer có dữ liệu.
        event.dataTransfer?.setData('text/plain', `${kind}`);
        this.keoKindMoi = kind;
    }

    onThaKhoiMoi(event: DragEvent, soTrang: number, lop: HTMLElement) {
        event.preventDefault();
        if (this.keoKindMoi === null) {
            return;
        }

        const box = lop.getBoundingClientRect();
        const widthRatio = CHU_KY_BLOCK_WIDTH_PT / 595;
        const heightRatio = CHU_KY_BLOCK_HEIGHT_PT / 842;
        const position: ITemplatePosition = {
            kind: this.keoKindMoi,
            pageNumber: soTrang,
            xRatio: this.kepTrongTrang((event.clientX - box.left) / box.width - widthRatio / 2, widthRatio),
            yRatio: this.kepTrongTrang((event.clientY - box.top) / box.height - heightRatio / 2, heightRatio),
            widthRatio,
            heightRatio
        };

        this.khois.update((khois) => [...khois.filter((khoi) => khoi.kind !== position.kind), this.taoKhoi(position)]);
        this.keoKindMoi = null;
        this.dungTuCuon();
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

        if (khoi.kind !== ETemplatePositionKind.ChuKyTuoi) {
            return doDam;
        }

        // Thứ tự đúng bằng thứ tự BE áp: đậm nhạt trước, nhuộm màu sau, nong nét sau cùng. Đảo lại là quầng
        // nong nét bị nhuộm thêm một lần nữa và ra sẫm hơn bản ký.
        const nhuom = this.coNhuomMau() ? ` url(#${this.mauFilterId})` : '';
        return `${doDam}${nhuom} ${this.nongNetCuaKhoi(khoi)}`;
    }

    /** Ba kênh màu 0..1 của một mã hex; mã lạ trả về đen, đúng cách BE lùi về khi gặp giá trị không đọc được. */
    kenhMau(hex: string): number[] {
        const so = Number.parseInt((hex || '').replace('#', ''), 16);
        if (Number.isNaN(so)) {
            return [0, 0, 0];
        }

        return [(so >> 16) & 255, (so >> 8) & 255, so & 255].map((kenh) => kenh / 255);
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

        // Quầng lấy đúng màu mực đã chọn; chưa chọn thì lấy màu mực thật của ảnh, đọc không được mới lùi về
        // màu bản dựng gốc đã cân cho ảnh chữ ký quét.
        const mauQuang = this.mauChuKyTuoi() ?? this.originalInkColor() ?? DO_DAY_NET_MAU_MUC;
        const lop = `drop-shadow(0 0 ${banKinh}px ${mauQuang})`;
        return `${lop} ${lop}`;
    }


    onBatDauDiChuyen(event: MouseEvent, index: number, lop: HTMLElement) {
        event.preventDefault();
        const khoi = this.khois()[index];
        const box = lop.getBoundingClientRect();
        this.keoKhoiIndex = index;
        this.keoLop = lop;
        this.keoLechX = event.clientX - (box.left + khoi.xRatio * box.width);
        this.keoLechY = event.clientY - (box.top + khoi.yRatio * box.height);
        this.ghiNhoConTro(event);
        this.batDauTuCuon();
    }

    onBatDauResize(event: MouseEvent, index: number, lop: HTMLElement) {
        event.preventDefault();
        event.stopPropagation();
        this.resizeKhoiIndex = index;
        this.keoLop = lop;
        this.ghiNhoConTro(event);
        this.batDauTuCuon();
    }

    /** Có đang kéo thứ gì không - khối đã đặt, góc đổi cỡ, hay mẫu khối mới từ cột trái. */
    dangKeo(): boolean {
        return this.keoKhoiIndex !== null || this.resizeKhoiIndex !== null || this.keoKindMoi !== null;
    }

    ghiNhoConTro(event: MouseEvent | DragEvent) {
        this.conTroX = event.clientX;
        this.conTroY = event.clientY;
    }

    /** Chuột chạy trong khung xem trước: theo dõi ở KHUNG chứ không ở từng trang, để kéo qua mép không đứt. */
    onKeoTrongKhung(event: MouseEvent) {
        if (!this.dangKeo()) {
            return;
        }

        this.ghiNhoConTro(event);
        this.capNhatKhoiTheoConTro();
    }

    /** Kéo mẫu khối mới ngang qua khung: chỉ cần vị trí con trỏ để vòng tự cuộn biết đường chạy. */
    onKeoQua(event: DragEvent) {
        event.preventDefault();
        if (this.keoKindMoi === null) {
            return;
        }

        this.ghiNhoConTro(event);
        this.batDauTuCuon();
    }

    /**
     * Vòng tự cuộn: con trỏ vào sát mép trên/dưới khung thì trang trượt theo, kể cả khi con trỏ đứng yên -
     * đó là lý do phải chạy theo khung hình chứ không bám vào sự kiện chuột.
     */
    batDauTuCuon() {
        if (this.maCuon === null) {
            this.maCuon = requestAnimationFrame(() => this.nhipTuCuon());
        }
    }

    dungTuCuon() {
        if (this.maCuon !== null) {
            cancelAnimationFrame(this.maCuon);
            this.maCuon = null;
        }
    }

    nhipTuCuon() {
        this.maCuon = null;

        const khung = this.khungCuon?.nativeElement;
        if (!khung || !this.dangKeo()) {
            return;
        }

        const buoc = this.buocCuon(khung.getBoundingClientRect());
        if (buoc !== 0) {
            khung.scrollTop += buoc;

            // Trang trượt dưới con trỏ đang đứng yên, nên khối phải tính lại theo mốc mới của trang.
            this.capNhatKhoiTheoConTro();
        }

        this.maCuon = requestAnimationFrame(() => this.nhipTuCuon());
    }

    /** Số điểm cần cuộn cho một khung hình; âm là lên, dương là xuống, 0 là con trỏ còn ở giữa khung. */
    buocCuon(box: DOMRect): number {
        const cachTren = this.conTroY - box.top;
        const cachDuoi = box.bottom - this.conTroY;

        if (cachTren < CUON_VUNG_MEP_PX) {
            return -this.tocDoCuon(cachTren);
        }
        if (cachDuoi < CUON_VUNG_MEP_PX) {
            return this.tocDoCuon(cachDuoi);
        }
        return 0;
    }

    /** Càng sát mép càng cuộn nhanh; ra hẳn ngoài khung thì giữ ở tốc độ tối đa chứ không tăng vô hạn. */
    tocDoCuon(khoangCach: number): number {
        const ti = Math.min(1, Math.max(0, (CUON_VUNG_MEP_PX - khoangCach) / CUON_VUNG_MEP_PX));
        return Math.ceil(ti * CUON_TOC_DO_TOI_DA_PX);
    }

    /**
     * Toạ độ lưu theo TỈ LỆ khổ trang chứ không theo pixel: người dùng đặt một lần trên file mẫu rồi áp cho
     * hồ sơ thật có khổ giấy khác, và mức phóng của trình duyệt không được ảnh hưởng kết quả.
     */
    capNhatKhoiTheoConTro() {
        const lop = this.keoLop;
        if (!lop || (this.keoKhoiIndex === null && this.resizeKhoiIndex === null)) {
            return;
        }

        const box = lop.getBoundingClientRect();
        this.khois.update((khois) =>
            khois.map((khoi, i) => {
                if (i === this.keoKhoiIndex) {
                    return {
                        ...khoi,
                        xRatio: this.kepTrongTrang((this.conTroX - this.keoLechX - box.left) / box.width, khoi.widthRatio),
                        yRatio: this.kepTrongTrang((this.conTroY - this.keoLechY - box.top) / box.height, khoi.heightRatio)
                    };
                }

                if (i !== this.resizeKhoiIndex) {
                    return khoi;
                }

                // Kéo góc chỉ đổi MỘT hệ số, không kéo méo: cỡ chữ trong khối suy từ chiều cao, khối méo là
                // chữ tràn ra ngoài hoặc lọt thỏm giữa khung.
                const rongMoi = (this.conTroX - box.left) / box.width - khoi.xRatio;
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
        this.keoLop = null;
        this.dungTuCuon();
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
        this.readOriginalInkColor();
    }

    /**
     * Đọc màu mực thật của ảnh chữ ký tươi để bảng màu bắt đầu từ đúng màu đang nhìn thấy trên giấy. Đọc
     * không được - kho object trả ảnh mà thiếu tiêu đề CORS nên canvas bị nhiễm - thì bảng màu lùi về đen.
     */
    async readOriginalInkColor() {
        const url = this.anhChuKyTuoiUrl();
        this.originalInkColor.set(url ? await this._inkColorService.extract(url) : null);
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
        this.originalInkColor.set(null);
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
            kyDe: this.kyDe(),
            doDamDauDo: this.doDamDauDo(),
            doDamChuKyTuoi: this.doDamChuKyTuoi(),
            doDayNetChuKyTuoi: this.doDayNetChuKyTuoi(),
            mauChuKySo: this.mauChuKySo(),
            mauChuKyTuoi: this.mauChuKyTuoi(),
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
        this.dungTuCuon();
        this.pdfTask?.destroy();
    }
}
