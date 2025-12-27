var _fileService;

var _docViewer;

var _localizer;

var _openFileHelper;

var _previouslyUploadedFilesDialog;

var _ocrService;
var _isTextRecognitionStarted = false;

var _ocrSettings;
var _ocrSettingsDialog;

var _imageProcessingCommandSettingsDialog;

var _blockUiDialog;

var _needOpenPdfFileWithRecognizedText = false;



// === "File" toolbar, "Previously uploaded files" button ===

/**
 Creates UI button for showing the list with previously uploaded files.
*/
function __createPreviousUploadFilesButton() {
    // create the button that allows to show a dialog with previously uploaded image files and select image file
    var button = new Vintasoft.Imaging.UI.UIElements.WebUiButtonJS({
        cssClass: "uploadedFilesList",
        title: "Previously Uploaded Files",
        localizationId: "previousUploadFilesButton",
        onClick: __previousUploadFilesButton_clicked
    });
    return button;
}

/**
 "Previously Uploaded Files" button is clicked.
 */
function __previousUploadFilesButton_clicked(event, uiElement) {
    var docViewer = uiElement.get_RootControl();
    if (docViewer != null) {
        // if dialog does not exist
        if (_previouslyUploadedFilesDialog == null)
            // create dialog
            _previouslyUploadedFilesDialog = new PreviouslyUploadedFilesDialogJS(_fileService, docViewer, _openFileHelper, __showErrorMessage);
        // show the dialog
        _previouslyUploadedFilesDialog.show();
    }
}



// === "OCR" toolbar ===

/**
 Creates UI panel with OCR functionality.
*/
function __createOcrUiPanel() {
    var label = new Vintasoft.Imaging.UI.UIElements.WebUiLabelElementJS({ "text": "OCR", localizationId: "ocrPanelLabel" });
    var button = new Vintasoft.Imaging.UI.UIElements.WebUiElementContainerJS([label], { cssClass: "vsui-subMenu-icon" });

    // create the button that allows to recognize text on all pages and open PDF file with recognized text
    var recognizeTextFromAllPagesAndOpenResultButton = new Vintasoft.Imaging.UI.UIElements.WebUiButtonJS({
        cssClass: "recognizeTextFromAllPagesAndOpenResultButton",
        title: "Recognize text from all pages and open file with recognized text",
        localizationId: "recognizeTextFromAllPagesAndOpenResultButton",
        onClick: __recognizeTextFromAllPagesAndOpenResultButton_clicked
    });

    // create the button that allows to recognize text on all pages and show a dialog with ability to download file with recognized text
    var recognizeTextFromAllPagesAndDownloadResultButton = new Vintasoft.Imaging.UI.UIElements.WebUiButtonJS({
        cssClass: "recognizeTextFromAllPagesAndDownloadResultButton",
        title: "Recognize text from all pages and download file with recognized text",
        localizationId: "recognizeTextFromAllPagesAndDownloadResultButton",
        onClick: __recognizeTextFromAllPagesAndDownloadResultButton_clicked
    });

    // create the button that allows to recognize text on focused page and show a dialog with ability to download file with recognized text
    var recognizeTextFromFocusedPageAndDownloadResultButton = new Vintasoft.Imaging.UI.UIElements.WebUiButtonJS({
        cssClass: "recognizeTextFromFocusedPageAndDownloadResultButton",
        title: "Recognize text from focused page and download file with recognized text",
        localizationId: "recognizeTextFromFocusedPageAndDownloadResultButton",
        onClick: __recognizeTextFromFocusedPageAndDownloadResultButton_clicked
    });

    // create the button that allows to recognize text in region on focused page and show a dialog with ability to download file with recognized text
    var recognizeTextFromRegionOfFocusedPageAndDownloadResultButton = new Vintasoft.Imaging.UI.UIElements.WebUiButtonJS({
        cssClass: "recognizeTextFromRegionOfFocusedPageAndDownloadResultButton",
        title: "Recognize text from region of focused page and download file with recognized text",
        localizationId: "recognizeTextFromRegionOfFocusedPageAndDownloadResultButton",
        onClick: __recognizeTextFromRegionOfFocusedPageAndDownloadResultButton_clicked
    });

    // create the button that allows to show OCR settings dialog
    var ocrSettingsButton = new Vintasoft.Imaging.UI.UIElements.WebUiButtonJS({
        cssClass: "ocrSettingsButton",
        title: "OCR settings",
        localizationId: "ocrSettingsButton",
        onClick: __ocrSettingsButton_clicked
    });

    // create "OCR" toolbar
    return new Vintasoft.Imaging.UI.Panels.WebUiPanelJS(
        [recognizeTextFromAllPagesAndOpenResultButton, recognizeTextFromAllPagesAndDownloadResultButton, recognizeTextFromFocusedPageAndDownloadResultButton, recognizeTextFromRegionOfFocusedPageAndDownloadResultButton, ocrSettingsButton, "rectangularSelectionToolButton"],
        { cssClass: "vsui-subMenu-contentPanel" }, button);
}

