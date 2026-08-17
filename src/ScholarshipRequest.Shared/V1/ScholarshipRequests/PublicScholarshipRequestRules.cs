using System.ComponentModel.DataAnnotations;

namespace ScholarshipRequest.Shared.V1.ScholarshipRequests;

public static class PublicScholarshipRequestRules
{
    public static Dictionary<string, string[]> Validate(CreatePublicScholarshipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            validationResults,
            validateAllProperties: true);

        foreach (var result in validationResults)
        {
            foreach (var memberName in result.MemberNames.DefaultIfEmpty(string.Empty))
            {
                AddError(errors, memberName, result.ErrorMessage ?? "ข้อมูลไม่ถูกต้อง");
            }
        }

        if (request.ScholarshipTypeId == Guid.Empty)
        {
            AddError(errors, nameof(request.ScholarshipTypeId), "กรุณาเลือกประเภททุน");
        }

        var hasStandardYear = request.YearLevel is >= 1 and <= 6;
        var hasOtherYear = !string.IsNullOrWhiteSpace(request.YearLevelOther);
        if (hasStandardYear == hasOtherYear)
        {
            AddError(
                errors,
                nameof(request.YearLevel),
                "กรุณาเลือกชั้นปี 1–6 หรือระบุชั้นปีอื่นอย่างใดอย่างหนึ่ง");
        }
        else if (request.YearLevel is not null && !hasStandardYear)
        {
            AddError(errors, nameof(request.YearLevel), "ชั้นปีต้องอยู่ระหว่าง 1–6");
        }

        if (HasMoreThanTwoDecimalPlaces(request.Gpax))
        {
            AddError(errors, nameof(request.Gpax), "GPAX รองรับทศนิยมไม่เกิน 2 ตำแหน่ง");
        }

        if (HasMoreThanTwoDecimalPlaces(request.RequestedAmount))
        {
            AddError(
                errors,
                nameof(request.RequestedAmount),
                "จำนวนเงินรองรับทศนิยมไม่เกิน 2 ตำแหน่ง");
        }

        foreach (var message in string.IsNullOrWhiteSpace(request.BankAccountNumber)
            ? []
            : ValidateBankAccount(request.BankAccountNumber))
        {
            AddError(
                errors,
                nameof(request.BankAccountNumber),
                message);
        }

        return errors.ToDictionary(
            error => error.Key,
            error => error.Value.Distinct(StringComparer.Ordinal).ToArray(),
            StringComparer.Ordinal);
    }

    public static string NormalizeBankAccount(string? value) =>
        string.IsNullOrEmpty(value)
            ? string.Empty
            : new string(value.Where(char.IsAsciiDigit).ToArray());

    public static string[] ValidateBankAccount(string? value)
    {
        var bankAccountNumber = value ?? string.Empty;
        var errors = new List<string>();
        if (bankAccountNumber.Any(character =>
                !char.IsAsciiDigit(character) && character is not ' ' and not '-'))
        {
            errors.Add("เลขบัญชีใช้ได้เฉพาะตัวเลข ช่องว่าง และเครื่องหมายขีด");
        }

        var normalizedBankAccount = NormalizeBankAccount(bankAccountNumber);
        if (normalizedBankAccount.Length is < 6 or > 30)
        {
            errors.Add("เลขบัญชีต้องมีตัวเลข 6–30 หลัก");
        }

        return errors.ToArray();
    }

    private static bool HasMoreThanTwoDecimalPlaces(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.ToEven) != value;

    private static void AddError(
        IDictionary<string, List<string>> errors,
        string propertyName,
        string message)
    {
        if (!errors.TryGetValue(propertyName, out var messages))
        {
            messages = [];
            errors[propertyName] = messages;
        }

        messages.Add(message);
    }
}
