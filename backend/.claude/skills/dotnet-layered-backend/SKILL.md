---
name: dotnet-layered-backend
description: "Use when writing or reviewing an ASP.NET Core RESTful Web API backend (controllers, services, repositories, entities, DTOs) that should follow this layered/N-tier convention — Controller → Service → Repository → UnitOfWork → EF Core, with interface+implementation in one file, Mapster mapping, GeneralException error codes, and MatTableDataSource pagination. The pattern itself was distilled from the cgc-marketplace (marketplace-api) codebase, but the target domain here is an AI-powered training/tutoring room product: session-based voice Q&A where a learner's spoken question is transcribed, grounded/answered by an AI (e.g. Gemini), and spoken back via TTS — not e-commerce. Baseline is .NET 8 with a .NET 10 upgrade-notes section (EF Core 10 global query filters, C# 14 syntax, package compatibility risks). Trigger on: \"สร้าง module ใหม่\", \"เขียน backend แบบ pom\", \"สร้าง CRUD API\", \"ตามแพทเทิร์น marketplace-api\", \"อัปเกรด .net10\", หรือเมื่อกำลังแก้ไข/เพิ่มโค้ดในโปรเจกต์ .NET ที่ใช้ BossupStandard/UnitOfWork/RepositoryBase/ServiceBase."
---

# .NET Layered Backend Style — RESTful API สำหรับระบบ AI อบรม/ตอบคำถามด้วยเสียง

สกิลนี้สรุป convention การเขียน **RESTful API** แบบ **Layered Architecture** (Controller → Service → Repository → UnitOfWork → EF Core)
รูปแบบ pattern เดิมดึงมาจากโค้ดจริงในโปรเจกต์ `cgc-marketplace` (marketplace-api) แต่แนวคิดโดเมนที่ใช้สกิลนี้จริงคือ**ระบบ AI สำหรับอบรม/สอน** — ผู้เรียนถามคำถามด้วยเสียง (push-to-talk) ระบบแปลงเสียงเป็นข้อความ ให้ AI (เช่น Gemini) ค้นหาข้อมูล/grounded search มาตอบ แล้วพูดคำตอบกลับด้วย TTS ไม่ใช่โดเมน e-commerce ของต้นแบบ — ตัวอย่างโค้ด Controller/Service/Repository ด้านล่างยังอ้างอิงชื่อ entity แบบเดิม (Product ฯลฯ) เพื่อสอน pattern แต่เวลาใช้งานจริงให้แทนที่ด้วย entity ของระบบอบรม (เช่น `TrainingSession`, `SessionQuestion`, `LessonConfig`)

โปรเจกต์ต้นแบบพึ่งพา internal shared library ชื่อ **`BossupStandard`** ซึ่งมี base class ให้ (`ServiceBase`, `RepositoryBase`, `UnitOfWorkBase`, `CoreDbContextBase`, `HttpStatusCodeException`, `MatTableDataSource`, `PageCriteria` ฯลฯ) — ถ้าโปรเจกต์ใหม่ไม่มี library นี้ ให้เขียน base class เทียบเท่าเองก่อน หรือถามผู้ใช้ว่ามี shared package แบบนี้หรือไม่

## 1. โครงสร้างโฟลเดอร์ (Folder Structure)

```
<ProjectName>/
├── Controllers/            # 1 controller ต่อ 1 resource, [ApiController][Route("[controller]")]
├── Services/                # I<X>Service + class <X>Service ในไฟล์เดียวกัน
├── Repository/              # I<X>Repository + class <X>Repository ในไฟล์เดียวกัน
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── Entity/               # EF Core entities (1 class ต่อไฟล์)
│   │   └── Config/            # Fluent API configuration (IEntityTypeConfiguration) ถ้ามี
│   ├── <Module>Configuration/ # เช่น ChatConfiguration/ สำหรับ fluent config ของ entity ที่ซับซ้อน
│   └── UnitOfWork/
│       └── UnitOfWork.cs      # map Interface -> Implementation ของทุก repository
├── Dto/                      # request/input models (มี [Required] validation)
├── ViewModel/                 # response/output models (แยกตาม module, module ใหญ่แยกเป็นโฟลเดอร์ย่อยได้ เช่น ViewModel/ProductViewModel/)
├── Enums/                     # enum ต่อ module เช่น ProductEnum.cs, OrderEnum.cs
├── Exceptions/                 # GeneralException.cs (static factory ของ error ทั้งระบบ) + exception เฉพาะโมดูล
├── Mapper/                     # AutoMapper profile (MapperProfile.cs) — ใช้คู่กับ Mapster (.Adapt<>())
├── Configurations/              # extension methods สำหรับ Program.cs (ServiceConfiguration, EntityFramworkConfiguration)
├── Constants/                    # ApplicationSetting.cs (ค่า config แบบ static)
├── Utilities/                     # helper เช่น HttpContextHelper.cs
├── ChatHub/                        # SignalR hub (ถ้ามี realtime feature)
├── Migrations/                      # EF Core migrations
└── Program.cs
```

