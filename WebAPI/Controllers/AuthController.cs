using Application.Features.Auth.Commands.ConfirmEmail;
using Application.Features.Auth.Commands.ForgotPassword;
using Application.Features.Auth.Commands.Login;
using Application.Features.Auth.Commands.RefreshToken;
using Application.Features.Auth.Commands.Register;
using Application.Features.Auth.Commands.ResetPassword;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[EnableRateLimiting("AuthPolicy")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        await mediator.Send(command);
        return Ok(new { Message = "Registration successful. Please check your email to confirm your account." });
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginCommand command)
    {
        // Capture UserAgent if not provided
        var userAgent = Request.Headers.UserAgent.ToString();
        var commandWithContext = command with { UserAgent = string.IsNullOrEmpty(command.UserAgent) ? userAgent : command.UserAgent };

        var response = await mediator.Send(commandWithContext);
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshTokenResponse>> Refresh([FromBody] RefreshTokenCommand command)
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        var commandWithContext = command with { UserAgent = string.IsNullOrEmpty(command.UserAgent) ? userAgent : command.UserAgent };

        var response = await mediator.Send(commandWithContext);
        return Ok(response);
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        await mediator.Send(new ConfirmEmailCommand(userId, token));
        return Ok(new { Message = "Email confirmed successfully." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        await mediator.Send(command);
        return Ok(new { Message = "If the email is valid, a password reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        await mediator.Send(command);
        return Ok(new { Message = "Password has been reset successfully." });
    }
}
