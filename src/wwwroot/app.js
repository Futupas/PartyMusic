'use strict';

document.getElementById('add-song-btn').onclick = e => {
    document.getElementById('search').classList.remove('hidden');
}

document.querySelector('#search > .background').onclick = e => {
    document.getElementById('search').classList.add('hidden');
}

