namespace ScholarshipRequest.Api.Security;

public interface IBankAccountProtector
{
    string Protect(string bankAccountNumber);

    string Unprotect(string protectedBankAccountNumber);

    string Mask(string lastFourDigits);
}
