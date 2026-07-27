namespace TripFlow.Application.Common;

public class AuthPolicyOptions
{
    public const string SectionName = "AuthPolicy";

    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
    public int EmailConfirmationCodeExpiryMinutes { get; set; } = 15;
    public int PasswordResetExpiryMinutes { get; set; } = 30;
    public int MinPasswordLength { get; set; } = 12;
}
