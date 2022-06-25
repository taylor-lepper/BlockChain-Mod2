const ethers = require("ethers");

function restoreHDWallet(mnemonic) {
	return ethers.utils.HDNode.fromMnemonic(mnemonic);
}

let mnemonic =
	"upset fuel enhance depart portion hope core animal innocent will athlete snack";

console.log(restoreHDWallet(mnemonic));

// wallet instance

function restoreHDWallet2(mnemonic) {
	return ethers.Wallet.fromMnemonic(mnemonic);
}

let mnemonic2 =
	"upset fuel enhance depart portion hope core animal innocent will athlete snack";

console.log(restoreHDWallet2(mnemonic2));
