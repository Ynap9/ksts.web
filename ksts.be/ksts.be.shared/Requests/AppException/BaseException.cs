using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ksts.be.shared.Request.AppException
{
    public class BaseException : System.Exception
    {
        /// <summary>
        /// Mã lỗi
        /// </summary>
        public readonly int ErrorCode;
        /// <summary>
        /// Chuỗi cần localize sẽ tra trong từ điển, nếu có truyền chuỗi này thì 
        /// sẽ không lấy message của error nữa mà lấy theo chuỗi này
        /// </summary>
        public readonly string? MessageLocalize;
        /// <summary>
        /// Chuỗi cần trả ra (Không localize chỉ dùng chung 1 biến ErrorCode)
        /// </summary>
        public string? ErrorMessage;
        /// <summary>
        /// Mảng chuỗi cần chả
        /// </summary>
        public string[]? ListParam;

        public BaseException(int errorCode) : base()
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Truyền chuỗi xuống CẢ Exception.Message chứ không chỉ giữ ở MessageLocalize: controller đọc
        /// MessageLocalize để dựng envelope nên trả về vẫn đúng, nhưng mọi chỗ khác trong hệ thống lại đọc
        /// ex.Message — log, LyDoLoi của từng file, LoiChung của cả lô. Bỏ trống thì những chỗ đó nhận đúng
        /// một câu vô nghĩa "Exception of type ... was thrown", đọc log không biết hỏng vì cái gì.
        /// </summary>
        public BaseException(int errorCode, string? messageLocalize) : base(messageLocalize)
        {
            ErrorCode = errorCode;
            MessageLocalize = messageLocalize;
        }
        public BaseException(int errorCode, params string[] listParam) : base()
        {
            ErrorCode = errorCode;
            ListParam = listParam;
        }
    }
}
