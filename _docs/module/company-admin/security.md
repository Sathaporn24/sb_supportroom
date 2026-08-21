# company-admin — Security Review

## Open Findings — all rounds

| Sev | Finding | Location | Status | Round | Routes to |
|---|---|---|---|---|---|
| 🟠 Important | SEC-01 · JWT ที่ออกแล้วไม่รับรู้การปิดบริษัท/ปิดบัญชี/เปลี่ยน role | `backend/src/SupportRoom.Api/CurrentUserMiddleware.cs:37` | 🔵 Open | SECURITY-1 | backend-engineer |
| 🟠 Important | SEC-02 · `MustChangePassword` บังคับเฉพาะ frontend และ bypass ผ่าน API ได้ | `backend/src/SupportRoom.Api/Configurations/AuthenticationConfiguration.cs:72` | 🔵 Open | SECURITY-1 | backend-engineer |
| 🟠 Important | SEC-03 · login ของ admin ไม่มี rate limiting | `backend/src/SupportRoom.Api/Program.cs:75` | 🔵 Open | SECURITY-1 | backend-engineer |

## Summary

SECURITY-1 audit ทั้ง Phase 1 backend และ Phase 2 frontend ของ Module A: tenant provisioning,
owner-only company registry, initial credential lifecycle, `Company`/`AdminUser` ที่ไม่มี query
filter, default category chain และ migration ที่เกี่ยวข้อง พบ **3 Important findings** จึงยังปิด
Security gate ไม่ได้และเป็น hard stop ก่อน deploy. จุดเสี่ยงอยู่ที่ authorization lifecycle รอบ
JWT/initial password และ abuse control ของ login; implementation ของ CP-1, CP-4..CP-14 และ
CH-1..CH-8 ส่วนที่เหลือกลับมาสะอาดตามหลักฐานใน `## Clean`.

## Findings — Phase 1 and Phase 2 (SECURITY-1)

### 🟠 Important · SEC-01 · JWT ที่ออกแล้วไม่รับรู้การปิดบริษัท/ปิดบัญชี/เปลี่ยน role — `backend/src/SupportRoom.Api/CurrentUserMiddleware.cs:37`

**Status**: 🔵 Open

**What**: ทุก protected request นำ `userId`/`role`/`company_id` จาก JWT ที่ verify แล้วมาใส่
`ICurrentUser` โดยไม่อ่าน `AdminUser` หรือ `Company` กลับมาตรวจสถานะปัจจุบัน. การเช็ค
`AdminUser.IsActive` และ `Company.IsActive` มีเฉพาะตอน login ที่
`IAuthService.cs:54-117`; `CompanyService.Update` เปลี่ยนแถวใน database แต่ไม่ได้ revoke token.
ค่า default `JWT_EXPIRY_MINUTES` คือ 480 นาที และ `.env.example` ยืนยันตรงๆ ว่านี่คือช่วงที่
บัญชีที่ถูก deactivate ยังทำงานได้.

**Attack**: พนักงานของบริษัทที่ login อยู่ก่อน owner กดปิดบริษัทเก็บ bearer token เดิมไว้แล้วเรียก
REST/SignalR โดยตรงต่อได้สูงสุด 8 ชั่วโมง รวมถึงอ่าน/แก้ข้อมูลบริษัทตาม role เดิม. ในกรณีที่บัญชี
owner ถูกปิดหรือลด role, token owner เดิมยังเรียก `GET /api/companies/all`, สร้าง tenant+admin ใหม่
และเปลี่ยนสถานะบริษัทได้จน token หมดอายุ.

**Fix**: ส่งให้ `backend-engineer` ทำ authorization lifecycle ที่ revalidate สถานะบัญชี, role,
company binding และ `Company.IsActive` จาก server-side state บนทุก back-office request หรือใช้
token version/security stamp/revocation mechanism ที่ให้ผลเทียบเท่า; ต้อง fail closed สำหรับแถวที่
หาย/ปิด/role เปลี่ยน และเพิ่ม test ที่นำ token เก่ามาใช้หลัง deactivate/demotion.

### 🟠 Important · SEC-02 · `MustChangePassword` บังคับเฉพาะ frontend และ bypass ผ่าน API ได้ — `backend/src/SupportRoom.Api/Configurations/AuthenticationConfiguration.cs:72`

**Status**: 🔵 Open

