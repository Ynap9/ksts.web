using ksts.be.external.Pdf.Interfaces;
using ksts.be.shared.Constants.Signing;
using System.Text;
using System.Text.RegularExpressions;

namespace ksts.be.external.Pdf.Implements
{
    /// <inheritdoc/>
    public class PdfSignatureInspector : IPdfSignatureInspector
    {
        /// <inheritdoc/>
        public bool HasSignature(byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                return false;
            }

            // Latin1 keeps one byte per char, so the scan never splits a marker in half. A signature
            // dictionary may never live inside an object stream (PDF 32000 §12.8.1), so plain text search
            // over the raw file is enough — no need to walk the xref chain just to answer yes or no.
            var text = Encoding.Latin1.GetString(bytes);
            return Regex.IsMatch(text, SigningConstants.SignatureValueMarker);
        }
    }
}
