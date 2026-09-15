using kssm.be.domain.DongGoi;
using kssm.be.domain.KySo.LoKy;
using kssm.be.domain.KySo.Template;
using kssm.be.shared.Constants.Template;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kssm.be.infrastructure.Persistence
{
    public  class KssmDbContext: DbContext
    {
        public KssmDbContext(DbContextOptions<KssmDbContext> options) : base(options)
        {
        }
        public DbSet<Template> Template { get; set; }
        public DbSet<TemplatePosition> TemplatePosition { get; set; }
        public DbSet<LoKy> LoKy { get; set; }
        public DbSet<LoKyFile> LoKyFile { get; set; }
        public DbSet<CodeValue> CodeValue { get; set; }
        public DbSet<MetadataField> MetadataField { get; set; }
        public DbSet<MetadataFieldAlias> MetadataFieldAlias { get; set; }
        public DbSet<PackageVariant> PackageVariant { get; set; }
        public DbSet<PackageFieldConfig> PackageFieldConfig { get; set; }
        public DbSet<PackageSession> PackageSession { get; set; }
        public DbSet<PackageDossier> PackageDossier { get; set; }
        public DbSet<PackageDocument> PackageDocument { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Template>(entity =>
            {


                // Mặc định phải là 100 (giữ nguyên ảnh) chứ không phải 0 của kiểu int: template cũ thêm cột
                // mà nhận 0 thì độ đậm bằng không, ảnh dấu và chữ ký tươi biến mất khỏi giấy đã ký.
                entity.Property(x => x.DoDamDauDo).HasDefaultValue(TemplateConstants.DoDamMacDinh);
                entity.Property(x => x.DoDamChuKyTuoi).HasDefaultValue(TemplateConstants.DoDamMacDinh);
                entity.Property(x => x.DoDayNetChuKyTuoi).HasDefaultValue(TemplateConstants.DoDayNetMacDinh);

                // Khối chữ ký số vốn vẽ chữ đen nên cột này có mặc định; còn màu mực chữ ký tươi để TRỐNG
                // nghĩa là chưa chọn, giữ nguyên mực ảnh gốc - đặt mặc định ở đó là mất hẳn trạng thái đó.
                entity.Property(x => x.MauChuKySo).HasDefaultValue(TemplateConstants.MauMacDinh);

            });
            modelBuilder.Entity<TemplatePosition>(entity =>
            {
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
            });
            modelBuilder.Entity<LoKy>(entity =>
            {
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
            });
            modelBuilder.Entity<LoKyFile>(entity =>
            {
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");

                // Lấy việc kế tiếp là truy vấn chạy liên tục suốt cả lô: lọc theo lô kèm trạng thái rồi sắp
                // theo thứ tự. Thiếu index này là mỗi lượt nhận việc quét lại toàn bảng.
                entity.HasIndex(e => new { e.LoKyId, e.TrangThai, e.ThuTu });
            });
            modelBuilder.Entity<CodeValue>(entity =>
            {
                entity.Property(e => e.Deleted).HasDefaultValue(false);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.HasIndex(e => new { e.CodeGroup, e.Code });
            });
            modelBuilder.Entity<MetadataField>(entity =>
            {
                entity.Property(e => e.Deleted).HasDefaultValue(false);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.HasIndex(e => e.FieldKey);
            });
            modelBuilder.Entity<MetadataFieldAlias>(entity =>
            {
                entity.Property(e => e.Deleted).HasDefaultValue(false);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.HasIndex(e => e.MetadataFieldId);
            });
            modelBuilder.Entity<PackageVariant>(entity =>
            {
                entity.Property(e => e.Deleted).HasDefaultValue(false);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.HasIndex(e => e.VariantKey);
            });
            modelBuilder.Entity<PackageFieldConfig>(entity =>
            {
                entity.Property(e => e.Deleted).HasDefaultValue(false);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
                entity.HasIndex(e => new { e.PackageVariantId, e.MetadataFieldId });
            });
            modelBuilder.Entity<PackageSession>(entity =>
            {
                entity.Property(e => e.Deleted).HasDefaultValue(false);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");

                // Lượt dọn phiên quá hạn lúc khởi động quét theo trạng thái kèm ngày tạo.
                entity.HasIndex(e => new { e.Status, e.CreatedDate });
            });
            modelBuilder.Entity<PackageDossier>(entity =>
            {
                entity.Property(e => e.Deleted).HasDefaultValue(false);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");

                // Mỗi đợt tải lên đều tra hồ sơ theo tên thư mục trong phạm vi phiên để gắn file vào đúng chỗ.
                entity.HasIndex(e => new { e.SessionId, e.FolderName });
            });
            modelBuilder.Entity<PackageDocument>(entity =>
            {
                entity.Property(e => e.Deleted).HasDefaultValue(false);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");

                // Tiến độ hỏi theo phiên kèm trạng thái; kết quả từng hồ sơ lọc theo DossierId.
                entity.HasIndex(e => new { e.SessionId, e.Status });
                entity.HasIndex(e => e.DossierId);
            });
            base.OnModelCreating(modelBuilder);
            // Configure your entity mappings here
        }
    }
}
