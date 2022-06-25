const ethers = require("ethers");

async function decryptWallet(json, password) {
	return ethers.Wallet.fromEncryptedJson(json, password);
}

async function createAndSaveWalletAsJSON(password) {
	const wallet = await ethers.Wallet.createRandom();
	console.log("Wallet before encryption ===");
	console.log(wallet);
	return wallet.encrypt(password);
}

(async () => {
	const password = "Kingsland University";

	//Encryption
	let json = await createAndSaveWalletAsJSON(password);

	//Decryption
	let walletDecrypted = await decryptWallet(json, password);
	console.log("Wallet after decryption ===");
	console.log(walletDecrypted);
})();
