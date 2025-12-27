namespace AspNetCoreOcrDemo.Controllers
{
    /// <summary>
    /// Specifies available formats for OCR result file.
    /// </summary>
    public enum OcrResultFileFormat : int
    {
        /// <summary>
        /// Save OCR result in a text document.
        /// </summary>
        Text = 0,

        /// <summary>
        /// Save OCR result in a formatted text document.
        /// </summary>
        FormattedText = 1,

        /// <summary>
        /// Save OCR result (text and source images) in a PDF document.
        /// </summary>
        PDF = 2,
    }
}