/**
 "Recognize text from all pages and download file with recognized text" button is clicked.
 */
function __recognizeTextFromAllPagesAndOpenResultButton_clicked(event, uiElement) {
    // specify that PDF document with text recognition result must be opened in viewer
    _needOpenPdfFileWithRecognizedText = true;
    // recognize text from all pages
    __recognizeText();
}

/**
 "Recognize text from all pages and download file with recognized text" button is clicked.
 */
function __recognizeTextFromAllPagesAndDownloadResultButton_clicked(event, uiElement) {
    // recognize text from all pages
    __recognizeText();
}

/**
 "Recognize text from focused page and download file with recognized text" button is clicked.
 */
function __recognizeTextFromFocusedPageAndDownloadResultButton_clicked(event, uiElement) {
    var docViewer = uiElement.get_RootControl();
    if (docViewer != null) {
        var imageViewer = docViewer.get_ImageViewer();
        // get index of focused page
        var focusedPageIndex = imageViewer.get_FocusedIndex();

        // recognize text from focused page
        __recognizeText([focusedPageIndex]);
    }
}

/**
 "Recognize text from region of focused page and download file with recognized text" button is clicked.
 */
function __recognizeTextFromRegionOfFocusedPageAndDownloadResultButton_clicked(event, uiElement) {
    var docViewer = uiElement.get_RootControl();
    if (docViewer != null) {
        var imageViewer = docViewer.get_ImageViewer();
        // get index of focused page
        var focusedPageIndex = imageViewer.get_FocusedIndex();

        var imageRegion = null;
        // get visual tool
        var visualTool = imageViewer.get_VisualTool();
        // if tool exists and tool is Rectangular selection
        if (visualTool != null && visualTool.get_Name() === "RectangularSelection") {
            // get the selection region
            var selectionRect = visualTool.get_Rectangle();
            // if selection region is not empty
            if (selectionRect.width !== 0 && selectionRect.height !== 0) {
                // create object that contains information about image region for text recognition
                imageRegion = {
                    type: "Text",
                    x: Math.ceil(selectionRect.x),
                    y: Math.ceil(selectionRect.y),
                    width: Math.ceil(selectionRect.width),
                    height: Math.ceil(selectionRect.height)
                };
            }
        }

        // if selection region is empty
        if (imageRegion == null) {
            __showErrorMessage("Please select region on image using rectangular selection.");
            return;
        }

        // recognize text from image region of focused page
        __recognizeText([focusedPageIndex], imageRegion);
    }
}

/**
 "OCR settings" button is clicked.
 */
function __ocrSettingsButton_clicked(event, uiElement) {
    _localizer.localizeDocument();
    _ocrSettingsDialog.show();
}



// === Recognize text ===

/**
 Sends an asynchronous request for text recognition.
*/
function __recognizeText(pageIndexes, imageRegion) {
    var images = _docViewer.get_ImageViewer().get_Images();
    // if there are no images
    if (images.get_Count() == 0) {
        // exit
        return;
    }
    // if text recognition is started already
    if (_isTextRecognitionStarted) {
        alert("Text recognition is started already.");
        return;
    }

    // page indexes are not specified
    if (pageIndexes == null) {
        pageIndexes = [];
        // add indexes for all pages
        for (var i = 0; i < images.get_Count(); i++) {
            pageIndexes.push(i);
        }
    }

    // infos about pages and recognition regions
    var ocrPageInfos = [];
    for (var i = 0; i < pageIndexes.length; i++) {
        // if bad index
        if (pageIndexes[i] < 0 || pageIndexes[i] >= images.get_Count())
            continue;

        // get image
        var image = images.getImage(pageIndexes[i]);
        // get image file
        var source = image.get_Source();

        // info about image
        var imageInfo = { fileInfo: { id: source.get_ImageId(), password: source.get_Password() }, pageIndex: image.get_PageIndex() };
        var imageRegions = [];
        if (imageRegion != null) {
            imageRegions = [imageRegion];
        }

        ocrPageInfos.push({ imageInfo: imageInfo, imageRegions: imageRegions });
    }

    var languageEnums = _ocrSettings._languages.toArray();
    var languages = [];
    for (var i = 0; i < languageEnums.length; i++) {
        languages.push(languageEnums[i].toString());
    }

    var resultFileFormat = _ocrSettings.get_ResultFileFormat().valueOf();
    // if PDF document with text recognition result must be opened in viewer
    if (_needOpenPdfFileWithRecognizedText) {
        // use PDF format
        resultFileFormat = 2;
    }

    // create parameters for web request
    var requestParams = {
        type: 'POST',
        data: {
            ocrPageInfos: ocrPageInfos,
            languages: languages,
            recognitionRegionType: _ocrSettings.get_RecognitionRegionType().valueOf(),
            binarization: _ocrSettings.get_ImageBinarizationMode().valueOf(),
            recognitionMode: _ocrSettings.get_RecognitionMode().valueOf(),
            resultFileFormat: resultFileFormat,
            resultFileName: _ocrSettings.get_ResultFileName()
        }
    }
    // create the web request for text recognition
    var request = new Vintasoft.Shared.WebRequestJS("RecognizeText", __recognizeText_started, __recognizeText_error, requestParams);
    // send the request to the OCR web service
    _ocrService.sendRequest(request);
}

