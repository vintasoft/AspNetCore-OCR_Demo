/**
 A dialog that allows to download OCR result.
*/
OcrResultDownloadDialogJS = function (url) {

    OcrResultDownloadDialogJS.prototype.show = function () {
        document.getElementById("downloadResultOfRecognition").href = url;

        $('#ocrResultDownloadDialog').modal('show');
    }

}