**กติกาการตั้งชื่อ:** PascalCase ทุก class/property, ชื่อไฟล์ = ชื่อ class หลักในไฟล์, interface ขึ้นต้นด้วย `I`, entity เป็นเอกพจน์ (`Product` ไม่ใช่ `Products`), DbSet ใน DbContext ใช้ชื่อเอกพจน์เดียวกับ entity

## 2. Layer แต่ละชั้นทำหน้าที่อะไร

### Controller
- `[ApiController] [Route("[controller]")]`, สืบทอดจาก `ControllerBase`
- **Resolve service ผ่าน `IServiceProvider.GetRequiredService<T>()`** ใน constructor แทน constructor injection ตรงๆ (เป็น pattern ที่ใช้สม่ำเสมอทั้งโปรเจกต์ — ต้องทำตาม)
- Controller ทำแค่ route + forward เข้า service, **ห้ามมี business logic ใน controller**

```csharp
[ApiController]
[Route("[controller]")]
public class ProductController : ControllerBase
{
    private readonly IProductService _service;

    public ProductController(IServiceProvider serviceProvider)
    {
        _service = serviceProvider.GetRequiredService<IProductService>();
    }

    [HttpPost()]
    public async Task<ProductViewModel> Create([FromBody] ProductDto input)
        => await _service.Create(input);

    [HttpGet("{id}")]
    public Task<GetProductByIdViewModel> GetByIdAsync([FromRoute] Guid id)
        => _service.GetByIdAsync(id);
}
```

### Service (`Services/I<X>Service.cs`)
- **Interface + implementation อยู่ไฟล์เดียวกัน**, namespace `<Project>.Services`
- Implementation สืบทอดจาก `ServiceBase<I<X>Service, ApplicationDbContext>`
- Constructor รับ `IUnitOfWork<ApplicationDbContext> unitOfWork, IServiceProvider serviceProvider, ILogger<I<X>Service> logger` แล้ว `: base(unitOfWork, serviceProvider, logger)`
- ดึง repository ผ่าน `unitOfWork.GetRepository<IXxxRepository>()`, ดึง service อื่นผ่าน `serviceProvider.GetRequiredService<IXxxService>()`
- **นี่คือชั้นที่มี business logic ทั้งหมด**: validate, throw `GeneralException.XXX()`, map ด้วย Mapster, เรียก repository, จบด้วย `_unitOfWork.Commit()`

```csharp
namespace cgc_marketplace.Services
{
    public interface IProductService
    {
        Task<ProductViewModel> Create(ProductDto input);
    }

    public class ProductService : ServiceBase<IProductService, ApplicationDbContext>, IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IUnitOfWork<ApplicationDbContext> unitOfWork, IServiceProvider serviceProvider, ILogger<IProductService> logger)
            : base(unitOfWork, serviceProvider, logger)
        {
            _productRepository = unitOfWork.GetRepository<IProductRepository>();
        }

        public async Task<ProductViewModel> Create(ProductDto input)
        {
            if (input.ShippingLocation == default(ShippingLocation))
                throw GeneralException.PleaseSelectAshippingLocation();

            var data = input.Adapt<Product>();
            _productRepository.Add(data);
            _unitOfWork.Commit();

            return data.Adapt<ProductViewModel>();
        }
    }
}
```

### Repository (`Repository/I<X>Repository.cs`)
- **Interface + implementation อยู่ไฟล์เดียวกัน** เช่นกัน, namespace `<Project>.Repository`
- Interface สืบทอด `IRepositoryBase<Entity, ApplicationDbContext>`
- Implementation สืบทอด `RepositoryBase<Entity, ApplicationDbContext>`, constructor รับ `ApplicationDbContext dbContext : base(dbContext)`
- ใช้ helper ที่ base ให้: `GetAll()`, `FindBy(predicate)`, `Get(id)`/`GetAsync(id)`, `Add()`, `Update()`, `Delete()`, และ `Context` (DbContext ตรงๆ) เวลาต้อง query cross-entity
- Method เฉพาะของ query ที่ซับซ้อน (filter, search, join) ให้เขียนไว้ที่ repository นี้ **ไม่ใช่ที่ service** — service เรียกใช้ผ่าน interface เท่านั้น

