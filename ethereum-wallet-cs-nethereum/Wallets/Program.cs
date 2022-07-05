﻿using System;
using static System.Console;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Nethereum.HdWallet;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using NBitcoin;
using Rijndael256;
using System.Threading;

namespace Wallets
{
    class EthereumWallet
    {
        const string CURRENT_NETWORK = "https://ropsten.infura.io/v3/c1f509a577f44113adbcfe2bfc3505cc"; //  Specify which network you are going to use.
        const string WORKING_DIRECTORY = @"Wallets\"; // Path where you want to store the Wallets.

        static void Main(string[] args)
        {
            MainAsync();
        }

        static async void MainAsync()
        {
            // Available commands
            string[] availableOperations =
            {
                "create", "load", "recover", "exit"
            };

            string input = string.Empty;
            bool isWalletReady = false;
            Wallet wallet = new Wallet(Wordlist.English, WordCount.Twelve);

            //  Initialize the Web3 instance and create the Storage Directory

            Web3 web3 = new Web3(CURRENT_NETWORK);
            Directory.CreateDirectory(WORKING_DIRECTORY);


            while (!input.ToLower().Equals("exit"))
            {
                if (!isWalletReady)
                {
                    do
                    {
                        input = ReceiveCommandCreateLoadOrRecover();

                    } while (!((IList)availableOperations).Contains(input));
                    switch (input)
                    {
                        /* Create a brand new wallet. 
                         * Users will receive a mnemonic phrase and public-private keypairs. */
                        case "create":
                            wallet = CreateWalletDialog();
                            isWalletReady = true;
                            break;

                        /* This command will decrypt the words and load wallet.
                         * Load wallet from JSON file containing an encrypted mnemonic phrase. */
                        case "load":
                            wallet = LoadWalletDialog();
                            isWalletReady = true;
                            break;

                        /* Recover wallet from mnemonic phrase which the user will provide.
                         * This is usefull if user already has an existing wallet, but has no json file for him 
                         * (for example, he uses this program for the first time).
                         * Command will create a new JSON file containing encrypted mnemonic phrase
                         * for this wallet.
                         * After encrypting the words and saving to disk, the program will load wallet.*/
                        case "recover":
                            wallet = RecoverWalletDialog();
                            isWalletReady = true;
                            break;

                        // Exit the program.
                        case "exit":
                            return;
                    }
                }
                else // Wallet already loaded.
                {
                    // Allowed functionalities
                    string[] walletAvailableOperations = {
                        "balance", "receive", "send", "exit"
                    };

                    string inputCommand = string.Empty;

                    while (!inputCommand.ToLower().Equals("exit"))
                    {
                        do
                        {
                            inputCommand = ReceiveCommandForEthersOperations();

                        } while (!((IList)walletAvailableOperations).Contains(inputCommand));
                        switch (inputCommand)
                        {
                            // Send transaction from own wallet to another address.
                            case "send":
                                SendTransactionDialog(wallet);
                                break;

                            // Shows the balances of addresses and total balance.
                            case "balance":
                                await GetWalletBallanceDialog(web3, wallet);
                                break;

                            // Shows available addresses under the control of your wallet which you can receive coins.
                            case "receive":
                                Receive(wallet);
                                break;
                            case "exit":
                                return;
                        }
                    }
                }
            }
        }

