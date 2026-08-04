function scrollIntoView(elementId) {
    var elem = document.getElementById(elementId);
    if (elem) {
        elem.scrollIntoView();
        window.location.hash = elementId;
    }
}