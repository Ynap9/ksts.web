
using ksts.be.domain.Auth;
using ksts.be.domain.LoKy;
using ksts.be.domain.Template;
using ksts.be.shared.Constants.Db;
using ksts.be.shared.Constants.Template;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ksts.be.infrastructure.Persistence
{
    public class KstsDbContext : IdentityDbContext<AppUser>
    {
        public KstsDbContext(DbContextOptions<KstsDbContext> options) : base(options)
        {
        }

        public DbSet<Template> Template { get; set; }
        public DbSet<TemplatePosition> TemplatePosition { get; set; }
        public DbSet<LoKy> LoKy { get; set; }
        public DbSet<LoKyFile> LoKyFile { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseOpenIddict();
            

            modelBuilder.Entity<Template>(entity =>
            {
                entity.HasIndex(x => new { x.IdUser, x.Deleted });
                entity.HasIndex(x => new { x.IdUser, x.TenTemplate });

                // Mặc định phải là 100 (giữ nguyên ảnh) chứ không phải 0 của kiểu int: template cũ thêm cột
                // mà nhận 0 thì độ đậm bằng không, ảnh dấu và chữ ký tươi biến mất khỏi giấy đã ký.
                entity.Property(x => x.DoDamDauDo).HasDefaultValue(TemplateConstants.DoDamMacDinh);
                entity.Property(x => x.DoDamChuKyTuoi).HasDefaultValue(TemplateConstants.DoDamMacDinh);
                entity.Property(x => x.DoDayNetChuKyTuoi).HasDefaultValue(TemplateConstants.DoDayNetMacDinh);

                // Khối chữ ký số vốn vẽ chữ đen nên cột này có mặc định; còn màu mực chữ ký tươi để TRỐNG
                // nghĩa là chưa chọn, giữ nguyên mực ảnh gốc - đặt mặc định ở đó là mất hẳn trạng thái đó.
                entity.Property(x => x.MauChuKySo).HasDefaultValue(TemplateConstants.MauMacDinh);

                entity.HasMany(x => x.Positions)
                      .WithOne(x => x.Template!)
                      .HasForeignKey(x => x.TemplateId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<TemplatePosition>(entity =>
            {
                entity.Property(e => e.Deleted).HasDefaultValue(0);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()");
            });

            modelBuilder.Entity<LoKy>(entity =>
            {
                entity.HasIndex(x => new { x.IdUser, x.Deleted });

                entity.HasMany(x => x.Files)
                      .WithOne(x => x.LoKy!)
                      .HasForeignKey(x => x.LoKyId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<LoKyFile>(entity =>
            {
                // Lấy việc kế tiếp là truy vấn chạy liên tục suốt lô vài nghìn file nên phải có index đúng
                // hình dạng câu lệnh: lọc theo lô + trạng thái, sắp theo thứ tự.
                entity.HasIndex(x => new { x.LoKyId, x.TrangThai, x.ThuTu });
                entity.HasIndex(x => new { x.LoKyId, x.TenFile });
            });
            modelBuilder.HasDefaultSchema(DbSchemas.Core);
            base.OnModelCreating(modelBuilder);
        }
    }
}
