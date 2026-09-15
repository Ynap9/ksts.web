using kssm.be.external.DongGoi.Interfaces;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace kssm.be.external.DongGoi.Implements
{
    public class TextSimilarity : ITextSimilarity
    {
        private static readonly Regex NgoacDon = new(@"\([^)]*\)", RegexOptions.Compiled);
        private static readonly Regex KhoangTrang = new(@"\s+", RegexOptions.Compiled);

        public string Normalize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var boNgoac = NgoacDon.Replace(text, " ");
            var phanRa = boNgoac.Replace('Đ', 'D').Replace('đ', 'd').Normalize(NormalizationForm.FormD);
            var giuLai = new StringBuilder(phanRa.Length);

            foreach (var kyTu in phanRa)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(kyTu) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                giuLai.Append(char.IsLetterOrDigit(kyTu) ? char.ToLowerInvariant(kyTu) : ' ');
            }

            return KhoangTrang.Replace(giuLai.ToString(), " ").Trim();
        }

        public double Score(string? left, string? right)
        {
            var a = Normalize(left);
            var b = Normalize(right);

            if (a.Length == 0 || b.Length == 0)
            {
                return a.Length == b.Length ? 1d : 0d;
            }

            if (string.Equals(a, b, StringComparison.Ordinal))
            {
                return 1d;
            }

            return Math.Max(TyLeLevenshtein(a, b), TyLeDice(a, b));
        }

        public double TyLeLevenshtein(string a, string b)
        {
            var khoangCach = KhoangCachLevenshtein(a, b);
            var daiNhat = Math.Max(a.Length, b.Length);
            return daiNhat == 0 ? 1d : 1d - (double)khoangCach / daiNhat;
        }

        public int KhoangCachLevenshtein(string a, string b)
        {
            var truoc = new int[b.Length + 1];
            var hienTai = new int[b.Length + 1];

            for (var j = 0; j <= b.Length; j++)
            {
                truoc[j] = j;
            }

            for (var i = 1; i <= a.Length; i++)
            {
                hienTai[0] = i;

                for (var j = 1; j <= b.Length; j++)
                {
                    var thay = a[i - 1] == b[j - 1] ? 0 : 1;
                    hienTai[j] = Math.Min(Math.Min(hienTai[j - 1] + 1, truoc[j] + 1), truoc[j - 1] + thay);
                }

                (truoc, hienTai) = (hienTai, truoc);
            }

            return truoc[b.Length];
        }

        public double TyLeDice(string a, string b)
        {
            var tuA = a.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var tuB = b.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (tuA.Length == 0 || tuB.Length == 0)
            {
                return 0d;
            }

            var conLai = tuB.ToList();
            var chung = 0;

            foreach (var tu in tuA)
            {
                var viTri = conLai.IndexOf(tu);
                if (viTri >= 0)
                {
                    conLai.RemoveAt(viTri);
                    chung++;
                }
            }

            return 2d * chung / (tuA.Length + tuB.Length);
        }
    }
}
