namespace AspNetCoreOcrDemo.Controllers
{
    /// <summary>
    /// Contains information about OCR status.
    /// </summary>
    public class OcrStatus
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="OcrStatus"/> class.
        /// </summary>
        public OcrStatus()
        {
        }



        string _description = string.Empty;
        /// <summary>
        /// Gets or sets a string that describes the state of the text recognition process.
        /// </summary>
        public string description
        {
            get { return _description; }
            set { _description = value; }
        }

        string _errorMessage;
        /// <summary>
        /// Gets or sets a string that describes an error occurred during text recognition.
        /// </summary>
        public string errorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; }
        }

        int _progress = 0;
        /// <summary>
        /// Gets or sets the progress, in percents, of synchronous text recognition.
        /// </summary>
        public int progress
        {
            get { return _progress; }
            set { _progress = value; }
        }

        int _pageCount = 0;
        /// <summary>
        /// Gets or sets the total number of pages, where text must be recognized.
        /// </summary>
        public int pageCount
        {
            get { return _pageCount; }
            set { _pageCount = value; }
        }

        int _currentPageIndex = -1;
        /// <summary>
        /// Gets or sets a zero-based index of page, where text is recognizing now.
        /// </summary>
        public int currentPageIndex
        {
            get { return _currentPageIndex; }
            set { _currentPageIndex = value; }
        }

        string _pathToResults;
        /// <summary>
        /// Gets or sets the relative path to a file with recognition results.
        /// </summary>
        public string pathToResults
        {
            get { return _pathToResults; }
            set { _pathToResults = value; }
        }

        string _recognizedText;
        /// <summary>
        /// Gets or sets the recognized text if recognition result is represented by text file.
        /// </summary>
        public string recognizedText
        {
            get { return _recognizedText; }
            set { _recognizedText = value; }
        }

    }
}