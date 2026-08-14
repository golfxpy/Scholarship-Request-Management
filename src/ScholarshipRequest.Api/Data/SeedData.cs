using ScholarshipRequest.Api.Domain.Masters;
using ScholarshipRequest.Api.Domain.Privacy;

namespace ScholarshipRequest.Api.Data;

public static class SeedData
{
    public static readonly Guid HatYaiCampusId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid ActivePdpaNoticeId = Guid.Parse("40000000-0000-0000-0000-000000000001");

    private static readonly DateTimeOffset SeedCreatedAt =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static IReadOnlyList<Campus> Campuses { get; } =
    [
        new()
        {
            Id = HatYaiCampusId,
            Code = "HATYAI",
            Name = "วิทยาเขตหาดใหญ่",
            IsActive = true,
            SortOrder = 1,
            CreatedAt = SeedCreatedAt
        }
    ];

    public static IReadOnlyList<AcademicUnit> AcademicUnits { get; } =
    [
        CreateAcademicUnit(1, "ENV", "คณะการจัดการสิ่งแวดล้อม"),
        CreateAcademicUnit(2, "TTM", "คณะการแพทย์แผนไทย"),
        CreateAcademicUnit(3, "NATRES", "คณะทรัพยากรธรรมชาติ"),
        CreateAcademicUnit(4, "DENT", "คณะทันตแพทยศาสตร์"),
        CreateAcademicUnit(5, "MEDTECH", "คณะเทคนิคการแพทย์"),
        CreateAcademicUnit(6, "LAW", "คณะนิติศาสตร์"),
        CreateAcademicUnit(7, "NUR", "คณะพยาบาลศาสตร์"),
        CreateAcademicUnit(8, "MED", "คณะแพทยศาสตร์"),
        CreateAcademicUnit(9, "PHAR", "คณะเภสัชศาสตร์"),
        CreateAcademicUnit(10, "FMS", "คณะวิทยาการจัดการ"),
        CreateAcademicUnit(11, "SCI", "คณะวิทยาศาสตร์"),
        CreateAcademicUnit(12, "ENG", "คณะวิศวกรรมศาสตร์"),
        CreateAcademicUnit(13, "LIBARTS", "คณะศิลปศาสตร์"),
        CreateAcademicUnit(14, "ECON", "คณะเศรษฐศาสตร์"),
        CreateAcademicUnit(15, "VET", "คณะสัตวแพทยศาสตร์"),
        CreateAcademicUnit(16, "AGRO", "คณะอุตสาหกรรมเกษตร")
    ];

    public static IReadOnlyList<ScholarshipType> ScholarshipTypes { get; } =
    [
        CreateScholarshipType(1, "NEED", "ทุนขาดแคลนทุนทรัพย์"),
        CreateScholarshipType(2, "MERIT", "ทุนส่งเสริมการศึกษา (เรียนดี)"),
        CreateScholarshipType(3, "WORK", "ทุนทำงานพิเศษ"),
        CreateScholarshipType(4, "EMERGENCY", "ทุนฉุกเฉิน/กรณีพิเศษ"),
        CreateScholarshipType(5, "ACTIVITY", "ทุนกิจกรรมนักศึกษา")
    ];

    public static IReadOnlyList<PdpaNotice> PdpaNotices { get; } =
    [
        new()
        {
            Id = ActivePdpaNoticeId,
            Version = "POC-v1",
            Content = "มหาวิทยาลัยสงขลานครินทร์ วิทยาเขตหาดใหญ่ จะเก็บรวบรวมและใช้ข้อมูลส่วนบุคคลที่ท่านให้ไว้เพื่อรับ ตรวจสอบ และพิจารณาคำขอทุนการศึกษา โดยให้เจ้าหน้าที่ผู้รับผิดชอบเข้าถึงเท่าที่จำเป็น ข้อความนี้ใช้สำหรับระบบ POC และต้องได้รับการทบทวนก่อนใช้งานจริง",
            EffectiveAt = SeedCreatedAt,
            IsActive = true,
            CreatedAt = SeedCreatedAt
        }
    ];

    private static AcademicUnit CreateAcademicUnit(int sequence, string code, string name) =>
        new()
        {
            Id = Guid.Parse($"20000000-0000-0000-0000-{sequence:000000000000}"),
            CampusId = HatYaiCampusId,
            Code = code,
            Name = name,
            IsActive = true,
            SortOrder = sequence,
            CreatedAt = SeedCreatedAt
        };

    private static ScholarshipType CreateScholarshipType(int sequence, string code, string name) =>
        new()
        {
            Id = Guid.Parse($"30000000-0000-0000-0000-{sequence:000000000000}"),
            Code = code,
            Name = name,
            IsActive = true,
            SortOrder = sequence,
            CreatedAt = SeedCreatedAt
        };
}
