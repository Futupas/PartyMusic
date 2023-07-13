'use strict';

const socket = new WebSocket(`ws://${window.location.host}/api/ws?isPlayer=no`);

socket.onopen = function(e) {
    console.log("[open] Connection established");
};

socket.onmessage = function(event) {
    console.log('[message] Data received from server', event.data);
    const data = JSON.parse(event.data);
    if (data.actionId === 'update_songs') {
        const songs = data.songs;
        document.querySelector('#main > div.song.current > div.text').innerText = songs[0].Title || 'No current song'; //todo duration
        const songsDiv = document.querySelector('#main > div.all-songs');
        songsDiv.innerHTML = '';
        for (let i = 1; i < songs.length; i++) {
            const newI = i;
            const div = document.createElement('div');
            div.classList.add('song');

            const text = document.createElement('div');
            text.classList.add('text');
            text.innerText = songs[i].Title; //todo duration
            div.appendChild(text);

            const removeButton = document.createElement('button');
            removeButton.classList.add('remove');
            // removeButton.innerHTML = '&#128465;';
            removeButton.onclick = async e => {
                await wsFetch(socket,("remove-song-from-queue",{"removeSongId":newI}))
                // await fetch('/api/remove-song-from-queue?songId=' + newI, {
                //     method: 'POST',
                // });
                if (!downloadResp.ok) {
                    alert('Couldn\'t remove song from queue.');
                }
            }
            const removeButtonImg = document.createElement('img');
            removeButtonImg.src = '/img/delete.png';
            removeButtonImg.alt = '&#128465;';
            removeButton.appendChild(removeButtonImg);
            div.appendChild(removeButton);

            songsDiv.appendChild(div);
        }
    } else if (data.actionId === 'play_pause_song') {
        // document.querySelector('#current > .pause').innerHTML = data.play ? '|&nbsp;|' : '&#9658;';
        const img = document.querySelector('#current > .pause > img');
        if (data.play) {
            img.src = '/img/pause.png';
            img.alt = '|&nbsp;|';
        } else {
            img.src = '/img/play.png';
            img.alt = '&#9658;';
        }
    } else if (data.actionId === 'set_volume') {
        document.getElementById('song-volume').value = data.volume;
    } else if (data.actionId === 'post') {
        onPostWSReceived(data);
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


document.getElementById('add-song-btn').onclick = e => {
    document.getElementById('search').classList.remove('hidden');
    document.getElementById('search').classList.remove('closed');
}

document.querySelector('#search > .background').onclick = e => {
    document.getElementById('search').classList.add('closed');
    setTimeout(() => {
        document.getElementById('search').classList.add('hidden');
    }, 200);
}

document.getElementById('search-song-submit').onclick = async e => {
    const query = document.getElementById('search-song-input').value?.trim();
    if (!query?.length) {
        alert('Empty query!');
        return;
    }
    document.getElementById('search-song-submit').classList.add('loading');
    
    const resp = await wsFetch(socket,"search",{"query":query ,"count":"10"});
    // const resp = await fetch('/api/search?query=' + encodeURI(query));
    const data = resp.data;
    
    const resultsDiv = document.querySelector('#search > .main > .results');
    resultsDiv.innerHTML = '';
    
    for (const song of data) {
        const songId = song.id;
        
        const div = document.createElement('div');
        div.classList.add('result');
        
        const text = document.createElement('div');
        text.classList.add('text');
        text.innerText = `${song.title} (${Math.floor(song.duration / 60)}:${song.duration % 60})`;
        div.appendChild(text);
        
        const btnDownload = document.createElement('button');
        btnDownload.classList.add('download');
        // btnDownload.innerText = 'D';
        btnDownload.appendChild(getImage('/img/download.png', 'PN', false));
        div.appendChild(btnDownload);

        const btnPlayNow = document.createElement('button');
        btnPlayNow.classList.add('play-now');
        // btnPlayNow.innerText = 'PN';
        btnPlayNow.appendChild(getImage('/img/playlist.png', 'PN', true));
        btnPlayNow.onclick = async e => {
            
            // const downloadResp = await ('/api/add-song-to-queue?start=yes&songId=' + songId, {
            //     method: 'POST',
            // });
            const downloadResp = await wsFetch(socket, ("add-song-to-queue"), { "songId": songId, "start": "no", method: 'POST' });
            if (!downloadResp.ok) {
                alert('Couldn\'t add song to queue.');
            }
        }
        div.appendChild(btnPlayNow);
        
        const btnAddToQueue = document.createElement('button');
        btnAddToQueue.classList.add('add-to-queue');
        // btnAddToQueue.innerText = 'A2Q';
        btnAddToQueue.appendChild(getImage('/img/playlist.png', 'PN', false));
        btnAddToQueue.onclick = async e => {
            const downloadResp = await wsFetch(socket, ("add-song-to-queue"), { "songId": songId, "start": "no", method: 'POST' });
            // const downloadResp = await fetch('/api/add-song-to-queue?start=no&songId=' + songId, {
            //     method: 'POST',
            // });
            if (!downloadResp.ok) {
                alert('Couldn\'t add song to queue.');
            }
        }
        div.appendChild(btnAddToQueue);
        
        
        
        if (song.exists) {
            btnDownload.classList.add('hidden');
            btnPlayNow.classList.remove('hidden');
            btnAddToQueue.classList.remove('hidden');
        } else {
            btnDownload.classList.remove('hidden');
            btnPlayNow.classList.add('hidden');
            btnAddToQueue.classList.add('hidden');
        }
        
        btnDownload.onclick = async e => {
            btnDownload.querySelector('img').src = '/img/loading.png';
            btnDownload.querySelector('img').classList.add('rotating');
            // const downloadResp = await fetch('/api/download?id=' + songId, {
            //     method: 'POST',
            // });
            const downloadResp = await wsFetch(socket, ("download"), { "id": songId, method: 'POST' });
            if (downloadResp.ok) {
                btnDownload.classList.add('hidden');
                btnPlayNow.classList.remove('hidden');
                btnAddToQueue.classList.remove('hidden');
            } else {
                btnDownload.querySelector('img').src = '/img/download.png';
                btnDownload.querySelector('img').classList.remove('rotating');
                alert('Couldn\'t download song.');
            }
        }

        resultsDiv.appendChild(div);
    }
    document.getElementById('search-song-submit').classList.remove('loading');
}

function getImage(src, alt, upsideDown = false) {
    const image = document.createElement('img');
    image.src = src;
    image.alt = alt;
    if (upsideDown) {
        image.style.transform = 'scaleY(-1)';
        image.style.transformOrigin = '50% 50%';
    }
    return image;
}

document.querySelector('#current > .restart').onclick = async e => {
    const resp = await wsFetch(socket, ("restart-song"), { method: 'POST' });
    // const resp = await fetch('/api/restart-song', {
    //     method: 'POST',
    // });
    if (!resp.ok) {
        alert('Couldn\'t restart song.');
    }
}
document.querySelector('#current > .pause').onclick = async e => {
    const resp = await wsFetch(socket,("play-pause-song"),{"play":play,method:'POST'});
    // const resp = await fetch('/api/play-pause-song', {
    //     method: 'POST',
    // });
    if (!resp.ok) {
        alert('Couldn\'t play/pause song.');
    }
}
document.querySelector('#current > .next-song').onclick = async e => {
    const resp = await wsFetch(socket, ("next-song"), {method:'POST'});
    // const resp = await fetch('/api/next-song', {
    //     method: 'POST',
    // });
    if (!resp.ok) {
        alert('Couldn\'t play next song.');
    }
}
document.getElementById('song-volume').oninput = async e => {
    const volume = document.getElementById('song-volume').value;
    const resp = await wsFetch(socket, ("set-volume"), { "volume": volume, method: 'POST'});
    // const resp = await fetch('/api/set-volume?volume=' + volume, {
    //     method: 'POST',
    // });
    if (!resp.ok) {
        alert('Couldn\'t change volume song.');
    }
}



