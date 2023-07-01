'use strict';

document.getElementById('set').onclick = async e => {
    const qrcode = new QRCode(document.querySelector("#qrcode > div"), {
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
    
    const player = await getPlayer();

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
            player.setVolume(data.volume * 100);
        } else if (data.actionId === 'play_pause_song') {
            if (data.play) {
                player.playVideo();
            } else {
                player.pauseVideo();
            }
        } else if (data.actionId === 'restart_song') {
            player.seekTo(0);
            player.playVideo();
            fetch('/api/play-pause-song?play=yes', { method: 'POST' });
        } else if (data.actionId === 'new_song') {
            player.loadVideoById(data.song.Id);
            player.playVideo();
            fetch('/api/play-pause-song?play=yes', { method: 'POST' });
        } else {
            console.warn('Unknown actionId', data.actionId);
        }
    };

    socket.onclose = function(event) {
        if (event.wasClean) {
            console.log(`[close] Connection closed cleanly, code=${event.code} reason=${event.reason}`);
        } else {
            alert('[close] Connection died');
        }
    };

    socket.onerror = function(error) {
        alert(`[error]`);
        console.error(error);
    };

}

/** @returns Promise<YT.Player> */
function getPlayer() {
    return new Promise((resolve, reject) => {
        // 2. This code loads the IFrame Player API code asynchronously.
        const tag = document.createElement('script');

        tag.src = "https://www.youtube.com/iframe_api";
        const firstScriptTag = document.getElementsByTagName('script')[0];
        firstScriptTag.parentNode.insertBefore(tag, firstScriptTag);
        
        
        window.onYouTubeIframeAPIReady = () => {
            const player = new YT.Player('player', {
                height: '390',
                width: '640',
                videoId: 'M7lc1UVf-VE',
                playerVars: {
                    'playsinline': 1
                },
                events: {
                    'onReady': e => resolve(player),
                    'onStateChange': async e => {
                        if (e.data === YT.PlayerState.ENDED) { // 0
                            const resp = await fetch('/api/next-song', {
                                method: 'POST',
                            });
                            if (!resp.ok) {
                                alert('Couldn\'t play next song.');
                            }

                        }
                    }
                }
            });
        }
    });
}