/**
 Text recognition is started.
*/
function __recognizeText_started() {
    // specify that text recognition is started
    _isTextRecognitionStarted = true;

    _ocrProgressDialog = new OcrProgressDialogJS(__abortRecognition);
    // show dialog that displays progress of text recognition process
    _ocrProgressDialog.show();

    // request the text recognition status
    __requestRecognitionStatus();
}

/**
 Text recognition is failed.
*/
function __recognizeText_error(data) {
    __showErrorMessage(data);
}

/**
 Sends an asynchronous request for getting information about text recognition.
*/
function __requestRecognitionStatus() {
    // create the parameters for web request
    var requestParams = {
        type: 'POST',
        data: {}
    }
    // create the web request for getting information about text recognition
    var request = new Vintasoft.Shared.WebRequestJS("GetRecognitionStatus", __getRecognitionStatus_success, __getRecognitionStatus_error, requestParams);
    // send the request to the OCR web service
    _ocrService.sendRequest(request);
}

/**
 Sends an asynchronous request for aborting text recognition.
*/
function __abortRecognition() {
    // if text recognition is started
    if (_isTextRecognitionStarted) {
        // create the parameters for web request
        var requestParams = {
            type: 'POST',
            data: {}
        }
        // create the web request for aborting text recognition
        var request = new Vintasoft.Shared.WebRequestJS("AbortRecognition", null, null, requestParams);
        // send the request to the OCR web service
        _ocrService.sendRequest(request);
    }
}

/**
 Request for getting status of text recognizing is executed successfully.
*/
function __getRecognitionStatus_success(data) {
    var status = data.status;

    // show the current status of text recognition

    var description = status.description;

    var pageCount = status.pageCount;
    var currentPage = status.currentPageIndex + 1;

    // update progress in OCR progress dialog
    _ocrProgressDialog.updateProgress(description + " [" + currentPage + "/" + pageCount + "]", status.progress);

    // if text recognition is NOT finished AND NOT aborted
    if (status.description !== "Finished" && status.description !== "Aborted") {
        // send new request for getting text recognition status after 250 milliseconds
        setTimeout(__requestRecognitionStatus, 250);
    }
    // if text recognition is finished OR aborted
    else {
        // specify that text recognition is NOT started
        _isTextRecognitionStarted = false;

        // close the OCR pprogress dialog
        _ocrProgressDialog.close();

        // if text recognition is aborted
        if (status.errorMessage != null) {
            // show the error
            __showErrorMessage(status);
        }
        // if text recognition is finished successfully
        else if (status.pathToResults != null) {
            console.log("Text recognition result:");
            console.log("=====");
            console.log(status.recognizedText);
            console.log("=====");

            // if PDF document with text recognition result must be opened in viewer
            if (_needOpenPdfFileWithRecognizedText) {
                // disable
                _needOpenPdfFileWithRecognizedText = false;

                __openRecognizedFile(status.pathToResults);
            }
            // if text recognition result must be downloaded as a file
            else {
                // open dialog with the results of text recognition
                __openRecognitionResultDialog(status.pathToResults);
            }
        }
    }
}

/**
 Request for getting status of text recognizing is failed.
*/
function __getRecognitionStatus_error(data) {
    __showErrorMessage(data);
}



// === Text recognition results ===

/**
 Opens a dialog window with results of text recognition.
*/
function __openRecognitionResultDialog(url) {
    var ocrResultDownloadDialog = new OcrResultDownloadDialogJS(url);
    // show dialog that allows to download OCR result
    ocrResultDownloadDialog.show();
}

/**
 Opens PDF document with text recognition result in viewer.
*/
function __openRecognizedFile(url) {
    
    /**
     Set text selection tool is file opened successfully.
    */
    function __openFile_success(data) {
        var textSelectionTool = _docViewer.getVisualToolById("TextSelectionTool");
        _docViewer.set_CurrentVisualTool(textSelectionTool);
    }

    // get file name from url
    var fileName = url.substring(url.lastIndexOf('/') + 1);
    // open file
    _docViewer.get_ImageViewer().get_Images().openFile(fileName, __openFile_success);
}



// === "Tools" toolbar ===

/**
 Creates UI button for activating the visual tool, which allows to pan images in image viewer.
*/
function __createPanToolButton() {
    // if touch device is used
    if (__isTouchDevice()) {
        return new Vintasoft.Imaging.UI.UIElements.WebUiVisualToolButtonJS({
            cssClass: "vsdv-tools-panButton",
            title: "Pan, Zoom",
            localizationId: "panToolButton"
        }, "PanTool,ZoomTool");
    }
    else {
        return new Vintasoft.Imaging.UI.UIElements.WebUiVisualToolButtonJS({
            cssClass: "vsdv-tools-panButton",
            title: "Pan",
            localizationId: "panToolButton"
        }, "PanTool");
    }
}

