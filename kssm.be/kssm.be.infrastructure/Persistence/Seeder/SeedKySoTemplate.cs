using kssm.be.domain.KySo.Template;
using kssm.be.shared.Constants;
using kssm.be.shared.Constants.Signing;
using kssm.be.shared.Constants.Template;
using Microsoft.EntityFrameworkCore;
using TemplateEntity = kssm.be.domain.KySo.Template.Template;

namespace kssm.be.infrastructure.Persistence.Seeder
{
    public static class SeedKySoTemplate
    {
        public const string Tt05TemplateName = "Ký số TT05";
        public const string Tt05TemplateDescription = "Template phục vụ ký số theo TT05";

        public static async Task SeedAsync(KssmDbContext db)
        {
            await SeedTt05TemplateAsync(db);
        }

        public static async Task SeedTt05TemplateAsync(KssmDbContext db)
        {
            var now = DateTimeConstants.VietnamNow;

            var template = await db.Template
                .FirstOrDefaultAsync(x => !x.Deleted && x.TenTemplate == Tt05TemplateName);

            if (template == null)
            {
                template = new TemplateEntity
                {
                    TenTemplate = Tt05TemplateName,
                    Description = Tt05TemplateDescription,
                    HienThiChuKySo = true,
                    NhoiChuKySoVaoAnh = false,
                    KyDe = false,
                    MauChuKySo = TemplateConstants.MauMacDinh,
                    CreatedDate = now,
                };

                db.Template.Add(template);
                await db.SaveChangesAsync();
            }

            var hasSignaturePosition = await db.TemplatePosition
                .AnyAsync(x => !x.Deleted
                    && x.TemplateId == template.Id
                    && x.Kind == TemplatePositionKindConstants.ChuKy);
            if (hasSignaturePosition) return;

            db.TemplatePosition.Add(BuildTopRightSignaturePosition(template.Id, now));
            await db.SaveChangesAsync();
        }

        public static TemplatePosition BuildTopRightSignaturePosition(int templateId, DateTime createdDate)
        {
            var pageWidth = SigningConstants.AppearanceReferencePageWidth;
            var pageHeight = SigningConstants.AppearanceReferencePageHeight;
            var margin = SigningConstants.AppearancePageMargin;

            return new TemplatePosition
            {
                TemplateId = templateId,
                Kind = TemplatePositionKindConstants.ChuKy,
                PageNumber = 1,
                XRatio = (pageWidth - margin - SigningConstants.AppearanceWidth) / pageWidth,
                YRatio = margin / pageHeight,
                WidthRatio = SigningConstants.AppearanceWidth / pageWidth,
                HeightRatio = SigningConstants.AppearanceHeight / pageHeight,
                CreatedDate = createdDate,
            };
        }
    }
}
