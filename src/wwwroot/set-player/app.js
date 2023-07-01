'use strict';

const audio = new Audio();
audio.autoplay = true;

document.getElementById('set').onclick = async e => {
    // new QRCode(document.getElementById("qrcode"), "https://webisora.com");
    // new QRCode(document.querySelector("#qrcode > div"), "https://webisora.com");
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
            // audio.currentTime = 0;
            // audio.play();
            fetch('/api/play-pause-song?play=yes', { method: 'POST' });
        } else if (data.actionId === 'new_song') {
            player.loadVideoById(data.song.Id);
            player.playVideo();
            // audio.src = `/data/${data.song.Id}.mp3`;
            // audio.play();
            fetch('/api/play-pause-song?play=yes', { method: 'POST' });
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

    audio.onended = async e => {
        const resp = await fetch('/api/next-song', {
            method: 'POST',
        });
        if (!resp.ok) {
            alert('Couldn\'t play next song.');
        }
    }

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
                    // 'onStateChange': onPlayerStateChange
                }
            });
            // resolve(player);
        }

        // // 3. This function creates an <iframe> (and YouTube player)
        // //    after the API code downloads.
        // const player = new YT.Player('player', {
        //     height: '390',
        //     width: '640',
        //     videoId: 'M7lc1UVf-VE',
        //     playerVars: {
        //         'playsinline': 1
        //     },
        //     events: {
        //         'onReady': onPlayerReady,
        //         // 'onStateChange': onPlayerStateChange
        //     }
        // });

        // 4. The API will call this function when the video player is ready.
        
    });
}

// // let player;
// let resolvePlayer;
//
// function onYouTubeIframeAPIReady() {
//     const player = new YT.Player('player', {
//         height: '390',
//         width: '640',
//         videoId: 'M7lc1UVf-VE',
//         playerVars: {
//             'playsinline': 1
//         },
//         events: {
//             'onReady': onPlayerReady,
//             'onStateChange': onPlayerStateChange
//         }
//     });
//     resolvePlayer(player);
// }