```csharp
namespace cgc_marketplace.Repository
{
    public interface IProductRepository : IRepositoryBase<Product, ApplicationDbContext>
    {
        IQueryable<Product> GetProductByMemberCode(string memberCode);
    }

    public class ProductRepository : RepositoryBase<Product, ApplicationDbContext>, IProductRepository
    {
        public ProductRepository(ApplicationDbContext dbContext) : base(dbContext) { }

        public IQueryable<Product> GetProductByMemberCode(string memberCode)
            => FindBy(p => p.MemberCode == memberCode);
    }
}
```

### UnitOfWork
- **ทุก repository ใหม่ต้องลงทะเบียนใน `Data/UnitOfWork/UnitOfWork.cs`** ที่ dictionary `Register` (interface -> implementation) ไม่งั้น `unitOfWork.GetRepository<IXxxRepository>()` จะ resolve ไม่ได้

```csharp
protected override Dictionary<Type, Type> Register => new()
{
    { typeof(IProductRepository), typeof(ProductRepository) },
    // ...เพิ่ม repository ใหม่ที่นี่
};
```

### Entity (`Data/Entity/<X>.cs`)
- Implement `IEntityMaster<Guid>` (จาก BossupStandard) → บังคับ audit fields + soft delete มาตรฐานทุก entity:
  - `Guid Id` (`[Key][DatabaseGenerated(DatabaseGeneratedOption.Identity)]`)
  - `string? CreateBy`, `DateTime CreateDate`, `string? UpdateBy`, `DateTime? UpdateDate`
  - `string? DeleteBy`, `bool IsDelete`, `DateTime? DeletedAt`
- Navigation property เป็น `virtual` (lazy loading proxies เปิดใช้งานใน `EntityFramworkConfiguration.cs`)
- Query ที่ไม่ต้องการ record ที่ถูกลบ **ต้อง filter `!x.IsDelete` เอง** (ไม่มี global query filter อัตโนมัติในโปรเจกต์นี้)

### Dto vs ViewModel — แยกกันเสมอ
- **`Dto/`** = input จาก client (request body/query) → ใส่ `[Required]` validation ตามต้องการ
- **`ViewModel/`** = output กลับไปหา client (response) → ไม่มี validation attribute, ออกแบบให้ตรงกับสิ่งที่ frontend ต้องใช้ (เช่น flatten field จาก entity หลายตัวเข้าด้วยกัน)
- ห้ามส่ง Entity ตรงๆ ออกจาก controller — ต้อง `.Adapt<XxxViewModel>()` ก่อนเสมอ
- โมดูลใหญ่ (เช่น Product) แยก ViewModel เป็นโฟลเดอร์ย่อย `ViewModel/ProductViewModel/`

### Mapping
- ใช้ **Mapster** (`.Adapt<T>()`) เป็นหลักในโค้ด business logic เพราะสั้นและ inline ได้ (`input.Adapt<Product>()`, `data.Adapt<ProductViewModel>()`)
- **AutoMapper** (`Mapper/MapperProfile.cs`) มีไว้เป็น config กลางสำหรับ mapping ที่ซับซ้อน/reuse บ่อย — ถ้า mapping ตรงไปตรงมาให้ใช้ Mapster พอ

### Exception handling
- Error ทั้งหมดโยนผ่าน static factory ใน `Exceptions/GeneralException.cs` (สืบทอด `HttpStatusCodeException`) — แต่ละ error มี **code (`ERR-00XX`), ข้อความ EN, ข้อความ TH** พร้อมกัน แล้วมี middleware กลาง (`app.UseExceptionMiddleware()`) แปลงเป็น response
- **เพิ่ม error case ใหม่ = เพิ่ม static method ใหม่ใน GeneralException** เดินตาม pattern เดิม ไม่ throw `Exception` เปล่าๆ หรือ string ตรงๆ

```csharp
public static GeneralException OrderCannotBeCancelled()
    => new(HttpStatusCode.BadRequest, "ERR-00014", "Order Cannot Be Cancelled", "ไม่สามารถยกเลิกคำสั่งซื้อได้");
```

### Pagination / DataTable
- List endpoint ที่ต้อง paginate ใช้ `MatTableDataSource<T>.CreateAsync(query, criteria)` โดย `criteria` เป็น `PageCriteria` (หรือ subclass เช่น `GetProductSearch : PageCriteria` ที่เพิ่ม filter field เอง)
- Controller รับ `[FromQuery] PageCriteria criteria` แล้วส่งต่อ service ตรงๆ

### Bootstrapping (`Configurations/`, `Program.cs`)
- ทุก DI registration รวมไว้เป็น extension method แยกไฟล์ตามหมวด: `ServiceConfiguration.AddServiceConfiguration()`, `EntityFramworkConfiguration.AddEntityFramworkConfiguration()`
- **Service ใหม่ทุกตัวต้อง `services.AddScoped<IXxxService, XxxService>();` ใน `ServiceConfiguration.cs`** (repository ลงทะเบียนที่ UnitOfWork แทน ไม่ใช่ DI ตรงๆ)
- `Program.cs` เรียก extension methods เหล่านี้แบบ chain สั้นๆ ไม่ยัด logic ลงตรงนั้น

