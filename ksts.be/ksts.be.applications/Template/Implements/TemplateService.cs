using AutoMapper;
using ksts.be.applications.Base;
using ksts.be.applications.Template.Dtos;
using ksts.be.applications.Template.Interfaces;
using ksts.be.domain.Auth;
using ksts.be.external.Colors.Interfaces;
using ksts.be.external.Images.Dtos;
using ksts.be.external.Images.Interfaces;
using ksts.be.external.Placement.Interfaces;
using ksts.be.external.Storage.Interfaces;
using ksts.be.infrastructure.Persistence;
using ksts.be.shared.Constants.Template;
using ksts.be.shared.HttpRequest.BaseRequest;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TemplateEntity = ksts.be.domain.Template.Template;
using TemplatePositionEntity = ksts.be.domain.Template.TemplatePosition;

namespace ksts.be.applications.Template.Implements
{
    /// <inheritdoc/>
    public class TemplateService : BaseService, ITemplateService
    {
        private readonly ITemplateImageStorage _imageStorage;
        private readonly IImageSizeReader _imageSizeReader;
        private readonly ISealPlacementResolver _placementResolver;
        private readonly IHexColorReader _hexColorReader;
        private readonly UserManager<AppUser> _userManager;

        public TemplateService(
            KstsDbContext kstsDbContext,
            ILogger<BaseService> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ITemplateImageStorage imageStorage,
            IImageSizeReader imageSizeReader,
            ISealPlacementResolver placementResolver,
            IHexColorReader hexColorReader,
            UserManager<AppUser> userManager
        ) : base(kstsDbContext, logger, httpContextAccessor, mapper)
        {
            _imageStorage = imageStorage;
            _imageSizeReader = imageSizeReader;
            _placementResolver = placementResolver;
            _hexColorReader = hexColorReader;
            _userManager = userManager;
        }

        /// <inheritdoc/>
        public ViewTemplateDto Create(AddTemplateDto input)
        {
            _logger.LogInformation($"{nameof(Create)} tenTemplate={input.TenTemplate}");

            var userId = getCurrentUserId();
            var entity = new TemplateEntity
            {
                IdUser = userId,
                TenTemplate = (input.TenTemplate ?? string.Empty).Trim(),
                CreatedBy = getCurrentName(),
                CreatedDate = GetVietnamTime(),
            };

            _kstsDbContext.Template.Add(entity);
            _kstsDbContext.SaveChanges();

            var result = _mapper.Map<ViewTemplateDto>(entity);
            result.CreatedBy = _userManager.Users
                .AsNoTracking()
                .Where(x => x.Id == userId)
                .Select(x => x.FullName)
                .FirstOrDefault();

            return result;
        }

        /// <inheritdoc/>
        public ViewTemplateDto Update(UpdateTemplateDto input)
        {
            _logger.LogInformation($"{nameof(Update)} id={input.Id}");

            var userId = getCurrentUserId();
            var entity = _kstsDbContext.Template
                .Include(x => x.Positions)
                .FirstOrDefault(x => x.Id == input.Id && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TemplateNotFound,
                    $"Không tìm thấy template Id={input.Id}.");

            if (entity.IdUser != userId && !IsSuperAdmin())
            {
                throw new UserFriendlyException(ErrorCodes.TemplateAccessDenied,
                    "Template này thuộc về người dùng khác.");
            }

            entity.TenTemplate = (input.TenTemplate ?? string.Empty).Trim();
            entity.ModifiedBy = getCurrentName();
            entity.ModifiedDate = GetVietnamTime();

            _kstsDbContext.SaveChanges();

            var result = _mapper.Map<ViewTemplateDto>(entity);
            result.CreatedBy = _userManager.Users
                .AsNoTracking()
                .Where(x => x.Id == entity.IdUser)
                .Select(x => x.FullName)
                .FirstOrDefault();

            return result;
        }

        /// <inheritdoc/>
        public Task<ViewTemplateDto> CreateConfigAsync(AddConfigTemplateDto input)
        {
            _logger.LogInformation($"{nameof(CreateConfigAsync)} id={input.Id}");

            return UpdateConfigAsync(_mapper.Map<UpdateConfigTemplateDto>(input));
        }

