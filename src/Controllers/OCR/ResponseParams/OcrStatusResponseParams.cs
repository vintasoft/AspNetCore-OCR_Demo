using Vintasoft.Shared.Web;

namespace AspNetCoreOcrDemo.Controllers
{
    /// <summary>
    /// Response from web controller.
    /// Contains information about OCR status.
    /// </summary>
    public class OcrStatusResponseParams : WebResponseParamsBase
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="OcrStatusResponseParams"/> class.
        /// </summary>
        public OcrStatusResponseParams()
            : base()
        {
        }



        OcrStatus _status;
        /// <summary>
        /// Gets or sets an OCR status.
        /// </summary>
        public OcrStatus status
        {
            get { return _status; }
            set { _status = value; }
        }

    }
}