/**
 Creates UI button for activating the visual tool, which allows to select text and work with annotations in image viewer.
*/
function __createTextSelectionToolButton() {
    return new Vintasoft.Imaging.UI.UIElements.WebUiVisualToolButtonJS({
        cssClass: "vsdv-tools-textSelectionToolButton",
        title: "Text selection",
        localizationId: "TextSelectionToolButton"
    }, "TextSelectionTool");
}



// === Init UI ===

/**
 Registers custom UI elements in "WebUiElementsFactoryJS".
*/
function __registerNewUiElements() {
    // register the "Previously uploaded files" button in web UI elements factory
    Vintasoft.Imaging.UI.UIElements.WebUiElementsFactoryJS.registerElement("previousUploadFilesButton", __createPreviousUploadFilesButton);

    // register custom "Image processing" panel in web UI elements factory
    Vintasoft.Imaging.UI.UIElements.WebUiElementsFactoryJS.registerElement("imageProcessingPanel", __createCustomImageProcessingPanel);

    // register the "OCR" panel in web UI elements factory
    Vintasoft.Imaging.UI.UIElements.WebUiElementsFactoryJS.registerElement("ocrMenuPanel", __createOcrUiPanel);

    // init progress bar style
    document.getElementById("ocrProgressBar").children[0].style.width = "0%";

    // register the "Pan" button in web UI elements factory
    Vintasoft.Imaging.UI.UIElements.WebUiElementsFactoryJS.registerElement("panToolButton", __createPanToolButton);
    // register the "Text selection" button in web UI elements factory
    Vintasoft.Imaging.UI.UIElements.WebUiElementsFactoryJS.registerElement("textSelectionToolButton", __createTextSelectionToolButton);
}

/**
 Initializes main menu of document viewer.
 @param {object} docViewerSettings Settings of document viewer.
*/
function __initMenu(docViewerSettings) {
    // get items of document viewer
    var items = docViewerSettings.get_Items();

    var uploadAndOpenFileButton = items.getItemByRegisteredId("uploadAndOpenFileButton");
    if (uploadAndOpenFileButton != null)
        uploadAndOpenFileButton.set_FileExtensionFilter(".bmp, .cur, .gif, .ico, .jpg, .jpeg, .jls, .pbm, .pcx, .pdf, .png, .tga, .tif, .tiff");

    var uploadAndAddFileButton = items.getItemByRegisteredId("uploadAndAddFileButton");
    if (uploadAndAddFileButton != null)
        uploadAndAddFileButton.set_FileExtensionFilter(".bmp, .cur, .gif, .ico, .jpg, .jpeg, .jls, .pbm, .pcx, .pdf, .png, .tga, .tif, .tiff");

    // get the main menu of document viewer
    var mainMenu = items.getItemByRegisteredId("mainMenu");
    // if main menu is found
    if (mainMenu != null) {
        // get items of main menu
        var mainMenuItems = mainMenu.get_Items();

        // add OCR menu to the main menu
        mainMenuItems.addItem("ocrMenuPanel");
    }

    // get the "File" menu panel
    var fileMenuPanel = items.getItemByRegisteredId("fileToolbarPanel");
    // if menu panel is found
    if (fileMenuPanel != null) {
        // get items of file menu panel
        var fileMenuPanelItems = fileMenuPanel.get_Items();

        // add the "Previous uploaded files" button to the menu panel
        fileMenuPanelItems.insertItem(2, "previousUploadFilesButton");
    }

    // get the "Visual tools" menu panel
    var toolsSubmenu = items.getItemByRegisteredId("visualToolsToolbarPanel");
    // if menu panel is found
    if (toolsSubmenu != null) {
        var toolsSubmenuItems = toolsSubmenu.get_Items();

        toolsSubmenuItems.addItem("textSelectionToolButton");
    }
}

/**
 Initializes side panel of document viewer.
 @param {object} docViewerSettings Settings of document viewer.
*/
function __initSidePanel(docViewerSettings) {
    // get items of document viewer
    var items = docViewerSettings.get_Items();

    var sidePanel = items.getItemByRegisteredId("sidePanel");
    if (sidePanel != null) {
        var sidePanelItems = sidePanel.get_PanelsCollection();

        sidePanelItems.addItem("imageProcessingPanel");
        var imageProcessingPanel = items.getItemByRegisteredId("imageProcessingPanel");
        if (imageProcessingPanel != null) {
            Vintasoft.Shared.subscribeToEvent(imageProcessingPanel, "settingsButtonClicked", __imageProcessingPanel_settingsButtonClicked);
        }

        sidePanelItems.addItem("textSelectionPanel");

        var textSearchPanel = Vintasoft.Imaging.UI.UIElements.WebUiElementsFactoryJS.createElementById("textSearchPanel");
        textSearchPanel.set_CreatePageResultHeaderContentCallback(__createPageSearchResultHeaderContent);
        sidePanelItems.addItem(textSearchPanel);
    }

    // get the thumbnail viewer panel of document viewer
    var thumbnailViewerPanel = items.getItemByRegisteredId("thumbnailViewerPanel");
    // if panel is found
    if (thumbnailViewerPanel != null)
        // subscribe to the "actived" event of the thumbnail viewer panel of document viewer
        Vintasoft.Shared.subscribeToEvent(thumbnailViewerPanel, "activated", __thumbnailsPanelActivated);
}

