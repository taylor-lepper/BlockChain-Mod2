const ethers = require("ethers");

function recoverWalletFromPrivaeKey(privateKey) {
	return new ethers.Wallet(privateKey);
}

async function saveWalletToJSON(wallet, password) {
	return wallet.encrypt(password);
}

async function revcoverWalletFromJSON(json, password) {
	return ethers.Wallet.fromEncryptedJson(json, password);
}

let privateKey =
	"0x495d5c34c912291807c25d5e8300d20b749f6be44a178d5c50f167d495f3315a";
let wallet = recoverWalletFromPrivaeKey(privateKey);
let password = "p@$$w0rd~3";

(async () => {
	const encryptedWallet = await saveWalletToJSON(wallet, password);
	const decryptedWallet = await revcoverWalletFromJSON(
		encryptedWallet,
		password
	);
	console.log(decryptedWallet);
})();
