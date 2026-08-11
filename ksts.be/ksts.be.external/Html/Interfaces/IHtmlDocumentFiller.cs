namespace ksts.be.external.Html.Interfaces
{
    /// <summary>
    /// Nhồi dữ liệu vào tài liệu HTML theo id của thẻ.
    /// Đi qua cây DOM chứ không thay chuỗi: giá trị người nhập có thể chứa ký tự của HTML, thay chuỗi thô
    /// sẽ vỡ thẻ hoặc mở đường chèn mã lạ vào bản in.
    /// </summary>
    public interface IHtmlDocumentFiller
    {
        /// <summary>
        /// Đổ <paramref name="giaTriTheoId"/> vào phần chữ của các thẻ tương ứng và
        /// <paramref name="htmlTheoId"/> vào phần bên trong các thẻ tương ứng, trả về HTML hoàn chỉnh.
        /// Id không có trong tài liệu thì bỏ qua để một thẻ bị gỡ khỏi mẫu không làm hỏng cả lô in.
        ///
        /// <paramref name="hienTheoId"/> bật tắt cả khối bằng lớp <c>hidden</c> của mẫu: giấy báo có mấy dòng
        /// chỉ xuất hiện ở một nhóm loại trúng tuyển, và chúng phải BIẾN MẤT chứ không phải để trống.
        /// </summary>
        string Fill(string html, IReadOnlyDictionary<string, string> giaTriTheoId,
            IReadOnlyDictionary<string, string> htmlTheoId,
            IReadOnlyDictionary<string, bool> hienTheoId);
    }
}