/**
 Returns UI elements, which will display information image page search result.
 @param {any} image Image, where text was searched.
 @param {any} imageIndex The number of pages, which have already been processed.
 @param {any} searchResults Search result.
*/
function __createPageSearchResultHeaderContent(image, imageIndex, searchResults) {
    return [new Vintasoft.Imaging.UI.UIElements.WebUiLabelElementJS({
        text: Vintasoft.Shared.VintasoftLocalizationJS.getStringConstant("vsui-textSearchPanel-pageLabel") + " #" + (imageIndex + 1),
        css: { cursor: "pointer" }
    })];
}

/**
 Creates the OCR settings dialog.
*/
function __createOcrSettingDialog() {
    // create the property grid with information about OCR settings
    var propertyGrid = new Vintasoft.Shared.WebPropertyGridJS(_ocrSettings);

    // create dialog that allows to change OCR settings
    return new Vintasoft.Imaging.UI.Dialogs.WebUiPropertyGridDialogJS(
        propertyGrid,
        {
            title: "OCR settings",
            localizationId: "ocrSettingsDialog",
            css: { height: '300px' }
        });
}

/**
 Initializes OcrSettingsDialog.
*/
function __initOcrSettingsDialog() {
    WebOcrSettingsJS.initOcrLanguages(_ocrService, function () {
        _ocrSettings = new WebOcrSettingsJS();
        // create dialog that allows to change OCR settings
        _ocrSettingsDialog = __createOcrSettingDialog();
        _docViewer.get_Items().addItem(_ocrSettingsDialog);
    })
}

/**
 Thumbnail viewer panel of document viewer is activated.
*/
function __thumbnailsPanelActivated() {
    var thumbnailViewer = this.get_ThumbnailViewer();
    if (thumbnailViewer != null) {
        // create the progress image
        var progressImage = new Image();
        progressImage.src = __getApplicationUrl() + "Images/fileUploadProgress.gif";
        // specify that the thumbnail viewer must use the progress image for indicating the thumbnail loading progress
        thumbnailViewer.set_ProgressImage(progressImage);

        // additional bottom space for text with page number under thumbnail
        var textCaptionHeight = 18;
        var padding = thumbnailViewer.get_ThumbnailPadding();
        padding[2] += textCaptionHeight
        thumbnailViewer.set_ThumbnailPadding(padding);
        thumbnailViewer.set_DisplayThumbnailCaption(true);
    }
}



// === Image processing ===

function __createCustomImageProcessingPanel() {
    var button = new Vintasoft.Imaging.UI.UIElements.WebUiButtonJS({
        cssClass: "vsdv-sidePanel-imageProcessingPanelButton",
        title: "Image processing",
        localizationId: "showPanelButton"
    });
    var element = new Vintasoft.Imaging.UI.Panels.WebUiImageProcessingPanelJS(
        {
            size: 25,
            cssClass: "vsui-sidePanel-content vsdv-sidePanel-imageProcessingPanel",
            localizationId: "imageProcessingPanel",
            commandNames: ["AutoInvert", "HalftoneRemoval", "BorderClear", "HolePunchRemoval", "LineRemoval", "Despeckle", "Deskew", "AutoTextOrientation"]
        },
        button
    );
    return element;
}

/**
 Creates an image processing dialog.
*/
function __createImageProcessingSettingsDialog(command) {
    // create the property grid for image processing command
    var propertyGrid = new Vintasoft.Shared.WebPropertyGridJS(command);

    var commandName = command.get_ActionName();
    // create the image processing dialog
    return new Vintasoft.Imaging.UI.Dialogs.WebUiPropertyGridDialogJS(
        propertyGrid,
        {
            title: "Image processing command settings (" + commandName + ")",
            cssClass: "vsui-dialog imageProcessingSettings",
            localizationId: "imageProcessingSettingsDialog-" + commandName
        });
}

/**
 The "Settings" button in image processing panel is clicked.
 @param {object} event Event.
 @param {object} command Selected image processing command.
*/
function __imageProcessingPanel_settingsButtonClicked(event, command) {
    if (command != null) {
        var docViewer = event.target.get_RootControl();

        // if previous image processing dialog exists
        if (_imageProcessingCommandSettingsDialog != null) {
            // remove dialog from web document viewer
            docViewer.get_Items().removeItem(_imageProcessingCommandSettingsDialog);
            // destroy dialog
            delete _imageProcessingCommandSettingsDialog;
            // clear link to dialog
            _imageProcessingCommandSettingsDialog = null;
        }

        // create the image processing dialog
        _imageProcessingCommandSettingsDialog = __createImageProcessingSettingsDialog(command);
        // add dialog to the web document viewer
        docViewer.get_Items().addItem(_imageProcessingCommandSettingsDialog);
        // localize the dialog
        _localizer.localizeDocument();
        // show the dialog
        _imageProcessingCommandSettingsDialog.show();
    }
}



