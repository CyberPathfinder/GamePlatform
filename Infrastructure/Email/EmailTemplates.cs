namespace Infrastructure.Email;

internal static class EmailTemplates
{
    private const string ButtonBaseStyle = "color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;";

    public static string ConfirmEmailHtml(string url) => $$"""
    <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
        <h2>Welcome to GameVault!</h2>
        <p>Thank you for registering. Please confirm your email address to activate your account.</p>
        <p style="margin: 30px 0;">
            <a href="{{url}}"
               style="background-color: #4f46e5; {ButtonBaseStyle}">
                Confirm Email Address
            </a>
        </p>
        <p>Or copy and paste this link in your browser:</p>
        <p style="color: #666; word-break: break-all;">{{url}}</p>
        <hr style="border: none; border-top: 1px solid #eee; margin: 20px 0;" />
        <p style="color: #999; font-size: 12px;">
            This link will expire in 24 hours.<br />
            If you didn't create a GameVault account, you can safely ignore this email.
        </p>
    </div>
    """;

    public static string ResetPasswordHtml(string url, string baseUrl) => $$"""
    <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
        <h2>Reset Your Password</h2>
        <p>You requested to reset your password. Click the button below to create a new password.</p>
        <p style="margin: 30px 0;">
            <a href="{{url}}"
               style="background-color: #dc2626; {ButtonBaseStyle}">
                Reset Password
            </a>
        </p>
        <p>Or copy and paste this link in your browser:</p>
        <p style="color: #666; word-break: break-all;">{{url}}</p>
        <hr style="border: none; border-top: 1px solid #eee; margin: 20px 0;" />
        <p style="color: #999; font-size: 12px;">
            This link will expire in 1 hour.<br />
            If you didn't request a password reset, please ignore this email or
            <a href="{{baseUrl}}/support">contact support</a> if you're concerned.
        </p>
    </div>
    """;
}
