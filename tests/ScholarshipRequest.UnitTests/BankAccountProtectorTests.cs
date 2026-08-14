using Microsoft.AspNetCore.DataProtection;
using ScholarshipRequest.Api.Security;

namespace ScholarshipRequest.UnitTests;

public sealed class BankAccountProtectorTests
{
    [Fact]
    public void ProtectAndUnprotect_ShouldRoundTripWithoutExposingPlainText()
    {
        var keyDirectory = Directory.CreateTempSubdirectory("scholarship-keys-");
        try
        {
            var firstProvider = DataProtectionProvider.Create(keyDirectory);
            var firstProtector = new DataProtectionBankAccountProtector(firstProvider);
            const string bankAccountNumber = "1234567890";

            var protectedValue = firstProtector.Protect(bankAccountNumber);
            var restartedProvider = DataProtectionProvider.Create(keyDirectory);
            var restartedProtector = new DataProtectionBankAccountProtector(restartedProvider);
            var unprotectedValue = restartedProtector.Unprotect(protectedValue);

            Assert.NotEqual(bankAccountNumber, protectedValue);
            Assert.DoesNotContain(bankAccountNumber, protectedValue, StringComparison.Ordinal);
            Assert.Equal(bankAccountNumber, unprotectedValue);
        }
        finally
        {
            keyDirectory.Delete(recursive: true);
        }
    }

    [Theory]
    [InlineData("7890", "******7890")]
    [InlineData("0001", "******0001")]
    public void Mask_ShouldExposeOnlyLastFourDigits(string lastFourDigits, string expected)
    {
        var keyDirectory = Directory.CreateTempSubdirectory("scholarship-mask-");
        try
        {
            var provider = DataProtectionProvider.Create(keyDirectory);
            var protector = new DataProtectionBankAccountProtector(provider);
            Assert.Equal(expected, protector.Mask(lastFourDigits));
        }
        finally
        {
            keyDirectory.Delete(recursive: true);
        }
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12A4")]
    [InlineData("12345")]
    public void Mask_ShouldRejectInvalidSuffix(string invalidSuffix)
    {
        var keyDirectory = Directory.CreateTempSubdirectory("scholarship-mask-invalid-");
        try
        {
            var provider = DataProtectionProvider.Create(keyDirectory);
            var protector = new DataProtectionBankAccountProtector(provider);
            Assert.Throws<ArgumentException>(() => protector.Mask(invalidSuffix));
        }
        finally
        {
            keyDirectory.Delete(recursive: true);
        }
    }
}
