using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Web3.Accounts;
using Nethereum.Util;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Hex.HexTypes;
using System.Numerics;

namespace API.Services
{
    public class BlockchainService
    {
        private readonly Web3 _web3;
        private readonly IConfiguration _configuration;
        private string _propertyContractAddress;
        private string _userContractAddress;

        public BlockchainService(IConfiguration configuration)
        {
            _configuration = configuration;
            
            // Connect to local Ganache blockchain
            var url = "http://127.0.0.1:7545"; // Ganache default URL
            _web3 = new Web3(url);
            
            // !!! Set contract addresses after deployment VIP
            _propertyContractAddress = ""; // UPDATE AFTER DEPLOYMENT
            _userContractAddress = ""; // UPDATE AFTER DEPLOYMENT
        }

        #region Network and Account Methods

        public async Task<List<string>> GetAccountsAsync()
        {
            return await _web3.Eth.Accounts.SendRequestAsync();
        }

        public async Task<decimal> GetBalanceAsync(string address)
        {
            var balance = await _web3.Eth.GetBalance.SendRequestAsync(address);
            return Web3.Convert.FromWei(balance.Value);
        }

        public void SetContractAddresses(string propertyAddress, string userAddress)
        {
            _propertyContractAddress = propertyAddress;
            _userContractAddress = userAddress;
        }

        public ContractAddressInfo GetContractAddresses()
        {
            return new ContractAddressInfo
            {
                PropertyContractAddress = _propertyContractAddress,
                UserContractAddress = _userContractAddress
            };
        }

        public async Task<TransactionReceipt> SendTransactionAsync(string fromAddress, string toAddress, decimal etherAmount, string privateKey)
        {
            // Create a new account with the private key
            var account = new Account(privateKey);
            var web3 = new Web3(account, _web3.Client.Url);

            // Convert ether to wei
            var weiAmount = Web3.Convert.ToWei(etherAmount);

            // Create and send transaction
            var transaction = await web3.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(toAddress, etherAmount);

            return transaction;
        }

        public async Task<TransactionReceipt> GetTransactionReceiptAsync(string transactionHash)
        {
            return await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
        }

        #endregion

        #region PropertyInfo Contract

        #region Function Definitions

        // These are PropertyInfo contract functions

        [Function("getProperty", typeof(PropertyDetailsDTO))]
        public class GetPropertyFunction : FunctionMessage
        {
            [Parameter("uint256", "_id", 1)]
            public BigInteger PropertyId { get; set; }
        }

        [FunctionOutput]
        public class PropertyDetailsDTO : IFunctionOutputDTO
        {
            [Parameter("string", "location", 1)]
            public string Location { get; set; }

            [Parameter("string", "specifications", 2)]
            public string Specifications { get; set; }

            [Parameter("bool", "rentalStatus", 3)]
            public bool RentalStatus { get; set; }

            [Parameter("string", "maintenanceSchedule", 4)]
            public string MaintenanceSchedule { get; set; }

            [Parameter("string[]", "photos", 5)]
            public List<string> Photos { get; set; }

            [Parameter("address", "owner", 6)]
            public string Owner { get; set; }
        }

        [Function("getOwnerProperties", typeof(PropertyIdsDTO))]
        public class GetOwnerPropertiesFunction : FunctionMessage
        {
            [Parameter("address", "_owner", 1)]
            public string OwnerAddress { get; set; }
        }

        [FunctionOutput]
        public class PropertyIdsDTO : IFunctionOutputDTO
        {
            [Parameter("uint256[]", "", 1)]
            public List<BigInteger> PropertyIds { get; set; }
        }

        [Function("registerProperty")]
        public class RegisterPropertyFunction : FunctionMessage
        {
            [Parameter("string", "_location", 1)]
            public string Location { get; set; }

            [Parameter("string", "_specifications", 2)]
            public string Specifications { get; set; }

            [Parameter("bool", "_rentalStatus", 3)]
            public bool RentalStatus { get; set; }

            [Parameter("string", "_maintenanceSchedule", 4)]
            public string MaintenanceSchedule { get; set; }

            [Parameter("string[]", "_photos", 5)]
            public List<string> Photos { get; set; }
        }

        [Function("updateProperty")]
        public class UpdatePropertyFunction : FunctionMessage
        {
            [Parameter("uint256", "_id", 1)]
            public BigInteger PropertyId { get; set; }

            [Parameter("string", "_location", 2)]
            public string Location { get; set; }

