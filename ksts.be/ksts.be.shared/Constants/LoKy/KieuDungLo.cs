namespace ksts.be.shared.Constants.LoKy
{
    /// <summary>
    /// Why a running batch was stopped. The background runner is what settles the final batch state, and it
    /// finishes AFTER the service has written to the database — without this it would overwrite a pause with
    /// a cancel.
    /// </summary>
    public enum KieuDungLo
    {
        /// <summary>Paused by the user: source files stay, the next start resumes from the next file.</summary>
        TamDung = 0,

        /// <summary>Cancelled for good: signed files stay on the store, source files get cleaned up.</summary>
        Huy = 1,
    }
}
