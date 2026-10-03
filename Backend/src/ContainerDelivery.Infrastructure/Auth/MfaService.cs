using OtpNet;
using System.Security.Cryptography;
using ContainerDelivery.Core.Entities;

namespace ContainerDelivery.Infrastructure.Auth;

public class MfaService : IMfaService
{
    public MfaSetupResult GenerateSecret(string userEmail)
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(key);
        var totp = new Totp(key, step: 30, totpSize: 6);
        
        var qrCodeUrl = $"otpauth://totp/ContainerDelivery:{userEmail}?secret={base32Secret}&issuer=ContainerDelivery&algorithm=SHA1&digits=6&period=30";
        
        var recoveryCodes = GenerateRecoveryCodes(10);

        return new MfaSetupResult(base32Secret, qrCodeUrl, recoveryCodes);
    }

    public bool VerifyCode(string base32Secret, string code, TimeSpan? tolerance = null)
    {
        try
        {
            var key = Base32Encoding.ToBytes(base32Secret);
            var totp = new Totp(key, step: 30, totpSize: 6);
            return totp.VerifyTotp(code, out _, tolerance ?? TimeSpan.FromSeconds(30));
        }
        catch
        {
            return false;
        }
    }

    public string[] GenerateRecoveryCodes(int count = 10)
    {
        var codes = new string[count];
        using var rng = RandomNumberGenerator.Create();
        
        for (int i = 0; i < count; i++)
        {
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            codes[i] = Convert.ToHexString(bytes).ToUpper();
        }
        
        return codes;
    }

    public bool VerifyRecoveryCode(string[] recoveryCodes, string code)
    {
        return recoveryCodes.Contains(code, StringComparer.OrdinalIgnoreCase);
    }

    public string[] RemoveUsedRecoveryCode(string[] recoveryCodes, string usedCode)
    {
        return recoveryCodes.Where(c => !c.Equals(usedCode, StringComparison.OrdinalIgnoreCase)).ToArray();
    }
}

public interface IMfaService
{
    MfaSetupResult GenerateSecret(string userEmail);
    bool VerifyCode(string base32Secret, string code, TimeSpan? tolerance = null);
    string[] GenerateRecoveryCodes(int count = 10);
    bool VerifyRecoveryCode(string[] recoveryCodes, string code);
    string[] RemoveUsedRecoveryCode(string[] recoveryCodes, string usedCode);
}

public record MfaSetupResult(string Secret, string QrCodeUrl, string[] RecoveryCodes);