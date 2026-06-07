using FireWatch.Gateway.DTOs.Out;
using FireWatch.Gateway.Interfaces.Services;
using FireWatch.Gateway.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FireWatch.Gateway.Controllers;

[ApiController]
[Route("auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Registra um novo usuário.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(request, ct);
        return CreatedAtAction(nameof(Register), result);
    }

    /// <summary>Autentica e retorna JWT + refresh token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(request, ct);
        return Ok(result);
    }

    /// <summary>Renova o access token usando o refresh token.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request, CancellationToken ct)
    {
        var result = await _auth.RefreshAsync(request.RefreshToken, ct);
        return Ok(result);
    }

    /// <summary>Revoga o refresh token (logout).</summary>
    [HttpPost("revoke")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Revoke(
        [FromBody] RefreshRequest request, CancellationToken ct)
    {
        await _auth.RevokeAsync(request.RefreshToken, ct);
        return NoContent();
    }

    /// <summary>Retorna dados do usuário autenticado.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Me() => Ok(new
    {
        id = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
        email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value,
        name = User.FindFirst(JwtRegisteredClaimNames.Name)?.Value,
        role = User.FindFirst(ClaimTypes.Role)?.Value
    });
}