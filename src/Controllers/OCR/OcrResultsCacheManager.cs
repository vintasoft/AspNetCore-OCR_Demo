using System.IO;

using Vintasoft.Data;
using Vintasoft.Imaging;
using Vintasoft.Imaging.Ocr.Results;
using Vintasoft.Imaging.Pdf;
using Vintasoft.Imaging.Pdf.Ocr;
using Vintasoft.Imaging.Web.Services;

namespace AspNetCoreOcrDemo.Controllers
{
    /// <summary>
    /// Manages cache of OCR results in web application.
    /// </summary>
    public class OcrResultsCacheManager : CacheManagerBase
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="OcrResultsCacheManager"/> class.
        /// </summary>
        /// <param name="workingDirectory">An absolute path to the working directory
        /// of cache manager.</param>
        /// <exception cref="ArgumentNullException">Thrown if
        /// <i>workingDirectory</i> is <b>null</b> or empty.
        /// </exception>
        /// <exception cref="FormatException">Thrown if 
        /// <i>workingDirectory</i> contains invalid characters.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">Thrown if
        /// <i>workingDirectory</i> does not exist.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown if storage
        ///  created by <see cref="CreateSessionDataStorage"/> cannot 
        ///  store <see cref="Stream"/> object.</exception>
        public OcrResultsCacheManager(string workingDirectory)
            : base(workingDirectory)
        {
        }

        #endregion



        #region Methods

        /// <summary>
        /// Returns a file name with "txt" extension for the specified file name.
        /// </summary>
        /// <param name="filename">A file name.</param>
        /// <returns>A file name with "txt" extension.</returns>
        public string GetOcrResultTxtId(string filename)
        {
            return string.Format("{0}.txt", Path.GetFileNameWithoutExtension(filename));
        }

        /// <summary>
        /// Gets a file name with "pdf" extension for the specified file name.
        /// </summary>
        /// <param name="filename">A file name.</param>
        /// <returns>A file name with "pdf" extension.</returns>
        public string GetOcrResultPDfId(string filename)
        {
            return string.Format("{0}.pdf", Path.GetFileNameWithoutExtension(filename));
        }

        /// <summary>
        /// Saves OCR result to a PDF document.
        /// </summary>
        /// <param name="resultFieName">A result file name.</param>
        /// <param name="images">Images, where text was recognized.</param>
        /// <param name="ocrResults">An array with recognition results.</param>
        /// <returns>A relative path to the result file.</returns>
        public string SaveOcrResultInPdfDocument(string resultFieName, VintasoftImage[] images, OcrPage[] ocrResults)
        {
            string filename = GetOcrResultPDfId(resultFieName);
            Stream pdfDocStream = null;
            try
            {
                // lock file in data storage
                pdfDocStream = (Stream)DataStorage.LockItem(filename);
                // create new PDF document
                using (PdfDocument document = new PdfDocument(Vintasoft.Imaging.Pdf.PdfFormat.Pdf_14))
                {
                    // create a PDF document builder that helps to create searchable PDF document
                    PdfDocumentBuilder documentBuilder = new PdfDocumentBuilder(document);
                    // specify that images must be placed over text
                    documentBuilder.PageCreationMode = PdfPageCreationMode.ImageOverText;

                    // for each image
                    for (int i = 0; i < images.Length; i++)
                    {
                        VintasoftImage image = images[i];
                        // if image is vector image (docx, pdf,...)
                        if (image.IsVectorImage)
                        {
                            // rasterize image
                            image.RenderingSettings.Resolution = ocrResults[i].Resolution;
                            image = (VintasoftImage)image.Clone();
                        }

                        // add PDF page with OCR result to the PDF document
                        documentBuilder.AddPage(image, ocrResults[i]);

                        if (images[i] != image)
                            image.Dispose();
                    }

                    // save PDF document
                    document.Save(pdfDocStream);
                }
            }
            finally
            {
                if (pdfDocStream != null)
                    // unlock file in data storage
                    DataStorage.UnlockItem(filename, pdfDocStream);
            }
            // return a relative path to the result file
            return Path.Combine(WorkingDirectory, filename);
        }

        /// <summary>
        /// Saves OCR result as a text file.
        /// </summary>
        /// <param name="resultFileName">A result file name.</param>
        /// <param name="ocrResults">An array with recognition results.</param>
        /// <param name="formatted">A value indicating whether the recognition result must be saved with formatting.</param>
        /// <param name="pagesDelimiter">A delimiter between page results.</param>
        /// <returns>A relative path to the result file.</returns>
        public string SaveOcrResultAsTextFile(string resultFileName, OcrPage[] ocrResults, bool formatted, string pagesDelimiter)
        {
            string filename = GetOcrResultTxtId(resultFileName);
            Stream txtFileStream = null;
            try
            {
                // lock file in data storage
                txtFileStream = (Stream)DataStorage.LockItem(filename);
                // create stream writer
                using (StreamWriter sw = new StreamWriter(txtFileStream, System.Text.Encoding.UTF8))
                {
                    // for each OCR result
                    for (int i = 0; i < ocrResults.Length; i++)
                    {
                        string pageText = string.Empty;
                        if (formatted)
                            // get OCR result as formatted text
                            pageText = ocrResults[i].GetFormattedText();
                        else
                            // get OCR result as NOT formatted text
                            pageText = ocrResults[i].GetText();

                        // add OCR result to the strean writer
                        sw.WriteLine(pageText);
                        // if we are processing NOT the last OCR result
                        if (i != (ocrResults.Length - 1))
                            // add the delimiter betwen OCR results
                            sw.WriteLine(pagesDelimiter);
                    }
                }
            }
            finally
            {
                if (txtFileStream != null)
                    // unlock file in data storage
                    DataStorage.UnlockItem(filename, txtFileStream);
            }
            // return a relative path to the result file
            return Path.Combine(WorkingDirectory, filename);
        }

        /// <summary>
        /// Returns an absolute path to a file, which stores the OCR results.
        /// </summary>
        /// <param name="resultId">An item identifier.</param>
        /// <returns>An absolute path to a file, which stores the OCR results.</returns>
        public override string GetItemPath(string resultId)
        {
            return Path.Combine(WorkingDirectory, resultId);
        }

        /// <summary>
        /// Clears the cache.
        /// </summary>
        /// <param name="resultId">Identifier of OCR result.</param>
        public override void ClearCache(string resultId)
        {
            if (DataStorage.Contains(resultId))
                DataStorage.DeleteItem(resultId);
        }

        /// <summary>
        /// Deletes old cache.
        /// </summary>
        /// <seealso cref="CacheManagerBase.CacheLifeTime"/>
        public override void DeleteOldCache()
        {
            RemoveExpiredFiles(WorkingDirectory, CacheLifeTime);
        }


        /// <summary>
        /// Creates a data storage for current cache manager.
        /// </summary>
        /// <param name="path">An absolute path to the directory,
        /// where storage must store files.</param>
        /// <returns>The data storage.</returns>
        protected override IDataStorage CreateSessionDataStorage(string path)
        {
            return new StreamDataStorage(path);
        }

        #endregion

    }
}