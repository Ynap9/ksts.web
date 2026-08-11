using ksts.be.external.Pdf.Dtos;
using ksts.be.external.Pdf.Interfaces;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace ksts.be.external.Pdf.Implements
{
    /// <inheritdoc/>
    public class PdfRevisionReader : IPdfRevisionReader
    {
        /// <inheritdoc/>
        public PdfRevisionDto Load(byte[] bytes)
        {
            if (bytes.Length < 32 || bytes[0] != '%' || bytes[1] != 'P')
            {
                throw new UserFriendlyException(ErrorCodes.PdfStructureUnsupported,
                    "File không phải PDF hợp lệ (thiếu header %PDF).");
            }

            var tailStart = Math.Max(0, bytes.Length - 2048);
            var tail = Encoding.Latin1.GetString(bytes, tailStart, bytes.Length - tailStart);
            var startxref = Regex.Matches(tail, @"startxref\s+(\d+)");
            if (startxref.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.PdfStructureUnsupported,
                    "Không tìm thấy startxref ở cuối file PDF.");
            }

            var lastXref = long.Parse(startxref[^1].Groups[1].Value);
            if (lastXref <= 0 || lastXref >= bytes.Length)
            {
                throw new UserFriendlyException(ErrorCodes.PdfStructureUnsupported,
                    $"startxref trỏ ra ngoài file ({lastXref}/{bytes.Length}).");
            }

            // Dựng chuỗi Latin1 MỘT lần: mỗi byte thành đúng một ký tự nên chỉ số ký tự == chỉ số byte, mà
            // file thật vài MB thì gọi GetString mỗi lần đọc object là chép lại từng đó bộ nhớ cho mỗi object.
            var revision = new PdfRevisionDto
            {
                Bytes = bytes,
                Text = Encoding.Latin1.GetString(bytes),
                LastXrefOffset = lastXref,
            };

            // Đi ngược chuỗi /Prev. daDoc chặn file lỗi trỏ vòng tròn làm lặp vô hạn.
            var daDoc = new HashSet<long>();
            long? offset = lastXref;
            string? trailerDauTien = null;

            while (offset is long cur && daDoc.Add(cur))
            {
                if (cur < 0 || cur >= bytes.Length) break;

                // Bảng cổ điển mở đầu bằng từ khoá "xref"; luồng là "N 0 obj <<...>> stream".
                var probe = Encoding.Latin1
                    .GetString(bytes, (int)cur, Math.Min(20, bytes.Length - (int)cur)).TrimStart();
                var laBang = probe.StartsWith("xref", StringComparison.Ordinal);
                var trailer = laBang ? ReadClassicTable(revision, cur) : ReadXrefStream(revision, cur);

                if (trailerDauTien == null)
                {
                    trailerDauTien = trailer;
                    revision.LastXrefIsStream = !laBang;
                }

                var prev = Regex.Match(trailer, @"/Prev\s+(\d+)");
                offset = prev.Success ? long.Parse(prev.Groups[1].Value) : null;
            }

            if (trailerDauTien == null)
            {
                throw new UserFriendlyException(ErrorCodes.PdfStructureUnsupported,
                    "Không đọc được trailer của file PDF.");
            }

            // Ký nối bản vào file mã hoá đòi mã hoá cả object mới bằng đúng khoá của file — ngoài phạm vi.
            // Fail-closed: thà trượt file còn hơn phát hành file hỏng.
            if (Regex.IsMatch(trailerDauTien, @"/Encrypt\s"))
            {
                throw new UserFriendlyException(ErrorCodes.PdfEncrypted,
                    "File PDF được mã hoá (/Encrypt) nên không ký số nối bản được.");
            }

            var root = Regex.Match(trailerDauTien, @"/Root\s+(\d+)\s+\d+\s+R");
            var size = Regex.Match(trailerDauTien, @"/Size\s+(\d+)");
            if (!root.Success || !size.Success)
            {
                throw new UserFriendlyException(ErrorCodes.PdfStructureUnsupported,
                    "Trailer PDF thiếu /Root hoặc /Size.");
            }

            var id = Regex.Match(trailerDauTien, @"/ID\s*(\[[^\]]*\])");
            var infoRef = Regex.Match(trailerDauTien, @"/Info\s+(\d+\s+\d+\s+R)");

            revision.RootObjectNumber = int.Parse(root.Groups[1].Value);
            revision.Size = int.Parse(size.Groups[1].Value);
            revision.IdRaw = id.Success ? id.Groups[1].Value : null;
            revision.InfoRaw = infoRef.Success ? infoRef.Groups[1].Value : null;

            // /DocMDP với /P 1 nghĩa là tác giả cấm mọi thay đổi tiếp; ký thêm là cố tình phá chữ ký chứng
            // thực đó. Dict chữ ký theo chuẩn không bao giờ nằm trong object stream nên quét byte thô là đủ.
            foreach (Match match in Regex.Matches(revision.Text, @"/DocMDP"))
            {
                var window = revision.Text.Substring(match.Index,
                    Math.Min(400, revision.Text.Length - match.Index));
                var p = Regex.Match(window, @"/P\s+(\d+)");
                if (p.Success && p.Groups[1].Value == "1")
                {
                    throw new UserFriendlyException(ErrorCodes.PdfLocked,
                        "File PDF có chữ ký chứng thực cấm mọi thay đổi (/DocMDP /P 1) nên không ký thêm được.");
                }
            }

            return revision;
        }

        /// <inheritdoc/>
        public string ReadClassicTable(PdfRevisionDto revision, long offset)
        {
            var text = revision.Text;
            var pos = (int)offset;
            var keyword = Regex.Match(text[pos..Math.Min(text.Length, pos + 20)], @"\s*xref\s*");
            pos += keyword.Length;

            while (true)
            {
                var head = Regex.Match(text[pos..Math.Min(text.Length, pos + 40)], @"^\s*(\d+)\s+(\d+)\s*");
                if (!head.Success) break;

                var start = int.Parse(head.Groups[1].Value);
                var count = int.Parse(head.Groups[2].Value);
                pos += head.Length;

                for (var i = 0; i < count; i++)
                {
                    var line = text.Substring(pos, Math.Min(20, text.Length - pos));
                    var entry = Regex.Match(line, @"(\d{10})\s+(\d{5})\s+([nf])");

                    // Đi NGƯỢC chuỗi xref nên bản gặp trước là bản mới hơn -> chỉ ghi khi số hiệu chưa có.
                    if (entry.Success && entry.Groups[3].Value == "n"
                        && !revision.Objects.ContainsKey(start + i))
                    {
                        revision.Objects[start + i] = new PdfObjectLocationDto
                        {
                            Type = 1,
                            Value = long.Parse(entry.Groups[1].Value),
                        };
                    }

                    pos += 20;
                }
            }

            var trailerAt = text.IndexOf("trailer", pos, StringComparison.Ordinal);
            if (trailerAt < 0) return string.Empty;

            var dictAt = text.IndexOf("<<", trailerAt, StringComparison.Ordinal);
            return dictAt < 0 ? string.Empty : ExtractDictionary(text, dictAt);
        }

        /// <inheritdoc/>
        public string ReadXrefStream(PdfRevisionDto revision, long offset)
        {
            var text = revision.Text;
            var dictAt = text.IndexOf("<<", (int)offset, StringComparison.Ordinal);
            if (dictAt < 0 || dictAt > offset + 60) return string.Empty;

            var dict = ExtractDictionary(text, dictAt);
            var data = ReadStreamData(revision, dictAt + dict.Length, dict);
            if (data == null) return dict;

            var widthMatch = Regex.Match(dict, @"/W\s*\[([^\]]*)\]");
            if (!widthMatch.Success) return dict;

            var widths = widthMatch.Groups[1].Value
                .Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
            var rowWidth = widths.Sum();
            if (rowWidth == 0) return dict;

            var sizeMatch = Regex.Match(dict, @"/Size\s+(\d+)");
            var size = sizeMatch.Success ? int.Parse(sizeMatch.Groups[1].Value) : 0;

            // /Index liệt kê các đoạn [số hiệu đầu, số lượng]; không khai thì mặc định là [0 Size].
            var indexMatch = Regex.Match(dict, @"/Index\s*\[([^\]]*)\]");
            var index = indexMatch.Success
                ? indexMatch.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse).ToArray()
                : new[] { 0, size };

            var pos = 0;
            for (var k = 0; k + 1 < index.Length; k += 2)
            {
                for (var n = index[k]; n < index[k] + index[k + 1]; n++)
                {
                    if (pos + rowWidth > data.Length) return dict;

                    var fields = new long[3];
                    var cursor = pos;
                    for (var c = 0; c < widths.Length && c < 3; c++)
                    {
                        long value = 0;
                        for (var b = 0; b < widths[c]; b++) value = (value << 8) | data[cursor++];
                        fields[c] = value;
                    }
                    pos += rowWidth;

                    // W[0] == 0 nghĩa là cột type bị lược bớt -> theo chuẩn thì mặc định là type 1.
                    var type = widths[0] == 0 ? 1 : (int)fields[0];
                    if (type != 1 && type != 2) continue;
                    if (revision.Objects.ContainsKey(n)) continue;

                    revision.Objects[n] = new PdfObjectLocationDto
                    {
                        Type = type,
                        Value = fields[1],
                        IndexInStream = (int)fields[2],
                    };
                }
            }

            return dict;
        }

        /// <inheritdoc/>
        public string? GetObjectBody(PdfRevisionDto revision, int number)
        {
            if (!revision.Objects.TryGetValue(number, out var location)) return null;
            var text = revision.Text;

            if (location.Type == 1)
            {
                if (location.Value < 0 || location.Value >= revision.Bytes.Length) return null;

                var objAt = text.IndexOf("obj", (int)location.Value, StringComparison.Ordinal);
                if (objAt < 0 || objAt > location.Value + 40) return null;

                var p = objAt + 3;
                while (p < text.Length && char.IsWhiteSpace(text[p])) p++;
                if (p >= text.Length) return null;

                // Dict thì cắt theo cặp << >>; không cắt tới endobj được vì thân stream nằm chen vào giữa.
                if (text[p] == '<' && p + 1 < text.Length && text[p + 1] == '<')
                {
                    return ExtractDictionary(text, p);
                }

                // Object không phải dict cũng phải đọc được: /Widths của font là một MẢNG object riêng, bỏ sót
                // nó thì tham chiếu tới nó không được đánh số lại và sẽ trỏ bậy sau khi cấy sang tài liệu đích.
                var end = text.IndexOf("endobj", p, StringComparison.Ordinal);
                return end < 0 ? null : text[p..end].Trim();
            }

            if (!revision.Objects.TryGetValue((int)location.Value, out var streamLocation)
                || streamLocation.Type != 1)
            {
                return null;
            }

            var streamDictAt = text.IndexOf("<<", (int)streamLocation.Value, StringComparison.Ordinal);
            if (streamDictAt < 0) return null;

            var streamDict = ExtractDictionary(text, streamDictAt);
            var raw = ReadStreamData(revision, streamDictAt + streamDict.Length, streamDict);
            if (raw == null) return null;

            var countMatch = Regex.Match(streamDict, @"/N\s+(\d+)");
            var firstMatch = Regex.Match(streamDict, @"/First\s+(\d+)");
            if (!countMatch.Success || !firstMatch.Success) return null;

            var count = int.Parse(countMatch.Groups[1].Value);
            var first = int.Parse(firstMatch.Groups[1].Value);
            if (first > raw.Length) return null;

            // Phần đầu object stream là các cặp "số hiệu offset", offset tính từ /First.
            var header = Encoding.Latin1.GetString(raw, 0, first)
                .Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var body = Encoding.Latin1.GetString(raw);

            for (var i = 0; i + 1 < header.Length && i / 2 < count; i += 2)
            {
                if (int.Parse(header[i]) != number) continue;

                var start = first + int.Parse(header[i + 1]);
                var end = i + 3 < header.Length ? first + int.Parse(header[i + 3]) : raw.Length;
                if (start < 0 || end > raw.Length || start >= end) return null;

                var slice = body[start..end];
                var d = slice.IndexOf("<<", StringComparison.Ordinal);
                return d >= 0 ? ExtractDictionary(slice, d) : slice.Trim();
            }

            return null;
        }

        /// <inheritdoc/>
        public byte[]? GetRawStreamBytes(PdfRevisionDto revision, int number)
        {
            if (!revision.Objects.TryGetValue(number, out var location) || location.Type != 1) return null;
            var text = revision.Text;

            // Neo theo từ khoá "obj" của CHÍNH object này: bắt "<<" tự do có thể vớ phải dict của object kế
            // tiếp khi object này không phải dict.
            var objAt = text.IndexOf("obj", (int)location.Value, StringComparison.Ordinal);
            if (objAt < 0 || objAt > location.Value + 40) return null;

            var dictAt = objAt + 3;
            while (dictAt < text.Length && char.IsWhiteSpace(text[dictAt])) dictAt++;
            if (dictAt + 1 >= text.Length || text[dictAt] != '<' || text[dictAt + 1] != '<') return null;

            var dict = ExtractDictionary(text, dictAt);
            var streamAt = text.IndexOf("stream", dictAt + dict.Length, StringComparison.Ordinal);

            // "stream" phải nằm ngay sau dict, chỉ cách bởi khoảng trắng; xa hơn là đã sang object khác.
            if (streamAt < 0 || streamAt > dictAt + dict.Length + 20) return null;

            var p = streamAt + 6;
            if (p + 1 < text.Length && text[p] == '\r' && text[p + 1] == '\n') p += 2;
            else if (p < text.Length && (text[p] == '\n' || text[p] == '\r')) p += 1;

            var lengthMatch = Regex.Match(dict, @"/Length\s+(\d+)");
            int length;
            if (lengthMatch.Success)
            {
                length = int.Parse(lengthMatch.Groups[1].Value);
            }
            else
            {
                // /Length gián tiếp: dò tới "endstream". Chấp nhận được vì đường này chỉ chạy trên tài liệu
                // khối chữ ký do CHÍNH TA sinh ra, không phải file của người dùng.
                var endAt = text.IndexOf("endstream", p, StringComparison.Ordinal);
                if (endAt < 0) return null;

                length = endAt - p;
                while (length > 0 && (text[p + length - 1] == '\n' || text[p + length - 1] == '\r')) length--;
            }

            if (p + length > revision.Bytes.Length) return null;

            var raw = new byte[length];
            Array.Copy(revision.Bytes, p, raw, 0, length);
            return raw;
        }

        /// <inheritdoc/>
        public byte[]? ReadStreamData(PdfRevisionDto revision, int afterDict, string dict)
        {
            var text = revision.Text;
            var streamAt = text.IndexOf("stream", afterDict, StringComparison.Ordinal);
            if (streamAt < 0) return null;

            // Sau từ khoá stream là CRLF hoặc LF; chuẩn cấm chỉ CR nhưng file thật vẫn có.
            var p = streamAt + 6;
            if (p + 1 < text.Length && text[p] == '\r' && text[p + 1] == '\n') p += 2;
            else if (p < text.Length && (text[p] == '\n' || text[p] == '\r')) p += 1;

            var lengthMatch = Regex.Match(dict, @"/Length\s+(\d+)");
            if (!lengthMatch.Success) return null;

            var length = int.Parse(lengthMatch.Groups[1].Value);
            if (p + length > revision.Bytes.Length) return null;

            var raw = new byte[length];
            Array.Copy(revision.Bytes, p, raw, 0, length);

            if (dict.Contains("/FlateDecode", StringComparison.Ordinal))
            {
                try
                {
                    using var input = new MemoryStream(raw);
                    using var zlib = new ZLibStream(input, CompressionMode.Decompress);
                    using var output = new MemoryStream();
                    zlib.CopyTo(output);
                    raw = output.ToArray();
                }
                catch
                {
                    return null;
                }
            }
            else if (Regex.IsMatch(dict, @"/Filter\s*/?\w"))
            {
                return null;
            }

            var predictor = Regex.Match(dict, @"/Predictor\s+(\d+)");
            if (predictor.Success && int.Parse(predictor.Groups[1].Value) >= 10)
            {
                var columnMatch = Regex.Match(dict, @"/Columns\s+(\d+)");
                raw = RemovePngPredictor(raw, columnMatch.Success ? int.Parse(columnMatch.Groups[1].Value) : 1);
            }

            return raw;
        }

        /// <inheritdoc/>
        public byte[] RemovePngPredictor(byte[] raw, int columns)
        {
            if (columns <= 0) return raw;

            var output = new List<byte>(raw.Length);
            var previous = new byte[columns];
            var q = 0;

            while (q + 1 + columns <= raw.Length)
            {
                var filter = raw[q++];
                var row = new byte[columns];
                Array.Copy(raw, q, row, 0, columns);
                q += columns;

                for (var i = 0; i < columns; i++)
                {
                    row[i] = filter switch
                    {
                        1 => (byte)(row[i] + (i > 0 ? row[i - 1] : 0)),
                        2 => (byte)(row[i] + previous[i]),
                        _ => row[i],
                    };
                }

                output.AddRange(row);
                previous = row;
            }

            return output.ToArray();
        }

        /// <inheritdoc/>
        public string ExtractDictionary(string text, int start)
        {
            var depth = 0;
            var i = start;

            while (i < text.Length - 1)
            {
                var c = text[i];
                if (c == '(')
                {
                    var level = 1;
                    i++;
                    while (i < text.Length && level > 0)
                    {
                        if (text[i] == '\\') { i += 2; continue; }
                        if (text[i] == '(') level++;
                        else if (text[i] == ')') level--;
                        i++;
                    }
                    continue;
                }

                if (c == '<' && text[i + 1] == '<') { depth++; i += 2; continue; }
                if (c == '>' && text[i + 1] == '>')
                {
                    depth--;
                    i += 2;
                    if (depth == 0) return text[start..i];
                    continue;
                }

                i++;
            }

            return text[start..];
        }
    }
}
