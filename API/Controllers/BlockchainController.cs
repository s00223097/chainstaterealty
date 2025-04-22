using Microsoft.AspNetCore.Mvc;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.Eth.DTOs;
using System.Numerics;
using System.Threading.Tasks;
using API.Services;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlockchainController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly BlockchainService _blockchainService;

        public BlockchainController(IConfiguration configuration, BlockchainService blockchainService)
        {
            _configuration = configuration;
            _blockchainService = blockchainService;
        }

        #region Network and Accounts

        [HttpGet("accounts")]
        public async Task<IActionResult> GetAccounts()
        {
            try
            {
                var accounts = await _blockchainService.GetAccountsAsync();
                return Ok(accounts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to get accounts: {ex.Message}");
            }
        }

        [HttpGet("balance/{address}")]
        public async Task<IActionResult> GetBalance(string address)
        {
            try
            {
                var balance = await _blockchainService.GetBalanceAsync(address);
                return Ok(new { address, balance = balance.ToString() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to get balance: {ex.Message}");
            }
        }

        [HttpPost("set-contracts")]
        public IActionResult SetContractAddresses([FromBody] ContractAddresses addresses)
        {
            try
            {
                _blockchainService.SetContractAddresses(addresses.PropertyAddress, addresses.UserAddress);
                return Ok(new { message = "Contract addresses updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to set contract addresses: {ex.Message}");
            }
        }

        [HttpGet("contract-addresses")]
        public IActionResult GetContractAddresses()
        {
            try
            {
                var addresses = _blockchainService.GetContractAddresses();
                return Ok(addresses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to get contract addresses: {ex.Message}");
            }
        }

        #endregion

        #region Property Management

        [HttpGet("property/{propertyId}")]
        public async Task<IActionResult> GetProperty(int propertyId)
        {
            try
            {
                var property = await _blockchainService.GetPropertyAsync(new BigInteger(propertyId));
                return Ok(property);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to get property: {ex.Message}");
            }
        }

        [HttpGet("properties/owner/{ownerAddress}")]
        public async Task<IActionResult> GetPropertiesByOwner(string ownerAddress)
        {
            try
            {
                var properties = await _blockchainService.GetOwnerPropertiesAsync(ownerAddress);
                return Ok(properties);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to get properties for owner: {ex.Message}");
            }
        }

        [HttpPost("property/register")]
        public async Task<IActionResult> RegisterProperty([FromBody] RegisterPropertyRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FromAddress))
                {
                    return BadRequest("From address is required");
                }

                var receipt = await _blockchainService.RegisterPropertyAsync(
                    request.Location,
                    request.Specifications,
                    request.RentalStatus,
                    request.MaintenanceSchedule,
                    request.Photos,
                    request.FromAddress,
                    request.PrivateKey
                );
                
                return Ok(new { 
                    transactionHash = receipt.TransactionHash,
                    blockNumber = receipt.BlockNumber.Value.ToString(),
                    gasUsed = receipt.GasUsed.Value.ToString(),
                    status = receipt.Status.Value.ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to register property: {ex.Message}");
            }
        }

        [HttpPut("property/{propertyId}/update")]
        public async Task<IActionResult> UpdateProperty(int propertyId, [FromBody] UpdatePropertyRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FromAddress))
                {
                    return BadRequest("From address is required");
                }

                var receipt = await _blockchainService.UpdatePropertyAsync(
                    new BigInteger(propertyId),
                    request.Location,
                    request.Specifications,
                    request.RentalStatus,
                    request.MaintenanceSchedule,
                    request.Photos,
                    request.FromAddress,
                    request.PrivateKey
                );
                
                return Ok(new { 
                    transactionHash = receipt.TransactionHash,
                    blockNumber = receipt.BlockNumber.Value.ToString(),
                    gasUsed = receipt.GasUsed.Value.ToString(),
                    status = receipt.Status.Value.ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to update property: {ex.Message}");
            }
        }

        [HttpPost("property/{propertyId}/transfer")]
        public async Task<IActionResult> TransferProperty(int propertyId, [FromBody] TransferPropertyRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FromAddress))
                {
                    return BadRequest("From address is required");
                }

                var receipt = await _blockchainService.TransferPropertyAsync(
                    new BigInteger(propertyId),
                    request.NewOwnerAddress,
                    request.FromAddress,
                    request.PrivateKey
                );
                
                return Ok(new { 
                    transactionHash = receipt.TransactionHash,
                    blockNumber = receipt.BlockNumber.Value.ToString(),
                    gasUsed = receipt.GasUsed.Value.ToString(),
                    status = receipt.Status.Value.ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to transfer property: {ex.Message}");
            }
        }

        #endregion

        #region User Management

        [HttpGet("user/{address}")]
        public async Task<IActionResult> GetUser(string address)
        {
            try
            {
                var user = await _blockchainService.GetUserAsync(address);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to get user: {ex.Message}");
            }
        }

        [HttpPost("user/register")]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterUserRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FromAddress))
                {
                    return BadRequest("From address is required");
                }

                var receipt = await _blockchainService.RegisterUserAsync(
                    request.FirstName,
                    request.LastName,
                    request.Email,
                    request.KycDocumentHash,
                    request.BankingInfoEncrypted,
                    request.FromAddress,
                    request.PrivateKey
                );
                
                return Ok(new { 
                    transactionHash = receipt.TransactionHash,
                    blockNumber = receipt.BlockNumber.Value.ToString(),
                    gasUsed = receipt.GasUsed.Value.ToString(),
                    status = receipt.Status.Value.ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to register user: {ex.Message}");
            }
        }

        #endregion

        #region Transaction Management

        [HttpPost("send-transaction")]
        public async Task<IActionResult> SendTransaction([FromBody] SendTransactionRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FromAddress) || string.IsNullOrEmpty(request.ToAddress))
                {
                    return BadRequest("From and To addresses are required");
                }

                var receipt = await _blockchainService.SendTransactionAsync(
                    request.FromAddress, 
                    request.ToAddress, 
                    request.EtherAmount, 
                    request.PrivateKey
                );
                
                return Ok(new { 
                    transactionHash = receipt.TransactionHash,
                    blockNumber = receipt.BlockNumber.Value.ToString(),
                    gasUsed = receipt.GasUsed.Value.ToString(),
                    status = receipt.Status.Value.ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to send transaction: {ex.Message}");
            }
        }

        [HttpGet("transaction/{transactionHash}")]
        public async Task<IActionResult> GetTransactionReceipt(string transactionHash)
        {
            try
            {
                var receipt = await _blockchainService.GetTransactionReceiptAsync(transactionHash);
                if (receipt == null)
                {
                    return NotFound($"Transaction receipt not found for hash: {transactionHash}");
                }
                
                return Ok(new {
                    transactionHash = receipt.TransactionHash,
                    blockNumber = receipt.BlockNumber.Value.ToString(),
                    blockHash = receipt.BlockHash,
                    gasUsed = receipt.GasUsed.Value.ToString(),
                    status = receipt.Status != null ? receipt.Status.Value.ToString() : "Unknown"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to get transaction receipt: {ex.Message}");
            }
        }

        #endregion
    }
    
    #region Request Models

    public class ContractAddresses
    {
        public string PropertyAddress { get; set; }
        public string UserAddress { get; set; }
    }

    public class RegisterPropertyRequest
    {
        public string Location { get; set; }
        public string Specifications { get; set; }
        public bool RentalStatus { get; set; }
        public string MaintenanceSchedule { get; set; }
        public List<string> Photos { get; set; }
        public string FromAddress { get; set; }
        public string PrivateKey { get; set; }
    }

    public class UpdatePropertyRequest
    {
        public string Location { get; set; }
        public string Specifications { get; set; }
        public bool RentalStatus { get; set; }
        public string MaintenanceSchedule { get; set; }
        public List<string> Photos { get; set; }
        public string FromAddress { get; set; }
        public string PrivateKey { get; set; }
    }

    public class TransferPropertyRequest
    {
        public string NewOwnerAddress { get; set; }
        public string FromAddress { get; set; }
        public string PrivateKey { get; set; }
    }

    public class RegisterUserRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string KycDocumentHash { get; set; }
        public string BankingInfoEncrypted { get; set; }
        public string FromAddress { get; set; }
        public string PrivateKey { get; set; }
    }

    public class SendTransactionRequest
    {
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public decimal EtherAmount { get; set; }
        public string PrivateKey { get; set; }
    }

    #endregion
} 