/**
 A dialog that displays progress of text recognition process.
*/
OcrProgressDialogJS = function (abortRecognitionFunc) {

    OcrProgressDialogJS.prototype.show = function () {
        $('#ocrProgressDialog').modal('show');
    }

    OcrProgressDialogJS.prototype.updateProgress = function (description, progress) {
        $('#ocrProgressBar').css('width', progress + '%');
    }

    OcrProgressDialogJS.prototype.close = function () {
        // set the progress to 100%
        $('#ocrProgressBar').css('width', '100%');

        // close the progress dialog
        $('#ocrProgressDialog').modal('hide');
    }

}