// === Image viewer events ===

function __imageViewer_focusedIndexChanged() {
    // get visual tool of image viewer
    var visualTool = _docViewer.get_ImageViewer().get_VisualTool();
    // if visual tool is WebHighlightToolJS and it highlights barcode recognition results
    if (visualTool != null && (visualTool instanceof Vintasoft.Imaging.UI.VisualTools.WebHighlightToolJS)) {
        // clear visual tool in image viewer
        _docViewer.clearCurrentVisualTool();
    }
}



// === Document viewer events ===

function __docViewer_warningOccured(event, eventArgs) {
    // show the alert if warning occured
    __showErrorMessage(eventArgs.message);
}

function __docViewer_asyncOperationStarted(event, data) {
    // get description of asynchronous operation
    var description = data.description;

    // if image is prepared for printing
    if (description === "Image prepared to print") {
        // do not block UI when images are preparing for printing
    }
    else {
        // block UI
        __blockUI(data.description);
    }
}

function __docViewer_asyncOperationFinished(event, data) {
    // unblock UI
    __unblockUI();
}

function __docViewer_asyncOperationFailed(event, data) {
    // get description of asynchronous operation
    var description = data.description;
    // get additional information about asynchronous operation
    var additionalInfo = data.data;
    // if additional information exists
    if (additionalInfo != null)
        // show error message
        __showErrorMessage(additionalInfo);
    // if additional information does NOT exist
    else
        // show error message
        __showErrorMessage(description + ": unknown error.");
}

function __docViewer_fileOpened(event, data) {
    var canDownloadFile = true;
    try {
        // try create URL
        new URL(data.fileId);
        // file, which is loaded from URL, cannot be downloaded
        canDownloadFile = false;
    }
    catch (ex) {
    }

    // get the "Download file" button
    var downloadFileButton = _docViewer.get_Items().getItemByRegisteredId("downloadFileButton");
    // if button is found
    if (downloadFileButton != null) {
        // if file cannot be downloaded
        if (!canDownloadFile)
            // disable the "Download file" button
            downloadFileButton.set_IsEnabled(false);
    }
}



// === Utils ===

/**
 Blocks the UI. 
 @param {string} text Message that describes why UI is blocked.
*/
function __blockUI(text) {
    _blockUiDialog = new BlockUiDialogJS(text);
}

/**
 Unblocks the UI.
*/
function __unblockUI() {
    if (_blockUiDialog != null) {
        _blockUiDialog.close();
        _blockUiDialog = null;
    }
}

/**
 Shows an error message.
 @param {object} data Information about error.
*/
function __showErrorMessage(data) {
    __unblockUI();
    new ErrorMessageDialogJS(data);
}

/**
 Returns application URL.
*/
function __getApplicationUrl() {
    var applicationUrl = window.location.toString();
    if (applicationUrl[applicationUrl.length - 1] != '/')
        applicationUrl = applicationUrl + '/';
    return applicationUrl;
}



// === Localization ===

/**
 Creates the dictionary for localization of application UI.
*/
function __createUiLocalizationDictionary() {
    setTimeout(function () {
        var tempDialogs = [];
        __createDocumentViewerDialogsForLocalization(tempDialogs);
        __createDemoDialogsForLocalization(tempDialogs);

        var localizationDict = _localizer.getDocumentLocalizationDictionary();
        var localizationDictString = JSON.stringify(localizationDict, null, '\t');
        console.log(localizationDictString);

        var floatingContainer = document.getElementById("documentViewerContainer");
        for (var i = 0; i < tempDialogs.length; i++) {
            floatingContainer.removeChild(tempDialogs[i].get_DomElement());
            delete tempDialogs[i];
        }
    }, 2000);
}

/**
 Creates the dialogs, which are used in Web Document Viewer, for localization.
*/
function __createDocumentViewerDialogsForLocalization(tempDialogs) {
    var floatingContainer = document.getElementById("documentViewerContainer");

    var documentPasswordDialog = new Vintasoft.Imaging.UI.Dialogs.WebUiDocumentPasswordDialogJS();
    documentPasswordDialog.render(floatingContainer);
    tempDialogs.push(documentPasswordDialog);

    var imageSelectionDialog = new Vintasoft.Imaging.UI.Dialogs.WebImageSelectionDialogJS();
    imageSelectionDialog.render(floatingContainer);
    tempDialogs.push(imageSelectionDialog);

    var printImagesDialog = new Vintasoft.Imaging.UI.Dialogs.WebPrintImagesDialogJS();
    printImagesDialog.render(floatingContainer);
    tempDialogs.push(printImagesDialog);

    var imageViewerSettingsDialog = new Vintasoft.Imaging.UI.Dialogs.WebImageViewerSettingsDialogJS();
    imageViewerSettingsDialog.render(floatingContainer);
    tempDialogs.push(imageViewerSettingsDialog);

    var thumbnailViewerSettingsDialog = new Vintasoft.Imaging.UI.Dialogs.WebThumbnailViewerSettingsDialogJS();
    thumbnailViewerSettingsDialog.render(floatingContainer);
    tempDialogs.push(thumbnailViewerSettingsDialog);

    var uploadImageFromUrlDialog = new Vintasoft.Imaging.UI.Dialogs.WebUiUploadImageFromUrlDialogJS();
    uploadImageFromUrlDialog.render(floatingContainer);
    tempDialogs.push(uploadImageFromUrlDialog);
}

