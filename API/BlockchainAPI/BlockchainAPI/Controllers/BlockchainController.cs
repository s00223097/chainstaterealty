using BlockchainAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BlockchainAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlockchainController : ControllerBase
    {
        private readonly BlockchainService _blockchainService;

        public BlockchainController(BlockchainService blockchainService)
        {
            _blockchainService = blockchainService;
        }

        [HttpGet("message")]
        public async Task<IActionResult> GetMessage()
        {
            var message = await _blockchainService.GetMessageAsync();
            return Ok(new { message });
        }
    }
}
