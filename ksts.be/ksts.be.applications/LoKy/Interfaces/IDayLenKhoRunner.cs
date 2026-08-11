namespace ksts.be.applications.LoKy.Interfaces
{
    /// <summary>
    /// Đẩy bản đã ký của một lô sang thư mục dùng chung trên kho object, chạy nền và nhiều file song song.
    ///
    /// Tách khỏi <see cref="ILoKyService"/> và đăng ký Singleton vì việc này sống LÂU HƠN request đã khởi
    /// động nó: service theo phạm vi request sẽ bị dispose kèm DbContext ngay khi trả lời xong, nên mỗi đơn
    /// vị việc phải tự mở scope riêng — đúng hình dạng của <see cref="IKySoRunner"/>.
    /// </summary>
    public interface IDayLenKhoRunner
    {
        /// <summary>Mở việc đẩy cho một lô rồi trả về ngay. Lô đang đẩy dở thì bỏ qua lời gọi thứ hai.</summary>
        void BatDau(int loKyId);

        /// <summary>Lô này có đang đẩy hay không.</summary>
        bool DangChay(int loKyId);

        /// <summary>Đẩy toàn bộ file đã ký xong của lô, rồi chốt trạng thái.</summary>
        Task ChayAsync(int loKyId, CancellationToken cancellationToken);

        /// <summary>
        /// Đẩy một file. Nuốt lỗi rồi cộng vào bộ đếm lỗi chứ không ném ra: một file hỏng không được phép
        /// làm dừng cả lô đang đẩy.
        /// </summary>
        Task DayMotFileAsync(int loKyId, int loKyFileId, CancellationToken cancellationToken);

        /// <summary>Cộng dồn kết quả một file bằng MỘT câu lệnh, tránh nhiều luồng ghi đè kết quả của nhau.</summary>
        Task CongDonAsync(int loKyId, bool thanhCong);

        /// <summary>Chốt lại cờ khi lô đẩy xong hoặc gãy giữa chừng.</summary>
        Task ChotTrangThaiAsync(int loKyId, string? loi);
    }
}