/**
 Creates the dialogs, which are used in this demo, for localization.
*/
function __createDemoDialogsForLocalization(tempDialogs) {
    var floatingContainer = document.getElementById("documentViewerContainer");

    var ocrSettingsDialog = __createOcrSettingDialog();
    ocrSettingsDialog.render(floatingContainer);
    tempDialogs.push(ocrSettingsDialog);


    // create commands
    var imageProcessingCommands = [];
    imageProcessingCommands.push(new Vintasoft.Imaging.ImageProcessing.DocCleanup.WebAutoInvertCommandJS());
    imageProcessingCommands.push(new Vintasoft.Imaging.ImageProcessing.DocCleanup.WebAutoTextOrientationCommandJS());
    imageProcessingCommands.push(new Vintasoft.Imaging.ImageProcessing.DocCleanup.WebBorderClearCommandJS());
    imageProcessingCommands.push(new Vintasoft.Imaging.ImageProcessing.DocCleanup.WebDeskewCommandJS());
    imageProcessingCommands.push(new Vintasoft.Imaging.ImageProcessing.DocCleanup.WebDespeckleCommandJS());
    imageProcessingCommands.push(new Vintasoft.Imaging.ImageProcessing.DocCleanup.WebHalftoneRemovalCommandJS());
    imageProcessingCommands.push(new Vintasoft.Imaging.ImageProcessing.DocCleanup.WebHolePunchRemovalCommandJS());
    imageProcessingCommands.push(new Vintasoft.Imaging.ImageProcessing.DocCleanup.WebLineRemovalCommandJS());

    for (var i = 0; i < imageProcessingCommands.length; i++) {
        // create the image processing dialog
        var imageProcessingCommandSettingsDialog = __createImageProcessingSettingsDialog(imageProcessingCommands[i]);
        imageProcessingCommandSettingsDialog.render(floatingContainer);
        tempDialogs.push(imageProcessingCommandSettingsDialog);
    }
}

/**
 Enables the localization of application UI.
*/
function __enableUiLocalization() {
    // if localizer is ready (localizer loaded localization dictionary)
    if (_localizer.get_IsReady()) {
        // localize DOM-elements of web page
        _localizer.localizeDocument();
    }
    // if localizer is NOT ready
    else
        // wait when localizer will be ready
        Vintasoft.Shared.subscribeToEvent(_localizer, "ready", function () {
            // localize DOM-elements of web page
            _localizer.localizeDocument();
        });

    // subscribe to the "dialogShown" event of document viewer
    Vintasoft.Shared.subscribeToEvent(_docViewer, "dialogShown", function (event, data) {
        _localizer.localizeDocument();
    });
}

/**
 Returns a value indicating whether touch device is used.
*/
function __isTouchDevice() {
    return (navigator.maxTouchPoints > 0) || (navigator.msMaxTouchPoints > 0);
}



// === Main ===

