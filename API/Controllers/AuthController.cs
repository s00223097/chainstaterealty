using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using API.Data;
using Shared.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication;

namespace API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "User registered successfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized("Invalid email or password");

            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }

        // Social login endpoints
        [HttpGet("login/{provider}")]
        public IActionResult SocialLogin(string provider, string returnUrl = "/")
        {
            if (!Enum.TryParse<SocialProvider>(provider, true, out var socialProvider))
                return BadRequest("Invalid provider");

            // Configure the provider properties
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, 
                Url.Action(nameof(SocialLoginCallback), "Auth", new { returnUrl }));

            // Challenge the provider
            return Challenge(properties, provider);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> SocialLoginCallback(string returnUrl = "/")
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return Redirect($"{returnUrl}?error=External_login_failure");

            // Sign in with the external login provider
            var result = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (result.Succeeded)
            {
                // Find the user
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (user != null)
                {
                    var token = GenerateJwtToken(user);
                    return Redirect($"{returnUrl}?token={token}");
                }
            }

            // If we got this far, we need to create a new user
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
                return Redirect($"{returnUrl}?error=No_email_provided");

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                // Add this login to the existing user
                var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
                if (addLoginResult.Succeeded)
                {
                    var token = GenerateJwtToken(existingUser);
                    return Redirect($"{returnUrl}?token={token}");
                }
                return Redirect($"{returnUrl}?error=Could_not_add_login");
            }

            // Create a new user
            var newUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true // Since the email comes from the provider, we assume it's verified
            };

            var createResult = await _userManager.CreateAsync(newUser);
            if (createResult.Succeeded)
            {
                var addLoginResult = await _userManager.AddLoginAsync(newUser, info);
                if (addLoginResult.Succeeded)
                {
                    var token = GenerateJwtToken(newUser);
                    return Redirect($"{returnUrl}?token={token}");
                }
                // If we couldn't add the login, delete the user to avoid orphaned accounts
                await _userManager.DeleteAsync(newUser);
            }

            return Redirect($"{returnUrl}?error=Could_not_create_user");
        }

        [HttpPost("verify-token")]
        public async Task<IActionResult> VerifyToken([FromBody] VerifyTokenModel model)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtSettings = _configuration.GetSection("JwtSettings");
                var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

                tokenHandler.ValidateToken(model.Token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value;
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                    return Unauthorized();

                return Ok(new { IsValid = true, Email = user.Email });
            }
            catch
            {
                return Unauthorized();
            }
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
