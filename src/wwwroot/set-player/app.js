'use strict';


document.getElementById('set').onclick = async e => {
    // new QRCode(document.getElementById("qrcode"), "https://webisora.com");
    // new QRCode(document.querySelector("#qrcode > div"), "https://webisora.com");
    var qrcode = new QRCode(document.querySelector("#qrcode > div"), {
        text: "https://webisora.com",
        width: 512,
        height: 512,
        colorDark : "black",
        colorLight : "white",
        correctLevel : QRCode.CorrectLevel.H
    });
    
    document.getElementById('set').remove();
    document.getElementById('main').classList.remove('hidden');


    const socket = new WebSocket(`ws://${window.location.host}/api/ws?isPlayer=yes`);

    socket.onopen = function(e) {
        console.log("[open] Connection established");
    };

    socket.onmessage = function(event) {
        console.log('[message] Data received from server', event.data);
        const data = JSON.parse(event.data);
        if (data.actionId === 'add_log') {
            const message = data.message;
            const div = document.createElement('div');
            div.innerText = message;
            document.getElementById('console').appendChild(div);
        } else {
            console.warn('Unknown actionId', event.data.actionId);
        }
    };

    socket.onclose = function(event) {
        if (event.wasClean) {
            console.log(`[close] Connection closed cleanly, code=${event.code} reason=${event.reason}`);
        } else {
            // e.g. server process killed or network down
            // event.code is usually 1006 in this case
            alert('[close] Connection died');
        }
    };

    socket.onerror = function(error) {
        alert(`[error]`);
        console.error(error);
    };

}