        /// <inheritdoc/>
        public async Task<ViewTemplateDto> UpdateConfigAsync(UpdateConfigTemplateDto input)
        {
            _logger.LogInformation($"{nameof(UpdateConfigAsync)} id={input.Id} soKhoi={input.Positions.Count}");

            var userId = getCurrentUserId();
            var entity = await _kstsDbContext.Template
                .Include(x => x.Positions)
                .FirstOrDefaultAsync(x => x.Id == input.Id && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TemplateNotFound,
                    $"Không tìm thấy template Id={input.Id}.");

            if (entity.IdUser != userId && !IsSuperAdmin())
            {
                throw new UserFriendlyException(ErrorCodes.TemplateAccessDenied,
                    "Template này thuộc về người dùng khác.");
            }

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

            entity.ModifiedBy = getCurrentName();
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

            _kstsDbContext.TemplatePosition.RemoveRange(entity.Positions);
            entity.Positions = input.Positions.Select(p => new TemplatePositionEntity
            {
                TemplateId = entity.Id,
                Kind = p.Kind,
                PageNumber = Math.Max(p.PageNumber, 1),
                XRatio = p.XRatio,
                YRatio = p.YRatio,
                WidthRatio = p.WidthRatio,
                HeightRatio = p.HeightRatio,
            }).ToList();

            await _kstsDbContext.SaveChangesAsync();

            return await GetByIdAsync(entity.Id);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(int id)
        {
            _logger.LogInformation($"{nameof(DeleteAsync)} id={id}");

            var userId = getCurrentUserId();
            var entity = await _kstsDbContext.Template.FirstOrDefaultAsync(x => x.Id == id && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TemplateNotFound,
                    $"Không tìm thấy template Id={id}.");

            if (entity.IdUser != userId && !IsSuperAdmin())
            {
                throw new UserFriendlyException(ErrorCodes.TemplateAccessDenied,
                    "Template này thuộc về người dùng khác.");
            }

            entity.Deleted = true;
            entity.DeletedBy = getCurrentName();
            entity.DeletedDate = GetVietnamTime();
            entity.AnhDauDoUrl = null;
            entity.AnhDauDoObjectKey = null;
            entity.AnhChuKyTuoiUrl = null;
            entity.AnhChuKyTuoiObjectKey = null;
            await _kstsDbContext.SaveChangesAsync();

            await _imageStorage.RemoveAllAsync(id);
        }

        /// <inheritdoc/>
        public async Task<ViewTemplateDto> GetByIdAsync(int id)
        {
            var userId = getCurrentUserId();
            var entity = await _kstsDbContext.Template
                .Include(x => x.Positions)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TemplateNotFound,
                    $"Không tìm thấy template Id={id}.");

            if (entity.IdUser != userId && !IsSuperAdmin())
            {
                throw new UserFriendlyException(ErrorCodes.TemplateAccessDenied,
                    "Template này thuộc về người dùng khác.");
            }

            var result = _mapper.Map<ViewTemplateDto>(entity);
            result.CreatedBy = await _userManager.Users
                .AsNoTracking()
                .Where(x => x.Id == entity.IdUser)
                .Select(x => x.FullName)
                .FirstOrDefaultAsync();

            return result;
        }

        /// <inheritdoc/>
        public async Task<BaseResponsePagingDto<ViewTemplateDto>> FindPagingAsync(FindPagingTemplateDto input)
        {
            _logger.LogInformation($"{nameof(FindPagingAsync)} keyword={input.Keyword}");

            var createdBy = getCurrentName();
            var isAdmin = IsSuperAdmin();
            var keyword = input.Keyword;
            var query = _kstsDbContext.Template
                .Include(x => x.Positions)
                .AsNoTracking()
                .Where(x => !x.Deleted
                    && (isAdmin || x.CreatedBy == createdBy)
                    && (string.IsNullOrWhiteSpace(keyword) || x.TenTemplate.Contains(keyword)));

            var totalItems = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.Id)
                .Paging(input)
                .ToListAsync();

            var dtos = _mapper.Map<List<ViewTemplateDto>>(items);
            var idUsers = dtos.Select(x => x.IdUser).Distinct().ToList();
            var fullNames = await _userManager.Users
                .AsNoTracking()
                .Where(x => idUsers.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.FullName);

            foreach (var dto in dtos)
            {
                dto.CreatedBy = fullNames.GetValueOrDefault(dto.IdUser);
            }

            return new BaseResponsePagingDto<ViewTemplateDto>
            {
                Items = dtos,
                TotalItems = totalItems,
            };
        }

        /// <inheritdoc/>
        public SampleFileDto GetSampleFile()
        {
            var path = TemplateConstants.GetSamplePdfPath();
            return new SampleFileDto
            {
                FileName = Path.GetFileName(path),
                Exists = File.Exists(path),
            };
        }

        /// <inheritdoc/>
        public Stream OpenSampleFile()
        {
            var path = TemplateConstants.GetSamplePdfPath();
            if (!File.Exists(path))
            {
                throw new UserFriendlyException(ErrorCodes.SealSampleFileMissing,
                    "Bản cài thiếu file PDF mẫu.");
            }

            return File.OpenRead(path);
        }

        /// <inheritdoc/>
        public async Task<SuggestedPlacementDto> GetSuggestedPlacementAsync(SuggestPlacementDto input)
        {
            _logger.LogInformation($"{nameof(GetSuggestedPlacementAsync)} pageNumber={input.PageNumber}");

            ImageSizeDto? dauDo = null;
            if (input.AnhDauDo != null)
            {
                _imageStorage.ValidateImage(input.AnhDauDo);
                using var buffer = new MemoryStream();
                await input.AnhDauDo.CopyToAsync(buffer);
                dauDo = _imageSizeReader.Read(buffer.ToArray());
            }

            ImageSizeDto? chuKyTuoi = null;
            if (input.AnhChuKyTuoi != null)
            {
                _imageStorage.ValidateImage(input.AnhChuKyTuoi);
                using var buffer = new MemoryStream();
                await input.AnhChuKyTuoi.CopyToAsync(buffer);
                chuKyTuoi = _imageSizeReader.Read(buffer.ToArray());
            }

            await using var pdfStream = input.FilePdf != null
                ? input.FilePdf.OpenReadStream()
                : OpenSampleFile();

            var placement = _placementResolver.Resolve(pdfStream, Math.Max(input.PageNumber, 1),
                dauDo, chuKyTuoi);

            return new SuggestedPlacementDto
            {
                PageNumber = placement.PageNumber,
                PageWidthPoints = placement.PageWidthPoints,
                PageHeightPoints = placement.PageHeightPoints,
                AnchorChucDanh = placement.AnchorChucDanh,
                AnchorTenNguoiKy = placement.AnchorTenNguoiKy,
                MidXRatio = placement.MidXRatio,
                MidYRatio = placement.MidYRatio,
                DauDo = placement.DauDo,
                ChuKyTuoi = placement.ChuKyTuoi,
                CanhBao = placement.CanhBao,
            };
        }
    }
}