            [Parameter("string", "_specifications", 3)]
            public string Specifications { get; set; }

            [Parameter("bool", "_rentalStatus", 4)]
            public bool RentalStatus { get; set; }

            [Parameter("string", "_maintenanceSchedule", 5)]
            public string MaintenanceSchedule { get; set; }

            [Parameter("string[]", "_photos", 6)]
            public List<string> Photos { get; set; }
        }

        [Function("transferProperty")]
        public class TransferPropertyFunction : FunctionMessage
        {
            [Parameter("uint256", "_id", 1)]
            public BigInteger PropertyId { get; set; }

            [Parameter("address", "_newOwner", 2)]
            public string NewOwnerAddress { get; set; }
        }

        #endregion

        // Query Methods (Read-only - no private keys needed)
        public async Task<PropertyDetailsDTO> GetPropertyAsync(BigInteger propertyId)
        {
            if (string.IsNullOrEmpty(_propertyContractAddress))
                throw new Exception("Property contract address not configured");

            var getPropertyFunction = new GetPropertyFunction
            {
                PropertyId = propertyId
            };

            var handler = _web3.Eth.GetContractQueryHandler<GetPropertyFunction>();
            return await handler.QueryAsync<PropertyDetailsDTO>(_propertyContractAddress, getPropertyFunction);
        }

        public async Task<List<BigInteger>> GetOwnerPropertiesAsync(string ownerAddress)
        {
            if (string.IsNullOrEmpty(_propertyContractAddress))
                throw new Exception("Property contract address not configured");

            var getOwnerPropertiesFunction = new GetOwnerPropertiesFunction
            {
                OwnerAddress = ownerAddress
            };

            var handler = _web3.Eth.GetContractQueryHandler<GetOwnerPropertiesFunction>();
            var result = await handler.QueryAsync<PropertyIdsDTO>(_propertyContractAddress, getOwnerPropertiesFunction);
            return result.PropertyIds;
        }

        // Transaction Methods (Write operations)
        public async Task<TransactionReceipt> RegisterPropertyAsync(
            string location, 
            string specifications, 
            bool rentalStatus, 
            string maintenanceSchedule, 
            List<string> photos,
            string fromAddress,
            string privateKey)
        {
            if (string.IsNullOrEmpty(_propertyContractAddress))
                throw new Exception("Property contract address not configured");

            // Create a new account with the private key
            var account = new Account(privateKey);
            var web3 = new Web3(account, _web3.Client.Url);

            var registerPropertyFunction = new RegisterPropertyFunction
            {
                Location = location,
                Specifications = specifications,
                RentalStatus = rentalStatus,
                MaintenanceSchedule = maintenanceSchedule,
                Photos = photos,
                FromAddress = fromAddress,
                Gas = 3000000 // Set gas limit - doesn't really matter because we're not committing to the blockchain
            };

            var handler = web3.Eth.GetContractTransactionHandler<RegisterPropertyFunction>();
            return await handler.SendRequestAndWaitForReceiptAsync(_propertyContractAddress, registerPropertyFunction);
        }

        public async Task<TransactionReceipt> UpdatePropertyAsync(
            BigInteger propertyId,
            string location, 
            string specifications, 
            bool rentalStatus, 
            string maintenanceSchedule, 
            List<string> photos,
            string fromAddress,
            string privateKey)
        {
            if (string.IsNullOrEmpty(_propertyContractAddress))
                throw new Exception("Property contract address not configured");

            // Create a new account with the private key
            var account = new Account(privateKey);
            var web3 = new Web3(account, _web3.Client.Url);

            var updatePropertyFunction = new UpdatePropertyFunction
            {
                PropertyId = propertyId,
                Location = location,
                Specifications = specifications,
                RentalStatus = rentalStatus,
                MaintenanceSchedule = maintenanceSchedule,
                Photos = photos,
                FromAddress = fromAddress,
                Gas = 3000000
            };

            var handler = web3.Eth.GetContractTransactionHandler<UpdatePropertyFunction>();
            return await handler.SendRequestAndWaitForReceiptAsync(_propertyContractAddress, updatePropertyFunction);
        }