### Enum
- แยก enum ตาม module ใน `Enums/<Module>Enum.cs` เช่น `ProductEnum.cs` มี `Grade`, `ShippingLocation`, `Menu`, `ProductStatus` ฯลฯ

### Entity relationship ที่ซับซ้อน (FK ชี้ไปตาราง/property เดียวกันซ้ำ)
เมื่อ entity มี FK สองอันชี้ไป entity เดียวกัน (เช่น `Chat.SenderId` และ `Chat.ReceiverId` ชี้ไป `User` ทั้งคู่) EF Core เดาความสัมพันธ์เองไม่ได้ → ต้องเขียน `IEntityTypeConfiguration<T>` แยกไฟล์ในโฟลเดอร์ `Data/<Module>Configuration/` แล้ว apply ใน `ApplicationDbContext.OnModelCreating`:

```csharp
public class ChatConfiguration : IEntityTypeConfiguration<Chat>
{
    public void Configure(EntityTypeBuilder<Chat> builder)
    {
        builder.HasOne(chat => chat.Sender).WithMany(user => user.Senders).HasForeignKey(chat => chat.SenderId);
        builder.HasOne(chat => chat.Receiver).WithMany(user => user.Receivers).HasForeignKey(chat => chat.ReceiverId);
    }
}
// ApplicationDbContext.OnModelCreating:
builder.ApplyConfiguration(new ChatConfiguration());
```

### External API calls — โปรเจกต์นี้เป็น RESTful API เดี่ยว ไม่ใช่ microservices
โปรเจกต์นี้เป็น **RESTful Web API ตัวเดียว (monolith)** ไม่ได้แตกเป็นหลาย internal microservice แบบโปรเจกต์ต้นแบบ (ที่มี PointService/Wallet แยกระบบ) — จึง**ไม่ใช้** `IServiceHttp` + internal service registry (`ApplicationSettingBase.ServiceUrl.GetService(...)`) ตามต้นแบบ

เวลาต้องเรียก**บริการภายนอกจริง** (third-party API เช่น Gemini, Google Slides API, Hugging Face TTS) ให้ห่อด้วย **Provider pattern ของโปรเจกต์เอง** (ดู `src/providers/*/index.ts`-เทียบเท่าฝั่ง .NET คือ `Providers.<Name>` project) แทน — แต่ละ Provider มี Interface + Real implementation อย่างน้อยหนึ่งตัว (สลับได้ด้วย env var เช่น `VOICE_QUESTION_PROVIDER=gemini` ถ้ามีมากกว่าหนึ่งตัวเลือกจริง) **ไม่มี Mock tier แล้ว** ทุก Provider ต้องเป็น Real เสมอ ค่า env var ที่ตั้งไม่ถูกต้องหรือไม่ได้ตั้งต้อง throw ตอน startup แทนที่จะ fallback แบบเงียบๆ, inject `HttpClient` ผ่าน `IHttpClientFactory` ตรงๆ ในตัว Real provider นั้น **ไม่ผูก HttpClient ตรงๆ ในโค้ด Service ชั้นธุรกิจ**:

```csharp
public interface IVoiceQuestionProvider
{
    Task<VoiceAnswer> AskAsync(string transcript, CancellationToken ct);
}

public sealed class GeminiVoiceQuestionProvider(IHttpClientFactory httpClientFactory) : IVoiceQuestionProvider
{
    public async Task<VoiceAnswer> AskAsync(string transcript, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(nameof(GeminiVoiceQuestionProvider));
        var response = await client.PostAsJsonAsync("...", new { transcript }, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw GeneralException.UpstreamError("เรียกบริการ AI ไม่สำเร็จ");
        }
        return await response.Content.ReadFromJsonAsync<VoiceAnswer>(cancellationToken: ct) ?? throw GeneralException.UpstreamError("ไม่พบคำตอบจากบริการ AI");
    }
}
```
Service (`ISessionQuestionService`) เรียก Provider ผ่าน interface ที่ resolve จาก `ServiceProvider.GetRequiredService<IVoiceQuestionProvider>()` เหมือน service อื่นๆ — error จากภายนอกถูกครอบด้วย `GeneralException.UpstreamError(...)` เสมอ ไม่ปล่อย exception ดิบออกไปถึง client