**What**: fallback authorization policy เช็คเพียง `RequireAuthenticatedUser()`. ค่า
`MustChangePassword` ไม่อยู่ใน JWT และไม่มี middleware/policy/service guard ฝั่ง server ที่จำกัด
บัญชีซึ่งยังไม่เปลี่ยนรหัสผ่าน. `AdminGuard.tsx` redirect ไป `/admin/change-password` จริง แต่เป็น
client-side UX ที่แก้ local state หรือข้ามด้วย API client อื่นได้.

**Attack**: ผู้โจมตีที่ดักได้หรือเดารหัสตั้งต้นซึ่ง owner ส่งให้นอกระบบ login หนึ่งครั้ง แล้วนำ JWT
ไปเรียก API โดยตรงโดยไม่เปิด frontend และไม่เปลี่ยนรหัสผ่าน จึงเข้าถึงข้อมูล/จัดการผู้ใช้ของบริษัท
ได้ตลอดอายุ token ทั้งที่ `MustChangePassword = true` ถูกออกแบบมาเป็นมาตรการบังคับ.

**Fix**: ส่งให้ `backend-engineer` บังคับสถานะนี้ที่ server boundary โดยอนุญาตเฉพาะ endpoint ที่
จำเป็นต่อการอ่านตัวตนและเปลี่ยนรหัสผ่าน (เช่น `/api/auth/me` และ
`/api/auth/change-password`) จนกว่าจะเปลี่ยนสำเร็จ; ห้ามอาศัย claim ที่ stale เพียงอย่างเดียว และ
เพิ่ม integration/authorization tests ที่พิสูจน์ว่า bearer token ของบัญชีใหม่เรียก business API ไม่ได้.

### 🟠 Important · SEC-03 · login ของ admin ไม่มี rate limiting — `backend/src/SupportRoom.Api/Program.cs:75`

**Status**: 🔵 Open

**What**: application ลงทะเบียน CORS, EF, services และ authentication แต่ไม่มี
`AddRateLimiter`/`UseRateLimiter` หรือ policy บน `[AllowAnonymous] POST /api/auth/login`.
Password policy ยอมรับรหัสที่ยาวอย่างน้อย 10 ตัวอักษรและรหัสตั้งต้นเป็นสิ่งที่ owner พิมพ์เอง;
ข้อความ login แบบคงที่กัน account enumeration ทางข้อความได้ แต่ไม่ได้จำกัดจำนวนครั้งที่เดา.

**Attack**: ผู้โจมตีที่รู้อีเมล admin ลูกค้า (ข้อมูลธุรกิจที่มักเดาได้) ส่ง password guesses หรือ
credential-stuffing เข้า login ได้ไม่จำกัด. ถ้า owner ตั้ง temporary password ที่คาดเดาได้ ผู้โจมตี
ได้ JWT role `admin` ก่อนเจ้าของบัญชีเปลี่ยนรหัส; แม้เดาไม่สำเร็จ การ hash และ failed-login log
ทุกครั้งยังใช้ทำ resource/log exhaustion ได้.

**Fix**: ส่งให้ `backend-engineer` ใช้ ASP.NET Core rate limiting ที่ server boundary สำหรับ login
(และกำหนด abuse policy สำหรับ sensitive authenticated provisioning route ตาม R-2) โดยผูกทั้ง
source/client และ normalized account อย่างระวังไม่ให้ใช้ล็อก victim แบบถาวร, คืน 429 สม่ำเสมอ,
ตั้ง proxy/forwarded-header trust ให้ถูกต้อง และเพิ่ม tests สำหรับ limit/reset/error envelope.

## Clean

- **Authentication/CSRF/CORS**: `CompanyController` มี `[Authorize]`; fallback policy ปิด endpoint
  ใหม่โดย default; JWT ตรวจ signature, issuer, audience, expiry และใช้ secret จาก environment ที่
  บังคับขั้นต่ำ 32 ตัวอักษร. Bearer token ส่งใน `Authorization` header ไม่ใช้ auth cookie จึงไม่มี
  ambient-credential CSRF บน company routes; CORS รับเฉพาะ explicit origins และไม่เปิด credentials.
- **CP-1/authorization per route**: `Create`, `GetAllIncludingInactive` และ `Update` เรียก
  `guard.EnsureOwner()` ก่อน business validation/query. non-owner แก้ `?company=` หรือ bypass
  frontend guard แล้วไม่ได้ company registry/tenant provisioning. `GET /api/companies` เดิมยังคืน
  active switchable companies เท่านั้น.
