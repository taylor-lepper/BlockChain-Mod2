const fs = require("fs");
const path = require("path");
const express = require("express");

const app = express();

// require ethers
const ethers = require("ethers");
const { privateEncrypt } = require("crypto");

// set provider to be ropsten
const provider = ethers.getDefaultProvider("ropsten");

// set wallets directory
const walletDirectory = "wallets/";

if (!fs.existsSync(walletDirectory)) {
	fs.mkdirSync(walletDirectory);
}

app.use(express.urlencoded({ extended: false }));
app.use(express.json());
app.set("view engine", "ejs");
app.engine("html", require("ejs").renderFile);
app.use(express.static("public"));

//  Homepage
app.get("/", (req, res) => {
	res.render(__dirname + "/views/index.html");
});

//  Page for creating a wallet
app.get("/create", (req, res) => {
	res.render(__dirname + "/views/create.html");
});

//  Create endpoint
app.post("/create", (req, res) => {
	// fetch user input fields (password and confirmPassword)
	let password = req.body.password;
	let confirmPassword = req.body.confirmPassword;

	// Make simple validation
	if (password !== confirmPassword) {
		res.render(path.join(__dirname, "views", "create.html"), {
			mnemonic: undefined,
			jsonWallet: undefined,
			filename: null,
			error: "Passwords do not match",
		});
		return false;
	}

	// Generate wallet from mnemonic
	const wallet = new ethers.Wallet.createRandom();

	// Encrypt and save as json file
	wallet.encrypt(password).then((jsonWallet) => {
		let filename =
			"UTC_JSON_WALLET_" +
			Math.round(+new Date() / 1000) +
			"_" +
			Math.random(10000, 10000) +
			".json";

		// Make a file with the wallet data
		fs.writeFile(walletDirectory + filename, jsonWallet, "utf-8", (err) => {
			if (err) {
				drawView(res, "create", {
					mnemonic: undefined,
					jsonWallet: undefined,
					filename: undefined,
					error: "Problem writing to disk" + err.message,
				});
			} else {
				drawView(res, "create", {
					mnemonic: wallet.mnemonic.phrase,
					jsonWallet: JSON.stringify(jsonWallet),
					filename: filename,
					error: undefined,
				});
			}
		});
	});
});

//load your wallet
app.get("/load", (req, res) => {
	res.render(__dirname + "/views/load.html");
});

app.post("/load", (req, res) => {
	// fetch user data (filename and password)
	let filename = req.body.filename;
	let password = req.body.password;

	fs.readFile(walletDirectory + filename, "utf8", (err, jsonWallet) => {
		//  Error handling
		if (err) {
			drawView(res, "load", {
				address: undefined,
				privateKey: undefined,
				mnemonic: undefined,
				error: "The file doesn't exist",
			});
			return;
		}

		// decrypt the wallet
		ethers.Wallet.fromEncryptedJson(jsonWallet, password)
			.then((wallet) => {
				drawView(res, "load", {
					address: wallet.address,
					privateKey: wallet.privateKey,
					mnemonic: wallet.mnemonic.phrase,
					error: undefined,
				});
			})
			.catch((err) => {
				drawView(res, "load", {
					address: undefined,
					privateKey: undefined,
					mnemonic: undefined,
					error: "Bad password: " + err.message,
				});
			});
	});
});

//recover wallet
app.get("/recover", (req, res) => {
	res.render(__dirname + "/views/recover.html");
});

//recover wallet
app.post("/recover", (req, res) => {
	// fetch user data (mnemonic and password)
	let mnemonic = req.body.mnemonic;
	let password = req.body.password;

	// make wallet instance of this mnemonic
	const wallet = ethers.Wallet.fromMnemonic(mnemonic);

	// encrypt and save the wallet
	wallet.encrypt(password).then((jsonWallet) => {
		let filename =
			"UTC_JSON_WALLET_" +
			Math.round(+new Date() / 1000) +
			"_" +
			Math.random(10000, 10000) +
			".json";

		// Make a file with the wallet data
		fs.writeFile(walletDirectory + filename, jsonWallet, "utf-8", (err) => {
			if (err) {
				drawView(res, "recover", {
					message: undefined,
					filename: undefined,
					mnemonic: undefined,
					error: "Recovery error: " + err.message,
				});
			} else {
				drawView(res, "recover", {
					message: "Wallet recovery successful!",
					filename: filename,
					mnemonic: wallet.mnemonic.phrase,
					error: undefined,
				});
			}
		});
	});
});

app.get("/balance", (req, res) => {
	res.render(__dirname + "/views/balance.html");
});

app.post("/balance", (req, res) => {
	// fetch user data (filename and password)
	let filename = req.body.filename;
	let password = req.body.password;

	//  read the file
	fs.readFile(walletDirectory + filename, "utf8", async (err, jsonWallet) => {
		if (err) {
			drawView(res, "balance", {
				wallets: undefined,
				error: "Error with file writing",
			});
		}

		ethers.Wallet.fromEncryptedJson(jsonWallet, password)
			.then(async (wallet) => {
				// generate 5 wallets from your master key

				let derivationPath = "m/44'/60'/0'/0/";
				let wallets = [];

				for (let i = 0; i < 5; i++) {
					let hdNode = ethers.utils.HDNode.fromMnemonic(
						wallet.mnemonic.phrase
					).derivePath(derivationPath + i);
					let walletInstance = new ethers.Wallet(
						hdNode.privateKey,
						provider
					);
					let balance = await walletInstance.getBalance();
					wallets.push({
						keypair: walletInstance,
						balance: ethers.utils.formatEther(balance),
					});
				}
				drawView(res, "balance", {
					wallets: wallets,
					error: undefined,
				});
			})
			.catch((err) => {
				drawView(res, "balance", {
					wallets: undefined,
					error: "Balance query error: " + err.message,
				});
			});
	});
});

app.get("/send", (req, res) => {
	res.render(__dirname + "/views/send.html");
});

app.post("/send", (req, res) => {
	// fetch user data (recipient,private key, and amount)
	let recipient = req.body.recipient;
	let privateKey = req.body.privateKey;
	let amount = req.body.amount;

	//  Simple validation
	if (
		recipient === "" ||
		(recipient === undefined && privateKey === "") ||
		(privateKey === undefined && amount === "") ||
		amount === undefined ||
		amount <= 0
	) {
		return;
	}

	let wallet;

	try {
		// make instance of the wallet
		wallet = new ethers.Wallet(privateKey, provider);
	} catch (err) {
		drawView(res, "send", {
			transactionHash: undefined,
			error: err.message,
		});
		return;
	}

	let gasPrice = 6;
	let gas = 21000;

	// broadcast the transaction to the network
	wallet
		.sendTransaction({
			to: recipient,
			value: ethers.utils.parseEther(amount),
			gasLimit: gas * gasPrice,
		})
		.then((transaction) => {
			drawView(res, "send", {
				transactionHash: transaction.hash,
				error: undefined,
			});
		})
		.catch((err) => {
			console.log(err);
			drawView(res, "send", {
				transactionHash: undefined,
				error: err.message,
			});
		});
});

//public address
//  0x6d3c9ECC4722E9eBe71A307Aa0619EA8a9F98Cc5

//private key
//   0x9868c72bcdc714dcfa7a9f9545a344e24102bde38caa2abe188e42b1db288fea

//reciever address
//    0xa5e041b243d1f302a0a108D33eB571f66Dd6bbeE

// Preset helper functions ===

function drawView(res, view, data) {
	res.render(__dirname + "/views/" + view + ".html", data);
}

app.listen(3000, () => {
	console.log("App running on http://localhost:3000");
});