        // Preset codes: Dialogs ============================
        private static Wallet CreateWalletDialog()
        {
            try
            {
                string password;
                string passwordConfirmed;
                do
                {
                    Write("Enter password for encryption: ");
                    password = ReadLine();
                    Write("Confirm password: ");
                    passwordConfirmed = ReadLine();
                    if (password != passwordConfirmed)
                    {
                        WriteLine("Passwords did not match!");
                        WriteLine("Try again.");
                    }
                } while (password != passwordConfirmed);

                // Create new Wallet with the provided password.
                Wallet wallet = CreateWallet(password, WORKING_DIRECTORY);
                return wallet;
            }
            catch (Exception)
            {
                WriteLine($"ERROR! Wallet in path {WORKING_DIRECTORY} can`t be created!");
                throw;
            }
        }
        private static Wallet LoadWalletDialog()
        {
            Write("Enter: Name of the file containing wallet: ");
            string nameOfWallet = ReadLine();
            Write("Enter: Password: ");
            string pass = ReadLine();
            try
            {
                // Loading the Wallet from an JSON file. Using the Password to decrypt it.
                Wallet wallet = LoadWalletFromJsonFile(nameOfWallet, WORKING_DIRECTORY, pass);
                return (wallet);

            }
            catch (Exception e)
            {
                WriteLine($"ERROR! Wallet {nameOfWallet} in path {WORKING_DIRECTORY} can`t be loaded!");
                throw e;
            }
        }
        private static Wallet RecoverWalletDialog()
        {
            try
            {
                Write("Enter: Mnemonic words with single space separator: ");
                string mnemonicPhrase = ReadLine();
                Write("Enter: password for encryption: ");
                string passForEncryptionInJsonFile = ReadLine();

                // Recovering the Wallet from Mnemonic Phrase
                Wallet wallet = RecoverFromMnemonicPhraseAndSaveToJson(
                    mnemonicPhrase, passForEncryptionInJsonFile, WORKING_DIRECTORY);
                return wallet;
            }
            catch (Exception e)
            {
                WriteLine("ERROR! Wallet can`t be recovered! Check your mnemonic phrase.");
                throw e;
            }
        }
        private static async Task GetWalletBallanceDialog(Web3 web3, Wallet wallet)
        {
            WriteLine("Balance:");
            try
            {
                // Getting the Balance and Displaying the Information.
                Balance(web3, wallet);
            }
            catch (Exception)
            {
                WriteLine("Error occured! Check your wallet.");
            }
        }
        private static void SendTransactionDialog(Wallet wallet)
        {
            WriteLine("Enter: Address sending ethers.");
            string fromAddress = ReadLine();
            WriteLine("Enter: Address receiving ethers.");
            string toAddress = ReadLine();
            WriteLine("Enter: Amount of coins in ETH.");
            double amountOfCoins = 0d;
            try
            {
                amountOfCoins = double.Parse(ReadLine());
            }
            catch (Exception)
            {
                WriteLine("Unacceptable input for amount of coins.");
            }
            if (amountOfCoins > 0.0d)
            {
                WriteLine($"You will send {amountOfCoins} ETH from {fromAddress} to {toAddress}");
                WriteLine($"Are you sure? yes/no");
                string answer = ReadLine();
                if (answer.ToLower() == "yes")
                {
                    // Send the Transaction.
                    Send(wallet, fromAddress, toAddress, amountOfCoins);
                }
            }
            else
            {
                WriteLine("Amount of coins for transaction must be positive number!");
            }
        }
        private static string ReceiveCommandCreateLoadOrRecover()
        {
            WriteLine("Choose working wallet.");
            WriteLine("Choose [create] to Create new Wallet.");
            WriteLine("Choose [load] to load existing Wallet from file.");
            WriteLine("Choose [recover] to recover Wallet with Mnemonic Phrase.");
            Write("Enter operation [\"Create\", \"Load\", \"Recover\", \"Exit\"]: ");
            string input = ReadLine().ToLower().Trim();
            return input;
        }
        private static string ReceiveCommandForEthersOperations()
        {
            Write("Enter operation [\"Balance\", \"Receive\", \"Send\", \"Exit\"]: ");
            string inputCommand = ReadLine().ToLower().Trim();
            return inputCommand;
        }

        // End preset codes ============================

        //  Implement these methods.

        public static Wallet CreateWallet(string password, string pathfile)
        {
            //  Create a new wallet via a random 12-word mnemonic.
            Wallet wallet = new Wallet(Wordlist.English, WordCount.Twelve);
            string words = string.Join(" ", wallet.Words);
            string fileName = string.Empty;

            try
            {
                //  Save the Wallet in the Directory path declared earlier.
                fileName = SaveWalletToJsonFile(wallet, password, pathfile);
            }
            catch (Exception e)
            {
                WriteLine($"ERROR! The file {fileName} can`t be saved! {e}");
                throw e;
            }

            WriteLine("New Wallet was created successfully!");
            WriteLine("Write down the following mnemonic words and keep them in a safe place.");
            WriteLine("---");

            //  Display the mnemonic phrase
            WriteLine("Mnemonic words:");
            WriteLine(words);
            WriteLine("---");

            //  Display the seed
            WriteLine("Seed: ");
            WriteLine(wallet.Seed);
            WriteLine("---");

            //  Implement and use PrintAddressesAndKeys to print all the Addresses and Keys.
            PrintAddressesAndKeys(wallet);

            return wallet;
        }

        private static void PrintAddressesAndKeys(Wallet wallet)
        {
            //  Print all the Addresses and the coresponding Private Keys.
            WriteLine("Address -> Private Key");
            int NUMBER_OF_DERIVATIONS = 20;

            for (int i = 0; i < NUMBER_OF_DERIVATIONS; i++)
            {
                WriteLine($"{wallet.GetAccount(i, 3).Address} -> {wallet.GetAccount(i, 3).PrivateKey}");
            }
            WriteLine(string.Empty);
        }

        private static string SaveWalletToJsonFile(Wallet wallet, string password, string pathfile)
        {
            // Encrypt the wallet
            string words = string.Join(" "), wallet.Words);
            string encryptedWords = Rijndael.Encrypt(words, password, KeySize.Aes256);

