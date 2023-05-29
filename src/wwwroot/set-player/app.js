'use strict';

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


document.getElementById('set').onclick = async e => {
    document.getElementById('set').remove();
    document.getElementById('main').classList.remove('hidden');

}