### File upload / Object storage
- รับไฟล์ผ่าน `[FromForm]` + Dto ที่มี `IFormFile`/`List<IFormFile>`
- โปรเจกต์นี้**ไม่มี** `BossupStandard.IBlobService` — ให้ต่อกับ **object storage ภายนอก** ผ่าน library/SDK ของผู้ให้บริการที่เลือกจริง (เช่น Supabase Storage client, `Azure.Storage.Blobs`, AWS S3 SDK) โดยห่อด้วย Provider interface ของโปรเจกต์เอง (`IObjectStorageProvider` + Real implementation เหมือน Provider อื่น ไม่มี Mock tier) ไม่เรียก SDK ภายนอกตรงๆ ในโค้ด Service
- Real provider เก็บแค่ URL ที่ได้กลับมาไว้ใน entity — **ไม่เก็บไฟล์ในเครื่อง/DB**
- validate ก่อนอัปโหลดเสมอ: เช็ค required, เช็คชนิดไฟล์, เช็ค limit จำนวนไฟล์ — โยน `GeneralException` ตาม pattern เดิมถ้าไม่ผ่าน (ห้ามยิงอัปโหลดก่อนแล้วค่อยเช็คทีหลัง)

```csharp
public interface IObjectStorageProvider
{
    Task<string> UploadAsync(IFormFile file, string bucket, string path, CancellationToken ct);
}
```
```csharp
if (!file.IsImage()) throw GeneralException.ValidationError("ไฟล์ต้องเป็นรูปภาพ");
var url = await _objectStorageProvider.UploadAsync(file, "lessons", "cover", ct);
data.CoverImageUrl = url;
```

### Auth & claims
- ดึงข้อมูลผู้ใช้จาก JWT claim ผ่าน `HttpContextHelper` (static helper, ต้อง `app.UseStaticHttpContext()` ใน `Program.cs` ก่อนถึงจะใช้ได้):

```csharp
var userId = HttpContextHelper.GetUserId();          // อ่าน claim "sub" แล้ว parse เป็น Guid
var value = HttpContextHelper.GetClaimValue("key");   // อ่าน claim อื่นๆ ตาม key
```
- ใช้แทนการ inject `IHttpContextAccessor` ตรงๆ ใน service/repository เพราะ static helper เรียกได้จากทุกที่โดยไม่ต้องผ่าน constructor
- **จุดที่ต้องระวัง**: ตอนนี้ controller ในโปรเจกต์ต้นแบบยังไม่มี `[Authorize]` ที่ endpoint ไหนเลย (มีแค่ `app.UseAuthorization()` เฉยๆ) — เวลาสร้าง endpoint ใหม่ที่ควรจำกัดสิทธิ์ ต้องใส่ `[Authorize]` เองอย่าคิดว่ามี auth คุ้มครองอยู่แล้วโดยอัตโนมัติ

### Logging
- `ServiceBase` มี `_logger` (`ILogger<IXxxService>`) ให้ใช้ได้ทันทีในทุก service โดยไม่ต้อง inject เอง
- ใช้ `_logger.LogInformation(...)` ก่อนยิง request ออกไปหา external Provider (Gemini/Google Slides/TTS ฯลฯ) และ `_logger.LogError(...)` เมื่อ response ล้มเหลว/exception — โดยเฉพาะจุดที่มี side-effect ข้ามระบบ เพราะเป็นจุดที่ debug ยากที่สุดถ้าไม่มี log
- ไม่ต้อง log ทุก method — เน้น log ที่ boundary ของระบบ (เรียก external Provider, exception ที่ไม่คาดคิด)

## 3. Checklist เวลาสร้างโมดูลใหม่ (เช่น "Review")

