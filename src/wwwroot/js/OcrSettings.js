/**
 OcrLanguages enum.
*/
var WebOcrLanguagesEnumJS = Vintasoft.Shared.EnumGenerator.create([

    /** None. */
    { "None": 0 },

    /** English. */
    { "English": 1 }

], true);


/**
 OcrRecognitionMode enum.
*/
var WebOcrRecognitionModeEnumJS = Vintasoft.Shared.EnumGenerator.create([

    /** Quality. */
    { "Quality": 0 },

    /** Speed. */
    { "Speed": 1 }

], false);

/**
 OcrRecognitionRegionType enum.
*/
var WebOcrRecognitionRegionTypeEnumJS = Vintasoft.Shared.EnumGenerator.create([

    /**  */
    { "SegmentedPage": 0 },

    /**  */
    { "SingleColumn": 1 },

    /**  */
    { "SingleBlockOfVertText": 2 },

    /**  */
    { "SingleBlock": 3 },

    /**  */
    { "SingleLine": 4 },

    /**  */
    { "SingleWord": 5 },

    /**  */
    { "CircleWord": 6 },

    /**  */
    { "SingleChar": 7 },

    /**  */
    { "SegmentedAndOrientedPage": 8 },

    /**  */
    { "SparseText": 9 },

    /**  */
    { "OrientedSparseText": 10 }

], false);

/**
 OcrImageBinarization enum.
*/
var WebOcrImageBinarizationEnumJS = Vintasoft.Shared.EnumGenerator.create([

    /** None */
    { "None": 0 },

    /** Global */
    { "Global": 1 },

    /** Adaptive */
    { "Adaptive": 2 }


], false);

/**
 OcrResultFileFormat enum.
*/
var WebOcrResultFileFormatEnumJS = Vintasoft.Shared.EnumGenerator.create([

    /** None */
    { "Text": 0 },

    /** Adaptive */
    { "FormattedText": 1 },

    /** Global */
    { "PDF": 2 }

], false);




WebOcrSettingsJS = function () {

    _PROTO = WebOcrSettingsJS.prototype;

    this._languages = new WebOcrLanguagesEnumJS(0);
    var allLanguages = this._languages.getAllAvailableNames();
    if (allLanguages.includes("English")) {
        this._languages = new WebOcrLanguagesEnumJS("English");
    }

    this._recognitionMode = new WebOcrRecognitionModeEnumJS(0);
    this._recognitionRegionType = new WebOcrRecognitionRegionTypeEnumJS(8);
    this._imageBinarizationMode = new WebOcrImageBinarizationEnumJS(2);
    this._resultFileFormat = new WebOcrResultFileFormatEnumJS(0);
    this._resultFileName = "ocrResultFile";


    /**
     Gets WebOcrLanguagesEnumJS. 
     @returns {WebOcrLanguagesEnumJS} WebOcrLanguagesEnumJS.
     @property @public
    */
    _PROTO.get_Languages = function () {
        return this._languages;
    }
    /**
     Sets Recognition Languages.
     @param {WebOcrLanguagesEnumJS} value WebOcrLanguagesEnumJS.
     @property @public
    */
    _PROTO.set_Languages = function (value) {
        this._languages = new WebOcrLanguagesEnumJS(value);
    }

    /**
     Gets WebOcrLanguagesEnumJS. 
     @returns {WebOcrRecognitionModeEnumJS} WebOcrLanguagesEnumJS.
     @property @public
    */
    _PROTO.get_RecognitionMode = function () {
        return this._recognitionMode;
    }
    /**
     Sets RecognitionMode.
     @param {WebOcrRecognitionModeEnumJS} value WebOcrRecognitionModeEnumJS.
     @property @public
    */
    _PROTO.set_RecognitionMode = function (value) {
        this._recognitionMode = new WebOcrRecognitionModeEnumJS(value);
    }

    /**
     Gets WebOcrLanguagesEnumJS. 
     @returns {WebOcrRecognitionRegionTypeEnumJS} WebOcrLanguagesEnumJS.
     @property @public
    */
    _PROTO.get_RecognitionRegionType = function () {
        return this._recognitionRegionType;
    }
    /**
     Sets RecognitionRegionType.
     @param {WebOcrRecognitionRegionTypeEnumJS} value WebOcrRecognitionRegionTypeEnumJS.
     @property @public
    */
    _PROTO.set_RecognitionRegionType = function (value) {
        this._recognitionRegionType = new WebOcrRecognitionRegionTypeEnumJS(value);
    }

    /**
     Gets WebOcrLanguagesEnumJS. 
     @returns {WebOcrImageBinarizationEnumJS} WebOcrLanguagesEnumJS.
     @property @public
    */
    _PROTO.get_ImageBinarizationMode = function () {
        return this._imageBinarizationMode;
    }
    /**
     Sets ImageBinarizationMode.
     @param {WebOcrImageBinarizationEnumJS} value WebOcrImageBinarizationEnumJS.
     @property @public
    */
    _PROTO.set_ImageBinarizationMode = function (value) {
        this._imageBinarizationMode = new WebOcrImageBinarizationEnumJS(value);
    }

    /**
     Gets WebOcrLanguagesEnumJS. 
     @returns {WebOcrResultFileFormatEnumJS} WebOcrLanguagesEnumJS.
     @property @public
    */
    _PROTO.get_ResultFileFormat = function () {
        return this._resultFileFormat;
    }
    /**
     Sets ResultFileFormat.
     @param {WebOcrResultFileFormatEnumJS} value WebOcrResultFileFormatEnumJS.
     @property @public
    */
    _PROTO.set_ResultFileFormat = function (value) {
        this._resultFileFormat = new WebOcrResultFileFormatEnumJS(value);
    }

    /**
     Gets result file name. 
     @returns {string} Result file name.
     @property @public
    */
    _PROTO.get_ResultFileName = function () {
        return this._resultFileName;
    }
    /**
     Sets result file name.
     @param {string} value Result file name.
     @property @public
    */
    _PROTO.set_ResultFileName = function (value) {
        this._resultFileName = value;
    }
}

WebOcrSettingsJS.initOcrLanguages = function (ocrService, resultFunc) {
    function __getOcrLanguages() {
        // create the parameters for web request
        var requestParams = {
            type: 'POST'
        }
        // create the web request for text recognition
        var request = new Vintasoft.Shared.WebRequestJS("GetSupportedLanguages", __initOcrLanguages, __errorOcrLanguages, requestParams);
        // send the request to the OCR web service
        ocrService.sendRequest(request);
    }

    function __initOcrLanguages(data) {
        __createOcrLanguagesEnum(data.languages);
    }

    function __errorOcrLanguages(data) {
        new ErrorMessageDialogJS(data.errorMessage);

        __createOcrLanguagesEnum(["English"]);
    }

    function __createOcrLanguagesEnum(languages) {
        var enumElements = [];
        enumElements.push({ "None": 0 });

        for (var i = 0; i < languages.length; i++) {
            var element = {};

            var elementName = languages[i];
            element[elementName] = Math.pow(2, i);

            enumElements.push(element);
        }

        WebOcrLanguagesEnumJS = Vintasoft.Shared.EnumGenerator.create(enumElements, true);

        if (resultFunc) {
            resultFunc();
        }
    }

    __getOcrLanguages();
}