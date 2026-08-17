namespace ksts.be.external.Pdf.Interfaces
{
    /// <summary>
    /// Tells whether a PDF already carries a digital signature, so the batch can refuse to sign it again
    /// unless the template explicitly allows signing over an existing signature.
    /// </summary>
    public interface IPdfSignatureInspector
    {
        /// <summary>Returns true when the file contains at least one filled signature dictionary.</summary>
        bool HasSignature(byte[] bytes);
    }
}
