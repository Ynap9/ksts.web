using ksts.be.external.Pdf.Dtos;
using ksts.be.external.Pdf.Interfaces;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using System.Collections;
using System.Globalization;
using System.Text;

namespace ksts.be.external.Pdf.Implements
{
    /// <inheritdoc/>
    public class PdfObjectWriter : IPdfObjectWriter
    {
        /// <inheritdoc/>
        public void WriteObject(StringBuilder builder, int number, object? value)
        {
            builder.Append(number.ToString(CultureInfo.InvariantCulture)).Append(" 0 obj\n");
            WriteValue(builder, value);
            builder.Append("\nendobj\n");
        }

        /// <inheritdoc/>
        public void WriteValue(StringBuilder builder, object? value)
        {
            switch (value)
            {
                case null:
                    builder.Append("null");
                    break;

                case PdfRaw raw:
                    builder.Append(raw.Text);
                    break;

                case PdfName name:
                    builder.Append('/');
                    foreach (var c in name.Value)
                    {
                        // Chuẩn: ngoài khoảng in được 0x21-0x7E và các ký tự phân tách thì phải viết dạng #xx.
                        if (c is > (char)0x20 and < (char)0x7F && "()<>[]{}/%#".IndexOf(c) < 0)
                        {
                            builder.Append(c);
                        }
                        else
                        {
                            builder.Append('#').Append(((int)c).ToString("X2", CultureInfo.InvariantCulture));
                        }
                    }
                    break;

                case bool flag:
                    builder.Append(flag ? "true" : "false");
                    break;

                case int number:
                    builder.Append(number.ToString(CultureInfo.InvariantCulture));
                    break;

                case long number:
                    builder.Append(number.ToString(CultureInfo.InvariantCulture));
                    break;

                case double number:
                    // InvariantCulture bắt buộc: máy đặt locale VN sẽ ghi "594,72" và file PDF thành rác.
                    builder.Append(number.ToString("0.####", CultureInfo.InvariantCulture));
                    break;

                case PdfRef reference:
                    builder.Append(reference.Number.ToString(CultureInfo.InvariantCulture)).Append(" 0 R");
                    break;

                case PdfStr text:
                    WriteString(builder, text.Value);
                    break;

                case PdfDict dict:
                    builder.Append("<<");
                    foreach (var item in dict.Items)
                    {
                        WriteValue(builder, new PdfName(item.Key));
                        builder.Append(' ');
                        WriteValue(builder, item.Value);
                    }
                    builder.Append(">>");
                    break;

                case IEnumerable list when value is not string:
                    builder.Append('[');
                    var first = true;
                    foreach (var item in list)
                    {
                        if (!first) builder.Append(' ');
                        WriteValue(builder, item);
                        first = false;
                    }
                    builder.Append(']');
                    break;

                default:
                    throw new UserFriendlyException(ErrorCodes.PdfPrepareFailed,
                        $"Không ghi được giá trị PDF kiểu {value.GetType().Name}.");
            }
        }

        /// <inheritdoc/>
        public void WriteString(StringBuilder builder, string value)
        {
            if (value.All(c => c is >= (char)0x20 and < (char)0x7F))
            {
                builder.Append('(');
                foreach (var c in value)
                {
                    // Ba ký tự này có nghĩa cú pháp bên trong chuỗi literal nên phải escape.
                    if (c is '(' or ')' or '\\') builder.Append('\\');
                    builder.Append(c);
                }
                builder.Append(')');
                return;
            }

            builder.Append('<').Append("FEFF");
            foreach (var b in Encoding.BigEndianUnicode.GetBytes(value))
            {
                builder.Append(b.ToString("X2", CultureInfo.InvariantCulture));
            }
            builder.Append('>');
        }

        /// <inheritdoc/>
        public string? AppendToArray(string dictRaw, string key, int newObjectNumber)
        {
            var marker = "/" + key;
            var at = -1;
            var from = 0;

            while (true)
            {
                var idx = dictRaw.IndexOf(marker, from, StringComparison.Ordinal);
                if (idx < 0) break;

                // Phải là RANH GIỚI token: "/Annots" không được khớp nhầm vào "/AnnotsExtra".
                var after = idx + marker.Length;
                if (after >= dictRaw.Length
                    || dictRaw[after] is ' ' or '\r' or '\n' or '\t' or '[' or '/' or '<')
                {
                    at = idx;
                    break;
                }

                from = idx + 1;
            }

            if (at < 0)
            {
                var close = dictRaw.LastIndexOf(">>", StringComparison.Ordinal);
                if (close < 0) return null;
                return dictRaw[..close] + marker + "[" + newObjectNumber + " 0 R]" + dictRaw[close..];
            }

            var p = at + marker.Length;
            while (p < dictRaw.Length && char.IsWhiteSpace(dictRaw[p])) p++;
            if (p >= dictRaw.Length || dictRaw[p] != '[') return null;

            // Mảng /Fields và /Annots chỉ chứa tham chiếu nên không lồng nhau, vẫn đếm độ sâu để không cắt
            // nhầm nếu gặp file khác thường.
            var depth = 0;
            var q = p;
            while (q < dictRaw.Length)
            {
                if (dictRaw[q] == '[') depth++;
                else if (dictRaw[q] == ']')
                {
                    depth--;
                    if (depth == 0) break;
                }
                q++;
            }

            if (q >= dictRaw.Length) return null;

            return dictRaw[..q] + " " + newObjectNumber + " 0 R" + dictRaw[q..];
        }
    }
}
