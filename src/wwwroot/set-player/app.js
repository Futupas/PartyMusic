'use strict';

const audio = new Audio();
audio.autoplay = true;

document.getElementById('set').onclick = async e => {
    // new QRCode(document.getElementById("qrcode"), "https://webisora.com");
    // new QRCode(document.querySelector("#qrcode > div"), "https://webisora.com");
    var qrcode = new QRCode(document.querySelector("#qrcode > div"), {
        text: `http://${window.location.host}/index.html`,
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
        } else if (data.actionId === 'set_volume') {
            audio.volume = data.volume / 100;
        } else if (data.actionId === 'play_pause_song') {
            if (data.play) {
                audio.play();
            } else {
                audio.pause();
            }
        } else if (data.actionId === 'restart_song') {
            audio.currentTime = 0;
        } else if (data.actionId === 'new_song') {
            audio.src = `/data/${data.song.Id}.mp3`;
        } else {
            console.warn('Unknown actionId', data.actionId);
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


