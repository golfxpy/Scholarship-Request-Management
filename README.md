# Scholarship Request Management

ระบบจัดการคำขอทุนการศึกษา พัฒนาด้วย .NET 10, Blazor WebAssembly และ ASP.NET Core REST API โดยมี PostgreSQL เป็นฐานข้อมูลเป้าหมาย

## Solution structure

- `src/ScholarshipRequest.Client` — Blazor WebAssembly UI
- `src/ScholarshipRequest.Api` — ASP.NET Core REST API
- `src/ScholarshipRequest.Shared` — versioned API contracts shared by Client and API
- `tests/ScholarshipRequest.UnitTests` — fast unit tests
- `tests/ScholarshipRequest.IntegrationTests` — API integration tests

Client และ API อ้างอิง Shared ได้ แต่ Client ไม่อ้างอิง API โดยตรง ทุก REST endpoint เริ่มภายใต้ `/api/v1` เพื่อรักษาความเข้ากันได้เมื่อขยายระบบ

## Foundation commands

```powershell
dotnet restore ScholarshipRequestManagement.sln
dotnet build ScholarshipRequestManagement.sln --no-restore
dotnet test ScholarshipRequestManagement.sln --no-build --no-restore
```

Dependency versions are managed centrally in `Directory.Packages.props`. Warnings are treated as build errors.
