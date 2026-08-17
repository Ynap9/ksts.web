using ksts.be.external.Images.Interfaces;
using ksts.be.external.Pdf.Dtos;
using ksts.be.external.Pdf.Interfaces;
using ksts.be.shared.Constants.Signing;
using ksts.be.shared.Constants.Template;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ksts.be.external.Pdf.Implements
{
    /// <inheritdoc/>
    public class PdfPreparer : IPdfPreparer
    {
        private readonly IPdfRevisionReader _revisionReader;
        private readonly IPdfObjectWriter _objectWriter;
        private readonly IPdfAppearanceBuilder _appearanceBuilder;
        private readonly IImageSizeReader _imageSizeReader;

        public PdfPreparer(IPdfRevisionReader revisionReader, IPdfObjectWriter objectWriter,
            IPdfAppearanceBuilder appearanceBuilder, IImageSizeReader imageSizeReader)
        {
            _revisionReader = revisionReader;
            _objectWriter = objectWriter;
            _appearanceBuilder = appearanceBuilder;
            _imageSizeReader = imageSizeReader;
        }

        /// <inheritdoc/>
        public PdfPreparedDto Prepare(byte[] pdf, PdfPrepareOptionsDto options)
        {
            var revision = _revisionReader.Load(pdf);

            var catalogRaw = _revisionReader.GetObjectBody(revision, revision.RootObjectNumber)
                ?? throw new UserFriendlyException(ErrorCodes.PdfStructureUnsupported,
                    "Không đọc được Catalog của file PDF.");

            var pagesMatch = Regex.Match(catalogRaw, @"/Pages\s+(\d+)\s+\d+\s+R");
            if (!pagesMatch.Success)
            {
                throw new UserFriendlyException(ErrorCodes.PdfStructureUnsupported,
                    "Catalog của file PDF không có /Pages.");
            }

            var pagesNo = int.Parse(pagesMatch.Groups[1].Value);
            var pageOrder = ReadPageOrder(revision, pagesNo);
            if (pageOrder.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.PdfStructureUnsupported,
                    "Không tìm được trang nào trong file PDF.");
            }

            var next = revision.Size;
            var sigFieldNo = next++;
            var sigValueNo = next++;

            var plan = PlanAnnotations(revision, pageOrder, pagesNo, options, next);
            next = plan.NextObjectNumber;

            // Đúng MỘT widget chữ ký thì field gộp luôn widget — giữ nguyên hình dạng của file production.
            // Nhiều widget thì một field CHA có nhiều /Kids: vẫn là MỘT chữ ký, chỉ hiển thị ở nhiều chỗ.
            // Tách thành nhiều field là nhiều chữ ký, tức nhiều lần gọi TSA và nhiều lần chạm khoá token.
            var chuKyWidgets = plan.Annotations.Where(a => a.LaChuKy).ToList();
            var annotationsKhac = plan.Annotations.Where(a => !a.LaChuKy).ToList();

            if (chuKyWidgets.Count == 1)
            {
                chuKyWidgets[0].Number = sigFieldNo;
            }
            else
            {
                foreach (var widget in chuKyWidgets) widget.Number = next++;
            }

            foreach (var annotation in annotationsKhac) annotation.Number = next++;

            var xrefNo = next++;
            var objects = BuildRevisionObjects(revision, catalogRaw, plan, chuKyWidgets, annotationsKhac,
                sigFieldNo, sigValueNo, options);

            return WriteRevision(pdf, revision, objects, xrefNo);
        }

        /// <inheritdoc/>
        public List<PdfAppearanceObjectDto> BuildRevisionObjects(PdfRevisionDto revision, string catalogRaw,
            PdfAnnotationPlanDto plan, IReadOnlyList<PdfAnnotationDto> chuKyWidgets,
            IReadOnlyList<PdfAnnotationDto> annotationsKhac, int sigFieldNo, int sigValueNo,
            PdfPrepareOptionsDto options)
        {
            var objects = new List<PdfAppearanceObjectDto>
            {
                new()
                {
                    Number = revision.RootObjectNumber,
                    DictText = BuildCatalogOverride(catalogRaw, sigFieldNo),
                },
            };

            // Gom theo TRANG: một trang có bao nhiêu khối thì /Annots của nó nhận bấy nhiêu, và phải nối dồn
            // vào cùng một bản dict — ghi đè trang hai lần thì lần sau xoá mất khối của lần trước.
            foreach (var group in plan.Annotations.GroupBy(a => a.PageObjectNumber))
            {
                var body = _revisionReader.GetObjectBody(revision, group.Key)
                    ?? throw new UserFriendlyException(ErrorCodes.PdfStructureUnsupported,
                        "Không đọc được trang cần đặt khối chữ ký.");

                foreach (var annotation in group)
                {
                    body = _objectWriter.AppendToArray(body, "Annots", annotation.Number)
                        ?? throw new UserFriendlyException(ErrorCodes.PdfStructureUnsupported,
                            "/Annots của trang là tham chiếu gián tiếp — dạng này chưa hỗ trợ ký nối bản.");
                }

                objects.Add(new PdfAppearanceObjectDto { Number = group.Key, DictText = body });
            }

            objects.AddRange(BuildSignatureField(chuKyWidgets, sigFieldNo, sigValueNo));

            foreach (var annotation in annotationsKhac)
            {
                objects.Add(new PdfAppearanceObjectDto
                {
                    Number = annotation.Number,
                    DictText = BuildStampAnnotation(annotation),
                });
            }

            objects.AddRange(plan.AppearanceObjects);
            objects.Add(new PdfAppearanceObjectDto
            {
                Number = sigValueNo,
                DictText = BuildSignatureValue(options),
            });

            return objects;
        }

        /// <inheritdoc/>
        public List<PdfAppearanceObjectDto> BuildSignatureField(IReadOnlyList<PdfAnnotationDto> chuKyWidgets,
            int sigFieldNo, int sigValueNo)
        {
            var result = new List<PdfAppearanceObjectDto>();
            var builder = new StringBuilder();

            if (chuKyWidgets.Count == 1)
            {
                var only = chuKyWidgets[0];
                var dict = new PdfDict()
                    .Add("Type", new PdfName("Annot"))
                    .Add("Subtype", new PdfName("Widget"))
                    .Add("FT", new PdfName("Sig"))
                    .Add("Ff", 0)
                    .Add("T", new PdfStr(SigningConstants.SignatureFieldNamePrefix + sigFieldNo))
                    .Add("V", new PdfRef(sigValueNo))
                    .Add("P", new PdfRef(only.PageObjectNumber))
                    .Add("F", 4)
                    .Add("Rect", new object[] { only.Rect.X, only.Rect.Y, only.Rect.Right, only.Rect.Top });

                if (only.AppearanceObjectNumber is int form)
                {
                    dict.Add("AP", new PdfDict().Add("N", new PdfRef(form)));
                }

                _objectWriter.WriteValue(builder, dict);
                result.Add(new PdfAppearanceObjectDto { Number = sigFieldNo, DictText = builder.ToString() });
                return result;
            }

            _objectWriter.WriteValue(builder, new PdfDict()
                .Add("FT", new PdfName("Sig"))
                .Add("Ff", 0)
                .Add("T", new PdfStr(SigningConstants.SignatureFieldNamePrefix + sigFieldNo))
                .Add("V", new PdfRef(sigValueNo))
                .Add("Kids", chuKyWidgets.Select(w => new PdfRef(w.Number)).ToArray()));
            result.Add(new PdfAppearanceObjectDto { Number = sigFieldNo, DictText = builder.ToString() });

            foreach (var widget in chuKyWidgets)
            {
                var widgetBuilder = new StringBuilder();
                var dict = new PdfDict()
                    .Add("Type", new PdfName("Annot"))
                    .Add("Subtype", new PdfName("Widget"))
                    .Add("Parent", new PdfRef(sigFieldNo))
                    .Add("P", new PdfRef(widget.PageObjectNumber))
                    .Add("F", 4)
                    .Add("Rect", new object[] { widget.Rect.X, widget.Rect.Y, widget.Rect.Right, widget.Rect.Top });

                if (widget.AppearanceObjectNumber is int form)
                {
                    dict.Add("AP", new PdfDict().Add("N", new PdfRef(form)));
                }

                _objectWriter.WriteValue(widgetBuilder, dict);
                result.Add(new PdfAppearanceObjectDto
                {
                    Number = widget.Number,
                    DictText = widgetBuilder.ToString(),
                });
            }

            return result;
        }

        /// <inheritdoc/>
        public string BuildStampAnnotation(PdfAnnotationDto annotation)
        {
            var builder = new StringBuilder();
            var dict = new PdfDict()
                .Add("Type", new PdfName("Annot"))
                .Add("Subtype", new PdfName("Stamp"))
                .Add("P", new PdfRef(annotation.PageObjectNumber))
                .Add("F", 4)
                .Add("Rect", new object[]
                {
                    annotation.Rect.X, annotation.Rect.Y, annotation.Rect.Right, annotation.Rect.Top,
                });

            if (annotation.AppearanceObjectNumber is int form)
            {
                dict.Add("AP", new PdfDict().Add("N", new PdfRef(form)));
            }

            _objectWriter.WriteValue(builder, dict);
            return builder.ToString();
        }

        /// <inheritdoc/>
        public string BuildSignatureValue(PdfPrepareOptionsDto options)
        {
            var builder = new StringBuilder();
            builder.Append("<</Type /Sig/Filter /Adobe.PPKLite/SubFilter /")
                .Append(SigningConstants.PdfSubFilter);

            builder.Append("/Name ");
            _objectWriter.WriteString(builder, options.TenNguoiKy);
            builder.Append("/Reason ");
            _objectWriter.WriteString(builder, options.LyDoKy ?? SigningConstants.DefaultReason);
            builder.Append("/Location ");
            _objectWriter.WriteString(builder, options.NoiKy ?? SigningConstants.DefaultLocation);

            // /M là giờ ký theo định dạng ngày tháng của PDF. SignedAt là giờ Việt Nam nên phần bù múi giờ
            // phải lấy TƯỜNG MINH từ hằng số; lấy offset của MÁY sẽ ghi sai khi server đặt múi giờ khác.
            builder.Append("/M (D:")
                .Append(options.SignedAt.ToString(SigningConstants.PdfDateFormat, CultureInfo.InvariantCulture))
                .Append('+')
                .Append(SigningConstants.VietnamUtcOffsetHours.ToString("00", CultureInfo.InvariantCulture))
                .Append("'00')");

            builder.Append(BuildByteRangePlaceholder());
            builder.Append("/Contents <")
                .Append(new string('0', SigningConstants.SignaturePlaceholderBytes * 2)).Append('>');
            builder.Append(">>");

            return builder.ToString();
        }

        /// <inheritdoc/>
        public string BuildByteRangePlaceholder()
        {
            var o = new string(' ', SigningConstants.ByteRangeFieldWidth);
            return "/ByteRange [" + string.Join(" ", Enumerable.Repeat(o, 4)) + "]";
        }

        /// <inheritdoc/>
        public PdfPreparedDto WriteRevision(byte[] pdf, PdfRevisionDto revision,
            IReadOnlyList<PdfAppearanceObjectDto> objects, int xrefNo)
        {
            var builder = new StringBuilder();
            var offsets = new Dictionary<int, long>();
            var baseLength = pdf.Length;

            // File gốc phải kết thúc bằng xuống dòng trước khi nối, nếu không "%%EOF" cũ dính vào object mới.
            if (!(pdf[^1] == '\n' || pdf[^1] == '\r')) builder.Append('\n');

            foreach (var obj in objects)
            {
                // Offset tính từ đầu FILE. builder.Length dùng thẳng làm số byte được vì mọi ký tự nối vào đều
                // dưới 256 và cuối cùng ghi ra bằng Latin1 — một ký tự thành đúng một byte.
                offsets[obj.Number] = baseLength + builder.Length;
                builder.Append(obj.Number.ToString(CultureInfo.InvariantCulture)).Append(" 0 obj\n")
                    .Append(obj.DictText);

                if (obj.StreamData != null)
                {
                    builder.Append("\nstream\n").Append(Encoding.Latin1.GetString(obj.StreamData))
                        .Append("\nendstream");
                }

                builder.Append("\nendobj\n");
            }

            var xrefOffset = baseLength + builder.Length;
            if (revision.LastXrefIsStream)
            {
                // Luồng xref LÀ một object nên phải tự khai chính mình trong bảng.
                offsets[xrefNo] = xrefOffset;
                var newSize = Math.Max(revision.Size, offsets.Keys.Max() + 1);
                builder.Append(BuildXrefStream(revision, xrefNo, offsets, newSize));
            }
            else
            {
                // Bảng xref cổ điển KHÔNG phải object nên không có mục cho chính nó.
                var newSize = Math.Max(revision.Size, offsets.Keys.Max() + 1);
                builder.Append(BuildXrefTable(revision, offsets, newSize, xrefOffset));
            }

            var appended = Encoding.Latin1.GetBytes(builder.ToString());
            var output = new byte[baseLength + appended.Length];
            Buffer.BlockCopy(pdf, 0, output, 0, baseLength);
            Buffer.BlockCopy(appended, 0, output, baseLength, appended.Length);

            return PatchByteRange(output, baseLength);
        }

        /// <inheritdoc/>
        public PdfPreparedDto PatchByteRange(byte[] output, int baseLength)
        {
            var text = Encoding.Latin1.GetString(output);

            // Dò NGƯỢC từ cuối file nên không bao giờ vớ phải chữ ký đã có sẵn trong bản cũ.
            var contentsAt = text.LastIndexOf("/Contents <", StringComparison.Ordinal);
            var byteRangeAt = text.LastIndexOf("/ByteRange [", StringComparison.Ordinal);
            if (contentsAt < baseLength || byteRangeAt < baseLength)
            {
                throw new UserFriendlyException(ErrorCodes.PdfPrepareFailed,
                    "Không định vị được chỗ trống chữ ký trong bản ký nối vừa dựng.");
            }

            var hexLength = SigningConstants.SignaturePlaceholderBytes * 2;
            var gapStart = contentsAt + "/Contents ".Length;
            var gapEnd = gapStart + 1 + hexLength + 1;

            var ranges = new long[] { 0, gapStart, gapEnd, output.Length - gapEnd };
            var realByteRange = "/ByteRange [" + string.Join(" ", ranges.Select(v =>
                v.ToString(CultureInfo.InvariantCulture).PadRight(SigningConstants.ByteRangeFieldWidth))) + "]";

            var placeholderLength = BuildByteRangePlaceholder().Length;
            if (realByteRange.Length != placeholderLength)
            {
                throw new UserFriendlyException(ErrorCodes.PdfPrepareFailed,
                    $"/ByteRange thật dài {realByteRange.Length} byte, khác chỗ trống {placeholderLength} byte.");
            }

            Encoding.Latin1.GetBytes(realByteRange).CopyTo(output, byteRangeAt);

            var noiDungKy = new byte[ranges[1] + ranges[3]];
            Buffer.BlockCopy(output, 0, noiDungKy, 0, (int)ranges[1]);
            Buffer.BlockCopy(output, (int)ranges[2], noiDungKy, (int)ranges[1], (int)ranges[3]);

            return new PdfPreparedDto
            {
                Bytes = output,
                ByteRange = ranges,
                NoiDungKy = noiDungKy,
                ContentsHexStart = gapStart + 1,
                ContentsHexLength = hexLength,
            };
        }

        /// <inheritdoc/>
        public PdfAnnotationPlanDto PlanAnnotations(PdfRevisionDto revision, IReadOnlyList<int> pageOrder,
            int pagesObjectNumber, PdfPrepareOptionsDto options, int nextObjectNumber)
        {
            var plan = new PdfAnnotationPlanDto { NextObjectNumber = nextObjectNumber };

            // Khối chữ ký không ghi phần bù múi giờ: giờ vẽ ra luôn là giờ VN nên "+07:00" chỉ làm khối dài
            // thêm mà không nói thêm được gì cho người đọc trong nước.
            var signedAtText = options.SignedAt
                .ToString(SigningConstants.AppearanceTimeFormat, CultureInfo.InvariantCulture);

            foreach (var placement in options.ViTri)
            {
                var veKhoiChu = placement.Kind == TemplatePositionKind.ChuKy;
                byte[]? anh = null;
                bool laChuKy;

                switch (placement.Kind)
                {
                    case TemplatePositionKind.ChuKy:
                        if (!options.HienThiChuKySo) continue;
                        laChuKy = true;
                        break;

                    case TemplatePositionKind.ChuKyTuoi:
                        if (options.AnhChuKyTuoi == null || options.AnhChuKyTuoi.Length == 0) continue;
                        anh = options.AnhChuKyTuoi;
                        laChuKy = options.NhoiChuKySoVaoAnh;
                        break;

                    // Con dấu đã được vẽ sẵn vào trang từ bước dựng giấy báo nên luồng ký KHÔNG vẽ lại: vẽ
                    // thêm là thành hai con dấu trên một tờ. Chỉ khi template chọn nhồi chữ ký số vào ảnh mới
                    // đặt lên đó một widget trong suốt để bấm vào dấu là ra bảng thông tin chữ ký.
                    case TemplatePositionKind.DauDo:
                        if (!options.NhoiChuKySoVaoAnh) continue;
                        laChuKy = true;
                        break;

                    default:
                        continue;
                }

                var pageIndex = Math.Clamp(placement.PageNumber - 1, 0, pageOrder.Count - 1);
                var pageNo = pageOrder[pageIndex];
                var pageRaw = _revisionReader.GetObjectBody(revision, pageNo)
                    ?? throw new UserFriendlyException(ErrorCodes.PdfStructureUnsupported,
                        $"Không đọc được trang {pageIndex + 1} của file PDF.");

                var pageBox = ReadPageBox(revision, pageRaw, pagesObjectNumber);
                var rect = ToPoints(placement, pageBox);
                if (veKhoiChu) rect = ApplyChuKyMinSize(rect, pageBox);
                if (anh != null) rect = ApplyChuKyTuoiFit(rect, pageBox, anh);
                rect = ClampToPage(rect, pageBox);

                if (rect.Width <= 0 || rect.Height <= 0) continue;

                var appearance = veKhoiChu
                    ? _appearanceBuilder.BuildText(options.TenNguoiKy, signedAtText, plan.NextObjectNumber,
                        rect.Width, rect.Height, options.MauChuKySo)
                    : anh != null
                        ? _appearanceBuilder.BuildImage(anh, options.DoDamChuKyTuoi,
                            options.DoDayNetChuKyTuoi, plan.NextObjectNumber, rect.Width, rect.Height,
                            options.MauChuKyTuoi)
                        : _appearanceBuilder.Transplant(
                            _appearanceBuilder.Draw(rect.Width, rect.Height, (_, _) => { }),
                            plan.NextObjectNumber, rect.Width, rect.Height);

                plan.AppearanceObjects.AddRange(appearance.Objects);
                plan.NextObjectNumber = appearance.Objects.Max(o => o.Number) + 1;

                plan.Annotations.Add(new PdfAnnotationDto
                {
                    PageObjectNumber = pageNo,
                    Rect = rect,
                    AppearanceObjectNumber = appearance.FormObjectNumber,
                    LaChuKy = laChuKy,
                });
            }

            // Template tắt hết phần hiển thị vẫn phải có một widget chữ ký, nếu không thì không có gì để gắn
            // giá trị chữ ký vào. Ô rỗng và không có mặt chữ ký chính là cách khai chữ ký VÔ HÌNH theo chuẩn.
            if (!plan.Annotations.Any(a => a.LaChuKy))
            {
                plan.Annotations.Add(new PdfAnnotationDto
                {
                    PageObjectNumber = pageOrder[0],
                    Rect = new PdfRectPointsDto(),
                    LaChuKy = true,
                });
            }

            return plan;
        }

        /// <inheritdoc/>
        public List<int> ReadPageOrder(PdfRevisionDto revision, int pagesObjectNumber)
        {
            var order = new List<int>();
            var stack = new Stack<int>();
            var visited = new HashSet<int>();
            stack.Push(pagesObjectNumber);

            while (stack.Count > 0)
            {
                var node = stack.Pop();

                // Cây trang hỏng có thể trỏ vòng lại chính nó và làm duyệt mãi không dừng.
                if (!visited.Add(node)) continue;

                var body = _revisionReader.GetObjectBody(revision, node);
                if (body == null) continue;

                // Có /Kids là node trung gian; không có là trang thật.
                var kidsMatch = Regex.Match(body, @"/Kids\s*\[(.*?)\]", RegexOptions.Singleline);
                if (!kidsMatch.Success)
                {
                    order.Add(node);
                    continue;
                }

                var kids = Regex.Matches(kidsMatch.Groups[1].Value, @"(\d+)\s+\d+\s+R")
                    .Select(m => int.Parse(m.Groups[1].Value)).ToList();
                for (var i = kids.Count - 1; i >= 0; i--) stack.Push(kids[i]);
            }

            return order;
        }

        /// <inheritdoc/>
        public PdfRectPointsDto ReadPageBox(PdfRevisionDto revision, string pageRaw, int pagesObjectNumber)
        {
            var boxMatch = Regex.Match(pageRaw, @"/MediaBox\s*\[([^\]]*)\]");
            if (!boxMatch.Success)
            {
                var parent = _revisionReader.GetObjectBody(revision, pagesObjectNumber) ?? string.Empty;
                boxMatch = Regex.Match(parent, @"/MediaBox\s*\[([^\]]*)\]");
            }

            var box = boxMatch.Success
                ? boxMatch.Groups[1].Value
                    .Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => double.Parse(v, CultureInfo.InvariantCulture)).ToArray()
                : Array.Empty<double>();

            if (box.Length != 4)
            {
                box = new[]
                {
                    0d, 0d,
                    SigningConstants.AppearanceReferencePageWidth,
                    SigningConstants.AppearanceReferencePageHeight,
                };
            }

            // /MediaBox có thể có gốc khác 0 và thứ tự toạ độ đảo nên phải chuẩn hoá.
            return new PdfRectPointsDto
            {
                X = Math.Min(box[0], box[2]),
                Y = Math.Min(box[1], box[3]),
                Width = Math.Abs(box[2] - box[0]),
                Height = Math.Abs(box[3] - box[1]),
            };
        }

        /// <inheritdoc/>
        public PdfRectPointsDto ToPoints(PdfPlacementDto placement, PdfRectPointsDto pageBox)
        {
            var width = placement.WidthRatio * pageBox.Width;
            var height = placement.HeightRatio * pageBox.Height;
            var distanceFromTop = placement.YRatio * pageBox.Height;

            return new PdfRectPointsDto
            {
                X = pageBox.X + placement.XRatio * pageBox.Width,
                Y = pageBox.Y + pageBox.Height - distanceFromTop - height,
                Width = width,
                Height = height,
            };
        }

        /// <inheritdoc/>
        public PdfRectPointsDto ApplyChuKyMinSize(PdfRectPointsDto rect, PdfRectPointsDto pageBox)
        {
            // Nhân theo khổ trang thật so với khổ tham chiếu để trang A3 không bị chữ ký teo bằng con tem còn
            // trang nhỏ hơn A4 thì không bị tràn.
            var scale = Math.Min(
                pageBox.Width / SigningConstants.AppearanceReferencePageWidth,
                pageBox.Height / SigningConstants.AppearanceReferencePageHeight);

            var nominalWidth = SigningConstants.AppearanceWidth * scale;
            var nominalHeight = SigningConstants.AppearanceHeight * scale;

            var width = rect.Width <= 0
                ? nominalWidth
                : Math.Max(rect.Width, nominalWidth * SigningConstants.AppearanceMinScale);
            var height = rect.Height <= 0
                ? nominalHeight
                : Math.Max(rect.Height, nominalHeight * SigningConstants.AppearanceMinScale);

            // Mép TRÊN của ô là chỗ người dùng thả khối nên giữ nguyên; phần cao thêm mọc xuống dưới.
            return new PdfRectPointsDto
            {
                X = rect.X,
                Y = rect.Top - height,
                Width = width,
                Height = height,
            };
        }

        /// <inheritdoc/>
        public PdfRectPointsDto ApplyChuKyTuoiFit(PdfRectPointsDto rect, PdfRectPointsDto pageBox, byte[] anh)
        {
            var kichThuoc = _imageSizeReader.Read(anh);
            if (kichThuoc.WidthPoints <= 0 || kichThuoc.HeightPoints <= 0 || rect.Width <= 0 || rect.Height <= 0)
            {
                return rect;
            }

            var tiLeAnh = kichThuoc.WidthPoints / kichThuoc.HeightPoints;

            // Lồng khít ảnh vào trong ô người dùng đặt: bám chiều nào chật hơn thì chiều kia co theo, nhờ đó
            // ảnh không bao giờ tràn khỏi ô mà cũng không bị kéo giãn.
            var width = rect.Width;
            var height = width / tiLeAnh;
            if (height > rect.Height)
            {
                height = rect.Height;
                width = height * tiLeAnh;
            }

            var tranBeRong = pageBox.Width * SealPlacementConstants.MaxChuKyTuoiWidthRatio;
            if (width > tranBeRong)
            {
                width = tranBeRong;
                height = width / tiLeAnh;
            }

            // Giữ nguyên TÂM ô người dùng đã đặt: thu nhỏ quanh tâm thì chữ ký vẫn nằm đúng chỗ được chọn.
            return new PdfRectPointsDto
            {
                X = rect.X + (rect.Width - width) / 2,
                Y = rect.Y + (rect.Height - height) / 2,
                Width = width,
                Height = height,
            };
        }

        /// <inheritdoc/>
        public PdfRectPointsDto ClampToPage(PdfRectPointsDto rect, PdfRectPointsDto pageBox)
        {
            var margin = SigningConstants.AppearancePageMargin
                * Math.Min(pageBox.Width / SigningConstants.AppearanceReferencePageWidth,
                    pageBox.Height / SigningConstants.AppearanceReferencePageHeight);

            var x = rect.X;
            var y = rect.Y;

            if (rect.Width + 2 * margin >= pageBox.Width)
            {
                x = pageBox.X + (pageBox.Width - rect.Width) / 2;
            }
            else
            {
                x = Math.Clamp(x, pageBox.X + margin, pageBox.X + pageBox.Width - margin - rect.Width);
            }

            if (rect.Height + 2 * margin >= pageBox.Height)
            {
                y = pageBox.Y + (pageBox.Height - rect.Height) / 2;
            }
            else
            {
                y = Math.Clamp(y, pageBox.Y + margin, pageBox.Y + pageBox.Height - margin - rect.Height);
            }

            return new PdfRectPointsDto { X = x, Y = y, Width = rect.Width, Height = rect.Height };
        }

        /// <inheritdoc/>
        public string BuildCatalogOverride(string catalogRaw, int signatureFieldNumber)
        {
            var at = catalogRaw.IndexOf("/AcroForm", StringComparison.Ordinal);
            if (at < 0)
            {
                // /SigFlags 3 nghĩa là tài liệu có chữ ký và chỉ được sửa bằng cách nối bản; thiếu cờ này thì
                // trình đọc có thể lưu đè cả tài liệu và giết chữ ký.
                var close = catalogRaw.LastIndexOf(">>", StringComparison.Ordinal);
                if (close < 0)
                {
                    throw new UserFriendlyException(ErrorCodes.PdfStructureUnsupported,
                        "Catalog của file PDF không hợp lệ.");
                }

                return catalogRaw[..close]
                    + "/AcroForm<</Fields[" + signatureFieldNumber + " 0 R]/SigFlags 3/DA(/Helv 0 Tf 0 g )>>"
                    + catalogRaw[close..];
            }

            var p = at + "/AcroForm".Length;
            while (p < catalogRaw.Length && char.IsWhiteSpace(catalogRaw[p])) p++;
            if (p + 1 >= catalogRaw.Length || catalogRaw[p] != '<' || catalogRaw[p + 1] != '<')
            {
                throw new UserFriendlyException(ErrorCodes.PdfStructureUnsupported,
                    "/AcroForm là tham chiếu gián tiếp — dạng này chưa hỗ trợ ký nối bản.");
            }

            var acroForm = _revisionReader.ExtractDictionary(catalogRaw, p);
            var updated = _objectWriter.AppendToArray(acroForm, "Fields", signatureFieldNumber)
                ?? throw new UserFriendlyException(ErrorCodes.PdfStructureUnsupported,
                    "/Fields của AcroForm là tham chiếu gián tiếp — dạng này chưa hỗ trợ ký nối bản.");

            if (!Regex.IsMatch(updated, @"/SigFlags\s+\d"))
            {
                var close = updated.LastIndexOf(">>", StringComparison.Ordinal);
                updated = updated[..close] + "/SigFlags 3" + updated[close..];
            }

            return catalogRaw[..p] + updated + catalogRaw[(p + acroForm.Length)..];
        }

        /// <inheritdoc/>
        public string BuildXrefStream(PdfRevisionDto revision, int xrefObjectNumber,
            IReadOnlyDictionary<int, long> offsets, int newSize)
        {
            var numbers = offsets.Keys.OrderBy(n => n).ToList();
            var index = new List<int>();
            var data = new List<byte>();
            var i = 0;

            while (i < numbers.Count)
            {
                var start = numbers[i];
                var count = 1;
                while (i + count < numbers.Count && numbers[i + count] == start + count) count++;

                index.Add(start);
                index.Add(count);

                for (var k = 0; k < count; k++)
                {
                    var offset = offsets[start + k];
                    data.Add(1);
                    data.Add((byte)((offset >> 24) & 0xFF));
                    data.Add((byte)((offset >> 16) & 0xFF));
                    data.Add((byte)((offset >> 8) & 0xFF));
                    data.Add((byte)(offset & 0xFF));
                    data.Add(0);
                    data.Add(0);
                }

                i += count;
            }

            var builder = new StringBuilder();
            builder.Append(xrefObjectNumber.ToString(CultureInfo.InvariantCulture)).Append(" 0 obj\n");
            builder.Append("<</Type /XRef/Size ").Append(newSize);
            builder.Append("/Index [").Append(string.Join(" ", index)).Append(']');
            builder.Append("/W [1 4 2]");
            builder.Append("/Root ").Append(revision.RootObjectNumber).Append(" 0 R");
            if (revision.InfoRaw != null) builder.Append("/Info ").Append(revision.InfoRaw);
            if (revision.IdRaw != null) builder.Append("/ID ").Append(revision.IdRaw);
            builder.Append("/Prev ").Append(revision.LastXrefOffset);
            builder.Append("/Length ").Append(data.Count).Append(">>\n");
            builder.Append("stream\n").Append(Encoding.Latin1.GetString(data.ToArray()))
                .Append("\nendstream\nendobj\n");
            builder.Append("startxref\n").Append(offsets[xrefObjectNumber]).Append("\n%%EOF\n");

            return builder.ToString();
        }

        /// <inheritdoc/>
        public string BuildXrefTable(PdfRevisionDto revision, IReadOnlyDictionary<int, long> offsets,
            int newSize, long xrefStart)
        {
            var numbers = offsets.Keys.OrderBy(n => n).ToList();
            var builder = new StringBuilder();
            builder.Append("xref\n");
            var i = 0;

            while (i < numbers.Count)
            {
                var start = numbers[i];
                var count = 1;
                while (i + count < numbers.Count && numbers[i + count] == start + count) count++;

                builder.Append(start).Append(' ').Append(count).Append('\n');
                for (var k = 0; k < count; k++)
                {
                    // Mỗi dòng đúng 20 byte: "nnnnnnnnnn ggggg n\r\n".
                    builder.Append(offsets[start + k].ToString("D10", CultureInfo.InvariantCulture))
                        .Append(" 00000 n\r\n");
                }

                i += count;
            }

            builder.Append("trailer\n<</Size ").Append(newSize);
            builder.Append("/Root ").Append(revision.RootObjectNumber).Append(" 0 R");
            if (revision.InfoRaw != null) builder.Append("/Info ").Append(revision.InfoRaw);
            if (revision.IdRaw != null) builder.Append("/ID ").Append(revision.IdRaw);
            builder.Append("/Prev ").Append(revision.LastXrefOffset).Append(">>\n");
            builder.Append("startxref\n").Append(xrefStart).Append("\n%%EOF\n");

            return builder.ToString();
        }
    }
}
