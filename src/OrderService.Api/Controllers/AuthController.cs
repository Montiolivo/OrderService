using Microsoft.AspNetCore.Mvc;
using OrderService.Infrastructure.Authentication;
using LoginRequest = OrderService.Api.Requests.LoginRequest;

namespace OrderService.Api.Controllers;

[ApiController]
[Route("auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly JwtTokenGenerator _tokenGenerator;

    public AuthController(JwtTokenGenerator tokenGenerator)
        => _tokenGenerator = tokenGenerator;

    /// <summary>
    /// Gera um token JWT. Use admin/admin para autenticar.
    /// </summary>
    [HttpPost("token")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Token([FromBody] LoginRequest request)
    {
        if (request.Username != "admin" || request.Password != "admin")
            return Unauthorized(new { message = "Invalid credentials." });

        var token = _tokenGenerator.GenerateToken(request.Username, role: "admin");

        return Ok(new TokenResponse(token, "Bearer"));
    }
}

public record TokenResponse(string AccessToken, string TokenType);
