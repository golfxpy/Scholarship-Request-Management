# Scholarship Request Management

ระบบบริหารจัดการคำขอทุนการศึกษา สำหรับรับคำขอจากนักศึกษาและสนับสนุนการทำงานของเจ้าหน้าที่ ตั้งแต่การค้นหา แก้ไข ลบแบบ Soft Delete อนุมัติ ปฏิเสธ ไปจนถึงการดูสรุปผลบน Dashboard

ระบบตัวอย่างนี้กำหนดบริบทเป็นมหาวิทยาลัยสงขลานครินทร์ วิทยาเขตหาดใหญ่ และออกแบบโครงสร้างไว้ให้เพิ่มวิทยาเขต บทบาท ขั้นตอนอนุมัติ รายงาน และการเชื่อมต่อระบบทะเบียนได้ในอนาคต

## ความสามารถหลัก

### นักศึกษา

- ยื่นคำขอทุนได้โดยไม่ต้องมีบัญชีผู้ใช้
- เลือกประเภททุนและค้นหาคณะ/หน่วยการเรียนของวิทยาเขตหาดใหญ่
- กรอกข้อมูลการศึกษา จำนวนเงิน เลขบัญชี และเหตุผลประกอบคำขอ
- อ่านและยอมรับประกาศความเป็นส่วนตัวก่อนส่งข้อมูล
- ระบบตรวจสอบข้อมูลทั้งที่หน้าเว็บและ REST API
- เมื่อบันทึกสำเร็จ ระบบออกเลขคำขอและกำหนดสถานะเริ่มต้นเป็น `Pending`

### เจ้าหน้าที่

- เข้าสู่ระบบด้วยบัญชี Staff
- ดู Dashboard สรุปจำนวนคำขอและยอดเงินแยกตามสถานะ/ประเภททุน
- ดูรายการแบบ 10 รายการต่อหน้า ค้นหาจากรหัสหรือชื่อนักศึกษา และกรองตามสถานะ/ประเภททุน
- เพิ่มคำขอแทนนักศึกษา โดยต้องระบุวิธีรับ Consent และหลักฐานประกอบ
- แก้ไขและลบแบบ Soft Delete ได้เฉพาะคำขอสถานะ `Pending`
- อนุมัติหรือปฏิเสธคำขอได้ครั้งเดียว โดยการปฏิเสธต้องระบุหมายเหตุ
- คำขอ `Approved` และ `Rejected` เป็นข้อมูลอ่านอย่างเดียวและเปิดกลับไม่ได้

## เทคโนโลยีที่ใช้

| ส่วน | เทคโนโลยี |
|---|---|
| Frontend | Blazor WebAssembly บน .NET 10 และ MudBlazor 9.7.0 |
| Backend | ASP.NET Core 10 REST API |
| Database | PostgreSQL 18.4 |
| ORM / Migration | Entity Framework Core 10.0.6 และ Npgsql 10.0.3 |
| Authentication | ASP.NET Core Identity, Session Cookie และ Antiforgery Token |
| Reverse proxy / Web server | nginx 1.29 Alpine |
| Container | Docker และ Docker Compose |
| Testing | xUnit, ASP.NET Core MVC Testing และ Testcontainers for PostgreSQL |
| API documentation | OpenAPI |

เวอร์ชันแพ็กเกจถูกควบคุมจากไฟล์ `Directory.Packages.props` ส่วน Docker image และ SDK ถูกกำหนดเวอร์ชันไว้ใน Dockerfile เพื่อให้สภาพแวดล้อมของผู้ตรวจใกล้เคียงกับเครื่องพัฒนา

## โครงสร้างโปรเจกต์

```text
Scholarship-Request-Management/
├─ src/
│  ├─ ScholarshipRequest.Client/       Blazor WebAssembly UI
│  ├─ ScholarshipRequest.Api/          ASP.NET Core REST API และ EF Core
│  └─ ScholarshipRequest.Shared/       API contracts ที่ใช้ร่วมกัน
├─ tests/
│  ├─ ScholarshipRequest.UnitTests/    Unit Tests
│  └─ ScholarshipRequest.IntegrationTests/ Integration Tests กับ PostgreSQL จริง
├─ compose.yaml
├─ .env.example
└─ ScholarshipRequestManagement.sln
```