- **CP-4/CP-5 enumeration**: duplicate email ใช้ข้อความคงที่ที่ไม่คืน company/role; slug active และ
  inactive แยกข้อความตาม contract โดยไม่ reactivate/overwrite แถวเดิม. ทั้งสอง lookup เกิดหลัง
  owner guard.
- **CP-6/CP-8/password storage**: request DTO ไม่มี `Role`/`CompanyId`; server hardcode
  `AdminRole.Admin`, `CompanyId` ใหม่ และ `MustChangePassword = true`. รหัสผ่านผ่าน ASP.NET
  Identity `PasswordHasher<AdminUser>`; path provisioning stage Company/AdminUser/categories แล้ว
  `Commit()` ครั้งเดียวและไม่เรียก `IAdminUserService.Create`.
- **CP-10/CP-11/data exposure**: `CompanyViewModel` มีเพียง `id`/`name`/`isActive`; response ไม่มี
  email/password/hash. provisioning log มี company/admin/actor ids เท่านั้น; safe request logger ไม่
  log path/query/body และ generic exception responseไม่คืน stack trace.
- **CP-12/tenant isolation**: ไม่มี `IgnoreQueryFilters()` ใน provisioning service,
  `CreateDefaultChain` หรือ migrations. `KnowledgeCategory` ยังคง company+soft-delete query filter;
  service stage parent/leaf โดยไม่ query/commit และ test resolve company context ก่อนอ่านกลับ.
- **CP-13/CP-14/update**: `/api/companies/all` owner-only และคืน inactive โดยไม่ขยาย DTO;
  `PUT /api/companies/{id}` owner-only. Frontend ใช้ `/all`, owner-only menu/route guard และแสดง
  CP-14 ตรง contract.
- **Frontend credential handling**: `createCompany()` response ถูก ignore; initial password อยู่ใน
  React state ของหน้าเท่านั้น ไม่เขียน local/session storage, query string หรือ log. ช่องกรอกเป็น
  `type="password"` + `autoComplete="new-password"`; summary ใช้ form state ตาม CP-10.
- **Injection/mass assignment**: company slug ถูก allowlist/normalize, route id URL-encode, EF queries
  parameterized, migration SQL เป็น static data-only SQL. DTO จำกัด writable fieldsและ frontend
  render string ผ่าน React escaping.
- **CH-3/migrations**: runtime สร้าง parent+leaf เชื่อมกันหนึ่งชุด; backfill ใช้ per-company
  `NOT EXISTS`/count guards และ corrective migration เปลี่ยนเฉพาะ timestamp ของ deterministic leaf,
  ไม่มี dynamic SQL หรือ query-filter bypass.

Focused verification: `dotnet test SupportRoom.slnx --no-restore --no-build --filter
"FullyQualifiedName~CompanyServiceTests|FullyQualifiedName~AuthorizationGuardTests|FullyQualifiedName~CompanyIsolationTests|FullyQualifiedName~KnowledgeCategoryServiceTests|FullyQualifiedName~DefaultCategoryChainCorrectiveMigrationTests"`
ผ่าน 42 tests (Application 40, Providers 2; Api.IntegrationTests ไม่มี test ที่ match filter). การรัน
ใน sandbox ครั้งแรกถูก runtime ปฏิเสธ named-pipe bind; รันซ้ำด้วย permission ที่อนุมัติแล้วจึงผ่าน.

## Accepted Risks

ไม่มี finding ของ SECURITY-1 ที่ถูกยอมรับ. Design-level decisions เดิมยังคงอยู่: R-8 อนุญาตให้
learner links ใช้ต่อจนหมดอายุ และ R-9 ยอมรับว่า owner รู้ initial password จน admin เปลี่ยนเอง;
การยอมรับสองข้อนี้ไม่ครอบสาม open findings ข้างบน.

## Change Log

- 2026-08-21 — SECURITY-1 audit Phase 1 backend + Phase 2 frontend: เปิด 3 Important findings
  เรื่อง stale authorization ของ JWT, server-side forced-password-change bypass และไม่มี login
  rate limiting; Security gate ยังไม่ผ่านและต้อง re-audit หลัง backend fix.
