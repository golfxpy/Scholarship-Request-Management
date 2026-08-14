using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ScholarshipRequest.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "campuses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pdpa_notices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    effective_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pdpa_notices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "request_number_counters",
                columns: table => new
                {
                    buddhist_year = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_number_counters", x => x.buddhist_year);
                    table.CheckConstraint("ck_request_number_counters_last_value", "last_value >= 0");
                });

            migrationBuilder.CreateTable(
                name: "scholarship_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scholarship_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "academic_units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_academic_units", x => x.id);
                    table.ForeignKey(
                        name: "FK_academic_units_campuses_campus_id",
                        column: x => x.campus_id,
                        principalTable: "campuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scholarship_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    student_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    student_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    faculty_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    major = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    education_level = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    year_level = table.Column<int>(type: "integer", nullable: true),
                    year_level_other = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    gpax = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    scholarship_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    protected_bank_account_number = table.Column<string>(type: "text", nullable: false),
                    bank_account_last_four = table.Column<string>(type: "character(4)", fixedLength: true, maxLength: 4, nullable: false),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    decision_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decided_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    submission_source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pdpa_notice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consent_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    consent_evidence_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    consent_obtained_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scholarship_requests", x => x.id);
                    table.CheckConstraint("ck_scholarship_requests_amount", "requested_amount > 0");
                    table.CheckConstraint("ck_scholarship_requests_bank_last_four", "bank_account_last_four ~ '^[0-9]{4}$'");
                    table.CheckConstraint("ck_scholarship_requests_consent_method", "consent_method IN ('Self', 'Document', 'Verbal', 'Other')");
                    table.CheckConstraint("ck_scholarship_requests_gpax", "gpax >= 0.00 AND gpax <= 4.00");
                    table.CheckConstraint("ck_scholarship_requests_source", "submission_source IN ('Public', 'Staff')");
                    table.CheckConstraint("ck_scholarship_requests_status", "status IN ('Pending', 'Approved', 'Rejected')");
                    table.CheckConstraint("ck_scholarship_requests_year_level", "(year_level BETWEEN 1 AND 6 AND year_level_other IS NULL) OR (year_level IS NULL AND year_level_other IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_scholarship_requests_academic_units_academic_unit_id",
                        column: x => x.academic_unit_id,
                        principalTable: "academic_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_scholarship_requests_campuses_campus_id",
                        column: x => x.campus_id,
                        principalTable: "campuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_scholarship_requests_pdpa_notices_pdpa_notice_id",
                        column: x => x.pdpa_notice_id,
                        principalTable: "pdpa_notices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_scholarship_requests_scholarship_types_scholarship_type_id",
                        column: x => x.scholarship_type_id,
                        principalTable: "scholarship_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "campuses",
                columns: new[] { "id", "code", "created_at", "is_active", "name", "sort_order" },
                values: new object[] { new Guid("10000000-0000-0000-0000-000000000001"), "HATYAI", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "วิทยาเขตหาดใหญ่", 1 });

            migrationBuilder.InsertData(
                table: "pdpa_notices",
                columns: new[] { "id", "content", "created_at", "effective_at", "is_active", "version" },
                values: new object[] { new Guid("40000000-0000-0000-0000-000000000001"), "มหาวิทยาลัยสงขลานครินทร์ วิทยาเขตหาดใหญ่ จะเก็บรวบรวมและใช้ข้อมูลส่วนบุคคลที่ท่านให้ไว้เพื่อรับ ตรวจสอบ และพิจารณาคำขอทุนการศึกษา โดยให้เจ้าหน้าที่ผู้รับผิดชอบเข้าถึงเท่าที่จำเป็น ข้อความนี้ใช้สำหรับระบบ POC และต้องได้รับการทบทวนก่อนใช้งานจริง", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "POC-v1" });

            migrationBuilder.InsertData(
                table: "scholarship_types",
                columns: new[] { "id", "code", "created_at", "description", "is_active", "name", "sort_order" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000001"), "NEED", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "ทุนขาดแคลนทุนทรัพย์", 1 },
                    { new Guid("30000000-0000-0000-0000-000000000002"), "MERIT", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "ทุนส่งเสริมการศึกษา (เรียนดี)", 2 },
                    { new Guid("30000000-0000-0000-0000-000000000003"), "WORK", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "ทุนทำงานพิเศษ", 3 },
                    { new Guid("30000000-0000-0000-0000-000000000004"), "EMERGENCY", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "ทุนฉุกเฉิน/กรณีพิเศษ", 4 },
                    { new Guid("30000000-0000-0000-0000-000000000005"), "ACTIVITY", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "ทุนกิจกรรมนักศึกษา", 5 }
                });

            migrationBuilder.InsertData(
                table: "academic_units",
                columns: new[] { "id", "campus_id", "code", "created_at", "is_active", "name", "sort_order" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000001"), "ENV", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "คณะการจัดการสิ่งแวดล้อม", 1 },
                    { new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000001"), "TTM", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "คณะการแพทย์แผนไทย", 2 },
                    { new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000001"), "NATRES", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "คณะทรัพยากรธรรมชาติ", 3 },
                    { new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000001"), "DENT", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "คณะทันตแพทยศาสตร์", 4 },
                    { new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000001"), "MEDTECH", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "คณะเทคนิคการแพทย์", 5 },
                    { new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000001"), "LAW", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "คณะนิติศาสตร์", 6 },
                    { new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000001"), "NUR", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "คณะพยาบาลศาสตร์", 7 },
                    { new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000001"), "MED", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "คณะแพทยศาสตร์", 8 },
                    { new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000001"), "PHAR", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "คณะเภสัชศาสตร์", 9 },
                    { new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000001"), "FMS", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "คณะวิทยาการจัดการ", 10 },
                    { new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000001"), "SCI", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "คณะวิทยาศาสตร์", 11 },
                    { new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000001"), "ENG", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "คณะวิศวกรรมศาสตร์", 12 },
                    { new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000001"), "LIBARTS", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "คณะศิลปศาสตร์", 13 },
                    { new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000001"), "ECON", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "คณะเศรษฐศาสตร์", 14 },
                    { new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000001"), "VET", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "คณะสัตวแพทยศาสตร์", 15 },
                    { new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000001"), "AGRO", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "คณะอุตสาหกรรมเกษตร", 16 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_academic_units_campus_id_code",
                table: "academic_units",
                columns: new[] { "campus_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_academic_units_campus_id_is_active_sort_order",
                table: "academic_units",
                columns: new[] { "campus_id", "is_active", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_campuses_code",
                table: "campuses",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pdpa_notices_is_active_effective_at",
                table: "pdpa_notices",
                columns: new[] { "is_active", "effective_at" });

            migrationBuilder.CreateIndex(
                name: "IX_pdpa_notices_version",
                table: "pdpa_notices",
                column: "version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scholarship_requests_academic_unit_id",
                table: "scholarship_requests",
                column: "academic_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_scholarship_requests_campus_id_submitted_at",
                table: "scholarship_requests",
                columns: new[] { "campus_id", "submitted_at" });

            migrationBuilder.CreateIndex(
                name: "IX_scholarship_requests_pdpa_notice_id",
                table: "scholarship_requests",
                column: "pdpa_notice_id");

            migrationBuilder.CreateIndex(
                name: "IX_scholarship_requests_request_no",
                table: "scholarship_requests",
                column: "request_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scholarship_requests_scholarship_type_id_submitted_at",
                table: "scholarship_requests",
                columns: new[] { "scholarship_type_id", "submitted_at" });

            migrationBuilder.CreateIndex(
                name: "IX_scholarship_requests_status_submitted_at",
                table: "scholarship_requests",
                columns: new[] { "status", "submitted_at" });

            migrationBuilder.CreateIndex(
                name: "IX_scholarship_requests_student_id",
                table: "scholarship_requests",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_scholarship_types_code",
                table: "scholarship_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "users",
                column: "NormalizedUserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "request_number_counters");

            migrationBuilder.DropTable(
                name: "scholarship_requests");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "academic_units");

            migrationBuilder.DropTable(
                name: "pdpa_notices");

            migrationBuilder.DropTable(
                name: "scholarship_types");

            migrationBuilder.DropTable(
                name: "campuses");
        }
    }
}