1. `Data/Entity/Review.cs` — implement `IEntityMaster<Guid>`, ใส่ audit + soft-delete fields, navigation properties เป็น `virtual`
2. เพิ่ม `public DbSet<Review> Review { get; set; }` ใน `ApplicationDbContext.cs`
3. `Dto/ReviewDto.cs` — input model + `[Required]` ตามต้องการ
4. `ViewModel/ReviewViewModel.cs` (หรือโฟลเดอร์ `ViewModel/ReviewViewModel/` ถ้าโมดูลใหญ่)
5. `Enums/ReviewEnum.cs` — ถ้ามี enum เฉพาะโมดูล
6. `Repository/IReviewRepository.cs` — **ต้องมีทั้ง interface** (`IRepositoryBase<Review, ApplicationDbContext>`) **และ implementation** `ReviewRepository` ในไฟล์เดียวกัน — ห้ามสร้างแค่ class เปล่าโดยไม่มี interface คู่กัน (resolve ผ่าน `unitOfWork.GetRepository<T>()` ได้เฉพาะ interface)
7. ลงทะเบียนใน `Data/UnitOfWork/UnitOfWork.cs` → `{ typeof(IReviewRepository), typeof(ReviewRepository) }`
8. `Services/IReviewService.cs` — **ต้องมีทั้ง interface** และ `ReviewService : ServiceBase<IReviewService, ApplicationDbContext>` implementation ในไฟล์เดียวกัน — เหตุผลเดียวกับข้อ 6 (resolve ผ่าน `IServiceProvider.GetRequiredService<T>()` ได้เฉพาะ interface)
9. ลงทะเบียนใน `Configurations/ServiceConfiguration.cs` → `services.AddScoped<IReviewService, ReviewService>();`
10. เพิ่ม error case ที่ต้องใช้ใน `Exceptions/GeneralException.cs` (ต่อ error code ถัดจากตัวล่าสุด)
11. `Controllers/ReviewController.cs` — resolve service ผ่าน `IServiceProvider`, endpoint ทำแค่ route + forward
12. `dotnet ef migrations add "AddReview" --project <ProjectName> --context ApplicationDbContext -- --environment Development` แล้ว `dotnet ef database update ...`
13. ถ้ามี list endpoint ที่ต้อง paginate → ใช้ `MatTableDataSource<T>` + `PageCriteria`
14. ถ้าโมดูลต้องเรียกบริการภายนอก (Gemini/Google Slides/TTS ฯลฯ) → เรียกผ่าน `I<X>Provider` (Provider pattern, ดูหัวข้อ "External API calls") ไม่ยิง `HttpClient` ตรงในตัว Service
15. ถ้าโมดูลต้องอัปโหลดไฟล์ → เรียกผ่าน `IObjectStorageProvider` (ดูหัวข้อ "File upload / Object storage") ไม่เรียก object storage SDK ตรงในตัว Service

## 4. สิ่งที่ต้องเลี่ยง (จากการอ่านโค้ดจริง)

- อย่าใส่ business logic ใน Controller หรือใน Repository (repository ทำหน้าที่ query เท่านั้น, logic/validation อยู่ที่ Service)
- อย่า throw `Exception`/`ArgumentException` เปล่าๆ — ใช้ `GeneralException.XXX()` เพื่อให้ error code/ข้อความ EN-TH สอดคล้องทั้งระบบ
- อย่า return Entity ตรงๆ จาก endpoint — ต้อง map เป็น ViewModel เสมอ
- อย่าลืม filter `!x.IsDelete` เวลา query list (ไม่มี global filter อัตโนมัติ)
- อย่าลืมลงทะเบียนทั้งสองที่เวลาเพิ่ม repository/service ใหม่ (UnitOfWork.Register สำหรับ repository, ServiceConfiguration สำหรับ service) — ลืมอันใดอันหนึ่งจะ resolve ไม่ได้ตอน runtime
- อย่า inject scoped service (repository/service ทั่วไป) ตรงๆ เข้า `BackgroundService` — ต้องผ่าน `IServiceScopeFactory.CreateScope()` เท่านั้น
- Controller ไม่จำเป็นต้อง map 1:1 กับ service ชื่อเดียวกันเสมอไป (เช่น `PointController` เรียก `IUserService.GetUserPointsAsync`) — ถ้า logic เกี่ยวข้องกับ entity ที่มี service อยู่แล้ว ใส่ method เพิ่มใน service เดิมได้ ไม่ต้องสร้าง service ใหม่ทุกครั้ง
- เวลาดึง repository จาก `unitOfWork.GetRepository<T>()` ให้ใช้ **interface เสมอ** (`IProductRepository` ไม่ใช่ `ProductRepository`) แม้ในโค้ดต้นแบบบางจุดจะเผลอใช้ concrete class ปนก็ตาม — ใช้ interface จะ resolve ตรงกับที่ลงทะเบียนใน UnitOfWork.Register และ mock/test ได้ง่ายกว่า
- **อย่าขาด `I<X>Service` หรือ `I<X>Repository`** เวลาสร้างโมดูลใหม่ แม้โมดูลจะดูง่าย/มี method เดียว — ทุก Service และ Repository ต้องมี interface คู่กันเสมอ (Controller resolve service ผ่าน `IServiceProvider.GetRequiredService<T>()` และ Service ดึง repository ผ่าน `unitOfWork.GetRepository<T>()` ได้เฉพาะ interface เท่านั้น การสร้างแค่ concrete class เปล่าจะ resolve ไม่ได้และ mock/test ไม่ได้)

### ข้อควรระวัง (ไม่ใช่ pattern ที่ควร copy แต่ต้องรู้เวลาอ่าน/แก้โค้ดเดิม)
- **ชื่อไฟล์ไม่ได้การันตีว่า interface/class ข้างในตรงกับชื่อไฟล์เสมอไป** — เช่น `Services/IPaymentService.cs` จริงๆ แล้วเก็บ `IOrderTransactionService`/`OrderTransactionService` ไว้ข้างใน ไม่มี `IPaymentService` อยู่เลย เวลาหา implementation ของ service ตัวไหนให้ `grep "class XxxService"` แทนการเดาจากชื่อไฟล์
- โค้ดต้นแบบมี dead code ที่ comment ทิ้งไว้ (เช่นใน `UserService`) และ credential/URL ฝังตรงในโค้ด (`AuthenticationHeaderValue("Basic", "...")` ใน `OrderTransactionService`) — เป็นสิ่งที่ควรหลีกเลี่ยงในโค้ดใหม่ (ย้ายไป config/secret แทน) ไม่ใช่ convention ที่ต้องทำตาม

