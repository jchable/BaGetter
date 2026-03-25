(function () {
    'use strict';

    const baget = {};
    globalThis.baget = baget;

    baget.copyTextToClipboard = function (text, elementToFocus) {
        navigator.clipboard.writeText(text).then(function () {
            console.log('Text copied to clipboard');
        }).catch(function (err) {
            console.log('Failed to copy text: ', err);
        });

        if (elementToFocus) {
            elementToFocus.focus();
        }
    };
})();
