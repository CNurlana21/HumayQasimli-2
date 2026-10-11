namespace HumayQasimli.Services
{
    public enum OtpVerifyResult
    {
        Success,
        InvalidCode,
        Expired,
        TooManyAttempts,
        NotFound
    }

    public enum OtpGenerateResult
    {
        Sent,
        CooldownActive
    }

    public interface IOtpService
    {
        Task<OtpGenerateResult> GenerateAndSendOtpAsync(string userId, string email);
        Task<OtpVerifyResult> VerifyOtpAsync(string userId, string code);
    }
}
