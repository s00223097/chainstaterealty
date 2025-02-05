using Microsoft.Extensions.Configuration;
using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using System.Numerics;
using System.Threading.Tasks;

namespace BlockchainAPI.Services
{
    //TESTING ONLY
    public class BlockchainService
    {
        private string rpcUrl = "http://127.0.0.1:7545"; // Ganache RPC URL

        //for the Test contract
        private string contractAddress = "0xAc4105FdB50254EEDA1aeFa982261d62B9aBacDb"; // Replace with your contract address

        //got these from ganache
        private string senderAddress = "0xE5C7A94F27F5D0BeD20Fe05e2194374569dcDd1C"; // Replace with your account address
        private string senderPrivateKey = "0x1ead5f0761a26d375680e564c5ba3fff7ffdb19a158c1e71be65761b3941dfcf\r\n"; // Use a secure method for private keys

        //this is found in build\contracts Test.json and contains the declarations details of teh contreact (variables, functions etc.)
        //will have to create a class that wraps this up for each contacrt type.
        private string abi = @"[
    {
      ""inputs"": [
        {
          ""internalType"": ""string"",
          ""name"": ""_message"",
          ""type"": ""string""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""constructor""
    },
    {
      ""anonymous"": false,
      ""inputs"": [
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""owner"",
          ""type"": ""address""
        },
        {
          ""indexed"": false,
          ""internalType"": ""string"",
          ""name"": ""initialMessage"",
          ""type"": ""string""
        }
      ],
      ""name"": ""ContractDeployed"",
      ""type"": ""event""
    },
    {
      ""inputs"": [],
      ""name"": ""message"",
      ""outputs"": [
        {
          ""internalType"": ""string"",
          ""name"": """",
          ""type"": ""string""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function"",
      ""constant"": true
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""string"",
          ""name"": ""_newMessage"",
          ""type"": ""string""
        }
      ],
      ""name"": ""updateMessage"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
 {
      ""inputs"": [],
      ""name"": ""getMessage"",
      ""outputs"": [
        {
          ""internalType"": ""string"",
          ""name"": """",
          ""type"": ""string""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function"",
      ""constant"": true
    }
]";

        private readonly Web3 web3;
        private readonly Contract contract;

        public BlockchainService(IConfiguration configuration)
        {
            web3 = new Web3(rpcUrl);
            contract = web3.Eth.GetContract(abi, contractAddress);
        }

        public async Task<string> GetMessageAsync()
        {
            var getFunction = contract.GetFunction("getMessage");
            return await getFunction.CallAsync<string>();
        }

        public async Task<string> UpdateMessageAsync(string newMessage)
        {
            var updateFunction = contract.GetFunction("updateMessage");
            var transactionHash = await updateFunction.SendTransactionAsync(senderAddress, new HexBigInteger(300000), newMessage);
            return transactionHash;
        }
    }
}