Client ติดต่อ Backend ผ่าน REST API ภายใต้ `/api/v1` และไม่อ้างอิงโค้ดภายในของ API โดยตรง โครงสร้าง API แยกตาม Feature เพื่อรองรับการเพิ่มความสามารถโดยไม่ต้องรวมทุกอย่างไว้ในไฟล์เดียว

## วิธีติดตั้งและรันระบบสำหรับกรรมการ

วิธีที่แนะนำคือ Docker Compose เพราะจะเตรียม Frontend, Backend, PostgreSQL, Migration และ Seed Data ให้ครบในคำสั่งเดียว ไม่ต้องติดตั้ง .NET SDK หรือ PostgreSQL แยกบนเครื่องผู้ตรวจ

### 1. โปรแกรมที่ต้องมี

- Git สำหรับดาวน์โหลด Source Code
- Docker Desktop บน Windows/macOS หรือ Docker Engine พร้อม Compose v2 บน Linux
- พอร์ต `8080` ว่าง หรือกำหนดพอร์ตอื่นใน `.env`
- อินเทอร์เน็ตสำหรับดาวน์โหลด Docker image และ NuGet packages ในการ Build ครั้งแรก

ตรวจสอบว่า Docker พร้อมใช้งาน:

```powershell
docker --version
docker compose version
docker info
```

หากใช้ Docker Desktop ต้องเปิดโปรแกรมและรอให้ Docker Engine อยู่ในสถานะพร้อมใช้งานก่อนดำเนินการต่อ

### 2. เข้าไปยังโฟลเดอร์โปรเจกต์

กรณี Clone จาก Git repository:

```powershell
git clone <repository-url>
Set-Location Scholarship-Request-Management
```

กรณีได้รับ Source Code เป็นไฟล์ ZIP ให้แตกไฟล์ แล้วเปิด PowerShell หรือ Terminal ที่โฟลเดอร์ซึ่งมีไฟล์ `compose.yaml`

### 3. สร้างไฟล์ Environment สำหรับเครื่องผู้ตรวจ

บน PowerShell:

```powershell
Copy-Item .env.example .env
notepad .env
```

บน macOS/Linux:

```bash
cp .env.example .env
```

แก้ค่า `POSTGRES_PASSWORD` ใน `.env` ก่อนรัน เช่น:

```dotenv
POSTGRES_DB=scholarship
POSTGRES_USER=scholarship
POSTGRES_PASSWORD=LocalDb@2569!
WEB_PORT=8080
DEMO_ADMIN_USERNAME=admin
DEMO_ADMIN_PASSWORD=Scholarship@2569
```

ข้อควรระวัง:

- ไม่ควร Commit ไฟล์ `.env` ขึ้น Git
- `POSTGRES_PASSWORD` ต้องไม่ปล่อยเป็นข้อความ `replace-with-local-development-password`
- บัญชี Demo ด้านบนใช้สำหรับการตรวจระบบในเครื่องเท่านั้น ห้ามนำไปใช้กับ Production

### 4. Build และเปิดระบบ

```powershell
docker compose up --build -d
```

การรันครั้งแรกอาจใช้เวลาหลายนาทีตามความเร็วอินเทอร์เน็ต Docker จะดำเนินการตามลำดับดังนี้:

1. เปิด PostgreSQL และรอจนฐานข้อมูลพร้อมใช้งาน
2. Build และเปิด ASP.NET Core API
3. API ใช้ EF Core Migration สร้าง/ปรับโครงสร้างฐานข้อมูล
4. API นำเข้า Master Data, บัญชีทดสอบ และข้อมูลคำขอตัวอย่าง
5. เปิด nginx เพื่อให้บริการ Blazor WebAssembly และส่งต่อ `/api` ไปยัง Backend

### 5. ตรวจสอบสถานะ Container

```powershell
docker compose ps
```

ควรพบ Service `db`, `api` และ `web` อยู่ในสถานะ `Up` และ `healthy` หาก Service ยังไม่พร้อม ให้รอประมาณ 10–30 วินาทีแล้วตรวจอีกครั้ง

ดู Log เมื่อต้องการตรวจสอบปัญหา:

```powershell
docker compose logs -f api
```

