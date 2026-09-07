using AutoMapper;
using kssm.be.applications.Base;
using kssm.be.applications.Template.Dtos;
using kssm.be.applications.Template.Interfaces;
using kssm.be.external.Colors.Interfaces;
using kssm.be.external.S3.Interfaces;
using kssm.be.infrastructure.Persistence;
using kssm.be.shared.Constants.Template;
using kssm.be.shared.Requests.AppException;
using kssm.be.shared.Requests.BaseRequest;
using kssm.be.shared.Requests.ErrorRequest;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TemplateEntity = kssm.be.domain.Template.Template;
using TemplatePositionEntity = kssm.be.domain.Template.TemplatePosition;

namespace kssm.be.applications.Template.Implements
{
    public class TemplateService : BaseService, ITemplateService
    {
        private readonly ITemplateImageStorage _imageStorage;
        private readonly IHexColorReader _hexColorReader;

        public TemplateService(
            KssmDbContext kstsDbContext,
            ILogger<BaseService> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ITemplateImageStorage imageStorage,
            IHexColorReader hexColorReader
        ) : base(kstsDbContext, logger, httpContextAccessor, mapper)
        {
            _imageStorage = imageStorage;
            _hexColorReader = hexColorReader;
        }

        public async Task<ViewTemplateDto> CreateAsync(AddTemplateDto input)
        {
            _logger.LogInformation($"{nameof(CreateAsync)} tenTemplate={input.TenTemplate}");

            var entity = new TemplateEntity
            {
                TenTemplate = (input.TenTemplate ?? string.Empty).Trim(),
                CreatedDate = GetVietnamTime(),
            };

            _kstsDbContext.Template.Add(entity);
            await _kstsDbContext.SaveChangesAsync();

            return _mapper.Map<ViewTemplateDto>(entity);
        }

        public async Task<ViewTemplateDto> UpdateAsync(UpdateTemplateDto input)
        {
            _logger.LogInformation($"{nameof(UpdateAsync)} id={input.Id}");

            var entity = await _kstsDbContext.Template
                .FirstOrDefaultAsync(x => x.Id == input.Id && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TemplateNotFound,
                    $"Không tìm thấy template Id={input.Id}.");

            entity.TenTemplate = (input.TenTemplate ?? string.Empty).Trim();
            entity.ModifiedDate = GetVietnamTime();

            await _kstsDbContext.SaveChangesAsync();

            return await GetByIdAsync(entity.Id);
        }

        public Task<ViewTemplateDto> CreateConfigAsync(AddConfigTemplateDto input)
        {
            _logger.LogInformation($"{nameof(CreateConfigAsync)} id={input.Id}");

            return UpdateConfigAsync(_mapper.Map<UpdateConfigTemplateDto>(input));
        }

        public async Task<ViewTemplateDto> UpdateConfigAsync(UpdateConfigTemplateDto input)
        {
            _logger.LogInformation($"{nameof(UpdateConfigAsync)} id={input.Id} soKhoi={input.Positions.Count}");

            var entity = await _kstsDbContext.Template
                .FirstOrDefaultAsync(x => x.Id == input.Id && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TemplateNotFound,
                    $"Không tìm thấy template Id={input.Id}.");

            foreach (var position in input.Positions)
            {
                if (position.PageNumber < 1
                    || position.WidthRatio <= 0 || position.HeightRatio <= 0
                    || position.XRatio < 0 || position.YRatio < 0
                    || position.XRatio + position.WidthRatio > 1
                    || position.YRatio + position.HeightRatio > 1)
                {
                    throw new UserFriendlyException(ErrorCodes.TemplatePositionInvalid,
                        $"Toạ độ khối {position.Kind} nằm ngoài trang hoặc có kích thước bằng 0.");
                }
            }

            entity.Thumbprint = (input.Thumbprint ?? string.Empty).Trim();
            entity.TenChungThu = input.TenChungThu;
            entity.LyDoKy = input.LyDoKy;
            entity.NoiKy = input.NoiKy;
            entity.HienThiChuKySo = input.HienThiChuKySo;
            entity.NhoiChuKySoVaoAnh = input.NhoiChuKySoVaoAnh;
            entity.KyDe = input.KyDe;

            // Kẹp về khoảng cho phép thay vì ném lỗi: giá trị này đến từ thanh trượt nên ra ngoài khoảng chỉ
            // xảy ra khi client gửi sai, mà đánh trượt cả lần lưu cấu hình vì một con số hiển thị là quá tay.
            entity.DoDamDauDo = Math.Clamp(input.DoDamDauDo,
                TemplateConstants.DoDamMin, TemplateConstants.DoDamMax);
            entity.DoDamChuKyTuoi = Math.Clamp(input.DoDamChuKyTuoi,
                TemplateConstants.DoDamMin, TemplateConstants.DoDamMax);
            entity.DoDayNetChuKyTuoi = Math.Clamp(input.DoDayNetChuKyTuoi,
                TemplateConstants.DoDayNetMin, TemplateConstants.DoDayNetMax);
            entity.MauChuKySo = _hexColorReader.Normalize(input.MauChuKySo) ?? TemplateConstants.MauMacDinh;
            entity.MauChuKyTuoi = _hexColorReader.Normalize(input.MauChuKyTuoi);

            entity.ModifiedDate = GetVietnamTime();

            if (input.AnhDauDo != null)
            {
                var uploaded = await _imageStorage.SaveAsync(input.AnhDauDo, entity.Id,
                    TemplateConstants.DauDoObjectName, entity.AnhDauDoObjectKey);
                entity.AnhDauDoUrl = uploaded.Url;
                entity.AnhDauDoObjectKey = uploaded.ObjectKey;
            }
            else if (input.XoaAnhDauDo)
            {
                await _imageStorage.RemoveAsync(entity.AnhDauDoObjectKey);
                entity.AnhDauDoUrl = null;
                entity.AnhDauDoObjectKey = null;
            }

            if (input.AnhChuKyTuoi != null)
            {
                var uploaded = await _imageStorage.SaveAsync(input.AnhChuKyTuoi, entity.Id,
                    TemplateConstants.ChuKyTuoiObjectName, entity.AnhChuKyTuoiObjectKey);
                entity.AnhChuKyTuoiUrl = uploaded.Url;
                entity.AnhChuKyTuoiObjectKey = uploaded.ObjectKey;
            }
            else if (input.XoaAnhChuKyTuoi)
            {
                await _imageStorage.RemoveAsync(entity.AnhChuKyTuoiObjectKey);
                entity.AnhChuKyTuoiUrl = null;
                entity.AnhChuKyTuoiObjectKey = null;
            }

            var cu = await _kstsDbContext.TemplatePosition
                .Where(x => x.TemplateId == entity.Id)
                .ToListAsync();
            _kstsDbContext.TemplatePosition.RemoveRange(cu);

            var moi = input.Positions.Select(p => new TemplatePositionEntity
            {
                TemplateId = entity.Id,
                Kind = p.Kind,
                PageNumber = Math.Max(p.PageNumber, 1),
                XRatio = p.XRatio,
                YRatio = p.YRatio,
                WidthRatio = p.WidthRatio,
                HeightRatio = p.HeightRatio,
                CreatedDate = GetVietnamTime(),
            }).ToList();
            await _kstsDbContext.TemplatePosition.AddRangeAsync(moi);

            await _kstsDbContext.SaveChangesAsync();

            return await GetByIdAsync(entity.Id);
        }

