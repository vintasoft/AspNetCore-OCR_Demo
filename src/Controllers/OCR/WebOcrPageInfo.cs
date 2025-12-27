using Vintasoft.Imaging.ImageProcessing.DocCleanup.Web.Services;
using Vintasoft.Shared.Web;

namespace AspNetCoreOcrDemo.Controllers
{
    /// <summary>
    /// Contains information about OCR page.
    /// </summary>
    public class WebOcrPageInfo
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="WebOcrPageInfo"/> class.
        /// </summary>
        public WebOcrPageInfo()
        {
            _imageInfo = new WebImageInfo();
        }



        WebImageInfo _imageInfo;
        /// <summary>
        /// Gets or sets the image info.
        /// </summary>
        public WebImageInfo imageInfo
        {
            get { return _imageInfo; }
            set { _imageInfo = value; }
        }

        WebImageRegion[] _imageRegions;
        /// <summary>
        /// Gets or sets image regions for text recognition.
        /// </summary>
        public WebImageRegion[] imageRegions
        {
            get { return _imageRegions; }
            set { _imageRegions = value; }
        }

    }
}
