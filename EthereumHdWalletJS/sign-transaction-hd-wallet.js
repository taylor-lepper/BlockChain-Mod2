const ethers = require("ethers");

deriveFiveWalletsFromHdNode();

async function signTransaction(wallet, toAddress, value) {
	let transaction = {
		nonce: 0,
		gasLimit: 21000,
		gasPrice: ethers.BigNumber.from("0x1"),
		to: toAddress,
		value: ethers.utils.parseEther(value),
		data: "0x",
	};
	const signedTransaction = await wallet.signTransaction(transaction);
	console.log("Signed Transaction:");
	console.log(signedTransaction);
	return signedTransaction;
}

let wallet =
	"0x3b8f83aa8b84e26ad0c2723a80da43c6c7b0c9497fe9b2c871ff14bad5f06f15";
let toAddress = "0xfDd85780CB96f4712a869aB4d04f9D80c3CeE283";
let value = "10";

console.log(signTransaction(wallet, toAddress, value));

//index 1 wallet
// Derivation path m/44'/60'/0'/0/1
// Address 0xBf5CDc23b1aCF7DE1B9aAcA91Dd686Ba1139D6a1
// Private key 0x3b8f83aa8b84e26ad0c2723a80da43c6c7b0c9497fe9b2c871ff14bad5f06f15

// Wallet {
//     _isSigner: true,
//     _signingKey: [Function (anonymous)],
//     _mnemonic: [Function (anonymous)],
//     address: '0xBf5CDc23b1aCF7DE1B9aAcA91Dd686Ba1139D6a1',
//     provider: null
//   }
