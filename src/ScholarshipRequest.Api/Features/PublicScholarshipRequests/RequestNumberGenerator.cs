using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ScholarshipRequest.Api.Data;
using ScholarshipRequest.Api.Time;

namespace ScholarshipRequest.Api.Features.PublicScholarshipRequests;

public interface IRequestNumberGenerator
{
    Task<string> NextAsync(CancellationToken cancellationToken = default);
}

public sealed class RequestNumberGenerator(
    ApplicationDbContext context,
    IClock clock) : IRequestNumberGenerator
{
    private const string CounterSql = """
        INSERT INTO request_number_counters (buddhist_year, last_value)
        VALUES (@buddhistYear, 1)
        ON CONFLICT (buddhist_year)
        DO UPDATE SET last_value = request_number_counters.last_value + 1
        RETURNING last_value AS "Value"
        """;

    private static readonly TimeZoneInfo BangkokTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");

    public async Task<string> NextAsync(CancellationToken cancellationToken = default)
    {
        var buddhistYear = GetBuddhistYear(clock.UtcNow);
        var currentTransaction = context.Database.CurrentTransaction
            ?? throw new InvalidOperationException(
                "Request numbers must be generated inside the request transaction.");

        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = CounterSql;
        command.Transaction = currentTransaction.GetDbTransaction();

        var yearParameter = command.CreateParameter();
        yearParameter.ParameterName = "buddhistYear";
        yearParameter.Value = buddhistYear;
        command.Parameters.Add(yearParameter);

        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        if (scalar is null or DBNull)
        {
            throw new InvalidOperationException(
                "PostgreSQL did not return the next request number value.");
        }

        var nextValue = Convert.ToInt64(scalar, CultureInfo.InvariantCulture);

        return Format(buddhistYear, nextValue);
    }

    public static int GetBuddhistYear(DateTimeOffset utcNow)
    {
        var bangkokTime = TimeZoneInfo.ConvertTime(utcNow, BangkokTimeZone);
        return bangkokTime.Year + 543;
    }

    public static string Format(int buddhistYear, long value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(buddhistYear, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);

        return FormattableString.Invariant($"SCH-{buddhistYear}-{value:000000}");
    }
}
