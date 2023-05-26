var currentTimestamp = null;
var pingInterval = setInterval(function () {
    fetch('/ping')
        .then(function (response) {
            if (response.ok) {
                return response.text();
            } else {
                throw new Error('Network response was not OK');
            }
        })
        .then(function (timestamp) {
            if (currentTimestamp == null) {
                currentTimestamp = timestamp;
            } else if (timestamp != currentTimestamp) {
                currentTimestamp = timestamp;
                clearInterval(pingInterval);
                showReloadWarning();
                setTimeout(reloadPage, 3000);
            }
        })
        .catch(function (error) {
            showStatusWarning();
        });
}, 1000);


// Display a visual warning indicating the server status is not 200
// You can customize the warning display according to your requirements
function showStatusWarning() {
    var warningElement = document.createElement('div');
    warningElement.textContent = 'Server status is not online';
    warningElement.style.background = 'red';
    warningElement.style.color = 'white';
    warningElement.style.padding = '10px';
    warningElement.style.position = 'fixed';
    warningElement.style.top = '0';
    warningElement.style.left = '0';
    document.body.appendChild(warningElement);
}

// Display a visual warning indicating the page is about to reload
// You can customize the warning display according to your requirements
function showReloadWarning() {
    var warningElement = document.createElement('div');
    warningElement.textContent = 'Page is about to reload';
    warningElement.style.background = 'yellow';
    warningElement.style.color = 'black';
    warningElement.style.padding = '10px';
    warningElement.style.position = 'fixed';
    warningElement.style.bottom = '0';
    warningElement.style.right = '0';
    document.body.appendChild(warningElement);
}

function reloadPage() {
    location.reload();
}
