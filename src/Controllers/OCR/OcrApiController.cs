using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Vintasoft.Data;
using Vintasoft.Imaging;
using Vintasoft.Imaging.ImageProcessing;
using Vintasoft.Imaging.ImageProcessing.DocCleanup.Web.Services;
using Vintasoft.Imaging.Ocr;
using Vintasoft.Imaging.Ocr.Results;
using Vintasoft.Imaging.Ocr.Tesseract;
using Vintasoft.Shared.Web;

namespace AspNetCoreOcrDemo.Controllers
{
    /// <summary>
    /// A Web API controller
    /// that handles HTTP requests from clients and allows to recognize text in images.
    /// </summary>
    [Route("vintasoft/api/[controller]/[action]")]
    [ApiController]
    public class OcrApiController : ControllerBase
    {

        #region Constants

        /// <summary>
        /// The count of maximum allowable active OCR processes.
        /// </summary>
        const int MAX_ACTIVE_OCR_PROCESSES_COUNT = 1;

        #endregion



        #region Fields

        /// <summary>
        /// Dictionary: session ID => active OCR status.
        /// </summary>
        static Dictionary<string, OcrStatus> _sessionToRecognitionStatus = new Dictionary<string, OcrStatus>();

        /// <summary>
        /// Dictionary: active OCR engine => session ID.
        /// </summary>
        static Dictionary<OcrEngineManager, string> _ocrEngineToSessionId = new Dictionary<OcrEngineManager, string>();

        /// <summary>
        /// Dictionary: session ID => path base of current request.
        /// </summary>
        static Dictionary<string, string> _sessionToRequestPathBase = new Dictionary<string, string>();

        /// <summary>
        /// Object that provides synchronous access to the _sessionToRecognitionStatus and _ocrEngineToSessionId dictionaries.
        /// </summary>
        static object _lockObject = new object();