/**
 Main function.
*/
function __main() {
    // set the session identifier
    var hiddenSessionFieldElement = document.getElementById('hiddenSessionField');
    Vintasoft.Shared.WebImagingEnviromentJS.set_SessionId(hiddenSessionFieldElement.value);

    // specify web services, which should be used in this demo

    _fileService = new Vintasoft.Shared.WebServiceControllerJS(__getApplicationUrl() + "vintasoft/api/MyVIntasoftFileApi");

    Vintasoft.Shared.WebServiceJS.defaultFileService = _fileService;
    Vintasoft.Shared.WebServiceJS.defaultImageCollectionService = new Vintasoft.Shared.WebServiceControllerJS(__getApplicationUrl() + "vintasoft/api/MyVintasoftImageCollectionApi");
    Vintasoft.Shared.WebServiceJS.defaultImageService = new Vintasoft.Shared.WebServiceControllerJS(__getApplicationUrl() + "vintasoft/api/MyVintasoftImageApi");

    Vintasoft.Shared.WebServiceJS.defaultImageProcessingService = new Vintasoft.Shared.WebServiceControllerJS(__getApplicationUrl() + "vintasoft/api/MyVintasoftImageProcessingApi");
    Vintasoft.Shared.WebServiceJS.defaultImageProcessingDocCleanupService = new Vintasoft.Shared.WebServiceControllerJS(__getApplicationUrl() + "vintasoft/api/MyVintasoftImageProcessingDocCleanupApi");

    // create the web service for recogining text in images
    _ocrService = new Vintasoft.Shared.WebServiceControllerJS(__getApplicationUrl() + "vintasoft/api/OcrApi");

    // create UI localizer
    _localizer = new Vintasoft.Shared.VintasoftLocalizationJS();

    // register new UI elements
    __registerNewUiElements();

    // create the document viewer settings
    var docViewerSettings = new Vintasoft.Imaging.DocumentViewer.WebDocumentViewerSettingsJS("documentViewerContainer", "documentViewer");
    // specify that document viewer can upload and add file
    docViewerSettings.set_CanAddFile(true);
    // specify that document viewer can upload image data from URL
    docViewerSettings.set_CanUploadImageFromUrl(true);
    // specify that document viewer should show "Export and download file" button instead of "Download file" button
    docViewerSettings.set_CanExportAndDownloadFile(true);
    docViewerSettings.set_CanDownloadFile(false);
    // specify that document viewer can clear session cache
    docViewerSettings.set_CanClearSessionCache(true);

    // initialize main menu of document viewer
    __initMenu(docViewerSettings);

    // initialize side panel of document viewer
    __initSidePanel(docViewerSettings);

    // create the document viewer
    _docViewer = new Vintasoft.Imaging.DocumentViewer.WebDocumentViewerJS(docViewerSettings);

    // subscribe to the "warningOccured" event of document viewer
    Vintasoft.Shared.subscribeToEvent(_docViewer, "warningOccured", __docViewer_warningOccured);
    // subscribe to the asyncOperationStarted event of document viewer
    Vintasoft.Shared.subscribeToEvent(_docViewer, "asyncOperationStarted", __docViewer_asyncOperationStarted);
    // subscribe to the asyncOperationFinished event of document viewer
    Vintasoft.Shared.subscribeToEvent(_docViewer, "asyncOperationFinished", __docViewer_asyncOperationFinished);
    // subscribe to the asyncOperationFailed event of document viewer
    Vintasoft.Shared.subscribeToEvent(_docViewer, "asyncOperationFailed", __docViewer_asyncOperationFailed);
    // subscribe to the fileOpened event of document viewer
    Vintasoft.Shared.subscribeToEvent(_docViewer, "fileOpened", __docViewer_fileOpened);

    // get the thumbnail viewer of document viewer
    var thumbnailViewer1 = _docViewer.get_ThumbnailViewer();
    thumbnailViewer1.set_CanDragThumbnails(true);
    thumbnailViewer1.set_CanNavigateThumbnailsUsingKeyboard(true);
    thumbnailViewer1.set_CanSelectThumbnailsUsingKeyboard(true);
    thumbnailViewer1.set_CanDeleteThumbnailsUsingKeyboard(true);

    // get the image viewer of document viewer
    var imageViewer1 = _docViewer.get_ImageViewer();
    // specify that image viewer must show images in the single continuous column mode
    imageViewer1.set_DisplayMode(new Vintasoft.Imaging.WebImageViewerDisplayModeEnumJS("SingleContinuousColumn"));
    // specify that image viewer must show images in the fit width mode
    imageViewer1.set_ImageSizeMode(new Vintasoft.Imaging.WebImageSizeModeEnumJS("FitToWidth"));

    // create the progress image
    var progressImage = new Image();
    progressImage.src = __getApplicationUrl() + "Images/fileUploadProgress.gif";
    // specify that the image viewer must use the progress image for indicating the image loading progress
    imageViewer1.set_ProgressImage(progressImage);

    // subscribe to the focusedIndexChanged event of image viewer
    Vintasoft.Shared.subscribeToEvent(imageViewer1, "focusedIndexChanged", __imageViewer_focusedIndexChanged);

    // names of visual tools in composite visual tool
    var visualToolNames = "PanTool";
    // if touch device is used
    if (__isTouchDevice()) {
        // get zoom tool from document viewer
        var zoomTool = _docViewer.getVisualToolById("ZoomTool");
        // specify that zoom tool should not disable context menu
        zoomTool.set_DisableContextMenu(false);

        // add name of zoom tool to the names of visual tools of composite visual tool
        visualToolNames = visualToolNames + ",ZoomTool";
    }
    // get the visual tool, which allows to pan and zoom images in image viewer
    var tool = _docViewer.getVisualToolById(visualToolNames);
    // set the visual tool as active visual tool in image viewer
    _docViewer.set_CurrentVisualTool(tool);

    __initOcrSettingsDialog();

    // copy the default file to the uploaded image files directory and open the file
    _openFileHelper = new OpenFileHelperJS(_docViewer, __showErrorMessage);
    _openFileHelper.openDefaultImageFile("OCR.tif");

    $(document).ready(function () {
        //// create the dictionary for localization of application UI
        //__createUiLocalizationDictionary();

        // enable the localization of application UI
        __enableUiLocalization();
    });
}



// run main function
__main();
