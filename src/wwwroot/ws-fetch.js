'use strict';

const wsFetchResolvers = {};

function generateRequestId() {
    const date = Date.now();
    const random = (Math.random() + '').split('.')[1];
    return date + '-' + random;
}

/**
 * Behaves almost like 'fetch', but with WebSocket
 * @param {WebSocket} socket
 * @param {string} postType
 * @param {Object.<string, *>} data
 * @return {Promise<Object>} Parsed result
 */
function wsFetch(socket, postType, data) {
    return new Promise((resolve, reject) => {
        const requestId = generateRequestId();
        wsFetchResolvers[requestId] = resolve;
        const dataToSend = {
            'actionId': 'post',
            requestId,
            postType,
            ...data 
        };
        socket.send(JSON.stringify(dataToSend));
    });
}

/**
 * 
 * @param {any} data
 */
function onPostWSReceived(data) {
    const resolve = wsFetchResolvers[data.requestId];
    resolve && resolve(data);
    wsFetchResolvers[data.requestId] = null;
}
