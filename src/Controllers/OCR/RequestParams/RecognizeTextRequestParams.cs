using Vintasoft.Imaging.Ocr;
using Vintasoft.Imaging.Ocr.Tesseract;
using Vintasoft.Shared.Web;

namespace AspNetCoreOcrDemo.Controllers
{
    /// <summary>
    /// Request to a web controller.
    /// Contains parameters, which must be sent to a web controller for text recognition in images.
    /// </summary>
    public class RecognizeTextRequestParams : WebImageFileRequestParams
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="RecognizeTextRequestParams"/> class.
        /// </summary>
        public RecognizeTextRequestParams()
            : base()
        {
        }



        WebOcrPageInfo[] _ocrPageInfos;
        /// <summary>
        /// Gets or sets information about pages, where text must be recognized.
        /// </summary>
        public WebOcrPageInfo[] ocrPageInfos
        {
            get { return _ocrPageInfos; }
            set { _ocrPageInfos = value; }
        }

        string[] _languages = new string[] { "English" };
        /// <summary>
        /// Gets or sets languages, which should be used for text recognition.
        /// </summary>
        public string[] languages
        {
            get { return _languages; }
            set { _languages = value; }
        }

        RecognitionRegionType _recognitionRegionType = RecognitionRegionType.RecognizePageWithPageSegmentationAndOrientationDetection;
        /// <summary>
        /// Gets or sets the type of the recognition region.
        /// </summary>
        public RecognitionRegionType recognitionRegionType
        {
            get { return _recognitionRegionType; }
            set { _recognitionRegionType = value; }
        }

        OcrBinarizationMode _binarization = OcrBinarizationMode.Adaptive;
        /// <summary>
        /// Gets or sets a binarization, which must be applied to the source image before text recognition.
        /// </summary>
        public OcrBinarizationMode binarization
        {
            get { return _binarization; }
            set { _binarization = value; }
        }

        TesseractOcrRecognitionMode _recognitionMode = TesseractOcrRecognitionMode.Quality;
        /// <summary>
        /// Gets or sets the recognition mode of Tesseract OCR.
        /// </summary>
        public TesseractOcrRecognitionMode recognitionMode
        {
            get { return _recognitionMode; }
            set { _recognitionMode = value; }
        }

        OcrResultFileFormat _resultFileFormat = OcrResultFileFormat.Text;
        /// <summary>
        /// Gets or sets the format of file, where text recognition results must be saved.
        /// </summary>
        public OcrResultFileFormat resultFileFormat
        {
            get { return _resultFileFormat; }
            set { _resultFileFormat = value; }
        }

        string _resultFileName = "ocrResult";
        /// <summary>
        /// Gets or sets the name of file, where text recognition results must be saved.
        /// </summary>
        public string resultFileName
        {
            get { return _resultFileName; }
            set { _resultFileName = value; }
        }

    }
}
