'use strict';

document.getElementById('add-song-btn').onclick = e => {
    document.getElementById('search').classList.remove('hidden');
}

document.querySelector('#search > .background').onclick = e => {
    document.getElementById('search').classList.add('hidden');
}

document.getElementById('search-song-submit').onclick = async e => {
    const query = document.getElementById('search-song-input').value?.trim();
    if (!query?.length) {
        alert('Empty query!');
        return;
    }
    
    const resp = await fetch('/api/search?query=' + encodeURI(query));
    if (!resp.ok) {
        console.error(resp);
        alert('Couldn\'t fetch');
        return;
    }
    const data = await resp.json();
    
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
        btnDownload.innerText = 'D';
        div.appendChild(btnDownload);

        const btnPlayNow = document.createElement('button');
        btnPlayNow.classList.add('play-now');
        btnPlayNow.innerText = 'PN';
        div.appendChild(btnPlayNow);
        
        const btnAddToQueue = document.createElement('button');
        btnAddToQueue.classList.add('add-to-queue');
        btnAddToQueue.innerText = 'A2Q';
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
            // const downloadResp = await fetch('/api/download', {
            //     method: 'POST',
            //     headers: {
            //         'Accept': 'application/json',
            //         'Content-Type': 'application/json'
            //     },
            //     body: JSON.stringify({ id: songId }),
            // });
            const downloadResp = await fetch('/api/download?id=' + songId, {
                method: 'POST',
            });
            if (downloadResp.ok) {
                btnDownload.classList.add('hidden');
                btnPlayNow.classList.remove('hidden');
                btnAddToQueue.classList.remove('hidden');
            } else {
                alert('Couldn\'t download song.');
            }
        }

        resultsDiv.appendChild(div);
    }
}
