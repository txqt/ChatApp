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