        #endregion



        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="OcrApiController"/> class.
        /// </summary>
        /// <param name="hostingEnvironment">Information about the web hosting environment an application is running in.</param>
        public OcrApiController(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        #endregion



        #region Properties

        string _tesseractOcrDllDirectory = null;
        /// <summary>
        /// Gets a directory where Tesseract5.Vintasoft.xXX.dll is located.
        /// </summary>
        public string TesseractOcrDllDirectory
        {
            get
            {
                if (_tesseractOcrDllDirectory == null)
                {
                    // Tesseract OCR dll filename
                    string dllFilename;
                    if (Vintasoft.Imaging.ImagingEnvironment.IsWindows)
                    {
                        // if is 64-bit system then
                        if (IntPtr.Size == 8)
                            dllFilename = "Tesseract5.Vintasoft.x64.dll";
                        else
                            dllFilename = "Tesseract5.Vintasoft.x86.dll";
                    }
                    else
                    {
                        dllFilename = "libTesseract5.Vintasoft.x64.so";
                    }

                    string contentRootPath = HostingEnvironment.ContentRootPath;
                    string appDataDirectory = Path.Combine(contentRootPath, "App_Data");
                    string dllDirectory = Path.Combine(appDataDirectory, "TesseractOCR");
                    string tesseractDllPath = Path.Combine(dllDirectory, dllFilename);
                    if (System.IO.File.Exists(tesseractDllPath))
                    {
                        _tesseractOcrDllDirectory = dllDirectory;
                    }
                    if (_tesseractOcrDllDirectory == null)
                        _tesseractOcrDllDirectory = contentRootPath;
                    else
                        _tesseractOcrDllDirectory = Path.GetFullPath(_tesseractOcrDllDirectory);
                }
                return _tesseractOcrDllDirectory;
            }
        }

        IWebHostEnvironment _hostingEnvironment;
        /// <summary>
        /// Gets an information about the web hosting environment an application is running in.
        /// </summary>
        public IWebHostEnvironment HostingEnvironment
        {
            get { return _hostingEnvironment; }
        }

        #endregion



        #region Methods

        #region PUBLIC

        /// <summary>
        /// Returns information about supported OCR languages.
        /// </summary>
        /// <returns>The response that contains information about supported OCR languages.</returns>
        [HttpPost]
        public GetSupportedOcrLanguagesResponseParams GetSupportedLanguages()
        {
            GetSupportedOcrLanguagesResponseParams answer = new GetSupportedOcrLanguagesResponseParams();
            TesseractOcr tesseractOcrEngine = null;
            try
            {
                // create Tesseract OCR engine
                tesseractOcrEngine = new TesseractOcr(TesseractOcrDllDirectory);

                // create OCR engine manager
                OcrEngineManager ocrEngineManager = new OcrEngineManager(tesseractOcrEngine);
                // get languages, which are supported by Tesseract OCR engine
                OcrLanguage[] ocrLanguages = ocrEngineManager.SupportedLanguages;
                // if languages are not found
                if (ocrLanguages.Length == 0)
                    throw new Exception("Languages are not found.");

                // create an array with supported languages
                string[] languages = new string[ocrLanguages.Length];
                for (int i = 0; i < languages.Length; i++)
                    languages[i] = ocrLanguages[i].ToString();

                // create HTTP answer with information about supported languages
                answer.languages = languages;
                answer.success = true;
            }
            catch (Exception e)
            {
                answer.success = false;
                answer.errorMessage = GetFullExceptionMessage(e);
            }
            finally
            {
                if (tesseractOcrEngine != null)
                    // dispose the Tesseract OCR engine
                    tesseractOcrEngine.Dispose();
            }
            return answer;
        }

        /// <summary>
        /// Starts the text recognition in the specified image file.
        /// </summary>
        /// <param name="requestParams">The OCR settings and information about image file.</param>
        /// <returns>The response that contains the status of text recognition process.</returns>
        [HttpPost]
        public WebResponseParamsBase RecognizeText([FromBody] RecognizeTextRequestParams requestParams)
        {
            WebResponseParamsBase answer = new WebResponseParamsBase();
            string sessionId = requestParams.sessionId;

            Dictionary<string, Stream> openedFiles = new Dictionary<string, Stream>();
            try
            {
                // if page info is not specified
                if (requestParams.ocrPageInfos == null)
                {
                    throw new ArgumentNullException("The OCR page infos cannot be null.");
                }
                // if file name is not specified
                if (requestParams.resultFileName == null)
                {
                    throw new ArgumentNullException("The OCR result filename cannot be null.");
                }
                if (requestParams.resultFileName == String.Empty)
                {
                    throw new ArgumentNullException("The OCR result filename cannot be empty.");
                }
                // get an array containing the characters, which are not allowed in file name
                char[] notAllowedFileNameChars = Path.GetInvalidFileNameChars();
                // if file name contains not allowed characters
                if (requestParams.resultFileName.IndexOfAny(notAllowedFileNameChars) != -1)
                    throw new FormatException(string.Format("The OCR result file name contains characters, which are not allowed in file name. Please do not use the following characters in file name: '{0}'.", new string(notAllowedFileNameChars)));
                // get an array containing the characters, which are not filtered by default configuration of IIS server
                char[] notAllowedIisChars = new char[] { '+' };
                // if file name contains not allowed characters
                if (requestParams.resultFileName.IndexOfAny(notAllowedIisChars) != -1)
                {
                    throw new FormatException(string.Format("The OCR result file name contains characters, which are filtered by default configuration of IIS server. Please do not use the following characters in file name: '{0}'.", new string(notAllowedIisChars)));
                }

                if (requestParams.languages == null || requestParams.languages.Length == 0)
                    throw new ArgumentException("The OCR languages are not specified.");
                if (requestParams.languages.Length > 5)
                    throw new ArgumentException(string.Format("You are trying to use {0} OCR languages for text recognition. Please use maximum 5 OCR languages for text recognition (this is limitation of this demo).", requestParams.languages.Length));



                // create a data storage for current session
                IDataStorage storage = CreateSessionDataStorage(sessionId);

                WebOcrPageInfo[] ocrPageInfos = requestParams.ocrPageInfos;
                // create an image collection
                using (ImageCollection images = new ImageCollection())
                {
                    try
                    {
                        for (int i = 0; i < ocrPageInfos.Length; i++)
                        {
                            WebOcrPageInfo ocrPageInfo = ocrPageInfos[i];
                            WebImageInfo imageInfo = ocrPageInfo.imageInfo;
                            WebImageFileInfo fileInfo = imageInfo.fileInfo;


                            string fileId = fileInfo.id;

                            Stream stream = null;
                            if (!openedFiles.ContainsKey(fileId))
                            {
                                // get a file stream from the data storage
                                stream = (Stream)storage.GetItemCopy(fileId);
                                openedFiles.Add(fileId, stream);

                                // if file with password
                                if (fileInfo.password != null)
                                {
                                    // add request answer
                                    DocumentPasswordManager manager = new DocumentPasswordManager(images, fileInfo.password);
                                }
                            }
                            else
                            {
                                stream = openedFiles[fileId];
                                stream.Position = 0;
                            }

                            int pageIndex = imageInfo.pageIndex;
                            // add the file stream to the image collection
                            images.Add(stream, true, pageIndex);
                        }

                        // if file stream does not have images
                        if (images.Count == 0)
                            throw new Exception("Image collection can not be empty.");

                        // register the OCR process for session
                        RegisterOcrProcess(sessionId);

                        // start the asynchronous text recognition
                        RecognizeAsync(requestParams, images);
                    }
                    catch
                    {
                        // clear image collection
                        images.ClearAndDisposeItems();
                        throw;
                    }
                }
                answer.success = true;
            }
            catch (Exception e)
            {
                if (IsOcrProcessExist(sessionId))
                    // remove information about OCR process
                    _sessionToRecognitionStatus.Remove(sessionId);
                answer.success = false;
                answer.errorMessage = GetFullExceptionMessage(e);
            }

            return answer;
        }

        /// <summary>
        /// Returns information about active OCR process.
        /// </summary>
        /// <param name="requestParams">Information about session.</param>
        /// <returns>Response from the server, which contains information about active recognition.</returns>
        [HttpPost]
        public OcrStatusResponseParams GetRecognitionStatus([FromBody] WebRequestParamsBase requestParams)
        {
            OcrStatusResponseParams answer = new OcrStatusResponseParams();
            try
            {
                // get the OCR status for session
                OcrStatus status = GetOcrStatus(requestParams.sessionId);
                answer.success = true;
                answer.status = status;

                // if text recognition is finished
                if (status.description == "Finished")
                {
                    lock (_lockObject)
                    {
                        // remove information about OCR process
                        _sessionToRecognitionStatus.Remove(requestParams.sessionId);
                    }
                }
            }
            catch (Exception e)
            {
                answer.success = false;
                answer.errorMessage = GetFullExceptionMessage(e);
            }
            return answer;
        }

        /// <summary>
        /// Aborts the active OCR process.
        /// </summary>
        /// <param name="requestParams">Information about session.</param>
        /// <returns>Response from the server that stores information about whether the recognition was aborted.</returns>
        [HttpPost]
        public WebResponseParamsBase AbortRecognition([FromBody] WebRequestParamsBase requestParams)
        {
            WebResponseParamsBase answer = new WebResponseParamsBase();
            try
            {
                // get session ID
                string sessionId = requestParams.sessionId;
                // if session has active OCR process
                if (IsOcrProcessExist(sessionId))
                {
                    // get OCR status
                    OcrStatus status = GetOcrStatus(sessionId);
                    // specify that OCR process must be aborted
                    status.description = "Aborted";
                }
                answer.success = true;
            }
            catch (Exception e)
            {
                answer.success = false;
                answer.errorMessage = GetFullExceptionMessage(e);
            }
            return answer;
        }

        #endregion


        #region PRIVATE

        #region OCR process and status

        /// <summary>
        /// Registers new OCR process for the specified session.
        /// </summary>
        /// <param name="sessionId">Session ID.</param>
        /// <returns>The OCR status.</returns>
        private OcrStatus RegisterOcrProcess(string sessionId)
        {
            lock (_lockObject)
            {
                // if specified session already has active OCR process
                if (IsOcrProcessExist(sessionId))
                {
                    // get the OCR status
                    OcrStatus previousOcrStatus = _sessionToRecognitionStatus[sessionId];
                    // if OCR process is aborted
                    if (previousOcrStatus.description == "Aborted")
                        // remove information about active OCR process for specified session
                        _sessionToRecognitionStatus.Remove(sessionId);
                    // if OCR process is NOT aborted
                    else
                        // error
                        throw new Exception("Specified session already has active OCR process.");
                }
                // if count of maximum allowable OCR processes is reached
                if (_sessionToRecognitionStatus.Keys.Count == MAX_ACTIVE_OCR_PROCESSES_COUNT)
                    // error
                    throw new Exception("The count of maximum allowable OCR processes is reached.");

                // create new OCR status
                OcrStatus status = new OcrStatus();
                // associate the OCR status with the session
                _sessionToRecognitionStatus.Add(sessionId, status);

                // return the OCR status
                return status;
            }
        }

        /// <summary>
        /// Returns the OCR status for specified session.
        /// </summary>
        /// <param name="sessionId">Session ID.</param>
        /// <returns>The OCR status.</returns>
        private OcrStatus GetOcrStatus(string sessionId)
        {
            lock (_lockObject)
            {
                if (!IsOcrProcessExist(sessionId))
                    throw new Exception("Specified session does not contain active recognition.");
                return _sessionToRecognitionStatus[sessionId];
            }
        }

        /// <summary>
        /// Returns a value indicating whether the specified session has active OCR process.
        /// </summary>
        /// <param name="sessionId">Session ID.</param>
        /// <returns>
        /// <b>true</b> - the specified session has active OCR process;
        /// <b>false</b> - the specified session does NOT active OCR process.
        /// </returns>
        private bool IsOcrProcessExist(string sessionId)
        {
            lock (_lockObject)
            {
                return _sessionToRecognitionStatus.ContainsKey(sessionId);
            }
        }

        #endregion


        #region OCR engine

        /// <summary>
        /// Creates a Tesseract OCR engine.
        /// </summary>
        /// <param name="binarizationMode">The OCR binarization mode.</param>
        /// <returns>A Tesseract OCR engine.</returns>
        private TesseractOcr CreateOcrEngine(OcrBinarizationMode binarizationMode)
        {
            // create the Tesseract OCR engine
            TesseractOcr tesseractOcrEngine = new TesseractOcr(TesseractOcrDllDirectory);

            // create the binarization command, which will binarize images before text recognition
            ChangePixelFormatToBlackWhiteCommand binarizationCommand = null;
            switch (binarizationMode)
            {
                case OcrBinarizationMode.Global:
                    binarizationCommand = new ChangePixelFormatToBlackWhiteCommand(BinarizationMode.Global);
                    break;

                case OcrBinarizationMode.Adaptive:
                    binarizationCommand = new ChangePixelFormatToBlackWhiteCommand(BinarizationMode.Adaptive);
                    break;
            }
            // set the binarization command
            tesseractOcrEngine.Binarization = binarizationCommand;

            return tesseractOcrEngine;
        }

        #endregion


        #region OCR engine manager

        /// <summary>
        /// Registers the OCR engine manager.
        /// </summary>
        /// <param name="engineManager">The OCR engine manager.</param>
        /// <param name="sessionId">The identifier of session, which is associated with the OCR engine manager.</param>
        private void RegisterOcrEngineManager(OcrEngineManager engineManager, string sessionId)
        {
            lock (_lockObject)
            {
                _ocrEngineToSessionId.Add(engineManager, sessionId);
                _sessionToRequestPathBase.Add(sessionId, this.HttpContext.Request.PathBase.Value);
            }
        }

        /// <summary>
        /// Returns the identifier of session, which is associated with specifief OCR engine manager.
        /// </summary>
        /// <param name="engineManager">The OCR engine manager.</param>
        /// <returns>The identifier of session, which is associated with specifief OCR engine manager.</returns>
        private string GetSessionForOcrEngineManager(OcrEngineManager engineManager)
        {
            lock (_lockObject)
            {
                return _ocrEngineToSessionId[engineManager];
            }
        }

        /// <summary>
        /// Unregisters the OCR engine manager. 
        /// </summary>
        /// <param name="engineManager">The OCR engine manager.</param>
        private void UnregisterOcrEngineManager(OcrEngineManager engineManager)
        {
            lock (_lockObject)
            {
                string sessionId = _ocrEngineToSessionId[engineManager];
                _ocrEngineToSessionId.Remove(engineManager);
                _sessionToRequestPathBase.Remove(sessionId);
            }
        }

        /// <summary>
        /// Text recognition is in progress.
        /// </summary>
        private void ocrEngineManager_Progress(object sender, ProgressEventArgs e)
        {
            OcrEngineManager engine = (OcrEngineManager)sender;
            string sessionId = GetSessionForOcrEngineManager(engine);
            OcrStatus status = GetOcrStatus(sessionId);
            // if text recognition must be aborted
            if (status.description == "Aborted")
            {
                e.Cancel = true;
            }
            // if text recognition must NOT be aborted
            else
            {
                // get previous progress
                int previousProgress = status.progress;
                // get current progress
                int currentProgress = e.Progress;
                // if new progress is less than previous progress
                if (previousProgress > currentProgress)
                    // OCR engine started to recognize text in next image
                    status.currentPageIndex = status.currentPageIndex + 1;
                status.progress = currentProgress;
            }
        }

        #endregion


        #region Text recognition

        /// <summary>
        /// Starts the asynchronous text recognition.
        /// </summary>
        /// <param name="requestParams">Recognition parameters.</param>
        /// <param name="images">Images, where text must be recognized.</param>
        /// <param name="pageIndexes">The indexes of pages, where text should be recognized.</param>
        private void RecognizeAsync(RecognizeTextRequestParams requestParams, ImageCollection images)
        {
            // create the OCR engine
            TesseractOcr ocrEngine = CreateOcrEngine(requestParams.binarization);

            OcrLanguage[] languages = new OcrLanguage[requestParams.languages.Length];
            for (int i = 0; i < requestParams.languages.Length; i++)
                languages[i] = (OcrLanguage)Enum.Parse(typeof(OcrLanguage), requestParams.languages[i]);

            // create and set the OCR engine settings
            TesseractOcrSettings ocrSettings = new TesseractOcrSettings(languages);
            ocrSettings.RecognitionMode = requestParams.recognitionMode;
            ocrSettings.RecognitionRegionType = requestParams.recognitionRegionType;

            // create an array with OCR recognition settings for each image
            List<ImageOcrRecognitionSettings> imageOcrRecognitionSettingsArray = new List<ImageOcrRecognitionSettings>();

            // for each image
            for (int i = 0; i < images.Count; i++)
            {
                WebImageRegion[] imageRegions = requestParams.ocrPageInfos[i].imageRegions;

                // OCR regions on image, by default text will be recognized in the whole image
                List<RecognitionRegion> ocrRegions = null;
                // if image regions are defined
                if (imageRegions != null && imageRegions.Length != 0)
                {
                    // get the text recognition regions on image
                    ocrRegions = GetOcrRegions(imageRegions, requestParams.recognitionRegionType, languages);
                    // if OCR regions are empty
                    if (ocrRegions.Count == 0)
                        // specify that text must be recognized in the whole image
                        ocrRegions = null;
                }

                // create the OCR recognition settings for image
                ImageOcrRecognitionSettings imageOcrRecognitionSettings = new ImageOcrRecognitionSettings(images[i], ocrSettings, ocrRegions);
                // save the OCR recognition settings for image
                imageOcrRecognitionSettingsArray.Add(imageOcrRecognitionSettings);
            }


            // create the OCR engine manager
            OcrEngineManager ocrEngineManager = new OcrEngineManager(ocrEngine);
            // subscribe to the Progress event
            ocrEngineManager.Progress += new EventHandler<ProgressEventArgs>(ocrEngineManager_Progress);
            // register the OCR engine manager
            RegisterOcrEngineManager(ocrEngineManager, requestParams.sessionId);


            // create the text recognition thread 
            Thread textRecognitionThread = new Thread(TextRecognitionThread);
            // specify that the thread must be run in background
            textRecognitionThread.IsBackground = true;
            // create parameters for the thread
            object[] textRecognitionThreadParams = new object[] {
                    requestParams.resultFileFormat,
                    requestParams.resultFileName,
                    imageOcrRecognitionSettingsArray.ToArray(),
                    ocrEngineManager,
                    ocrEngine,
                    requestParams.sessionId
            };

            // start the text recognition in a background thread
            textRecognitionThread.Start(textRecognitionThreadParams);
        }

        /// <summary>
        /// Returns the OCR regions, where text must be recognized.
        /// </summary>
        /// <param name="imageRegions">Image regions, which were returned by the image segmentation command.</param>
        /// <param name="recognitionRegionType">A type of recognition region.</param>
        /// <param name="languages">The recognition languages.</param>
        /// <returns>The OCR regions, where text must be recognized.</returns>
        private List<RecognitionRegion> GetOcrRegions(WebImageRegion[] imageRegions, RecognitionRegionType recognitionRegionType, OcrLanguage[] languages)
        {
            // regions, where text must be recognized
            List<RecognitionRegion> ocrRegions = new List<RecognitionRegion>();
            // for each image region
            for (int i = 0; i < imageRegions.Length; i++)
            {
                WebImageRegion imageRegion = imageRegions[i];
                // if image region contains text
                if (imageRegion.type == "Text")
                {
                    RegionOfInterest regionOfInterest = new RegionOfInterest(imageRegion.x, imageRegion.y, imageRegion.width, imageRegion.height);
                    RecognitionRegion recognitionRegion = new RecognitionRegion(regionOfInterest, languages, recognitionRegionType, 0);
                    // add image region to the OCR regions
                    ocrRegions.Add(recognitionRegion);
                }
            }
            return ocrRegions;
        }

        /// <summary>
        /// The text recognition thread.
        /// </summary>
        /// <param name="obj">The object with text recognition parameters.</param>
        private void TextRecognitionThread(object obj)
        {
            // get the text recognition parameters
            object[] objArray = (object[])obj;
            OcrResultFileFormat resultFileFormat = (OcrResultFileFormat)objArray.GetValue(0);
            string resultFileName = (string)objArray.GetValue(1);
            ImageOcrRecognitionSettings[] imageOcrRecognitionSettings = (ImageOcrRecognitionSettings[])objArray.GetValue(2);
            OcrEngineManager ocrEngineManager = (OcrEngineManager)objArray.GetValue(3);
            TesseractOcr tesseractOcrEngine = (TesseractOcr)objArray.GetValue(4);
            string sessionId = (string)objArray.GetValue(5);

            bool exceptionCatched = false;
            bool aborted = false;

            // get the OCR status
            OcrStatus ocrStatus = GetOcrStatus(sessionId);

            // an array with images, where text must be recognized
            List<VintasoftImage> images = new List<VintasoftImage>();
            for (int i = 0; i < imageOcrRecognitionSettings.Length; i++)
            {
                if (imageOcrRecognitionSettings[i] != null)
                {
                    images.Add(imageOcrRecognitionSettings[i].Image);
                }
            }

            try
            {
                // specify that OCR process is started
                ocrStatus.description = "RecognitionStarted";
                ocrStatus.pageCount = images.Count;
                ocrStatus.currentPageIndex = 0;

                // recognize text in images
                OcrPage[] pages = ocrEngineManager.Recognize(imageOcrRecognitionSettings);

                // if OCR process must be aborted
                if (ocrStatus.description == "Aborted")
                {
                    aborted = true;
                    return;
                }

                // specify that OCR process is finished
                ocrStatus.description = "RecognitionFinished";

                // specify that 
                ocrStatus.description = "ResultSavingStarted";

                string absolutePathToResult = null;

                // create the OCR result cache manager
                OcrResultsCacheManager cacheManager = CreateOcrResultCacheManager(sessionId);

                resultFileName = GetAvailableResultFilename(cacheManager, resultFileName);

                switch (resultFileFormat)
                {
                    case OcrResultFileFormat.FormattedText:
                        // save the OCR results to a formatted text file
                        absolutePathToResult = cacheManager.SaveOcrResultAsTextFile(resultFileName, pages, true, "\n");
                        break;

                    case OcrResultFileFormat.Text:
                        // save the OCR results to a text file
                        absolutePathToResult = cacheManager.SaveOcrResultAsTextFile(resultFileName, pages, false, "\n");
                        break;

                    case OcrResultFileFormat.PDF:
                        // save the OCR results to a PDF document
                        absolutePathToResult = cacheManager.SaveOcrResultInPdfDocument(resultFileName, images.ToArray(), pages);
                        break;
                }

                // if OCR resuts are saved successfully
                if (absolutePathToResult != null)
                {
                    string pathBaseValue = _sessionToRequestPathBase[sessionId];
                    string projectPath = GetProjectPath();
                    string relativePath = absolutePathToResult.Replace(projectPath, "").Replace(@"\", "/");

                    // convert absolute file path to a relative file path
                    ocrStatus.pathToResults = string.Format("{0}{1}", pathBaseValue, relativePath);

                }

                // specify that the OCR results are saved successfully
                ocrStatus.description = "ResultSavingFinished";

                StringBuilder recognizedTextStringBuilder = new StringBuilder();
                switch (resultFileFormat)
                {
                    case OcrResultFileFormat.FormattedText:
                        for (int i = 0; i < pages.Length; i++)
                        {
                            recognizedTextStringBuilder.Append(pages[i].GetFormattedText());

                            if (i != (pages.Length - 1))
                                recognizedTextStringBuilder.AppendLine();
                        }
                        break;

                    case OcrResultFileFormat.Text:
                        for (int i = 0; i < pages.Length; i++)
                        {
                            recognizedTextStringBuilder.Append(pages[i].GetText());

                            if (i != (pages.Length - 1))
                                recognizedTextStringBuilder.AppendLine();
                        }
                        break;
                }
                string recognizedTextString = recognizedTextStringBuilder.ToString();
                if (recognizedTextString != "")
                {
                    ocrStatus.recognizedText = recognizedTextString;
                }
            }
            catch (Exception e)
            {
                exceptionCatched = true;
                ocrStatus.description = "Finished";
                ocrStatus.errorMessage = GetFullExceptionMessage(e);
            }
            finally
            {
                // dispose the Tesseract OCR engine
                tesseractOcrEngine.Dispose();
                ocrEngineManager.Progress -= new EventHandler<ProgressEventArgs>(ocrEngineManager_Progress);
                // unregister the OCR engine manager
                UnregisterOcrEngineManager(ocrEngineManager);

                if (images.Count > 0)
                {
                    Stream stream = images[0].SourceInfo.Stream;

                    // dispose images 
                    for (int i = 0; i < images.Count; i++)
                    {
                        images[i].Dispose();
                    }

                    stream.Dispose();
                }

                if (!exceptionCatched && !aborted)
                    ocrStatus.description = "Finished";
            }
        }

        #endregion


        /// <summary>
        /// Returns a project path.
        /// </summary>
        /// <returns>Project path.</returns>
        private string GetProjectPath()
        {
            return HostingEnvironment.WebRootPath;
        }

        /// <summary>
        /// Returns a name of directory, where uploaded files must be stored.
        /// </summary>
        /// <returns>A name of directory, where uploaded files must be stored.</returns>
        private string GetUploadDirectoryName()
        {
            return "UploadedImageFiles";
        }

        /// <summary>
        /// Creates a cache manager that manages cache of OCR results.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns>A cache manager that manages cache of OCR results.</returns>
        private OcrResultsCacheManager CreateOcrResultCacheManager(string sessionId)
        {
            if (sessionId == null)
                throw new ArgumentNullException("sessionId");

            string projectDirectory = GetProjectPath();
            string uploadDirectory = GetUploadDirectoryName();
            string path = Path.Combine(Path.Combine(projectDirectory, uploadDirectory), sessionId);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            OcrResultsCacheManager ocrResultsCacheManager = new OcrResultsCacheManager(path);
            ocrResultsCacheManager.CacheLifeTime = 24 * 60;

            return ocrResultsCacheManager;
        }

        /// <summary>
        /// Creates a data storage for current session.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns>The data storage.</returns>
        /// <remarks>By default method creates the <see cref="StreamDataStorage"/>
        /// storage that works with "{Project Path}/UploadedImageFiles[/{SessionID}]" directory.<br/>
        /// <br/>
        /// <b>Important:</b> The overridden method must return data storage that
        /// can store <see cref="Stream"/> objects.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if <i>sessionId</i> is <b>null</b>.
        /// </exception>
        /// <exception cref="NotSupportedException">Thrown if
        /// the physical path to the root of the application is not defined,
        /// i.e. service is used in not ASP.NET application.
        /// </exception>
        private IDataStorage CreateSessionDataStorage(string sessionId)
        {
            if (sessionId == null)
                throw new ArgumentNullException("sessionId");

            string projectDirectory = GetProjectPath();
            string uploadDirectory = GetUploadDirectoryName();
            string path = Path.Combine(Path.Combine(projectDirectory, uploadDirectory), sessionId);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return new StreamDataStorage(path);
        }

        /// <summary>
        /// Returns the full exception message recursively.
        /// </summary>
        private string GetFullExceptionMessage(Exception ex)
        {
            string projectPath = GetProjectPath();
            string ocrDllDirectory = TesseractOcrDllDirectory;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine(ex.Message.Replace(ocrDllDirectory, string.Empty).Replace(projectPath, string.Empty));
            Exception innerException = ex.InnerException;
            while (innerException != null)
            {
                string exceptionMessage = innerException.Message.Replace(ocrDllDirectory, string.Empty).Replace(projectPath, string.Empty);
                sb.AppendLine(string.Format("Inner exception: {0}", exceptionMessage));
                innerException = innerException.InnerException;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns available name for result file.
        /// </summary>
        /// <param name="cacheManager">The OCR result cache manager.</param>
        /// <param name="resultFileName">The name of result file.</param>
        /// <returns>Available name for result file.</returns>
        private string GetAvailableResultFilename(OcrResultsCacheManager cacheManager, string resultFileName)
        {
            // get available filename
            string availableFileName = resultFileName;
            // get filename with .txt-extension
            string txtFilename = cacheManager.GetOcrResultTxtId(availableFileName);
            // get filename with .pdf-extension
            string pdfFilename = cacheManager.GetOcrResultPDfId(availableFileName);

            int index = 0;
            IDataStorage storage = cacheManager.DataStorage;
            // while storage contains file with .txt- or .pdf-extension
            while (storage.Contains(txtFilename) || storage.Contains(pdfFilename))
            {
                // increase counter for available filename
                index++;

                // get new available filename
                availableFileName = resultFileName + "_" + index;
                // get filename with .txt-extension
                txtFilename = cacheManager.GetOcrResultTxtId(availableFileName);
                // get filename with .pdf-extension
                pdfFilename = cacheManager.GetOcrResultPDfId(availableFileName);
            }

            // return available filename
            return availableFileName;
        }

        #endregion

        #endregion

    }
}