using Vintasoft.Shared.Web;

namespace AspNetCoreOcrDemo.Controllers
{
    /// <summary>
    /// Response from web controller.
    /// Contains information about supported OCR languages.
    /// </summary>
    public class GetSupportedOcrLanguagesResponseParams : WebResponseParamsBase
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="GetSupportedOcrLanguagesResponseParams"/> class.
        /// </summary>
        public GetSupportedOcrLanguagesResponseParams()
            : base()
        {
        }



        string[] _languages;
        /// <summary>
        /// Gets or sets an array of supported OCR languages.
        /// </summary>
        public string[] languages
        {
            get { return _languages; }
            set { _languages = value; }
        }

    }
}