using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.Web3.Accounts.Managed;

namespace BlockchainClient
{
    public partial class MainWindow : Window
    {
        private string rpcUrl = "http://127.0.0.1:7545"; // Ganache RPC URL
        private string contractAddress = "0xAc4105FdB50254EEDA1aeFa982261d62B9aBacDb"; // Replace with your contract address
        private string senderAddress = "0xE5C7A94F27F5D0BeD20Fe05e2194374569dcDd1C"; // Replace with your account address
        private string senderPrivateKey = "0x1ead5f0761a26d375680e564c5ba3fff7ffdb19a158c1e71be65761b3941dfcf\r\n"; // Use a secure method for private keys
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


        private Web3 web3;
        private Contract contract;

        public MainWindow()
        {
            InitializeComponent();

            Initialize();

        }

        private void Initialize()
        {
            var account = new ManagedAccount(senderAddress, senderPrivateKey);
            web3 = new Web3(account, rpcUrl);
            contract = web3.Eth.GetContract(abi, contractAddress);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var getFunction = contract.GetFunction("getMessage");
            var value = await getFunction.CallAsync<string>();
            txtMessage.Text = "Stored Message: " + value;
        }
    }
}