กด `Ctrl+C` เพื่อออกจากหน้าดู Log โดย Container จะยังทำงานต่อ

### 6. เปิดระบบ

| หน้าจอ | URL |
|---|---|
| หน้าแรก | [http://localhost:8080](http://localhost:8080) |
| แบบฟอร์มยื่นคำขอของนักศึกษา | [http://localhost:8080/apply](http://localhost:8080/apply) |
| เข้าสู่ระบบเจ้าหน้าที่ | [http://localhost:8080/admin/login](http://localhost:8080/admin/login) |
| Dashboard เจ้าหน้าที่ | [http://localhost:8080/admin](http://localhost:8080/admin) |
| รายการคำขอ | [http://localhost:8080/admin/requests](http://localhost:8080/admin/requests) |

หากแก้ `WEB_PORT` ใน `.env` ให้เปลี่ยนเลขพอร์ตใน URL ตามค่าดังกล่าว

### 7. บัญชีผู้ใช้ทดสอบ

| รายการ | ค่าเริ่มต้น |
|---|---|
| Username | `admin` |
| Password | `Scholarship@2569` |
| Role | `Staff` |

บัญชีนี้ถูกสร้างเฉพาะเมื่อระบบทำงานใน Environment `Development` และเปิดใช้งาน Development Demo Seed รหัสผ่านถูก Hash ด้วย ASP.NET Core Identity ก่อนเก็บลง PostgreSQL

### 8. หยุดหรือเปิดระบบใหม่

หยุดระบบโดยเก็บข้อมูลในฐานข้อมูลไว้:

```powershell
docker compose down
```

เปิดระบบครั้งถัดไปโดยไม่ต้อง Build ใหม่:

```powershell
docker compose up -d
```

Build ใหม่เมื่อ Source Code หรือ Dependency เปลี่ยน:

```powershell
docker compose up --build -d
```

## วิธีนำเข้าข้อมูลตัวอย่าง (Seed Data)

ไม่ต้อง Import ไฟล์ SQL ด้วยตนเอง ระบบจะนำเข้า Seed Data อัตโนมัติระหว่าง API Startup หลังจาก EF Core Migration สำเร็จ

### ข้อมูลที่ระบบสร้างให้อัตโนมัติ

- วิทยาเขตหาดใหญ่
- คณะ/หน่วยการเรียน 16 รายการ
- ประเภททุน 5 ประเภท
- ประกาศความเป็นส่วนตัวเวอร์ชัน `POC-v1`
- บัญชี Staff ตามค่า `DEMO_ADMIN_USERNAME` และ `DEMO_ADMIN_PASSWORD`
- คำขอจำลอง 25 รายการ ครบทั้ง 5 ประเภททุน
- สถานะจำลอง `Pending` 10 รายการ, `Approved` 8 รายการ และ `Rejected` 7 รายการ

ข้อมูลจำลองใช้เลขคำขอรูปแบบ `DEMO-2569-xxxxxx` เพื่อไม่ชนกับเลขคำขอจริงที่ระบบสร้างในรูปแบบ `SCH-<ปี พ.ศ.>-xxxxxx`

### ขั้นตอนนำเข้า Seed Data ครั้งแรก

1. ตรวจว่า `ASPNETCORE_ENVIRONMENT` ของ API เป็น `Development` ซึ่งกำหนดไว้แล้วใน `compose.yaml`
2. ตรวจว่า `DevelopmentDemoSeed__Enabled` เป็น `true` ซึ่งกำหนดไว้แล้วใน `compose.yaml`
3. กำหนด Username/Password ใน `.env`
4. รันคำสั่ง:

```powershell
docker compose up --build -d
```

5. รอให้ `api` เป็น `healthy`
6. Login ด้วยบัญชีทดสอบและเปิดหน้า `/admin/requests`
7. ระบบควรแสดง 25 รายการ แบ่งเป็นหน้า 10, 10 และ 5 รายการ

Seeder เป็นแบบ Idempotent จึงสามารถ Restart Container ได้โดยไม่สร้างบัญชีหรือคำขอจำลองซ้ำ และใช้ PostgreSQL advisory transaction lock เพื่อป้องกันการ Seed ซ้ำเมื่อ API หลาย Instance เริ่มพร้อมกัน

### สร้างฐานข้อมูลตัวอย่างใหม่ทั้งหมด

ใช้เฉพาะเมื่อต้องการล้างข้อมูลทดสอบเดิมและเริ่มใหม่:

```powershell
docker compose down --volumes
docker compose up --build -d
```

คำสั่ง `down --volumes` จะลบข้อมูล PostgreSQL และ Data Protection keys ของโปรเจกต์นี้ทั้งหมด ข้อมูลเดิมจะกู้คืนไม่ได้ หลังเปิดระบบใหม่ Migration และ Seed Data จะทำงานอีกครั้งโดยอัตโนมัติ

หากเปลี่ยน `POSTGRES_PASSWORD`, `DEMO_ADMIN_USERNAME` หรือ `DEMO_ADMIN_PASSWORD` หลังจาก Seed ครั้งแรก ค่าใหม่จะไม่แก้บัญชี/ฐานข้อมูลเดิมใน Volume โดยอัตโนมัติ สำหรับฐานข้อมูล Demo สามารถใช้ขั้นตอนล้าง Volume ด้านบนเพื่อสร้างใหม่ได้

### การปิด Seed อัตโนมัตินอก Development

ระบบตรวจทั้ง Environment และ Flag ก่อน Seed บัญชี/คำขอจำลอง ดังนั้นเมื่อ API ไม่ได้ทำงานใน `Development` ระบบจะไม่สร้าง Demo account และคำขอ 25 รายการ แม้จะตั้ง Flag เป็น `true` ก็ตาม

สำหรับ Production ต้องใช้ Migration/กระบวนการสร้างบัญชีที่ควบคุมสิทธิ์แยกต่างหาก และห้ามใช้ Credential ตัวอย่างใน README นี้

## แนวทางตรวจระบบแบบรวดเร็วสำหรับกรรมการ

1. เปิด `/apply` กรอกคำขอที่ถูกต้อง ยอมรับ PDPA และตรวจว่าได้รับเลขคำขอจาก Server
2. เปิด `/admin/login` และเข้าสู่ระบบด้วยบัญชีทดสอบ
3. ตรวจ Dashboard ว่ามีจำนวน `Pending`, `Approved` และ `Rejected`
4. เปิดรายการคำขอและตรวจ Pagination 10 รายการต่อหน้า
5. ทดลองค้นหาจากรหัส/ชื่อนักศึกษา และใช้ตัวกรองสถานะร่วมกับประเภททุน
6. เปิดคำขอ `Pending` แล้วทดลองแก้ไข
7. ทดลองปฏิเสธโดยไม่กรอกหมายเหตุ ระบบต้องไม่อนุญาต
8. กรอกหมายเหตุแล้วปฏิเสธ ระบบต้องเปลี่ยนเป็น `Rejected` และไม่แสดงปุ่มแก้ไข/ลบ/ตัดสินอีก
9. ทดลองอนุมัติคำขอ `Pending` โดยไม่กรอกหมายเหตุ ระบบต้องอนุญาต
10. ทดลองลบคำขอ `Pending` และตรวจว่ารายการนั้นไม่แสดงในรายการปกติหรือ Dashboard

## กติกาการทำงานของระบบ

- นักศึกษายื่นคำขอโดยไม่ต้อง Login และคำขอใหม่เริ่มเป็น `Pending`
- เจ้าหน้าที่สร้างคำขอแทนนักศึกษาได้ แต่ต้องระบุวิธีรับ Consent เป็น `Document`, `Verbal` หรือ `Other` พร้อมหลักฐาน
- รหัสนักศึกษาของคำขอเดิมแก้ไม่ได้ เพื่อไม่ให้หลักฐาน Consent ถูกนำไปผูกกับบุคคลอื่น
- แก้ไข ลบ อนุมัติ หรือปฏิเสธได้เฉพาะ `Pending`
- การเปลี่ยนสถานะรองรับเฉพาะ `Pending → Approved` หรือ `Pending → Rejected`
- `Approved` และ `Rejected` เปิดกลับหรือเปลี่ยนผลไม่ได้
- Reject ต้องมีหมายเหตุ ส่วน Approve ไม่บังคับหมายเหตุ
- Update, Delete และ Decision ตรวจ `UpdatedAt` เพื่อป้องกันเจ้าหน้าที่เขียนทับข้อมูลที่มีผู้อื่นแก้ไขล่าสุด
- Dashboard และรายการปกติไม่รวมคำขอที่ถูก Soft Delete
- รายการคำขอไม่ส่งข้อมูลบัญชีธนาคาร ส่วนรายละเอียดแสดงเพียงรูปแบบ Mask เช่น `******7890`

## REST API หลัก

### Public API

- `GET /api/v1/public/scholarship-types`
- `GET /api/v1/public/academic-units?query=...`
- `GET /api/v1/public/pdpa-notice`
- `POST /api/v1/public/scholarship-requests`

### Authentication API

- `GET /api/v1/auth/antiforgery-token`
- `POST /api/v1/auth/login`
- `GET /api/v1/auth/session`
- `POST /api/v1/auth/logout`

### Admin API

- `GET /api/v1/admin/dashboard`
- `GET /api/v1/admin/scholarship-requests`
- `GET /api/v1/admin/scholarship-requests/{id}`
- `POST /api/v1/admin/scholarship-requests`
- `PUT /api/v1/admin/scholarship-requests/{id}`
- `DELETE /api/v1/admin/scholarship-requests/{id}?expectedUpdatedAt=...`
- `POST /api/v1/admin/scholarship-requests/{id}/decision`

Admin API ทุก Endpoint ตรวจสิทธิ์ `Staff` และสถานะ `IsActive` จาก PostgreSQL ฝั่ง Server ส่วนคำสั่งที่เปลี่ยนข้อมูลต้องส่ง Antiforgery Token

## การ Build และทดสอบ Source Code

ส่วนนี้ใช้สำหรับผู้ตรวจที่ติดตั้ง .NET SDK 10 และต้องการ Build/Test โดยตรง

```powershell
dotnet restore ScholarshipRequestManagement.sln
dotnet build ScholarshipRequestManagement.sln --no-restore
dotnet test tests/ScholarshipRequest.UnitTests/ScholarshipRequest.UnitTests.csproj --no-restore
dotnet test tests/ScholarshipRequest.IntegrationTests/ScholarshipRequest.IntegrationTests.csproj --no-restore
dotnet format ScholarshipRequestManagement.sln --verify-no-changes --no-restore
dotnet list ScholarshipRequestManagement.sln package --vulnerable --include-transitive --no-restore
```

Integration Tests ใช้ Testcontainers เพื่อเปิด PostgreSQL Container ชั่วคราว ดังนั้น Docker Engine ต้องทำงานอยู่ก่อนรันทดสอบ

ผลตรวจล่าสุดของ Source Code ชุดนี้:

- Build ผ่านโดยไม่มี Warning หรือ Error
- Unit Tests ผ่าน 69 รายการ
- Integration Tests ผ่าน 33 รายการ
- รวม 102 Tests

## การจัดการฐานข้อมูลแบบนักพัฒนา

เมื่อต้องการใช้ EF Core CLI ให้ Restore เครื่องมือของ Repository ก่อน:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/ScholarshipRequest.Api --startup-project src/ScholarshipRequest.Api
dotnet tool run dotnet-ef migrations has-pending-model-changes --project src/ScholarshipRequest.Api --startup-project src/ScholarshipRequest.Api
```

การเชื่อมต่อฐานข้อมูลนอก Docker ต้องกำหนด Connection String ผ่าน Environment Variable `ConnectionStrings__DefaultConnection`

## ความปลอดภัยและข้อมูลส่วนบุคคล

- Password จัดเก็บเป็น Salted Hash ด้วย ASP.NET Core Identity
- Authentication ใช้ HttpOnly Session Cookie และไม่เก็บ Credential/Token ใน Browser Storage
- คำสั่งเปลี่ยนข้อมูลตรวจ Antiforgery Token
- API ฝั่ง Admin ตรวจ Role และ `IsActive` ทุกคำขอ
- เลขบัญชีถูกปกป้องก่อนบันทึก และไม่ถูกส่งกลับแบบเต็ม
- Response ที่มีข้อมูลส่วนบุคคลกำหนด `Cache-Control: no-store`
- Web port ของ Compose bind เฉพาะ `127.0.0.1` เพื่อไม่เปิดบัญชี Demo ให้เครื่องอื่นในเครือข่ายเข้าถึง
- Data Protection keys แยกอยู่ใน Named Volume เพื่อให้ข้อมูลที่ปกป้องไว้ยังอ่านได้หลัง Restart

ก่อนใช้งานจริงต้องเปลี่ยน Credential, ปิด Demo Seed, เปิด HTTPS, ป้องกัน Data Protection keys ด้วยระบบจัดการ Secret/Key ที่เหมาะสม, กำหนด Retention Policy, เพิ่ม Rate Limiting และกำหนดผู้มีสิทธิ์เข้าถึงข้อมูลบัญชีอย่างชัดเจน

## การแก้ปัญหาเบื้องต้น

### พอร์ต 8080 ถูกใช้งาน

แก้ `WEB_PORT` ใน `.env` เช่น:

```dotenv
WEB_PORT=8081
```

จากนั้นเปิดระบบที่ `http://localhost:8081`

### Container ไม่เป็น healthy

```powershell
docker compose ps
docker compose logs db
docker compose logs api
docker compose logs web
```

ตรวจว่า Docker Engine ทำงาน, `.env` มี `POSTGRES_PASSWORD` และเครื่องมีพื้นที่ดิสก์เพียงพอ

### เปลี่ยนรหัส PostgreSQL แล้ว API เชื่อมต่อไม่ได้

PostgreSQL ใช้ `POSTGRES_PASSWORD` เฉพาะตอนสร้าง Data Volume ครั้งแรก การแก้ `.env` ภายหลังไม่เปลี่ยนรหัสของ Role ในฐานข้อมูลเดิม สำหรับข้อมูล Demo ให้ล้าง Volume และสร้างใหม่ หรือเปลี่ยนรหัสของ Database Role อย่างถูกต้อง

### เปลี่ยนบัญชี Demo แล้ว Login ด้วยค่าใหม่ไม่ได้

Seeder ไม่เขียนทับบัญชีที่สร้างไว้แล้ว การเปลี่ยน `DEMO_ADMIN_USERNAME` หรือ `DEMO_ADMIN_PASSWORD` หลัง Seed ครั้งแรกจึงไม่แก้บัญชีเดิม สำหรับการทดสอบใหม่ทั้งหมดให้ใช้ `docker compose down --volumes` แล้วเปิดระบบใหม่

### รัน Client DevServer แยกแล้วเรียก API ไม่ได้

Blazor WebAssembly แบบ Standalone ใช้ nginx ใน Compose เป็น Same-origin reverse proxy สำหรับ `/api` ปัจจุบันยังไม่มี CORS/Dev proxy สำหรับการรัน Client แยกเดี่ยว จึงแนะนำให้ใช้ Docker Compose เมื่อตรวจการทำงานครบทั้งระบบ

## แนวทางขยายระบบในอนาคต

โครงสร้างปัจจุบันเตรียมจุดขยายไว้โดยไม่เปลี่ยน Workflow หลัก ได้แก่:

1. ประวัติ Audit/Event ของการแก้ไข การตัดสิน Consent และการเข้าถึงข้อมูลสำคัญ
2. รายงานตามช่วงวันที่และ Export CSV/PDF
3. แยก Role Staff, Reviewer และ Approver รวมถึง Two-person approval
4. หน้าติดตามสถานะสำหรับนักศึกษา การแจ้งเตือน และเอกสารแนบ
5. การจัดการ Master Data หลายวิทยาเขตและเชื่อมต่อระบบทะเบียน
6. Secret management, Rate limiting, Observability, Backup/Restore และ TLS deployment profile
7. PostgreSQL trigram/partial indexes และ Cursor pagination เมื่อปริมาณข้อมูลเพิ่มขึ้นจริง

POC นี้ตั้งใจยังไม่เปิดเผยเลขบัญชีเต็ม, ไม่มี Restore/Reopen, ไม่มี User Management, ไม่ส่ง Email, ไม่รับไฟล์แนบ และไม่มี Public status tracking เพื่อให้ฟังก์ชันหลักตามโจทย์เสถียรก่อนเพิ่มความสามารถเสริม
