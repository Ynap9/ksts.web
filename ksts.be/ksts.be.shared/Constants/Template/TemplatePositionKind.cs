using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ksts.be.shared.Constants.Template
{
    /// <summary>
    /// Loại khối được đặt lên trang. Đánh số TƯỜNG MINH vì giá trị này đi thẳng ra JSON cho FE
    /// (enum serialize thành SỐ) - chèn thêm phần tử vào giữa là đổi nghĩa dữ liệu đã lưu.
    /// </summary>
    public enum TemplatePositionKind
    {
        /// <summary>Khối chữ ký số (2 dòng chữ: tên người ký + giờ ký).</summary>
        ChuKy = 0,

        /// <summary>Ảnh dấu đỏ của cơ quan.</summary>
        DauDo = 1,

        /// <summary>Ảnh chữ ký tươi (chữ ký tay đã quét).</summary>
        ChuKyTuoi = 2,
    }
}
