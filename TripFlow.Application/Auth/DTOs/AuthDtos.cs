using TripFlow.Domain.Enums;

namespace TripFlow.Application.Auth.DTOs;

public record RegisterRequest(string Name, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record GoogleLoginRequest(string IdToken);
public record ConfirmEmailRequest(string Email, string Code);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);

public record UserDto(Guid Id, string Name, string Email, GlobalRole GlobalRole, bool EmailConfirmed);

public record AuthResult(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt, UserDto User);