        public async Task DeleteAsync(int id)
        {
            _logger.LogInformation($"{nameof(DeleteAsync)} id={id}");

            var entity = await _kstsDbContext.Template.FirstOrDefaultAsync(x => x.Id == id && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TemplateNotFound,
                    $"Không tìm thấy template Id={id}.");

            var thoiDiem = GetVietnamTime();

            entity.Deleted = true;
            entity.DeletedDate = thoiDiem;
            entity.AnhDauDoUrl = null;
            entity.AnhDauDoObjectKey = null;
            entity.AnhChuKyTuoiUrl = null;
            entity.AnhChuKyTuoiObjectKey = null;

            var positions = await _kstsDbContext.TemplatePosition
                .Where(x => x.TemplateId == id && !x.Deleted)
                .ToListAsync();
            foreach (var position in positions)
            {
                position.Deleted = true;
                position.DeletedDate = thoiDiem;
            }

            await _kstsDbContext.SaveChangesAsync();

            await _imageStorage.RemoveAllAsync(id);
        }

        public async Task<ViewTemplateDto> GetByIdAsync(int id)
        {
            var entity = await _kstsDbContext.Template
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TemplateNotFound,
                    $"Không tìm thấy template Id={id}.");

            var positions = await _kstsDbContext.TemplatePosition
                .AsNoTracking()
                .Where(x => x.TemplateId == id && !x.Deleted)
                .OrderBy(x => x.Kind)
                .ToListAsync();

            var result = _mapper.Map<ViewTemplateDto>(entity);
            result.Positions = _mapper.Map<List<TemplatePositionDto>>(positions);

            return result;
        }

        public SampleFileDto GetSampleFile()
        {
            var path = TemplateConstants.GetSamplePdfPath();
            return new SampleFileDto
            {
                FileName = Path.GetFileName(path),
                Exists = File.Exists(path),
            };
        }

        public Stream OpenSampleFile()
        {
            var path = TemplateConstants.GetSamplePdfPath();
            if (!File.Exists(path))
            {
                throw new UserFriendlyException(ErrorCodes.TemplateSampleFileMissing,
                    "Bản cài thiếu file PDF mẫu.");
            }

            return File.OpenRead(path);
        }

        public async Task<BaseResponsePagingDto<ViewTemplateDto>> FindPagingAsync(FindPagingTemplateDto input)
        {
            _logger.LogInformation($"{nameof(FindPagingAsync)} keyword={input.Keyword}");

            var keyword = input.Keyword;
            var query = _kstsDbContext.Template
                .AsNoTracking()
                .Where(x => !x.Deleted
                    && (string.IsNullOrWhiteSpace(keyword) || x.TenTemplate.Contains(keyword)));

            var totalItems = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.Id)
                .Paging(input)
                .ToListAsync();


            var ids = items.Select(x => x.Id).ToList();
            var positions = await _kstsDbContext.TemplatePosition
                .AsNoTracking()
                .Where(x => ids.Contains(x.TemplateId) && !x.Deleted)
                .OrderBy(x => x.Kind)
                .ToListAsync();

            var dtos = _mapper.Map<List<ViewTemplateDto>>(items);
            foreach (var dto in dtos)
            {
                dto.Positions = _mapper.Map<List<TemplatePositionDto>>(
                    positions.Where(x => x.TemplateId == dto.Id).ToList());
            }

            return new BaseResponsePagingDto<ViewTemplateDto>
            {
                Items = dtos,
                TotalItems = totalItems,
            };
        }
    }
}
