export enum ETemplatePositionKind {
  ChuKy = 0,
  DauDo = 1,
  ChuKyTuoi = 2
}

export const TEMPLATE_POSITION_LABELS: Record<ETemplatePositionKind, string> = {
  [ETemplatePositionKind.ChuKy]: 'Chữ ký số',
  [ETemplatePositionKind.DauDo]: 'Dấu đỏ',
  [ETemplatePositionKind.ChuKyTuoi]: 'Chữ ký tươi'
};

export const CHU_KY_BLOCK_WIDTH_PT = 170;
export const CHU_KY_BLOCK_HEIGHT_PT = 30;

export const KHOI_MIN_RATIO = 0.03;

/**
 * Edge band of the preview viewport that triggers auto-scrolling while a block is being dragged.
 * Without it a drag stops dead at the viewport edge and the user has to drop, scroll, drag again.
 */
export const CUON_VUNG_MEP_PX = 60;

/** Largest scroll step per animation frame, reached when the pointer sits right on the edge. */
export const CUON_TOC_DO_TOI_DA_PX = 18;

/** Mức phóng dự phòng khi chưa đo được bề rộng cột chứa preview. */
export const PDF_PREVIEW_SCALE = 1.4;

export const PDF_PHAN_MO_RONG = '.pdf';

export const ANH_ALLOWED_TYPES = 'image/png,image/jpeg';

/**
 * Độ đậm ảnh dấu đỏ / chữ ký tươi, tính theo phần trăm so với ảnh gốc: 100 là giữ nguyên.
 * Phải khớp TemplateConstants bên BE — BE kẹp lại theo đúng khoảng này, và bản xem trước phải dùng cùng
 * công thức với phép BE ghi vào /Decode của PDF thì kéo thanh mới thấy đúng bản in ra.
 */
export const DO_DAM_MAC_DINH = 140;
export const DO_DAM_MIN = 40;
export const DO_DAM_MAX = 250;

/** Độ đậm gồm hai vế: tăng tương phản f, đồng thời hạ độ sáng b = 1 - (f - 1) * hệ số, chặn dưới bởi sàn. */
export const DO_DAM_HE_SO_GIAM_SANG = 0.5;
export const DO_DAM_SANG_TOI_THIEU = 0.5;

/**
 * Độ dày nét chữ ký tươi, phần trăm: 0 giữ nguyên ảnh gốc, 100 là mức bản dựng thiết kế gốc đã cân.
 * Khác hẳn độ đậm: độ đậm chỉ làm nét sẫm màu hơn tại chỗ, độ dày mới làm nét to ra.
 * Phải khớp TemplateConstants bên BE — BE kẹp lại theo đúng khoảng này.
 */
export const DO_DAY_NET_MAC_DINH = 100;
export const DO_DAY_NET_MIN = 0;
export const DO_DAY_NET_MAX = 200;

/** Bán kính nong nét ở mức 100%, theo tỉ lệ bề rộng ảnh hiển thị — bản gốc cân 0,35px cho ảnh rộng 158px. */
export const DO_DAY_NET_BAN_KINH_TI_LE = 0.35 / 158;

/**
 * Màu mực dùng cho bản xem trước KHI template chưa chọn màu riêng. BE nong nét bằng cách vẽ chồng chính ảnh
 * nên giữ đúng màu mực thật, còn CSS drop-shadow bắt buộc phải nêu một màu — lấy đúng màu bản dựng gốc dùng
 * cho ảnh chữ ký quét. Hệ quả: chữ ký mực đen kéo thanh lên cao sẽ thấy quầng hơi ngả xanh ở bản xem trước,
 * bản in ra thì không. Chọn màu mực rồi thì quầng lấy đúng màu đó, khớp hơn với bản ký.
 */
export const DO_DAY_NET_MAU_MUC = '#1e3a8a';

/**
 * Màu mặc định của khối chữ ký số và mực chữ ký tươi. Phải khớp TemplateConstants.MauMacDinh bên BE, và ĐEN
 * mang nghĩa "giữ nguyên" chứ không phải "nhuộm thành đen": chữ ký số vốn vẽ chữ đen, ảnh chữ ký tươi giữ
 * đúng màu mực của ảnh gốc.
 */
export const MAU_MAC_DINH = '#000000';

/** Id của bộ lọc SVG nhuộm màu ảnh chữ ký tươi trên bản xem trước. */
export const MAU_CHU_KY_TUOI_FILTER_ID = 'mauChuKyTuoi';
