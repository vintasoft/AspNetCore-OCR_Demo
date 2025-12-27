namespace AspNetCoreOcrDemo.Controllers
{
    /// <summary>
    /// Specifies available modes of image binarization.
    /// </summary>
    public enum OcrBinarizationMode : int
    {

        /// <summary>
        /// No binarization.
        /// </summary>
        None = 0,

        /// <summary>
        /// Binarize an image using Otsu algorithm.
        /// </summary>
        Global = 1,

        /// <summary>
        /// Binarize an image using adaptive local-window algorithm.
        /// </summary>
        Adaptive = 2,

    }
}