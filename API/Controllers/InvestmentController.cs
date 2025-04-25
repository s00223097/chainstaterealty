using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Data;
using Shared.Model;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InvestmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InvestmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Investment
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Investment>>> GetInvestments()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            return await _context.Investments
                .Include(i => i.Property)
                .Where(i => i.UserId == userId)
                .ToListAsync();
        }

        // GET: api/Investment/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Investment>> GetInvestment(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var investment = await _context.Investments
                .Include(i => i.Property)
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

            if (investment == null)
            {
                return NotFound();
            }

            return investment;
        }

        // POST: api/Investment
        [HttpPost]
        public async Task<ActionResult<Investment>> CreateInvestment(Investment investment)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var property = await _context.Properties.FindAsync(investment.PropertyId);
            if (property == null)
            {
                return NotFound("Property not found");
            }

            if (property.AvailableShares < investment.Shares)
            {
                return BadRequest("Not enough shares available");
            }

            investment.UserId = userId;
            investment.TotalInvestment = investment.Shares * property.SharePrice;

            property.AvailableShares -= investment.Shares;

            _context.Investments.Add(investment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetInvestment), new { id = investment.Id }, investment);
        }

        // DELETE: api/Investment/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInvestment(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var investment = await _context.Investments
                .Include(i => i.Property)
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

            if (investment == null)
            {
                return NotFound();
            }

            // Return shares to property
            investment.Property.AvailableShares += investment.Shares;

            _context.Investments.Remove(investment);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
} 