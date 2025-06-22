window.scrollToBottom = (elementId) => {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};
window.scrollToBottomFallback = (elementId) => {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollTo({
            top: element.scrollHeight,
            behavior: 'smooth'
        });
    }
};
function scrollToBottomRef(element) {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
}
window.scrollToElement = (id) => {
    const el = document.getElementById(id);
    if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'start' })
    }
};

window.signalRAddBeforeUnload = function (dotnetHelper) {
    window.addEventListener('beforeunload', () => {
        // Gọi về .NET để thực thi StopAsync()
        dotnetHelper.invokeMethodAsync('OnBrowserUnload');
    });
};

window.openFileUpload = () => {
    setTimeout(() => {
        var fileInput = document.getElementById('fileInput');
        if (fileInput) {
            fileInput.click();
        } else {
            console.error("fileInput không tồn tại!");
        }
    }, 100);
};

window.clickElement = (element) => {
    element.click();
};