            // Save the Wallet as JSON to disk
            DateTime now = DateTime.Now;
            var walletJSONData = new { encryptedWords, date = now.ToString() };
            string JSON = JsonConvert.SerializeObject(walletJSONData);
            Random random = new Random();
            var fileName = "EthereumWallet_"
                + now.Year + "-"
                + now.Month + "-"
                + now.Day + "-"
                + now.Hour + "-"
                + now.Minute + "-"
                + now.Second + "-"
                + random.Next() + ".json";
            File.WriteAllText(Path.Combine(pathfile, fileName), JSON);
            WriteLine($"Wallet saved in {fileName}");
            return fileName;
        }

        private static Wallet LoadWalletFromJsonFile(string nameOfWalletFile, string path, string password)
        {
            //  Implement the logic that is needed to Load the Wallet from JSON.
            string pathToFile = Path.Combine(path, nameOfWalletFile);
            string words = string.Empty;
            WriteLine($"Read from {pathToFile}");

            // Read the Wallet from disk and decrypt
            try
            {
                string file = File.ReadAllText(pathToFile);
                dynamic results = JsonConvert.DeserializeObject<dynamic>(file);
                string encryptedWords = results.encryptedWords;
                words = Rijndael.Decrypt(encryptedWords, password, KeySize.Aes256);
                string dateAndTime = results.date;
            }
            catch (Exception e)
            {
                WriteLine($"Load error: {e.Message}");
                
            }
            return Recover(words);
        }

        private static Wallet Recover(string words)
        {
            //  Recover a Wallet from existing mnemonic phrase.
            Wallet wallet = new Wallet(words, null);
            WriteLine("Wallet was successfull recovered");
            WriteLine($"Mnemonic: {string.Join(" ", wallet.Words)}");
            WriteLine($"Seed: {string.Join(" ", string.Join(" ", wallet.Seed))}");
            WriteLine();
            PrintAddressesAndKeys(wallet);
            return wallet;
        }

        public static Wallet RecoverFromMnemonicPhraseAndSaveToJson(string words, string password, string pathfile)
        {
            //  Recover from mnemonic phrases and save to JSON.
            Wallet wallet = Recover(words);
            string fileName = string.Empty;


            //  Save the wallet to JSON.
            try
            {
                fileName = SaveWalletToJsonFile(wallet, password, pathfile);
            }
            catch (Exception e)
            {
                WriteLine($"Error! The file {fileName} cannot be saved: {e.Message}");
                throw e;
            }

            return wallet;
        }

        public static void Receive(Wallet wallet)
        {
            //  Print all available addresses in Wallet.
            if(wallet.GetAddresses().Count() > 0)
            {
                int NUMBER_OF_ITERATIONS = 20;
                for (int i = 0; i < NUMBER_OF_ITERATIONS; i++)
                {
                    WriteLine(wallet.GetAccount(i, 3).Address);
                }
                WriteLine();
            }
            else
            {
                WriteLine("No addresses found!");
            }
        }

        private static async void Send(Wallet wallet, string fromAddress, string toAddress, double amountOfCoins)
        {
            // Generate and Send a transaction.
            // Check if sending address is in the wallet by verifying if the private key exists.
            var privateKeyFrom = wallet.GetAccount(fromAddress).PrivateKey;


            if (privateKeyFrom == string.Empty)
            {
                WriteLine("Keys of sending address is not found in current wallet.");
            }

            var accountFrom = new Account(privateKeyFrom, Chain.Ropsten);

            // Initialize web3 and normalize transaction value.
            Web3 web3 = new Web3(accountFrom, CURRENT_NETWORK);
            System.Numerics.BigInteger wei = Web3.Convert.ToWei(amountOfCoins);


            // Broadcast transaction.
            try
            {
                var transactionReceipt = await web3.TransactionManager.TransactionReceiptService.SendRequestAndWaitForReceiptAsync(new TransactionInput() { from = accountFrom.Address, toAddress = toAddress, Value = new HexBigInteger(wei) }, null);
                WriteLine("\nTransaction has been sent successfully! \n" + "Trasaction hash: " + transactionReceipt.TransactionHash);
                    
            }
            catch (Exception e)
            {
                WriteLine($"Error! The transaction can't be completed: {e.Message}");
                throw e;
            }
        }

        private static void Balance(Web3 web3, Wallet wallet)
        {
            // Print all addresses and their corresponding balance.
            decimal totalBalance = 0.0m;
            int NUMBER_OF_ITERATIONS = 20;

            // Print the balance of each address.
            // Track these balances and print the total balance of the wallet as well at the end.
            for (int i = 0; i < NUMBER_OF_ITERATIONS; i++)
            {
                var address = wallet.GetAccount(i, 3).Address;
                var balance = web3.Eth.GetBalance.SendRequestAsync(address).Result;
                var etherAmount = Web3.Convert.FromWei(balance.Value);
                totalBalance += etherAmount;
            }

            WriteLine($"Total balance: {totalBalance} ETH");
        }
    }
}
