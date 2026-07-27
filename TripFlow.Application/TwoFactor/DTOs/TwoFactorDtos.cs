namespace TripFlow.Application.TwoFactor.DTOs;

public record TwoFactorSetupResult(string Secret, string OtpAuthUri, string QrCodePngBase64);

public record EnableTwoFactorRequest(string Code);
public record DisableTwoFactorRequest(string Password, string Code);
public record VerifyTwoFactorRequest(string Email, string ChallengeToken, string? Code, string? RecoveryCode);
