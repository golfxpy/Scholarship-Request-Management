using Microsoft.AspNetCore.DataProtection;

namespace ScholarshipRequest.Api.Security;

public sealed class DataProtectionBankAccountProtector : IBankAccountProtector
{
    private const string Purpose = "ScholarshipRequest.BankAccount.v1";
    private readonly IDataProtector _protector;

    public DataProtectionBankAccountProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Protect(string bankAccountNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bankAccountNumber);
        return _protector.Protect(bankAccountNumber);
    }

    public string Unprotect(string protectedBankAccountNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedBankAccountNumber);
        return _protector.Unprotect(protectedBankAccountNumber);
    }

    public string Mask(string lastFourDigits) =>
        lastFourDigits is { Length: 4 } && lastFourDigits.All(char.IsAsciiDigit)
            ? $"******{lastFourDigits}"
            : throw new ArgumentException("The bank-account suffix must contain four digits.", nameof(lastFourDigits));
}
