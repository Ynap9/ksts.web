namespace kssm.be.external.KySo.SaoMai.Interfaces
{
    /// <summary>
    /// Báo ngược sang sao_mai_be khi một lô ký của dự án chạy trọn, để bên đó chuyển dự án sang trạng thái
    /// đã ký số theo luật nghiệp vụ của mình. Service này KHÔNG tự ghi trạng thái vào MongoDB.
    /// </summary>
    public interface IProjectNotifier
    {
        /// <summary>
        /// Gửi tin báo lô đã ký xong. Trả về việc bên kia đã nhận hay chưa; hỏng thì chỉ ghi log chứ không
        /// ném — file đã ký vẫn nguyên vẹn trên kho, đánh trượt cả lô vì một lời gọi phụ là sai.
        /// </summary>
        Task<bool> NotifySignedAsync(string projectId, int loKyId, int completed, int failed,
            CancellationToken cancellationToken = default);
    }
}
