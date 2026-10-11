using HumayQasimli.Data;
using HumayQasimli.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HumayQasimli.Services
{
    /// <summary>
    /// Generates and validates 6-digit email OTP codes with expiry, resend cooldown
    /// and a maximum attempt limit to prevent brute forcing.
    /// </summary>
    public class OtpService : IOtpService
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        private int CodeLength => int.TryParse(_configuration["OtpSettings:CodeLength"], out var v) ? v : 6;
        private int ExpiryMinutes => int.TryParse(_configuration["OtpSettings:ExpiryMinutes"], out var v) ? v : 10;
        private int MaxAttempts => int.TryParse(_configuration["OtpSettings:MaxAttempts"], out var v) ? v : 5;
        private int ResendCooldownSeconds => int.TryParse(_configuration["OtpSettings:ResendCooldownSeconds"], out var v) ? v : 60;

        public OtpService(ApplicationDbContext db, IEmailSender emailSender, IConfiguration configuration)
        {
            _db = db;
            _emailSender = emailSender;
            _configuration = configuration;
        }

        public async Task<OtpGenerateResult> GenerateAndSendOtpAsync(string userId, string email)
        {
            var existing = await _db.EmailVerificationOtps
                .Where(o => o.UserId == userId && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (existing != null && existing.LastSentAt.HasValue &&
                (DateTime.UtcNow - existing.LastSentAt.Value).TotalSeconds < ResendCooldownSeconds)
            {
                return OtpGenerateResult.CooldownActive;
            }

            var code = GenerateNumericCode(CodeLength);
            var now = DateTime.UtcNow;

            var otp = new EmailVerificationOtp
            {
                UserId = userId,
                Code = code,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(ExpiryMinutes),
                Attempts = 0,
                IsUsed = false,
                LastSentAt = now
            };

            _db.EmailVerificationOtps.Add(otp);
            await _db.SaveChangesAsync();

            var subject = "Humay Qasimli - Email tesdiq kodu";
            var body = $@"<div style='font-family:Arial,sans-serif;max-width:480px;margin:0 auto'>
                <h2>Email tesdiq kodu</h2>
                <p>Tesdiq kodunuz:</p>
                <p style='font-size:32px;font-weight:bold;letter-spacing:4px'>{code}</p>
                <p>Bu kod {ExpiryMinutes} deqiqe erzinde etibarlidir.</p>
                <p>Eger bu sorgunu siz gondermemisinizse, bu mesaji nezere almayin.</p>
            </div>";

            await _emailSender.SendEmailAsync(email, subject, body);

            return OtpGenerateResult.Sent;
        }

        public async Task<OtpVerifyResult> VerifyOtpAsync(string userId, string code)
        {
            var otp = await _db.EmailVerificationOtps
                .Where(o => o.UserId == userId && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otp == null)
            {
                return OtpVerifyResult.NotFound;
            }

            if (otp.Attempts >= MaxAttempts)
            {
                return OtpVerifyResult.TooManyAttempts;
            }

            if (otp.ExpiresAt < DateTime.UtcNow)
            {
                return OtpVerifyResult.Expired;
            }

            if (otp.Code != code)
            {
                otp.Attempts += 1;
                await _db.SaveChangesAsync();
                return otp.Attempts >= MaxAttempts ? OtpVerifyResult.TooManyAttempts : OtpVerifyResult.InvalidCode;
            }

            otp.IsUsed = true;
            await _db.SaveChangesAsync();
            return OtpVerifyResult.Success;
        }

        private static string GenerateNumericCode(int length)
        {
            var random = System.Security.Cryptography.RandomNumberGenerator.Create();
            var bytes = new byte[4];
            random.GetBytes(bytes);
            var value = BitConverter.ToUInt32(bytes, 0);
            var max = (int)Math.Pow(10, length);
            var code = (value % (uint)max).ToString().PadLeft(length, '0');
            return code;
        }
    }
}