        public async Task<TransactionReceipt> TransferPropertyAsync(
            BigInteger propertyId,
            string newOwnerAddress,
            string fromAddress,
            string privateKey)
        {
            if (string.IsNullOrEmpty(_propertyContractAddress))
                throw new Exception("Property contract address not configured");

            // Create a new account with the private key
            var account = new Account(privateKey);
            var web3 = new Web3(account, _web3.Client.Url);

            var transferPropertyFunction = new TransferPropertyFunction
            {
                PropertyId = propertyId,
                NewOwnerAddress = newOwnerAddress,
                FromAddress = fromAddress,
                Gas = 3000000
            };

            var handler = web3.Eth.GetContractTransactionHandler<TransferPropertyFunction>();
            return await handler.SendRequestAndWaitForReceiptAsync(_propertyContractAddress, transferPropertyFunction);
        }

        #endregion

        #region UserInfo Contract

        #region Function Definitions

        // User Function Definitions
        [Function("getUserInfo", typeof(UserDetailsDTO))]
        public class GetUserInfoFunction : FunctionMessage
        {
            // No parameters needed as the function uses msg.sender
        }

        [FunctionOutput]
        public class UserDetailsDTO : IFunctionOutputDTO
        {
            [Parameter("string", "", 1)]
            public string FirstName { get; set; }

            [Parameter("string", "", 2)]
            public string LastName { get; set; }

            [Parameter("string", "", 3)]
            public string Email { get; set; }
        }

        [Function("registerUser")]
        public class RegisterUserFunction : FunctionMessage
        {
            [Parameter("string", "_firstname", 1)]
            public string FirstName { get; set; }

            [Parameter("string", "_lastname", 2)]
            public string LastName { get; set; }

            [Parameter("string", "_email", 3)]
            public string Email { get; set; }

            [Parameter("string", "_kycDocumentHash", 4)]
            public string KycDocumentHash { get; set; }

            [Parameter("string", "_bankingInfoEncrypted", 5)]
            public string BankingInfoEncrypted { get; set; }
        }

        #endregion

        // Query Methods (Read-only)
        public async Task<UserDetailsDTO> GetUserAsync(string userAddress)
        {
            if (string.IsNullOrEmpty(_userContractAddress))
                throw new Exception("User contract address not configured");

            // Since getUserInfo uses msg.sender, we need to create a custom method to call the contract
            // from the user's address to get their information
            
            // This is a simplified approach that doesn't require the user's private key
            // In a real implementation, you would need to call the contract from the user's account
            var function = _web3.Eth.GetContract(GetUserInfoABI(), _userContractAddress)
                .GetFunction("getUserInfo");
            
            var result = await function.CallDeserializingToObjectAsync<UserDetailsDTO>();
            return result;
        }

        // Transaction Methods (Write operations)
        public async Task<TransactionReceipt> RegisterUserAsync(
            string firstName,
            string lastName,
            string email,
            string kycDocumentHash,
            string bankingInfoEncrypted,
            string fromAddress,
            string privateKey)
        {
            if (string.IsNullOrEmpty(_userContractAddress))
                throw new Exception("User contract address not configured");

            // Create a new account with the private key
            var account = new Account(privateKey);
            var web3 = new Web3(account, _web3.Client.Url);

            var registerUserFunction = new RegisterUserFunction
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                KycDocumentHash = kycDocumentHash,
                BankingInfoEncrypted = bankingInfoEncrypted,
                FromAddress = fromAddress,
                Gas = 3000000
            };

            var handler = web3.Eth.GetContractTransactionHandler<RegisterUserFunction>();
            return await handler.SendRequestAndWaitForReceiptAsync(_userContractAddress, registerUserFunction);
        }
        
        // Helper method to get UserInfo ABI (basically a list of functions and their parameters) for direct function calls
        private string GetUserInfoABI()
        {
            // Simplified ABI for the getUserInfo function 
            // No need for private key - ABI is public (what is abi ? = application binary interface)
            // Rationale=
            // We want to be able to call the getUserInfo function from the contract
            // We can't call it directly because it's a view function (read-only) thats's accessible to every part of the app.
            // So we need to use the ABI to call it
            return @"[
                {
                    ""inputs"": [],
                    ""name"": ""getUserInfo"",
                    ""outputs"": [
                        {
                            ""internalType"": ""string"",
                            ""name"": """",
                            ""type"": ""string""
                        },
                        {
                            ""internalType"": ""string"",
                            ""name"": """",
                            ""type"": ""string""
                        },
                        {
                            ""internalType"": ""string"",
                            ""name"": """",
                            ""type"": ""string""
                        }
                    ],
                    ""stateMutability"": ""view"",
                    ""type"": ""function""
                }
            ]";
        }

        #endregion
    }

    public class ContractAddressInfo
    {
        public string PropertyContractAddress { get; set; }
        public string UserContractAddress { get; set; }
    }
} 