## 5. Correctness Rules (แปลงมาจากบั๊กที่เจอจริงตอนรีวิว)

1. **Validate ให้ครบก่อนค่อยยิง side-effect ข้ามระบบ** — ถ้า method ต้องเรียก external service ที่มีผลจริง (ตัดพอยต์, ส่ง notification ฯลฯ) ให้ validate input ทั้งหมดให้จบก่อนบรรทัดแรกที่มี side-effect เสมอ (เคยเจอ: `OrderService.Create` เรียก `BurnPoint` ก่อนเช็ค `ShippingLocation` ทำให้พอยต์ถูกตัดไปแล้วแม้ order จะไม่ถูกสร้างจริง)
2. **`_unitOfWork.Commit()` ต้องถูกเรียกแน่นอนในทุก branch ที่มีการแก้ข้อมูล** ก่อน return — อย่าปล่อยให้ commit "ได้บังเอิญ" จาก service อื่นที่ใช้ DbContext เดียวกัน เพราะถ้า branch นั้นไม่ได้เรียก service อื่นเลย จะไม่มีอะไร save ให้ (เคยเจอ: `CancelOrder` ไม่ commit ตรงๆ พึ่ง commit ของ `Refunt()` ซึ่งไม่ถูกเรียกถ้า order ไม่มี transaction)
3. **Error code ต้องไม่ซ้ำกันข้าม static method ใน `GeneralException`** — ก่อนเพิ่ม error case ใหม่ให้เช็คโค้ดล่าสุดที่ใช้แล้ว +1 เสมอ (เคยเจอ: `UploadLimit()` กับ `YouCanOnlyUploadUpTo5Files()` ใช้ `ERR-00011` ซ้ำกัน)
4. **เช็ค entity existence (`is null` → `GeneralException.NotFound()`) ให้ครบทุก method ที่รับ id** ไม่ใช่แค่ `Delete`/`Get` — โดยเฉพาะ `Update` ที่มักถูกลืมเพราะดูเหมือนไม่จำเป็น (เคยเจอ: `ProductService.Update` ไม่เช็ค null ก่อน `Adapt`)
5. **ข้อความ error EN/TH ต้องตรงกับ error code นั้นจริงๆ** อย่า copy-paste จาก method อื่นแล้วลืมแก้ข้อความ (เคยเจอ: `PleaseSelectAshippingLocation()` ข้อความ EN ดันเขียนว่า "Please select a payment method")
6. **Controller ที่แค่ forward ผลจาก service ไม่ต้อง `.Adapt<>()` ซ้ำ** ถ้า service return ViewModel ที่ตรงกับ response อยู่แล้ว — mapping ซ้ำซ้อนไม่ error แต่ทำให้อ่านโค้ดสับสนว่ามี transform อะไรเพิ่มหรือเปล่า

## 6. อัปเกรดเป็น .NET 10 — มีอะไรเปลี่ยน

โค้ดต้นแบบ (`cgc-marketplace.csproj`) ตอนนี้อยู่ที่ **`net8.0`** (`Nullable`/`ImplicitUsings` เปิดอยู่แล้ว) ตอนอัปเป็น `net10.0` ตัว **layered architecture ในหัวข้อ 1-5 ข้างบนไม่เปลี่ยน** เพราะเป็น pattern ระดับ code organization ไม่ใช่ syntax แต่มีของใหม่ที่ควรพิจารณาใช้/ต้องเช็คก่อนอัป ดังนี้

### ต้องเช็คก่อนอัป (ความเสี่ยงจริง)
- **`BossupStandard*` (8.0.x) และ `Pomelo.EntityFrameworkCore.MySql` (8.0.x)** เป็น dependency ที่ต้องรอผู้ดูแล package ปล่อยเวอร์ชันที่ target `net10.0`/EF Core 10 ก่อน — ห้ามอัป `TargetFramework` เฉยๆ โดยไม่เช็ค compatibility ของสอง package นี้ก่อน เพราะ base class ทั้งหมด (`ServiceBase`, `RepositoryBase`, `UnitOfWorkBase`, `CoreDbContextBase`) มาจาก BossupStandard
- อัป `Microsoft.EntityFrameworkCore*` ทั้งชุด (Core/Design/Proxies/Tools) ให้เป็นเวอร์ชันตรงกับ EF Core 10 พร้อมกันทีเดียว (เวอร์ชัน EF Core อิงตามเวอร์ชัน .NET เสมอ)

### EF Core 10 — ใช้แก้ pain point ที่ระบุไว้ในหัวข้อ 2/4 ได้จริง
- **Multiple named query filter**: EF Core 10 ให้ประกาศ `HasQueryFilter` หลายอันต่อ entity แบบมีชื่อ/ปิดเปิดแยกกันได้ (เดิม EF Core อนุญาตแค่ filter เดียวต่อ entity) → **ทำ global soft-delete filter (`!x.IsDelete`) ที่ `OnModelCreating` ได้จริงแล้ว** แทนที่จะต้อง `.Where(x => !x.IsDelete)` มือทุก query อย่างที่ระบุไว้ในหัวข้อ 2 (Entity) และหัวข้อ 4 (สิ่งที่ต้องเลี่ยง) — ถ้าอัปเป็น net10 แล้ว **ให้ทำ global filter นี้เป็นมาตรฐานใหม่** แล้วค่อย `.IgnoreQueryFilters()` เฉพาะจุดที่ตั้งใจอยากเห็น record ที่ถูกลบ (เช่นหน้า admin)
- **`LeftJoin`/`RightJoin` แบบ native LINQ**: ใช้แทน pattern เดิมที่ repository เขียน `GroupJoin` + `SelectMany` + `DefaultIfEmpty` เอง (ดูตัวอย่าง `ProductRepository.GetBestSellingProducts`) — โค้ด join ใหม่จะสั้นและอ่านง่ายขึ้น
- ตรวจ syntax `HasQueryFilter`/join ใหม่กับเอกสาร EF Core 10 อีกครั้งตอน implement จริง เพราะ API อาจปรับรายละเอียดหลัง preview

### C# 14 (มาพร้อม `net10.0` โดยอัตโนมัติถ้าไม่ pin `<LangVersion>`)
มีของใหม่ที่ "ใช้ได้" ไม่ใช่ "ต้องใช้" — ใช้เฉพาะจุดที่ทำให้โค้ดอ่านง่ายขึ้นจริง อย่าไล่รีไรท์โค้ดเดิมทั้งหมด:
- **`field` keyword**: เขียน custom property accessor ที่ยังอิง auto-backing-field ได้โดยไม่ต้องประกาศ private field เอง เหมาะกับ ViewModel/Entity ที่มี validation เล็กๆ ใน setter
- **Null-conditional assignment**: `obj?.Prop = value;` ย่อจาก `if (obj != null) obj.Prop = value;` ได้ตรงๆ
- **Extension members** (`extension(Type x) { ... }` block): รวม extension method/property ของ type เดียวกันไว้ในบล็อกเดียว อาจเอามาใช้แทน helper แบบ `HttpContextHelper`/`AsQueryable<T>` แบบ static class เดิมได้ถ้าต้องการ
- **Primary constructor** (มีมาตั้งแต่ C# 12 ใช้ได้อยู่แล้วใน net8 ด้วย ไม่ใช่ของใหม่ใน 10 แต่ยังไม่ถูกใช้ในโค้ดต้นแบบเลย): ทางเลือกลดโค้ด constructor ของ Controller/Service ได้ เช่น
  ```csharp
  public class ProductController(IServiceProvider serviceProvider) : ControllerBase
  {
      private readonly IProductService _service = serviceProvider.GetRequiredService<IProductService>();
  }
  ```
  ถ้าจะใช้ควรใช้ให้ **สม่ำเสมอทั้งโปรเจกต์** ไม่ใช่ปนกับ constructor แบบเดิม เพราะจะทำให้สไตล์ไม่คงเส้นคงวา (ขัดกับเป้าหมายหลักของ skill นี้)

### ทางเลือก (ไม่บังคับ)
- **OpenAPI**: ย้ายจาก `Swashbuckle.AspNetCore` + `Microsoft.OpenApi` (1.6.x) ไปใช้ `Microsoft.AspNetCore.OpenApi` ที่ built-in ใน SDK (`builder.Services.AddOpenApi()` + `app.MapOpenApi()`) ได้ — Swashbuckle เดิมยังรันบน net10 ได้ปกติ ไม่จำเป็นต้องย้ายถ้าไม่มีปัญหา
- ไม่แนะนำเปลี่ยนจาก Controller-based API ไปเป็น Minimal API — โปรเจกต์นี้ใช้ Controllers ทั้งระบบ การผสมสองแบบจะทำให้โครงสร้างในหัวข้อ 1 เพี้ยน ถ้าไม่มีเหตุผลจำเป็นให้คงไว้แบบเดิม
