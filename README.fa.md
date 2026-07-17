<div dir="rtl" align="right">

# دفترچه تلفن

## نمای کلی پروژه

`PhoneBook` یک <span dir="ltr">REST API</span> برای مدیریت مخاطبین است که به‌عنوان تمرین مصاحبه فنی ساخته شده است. این پروژه معماری <span dir="ltr">Clean Architecture</span>، طراحی دامنه‌محور، الگوی `CQRS`، رفتارهای pipeline در `MediatR`، اعتبارسنجی با `FluentValidation`، نگاشت آبجکت‌ها با `Mapster`، یک repository درون‌حافظه‌ای و thread-safe، پاسخ‌های یکپارچه API، و تست‌های خودکار در لایه‌های اصلی معماری را نشان می‌دهد.

هدف اصلی پروژه نمایش یک طراحی backend قابل نگهداری است؛ جایی که قوانین کسب‌وکار در لایه `Domain`، use caseها در لایه `Application`، جزئیات فنی در لایه `Infrastructure`، و نگرانی‌های HTTP در پروژه `PhoneBook.Api` قرار می‌گیرند.

## قابلیت‌ها

- ایجاد مخاطب.
- به‌روزرسانی مخاطب موجود.
- حذف مخاطب.
- دریافت مخاطب با شناسه.
- دریافت لیست مخاطبین با صفحه‌بندی deterministic.
- فیلتر کردن مخاطبین بر اساس `Tag` همراه با صفحه‌بندی deterministic.
- اعتبارسنجی ورودی‌ها پیش از اجرای handlerها.
- نرمال‌سازی قالب‌های پشتیبانی‌شده شماره موبایل ایران به قالب canonical یعنی `+989xxxxxxxxx`.
- جلوگیری از تکرار شماره موبایل canonical.
- ذخیره زمان ایجاد و به‌روزرسانی بر اساس UTC.
- بازگرداندن قراردادهای پاسخ موفق و خطای یکپارچه.
- ارائه `Swagger/OpenAPI` در محیط `Development`.
- اجرا با .NET SDK محلی یا `Dockerfile` موجود.
- پوشش رفتارها با تست‌های domain، application، infrastructure، concurrency و integration HTTP.

## پشته فناوری

- `.NET 9` و `ASP.NET Core 9`
- `ASP.NET Core MVC Controllers`
- <span dir="ltr">Clean Architecture</span>
- <span dir="ltr">Domain-Driven Design</span>
- `CQRS`
- `MediatR`
- `FluentValidation`
- `Mapster`
- `Swashbuckle.AspNetCore`
- `xUnit`
- `FluentAssertions`
- `NSubstitute`
- `Microsoft.AspNetCore.Mvc.Testing`
- `coverlet.collector`
- Repository درون‌حافظه‌ای thread-safe
- `Dockerfile` برای اجرای containerized

## معماری

راهکار از معماری <span dir="ltr">Clean Architecture</span> پیروی می‌کند. وابستگی‌ها به سمت داخل هستند: پروژه `PhoneBook.Api` به `PhoneBook.Application` و `PhoneBook.Infrastructure` وابسته است، پروژه `PhoneBook.Infrastructure` به abstractionهای `PhoneBook.Application` و به `PhoneBook.Domain` وابسته است، پروژه `PhoneBook.Application` به `PhoneBook.Domain` وابسته است، و پروژه `PhoneBook.Domain` به هیچ پروژه دیگری در solution وابسته نیست.

- لایه `API`: مسئول controllerهای HTTP، قراردادهای request/response، `Swagger`، تبدیل exceptionها به HTTP، و پیکربندی composition root است.
- لایه `Application`: مسئول use caseها، commandها، queryها، handlerها، validatorها، پیکربندی mapping، exceptionهای application، pipeline behaviorها و abstractionهای repository است.
- لایه `Domain`: مسئول aggregate مخاطب، value objectهای منتخب، abstractionهای entity و invariantهای کسب‌وکار است.
- لایه `Infrastructure`: مسئول پیاده‌سازی درون‌حافظه‌ای abstractionهای persistence در لایه `Application` است.

<div dir="ltr" align="left">

```mermaid
flowchart LR
    Client[HTTP Client] --> Api[PhoneBook.Api]
    Api --> Application[PhoneBook.Application]
    Api --> Infrastructure[PhoneBook.Infrastructure]
    Infrastructure --> Application
    Application --> Domain[PhoneBook.Domain]
    Infrastructure --> Domain
```

</div>

<div dir="ltr" align="left">

```mermaid
flowchart BT
    Domain[Domain<br/>No solution dependencies]
    Application[Application<br/>Use cases and abstractions]
    Infrastructure[Infrastructure<br/>Persistence implementation]
    Api[API<br/>HTTP and composition root]

    Application --> Domain
    Infrastructure --> Application
    Infrastructure --> Domain
    Api --> Application
    Api --> Infrastructure
```

</div>

## ساختار راهکار

<div dir="ltr" align="left">

```text
PhoneBook.sln
src/
  PhoneBook.Domain/
    Contacts/
    Abstractions/
  PhoneBook.Application/
    Abstractions/Persistence/
    Behaviors/
    Common/
    Contacts/
  PhoneBook.Infrastructure/
    Persistence/
  PhoneBook.Api/
    Contracts/
    Controllers/
    ExceptionHandling/
    Mappings/
tests/
  PhoneBook.Domain.UnitTests/
  PhoneBook.Application.UnitTests/
  PhoneBook.Infrastructure.UnitTests/
  PhoneBook.Api.IntegrationTests/
```

</div>

- پروژه `PhoneBook.Domain`: برای مدل‌سازی رفتار مخاطب و محافظت از قوانین کسب‌وکار بدون وابستگی به frameworkها وجود دارد.
- پروژه `PhoneBook.Application`: برای هماهنگ‌سازی use caseها از طریق پیام‌های `CQRS`، handlerها، validatorها، mapping و قراردادهای persistence وجود دارد.
- پروژه `PhoneBook.Infrastructure`: برای ارائه پیاده‌سازی in-memory repository پشت abstraction لایه `Application` وجود دارد.
- پروژه `PhoneBook.Api`: برای ارائه برنامه از طریق endpointهای REST و پیکربندی runtime وجود دارد.
- پروژه `PhoneBook.Domain.UnitTests`: رفتار value objectها، رفتار aggregate، اعتبارسنجی نام‌های primitive و invariantهای domain را بررسی می‌کند.
- پروژه `PhoneBook.Application.UnitTests`: validatorها، pipeline behavior و handlerها را بررسی می‌کند.
- پروژه `PhoneBook.Infrastructure.UnitTests`: رفتار repository، صفحه‌بندی، یکتایی، snapshot isolation و concurrency را بررسی می‌کند.
- پروژه `PhoneBook.Api.IntegrationTests`: رفتار واقعی HTTP را با `WebApplicationFactory` بررسی می‌کند.

<div dir="ltr" align="left">

```mermaid
flowchart TD
    Root[PhoneBook]
    Root --> Src[src]
    Root --> Tests[tests]
    Src --> Domain[PhoneBook.Domain]
    Src --> Application[PhoneBook.Application]
    Src --> Infrastructure[PhoneBook.Infrastructure]
    Src --> Api[PhoneBook.Api]
    Tests --> DomainTests[Domain.UnitTests]
    Tests --> ApplicationTests[Application.UnitTests]
    Tests --> InfrastructureTests[Infrastructure.UnitTests]
    Tests --> ApiTests[Api.IntegrationTests]
```

</div>

## جریان درخواست

درخواست از طریق controller در پروژه `PhoneBook.Api` وارد می‌شود، با `Mapster` به command یا query لایه `Application` تبدیل می‌شود، از طریق `MediatR` ارسال می‌شود، در pipeline اعتبارسنجی می‌شود، و سپس توسط handler مربوطه پردازش می‌شود. handlerها از abstraction مربوط به repository استفاده می‌کنند، در صورت نیاز objectهای domain را ایجاد یا به‌روزرسانی می‌کنند، و DTO پاسخ را برمی‌گردانند. exceptionهای مورد انتظار توسط `GlobalExceptionHandler` به پاسخ‌های پایدار API تبدیل می‌شوند.

<div dir="ltr" align="left">

```text
Controller -> Mapster -> MediatR -> ValidationBehavior -> Handler -> Repository -> Domain -> Response
```

</div>

<div dir="ltr" align="left">

```mermaid
sequenceDiagram
    participant Client
    participant Controller
    participant Mapster
    participant MediatR
    participant Validation as ValidationBehavior
    participant Handler
    participant Repository
    participant Domain

    Client->>Controller: HTTP request
    Controller->>Mapster: Adapt request to command/query
    Controller->>MediatR: Send command/query
    MediatR->>Validation: Execute validators
    Validation->>Handler: Continue when valid
    Handler->>Domain: Create/update/validate aggregate
    Handler->>Repository: Read or persist contact
    Repository->>Domain: Rehydrate stored snapshots
    Handler->>Mapster: Adapt domain model to response DTO
    Controller->>Client: HTTP response
```

</div>

## CQRS

الگوی `CQRS` عملیات نوشتن را از عملیات خواندن جدا می‌کند. این انتخاب باعث می‌شود هر use case کوچک، صریح و قابل تست بماند.

- Commandها state را تغییر می‌دهند: `CreateContactCommand`، `UpdateContactCommand` و `DeleteContactCommand`.
- Queryها state را می‌خوانند: `GetContactByIdQuery`، `GetContactsQuery` و `GetContactsByTagQuery`.
- Handlerها جریان application برای یک command یا query را اجرا می‌کنند و به abstractionهایی مثل `IContactRepository` وابسته هستند.

## طراحی دامنه

کلاس `Contact` نقش aggregate root را دارد. این کلاس state مخاطب را در اختیار دارد و مسیرهای کنترل‌شده‌ای برای ایجاد، به‌روزرسانی و rehydrate کردن ارائه می‌کند.

این aggregate از value objectهای زیر استفاده می‌کند:

- `ContactId`
- `PhoneNumber`
- `Tag`

ویژگی‌های `FirstName` و `LastName` اکنون propertyهای primitive از نوع `string` هستند. اعتبارسنجی آن‌ها داخل aggregate `Contact` انجام می‌شود: مقدارها اجباری هستند، trim می‌شوند، مقدار خالی یا فقط whitespace رد می‌شود، و طول آن‌ها نباید بیشتر از ۱۰۰ کاراکتر باشد.

قوانین domain شامل اجباری بودن نام و tag، حداکثر طول متن ۱۰۰ کاراکتر، خالی نبودن شناسه مخاطب، معتبر بودن شماره موبایل ایران، UTC بودن timestampها، و زودتر نبودن زمان به‌روزرسانی از زمان ایجاد است. value object با نام `PhoneNumber` قالب‌های محلی و بین‌المللی پشتیبانی‌شده را نرمال می‌کند. value object با نام `Tag` به‌صورت case-insensitive مقایسه می‌شود.

<div dir="ltr" align="left">

```mermaid
classDiagram
    class Contact {
        +ContactId Id
        +string FirstName
        +string LastName
        +PhoneNumber PhoneNumber
        +Tag Tag
        +DateTime CreatedAtUtc
        +DateTime? UpdatedAtUtc
        +Create()
        +Update()
        +Rehydrate()
    }
    class ContactId
    class PhoneNumber
    class Tag
    Contact *-- ContactId
    Contact *-- PhoneNumber
    Contact *-- Tag
```

</div>

## الگوی Repository

رابط `IContactRepository` در لایه `Application` قرار دارد، چون use caseهای application مشخص می‌کنند به چه عملیات persistence نیاز دارند. لایه `Infrastructure` این abstraction را با `InMemoryContactRepository` پیاده‌سازی می‌کند.

repository به‌صورت singleton ثبت شده و dictionaryهای داخلی خود را با یک lock محافظت می‌کند. store مخاطبین و index شماره موبایل canonical در یک مرز synchronization بررسی و تغییر می‌کنند؛ بنابراین یکتایی شماره موبایل درون همین process به‌صورت atomic تضمین می‌شود.

repository snapshotهای immutable ذخیره می‌کند و هنگام خواندن، instanceهای جدید `Contact` را rehydrate می‌کند. این کار جلوی تغییر تصادفی state ذخیره‌شده بدون فراخوانی `UpdateAsync` را می‌گیرد.

## اعتبارسنجی

کتابخانه `FluentValidation`، commandها و queryهای لایه `Application` را پیش از اجرای handlerها اعتبارسنجی می‌کند. validatorها از assembly لایه `Application` کشف می‌شوند و توسط `ValidationBehavior<TRequest, TResponse>` که یک pipeline behavior در `MediatR` است اجرا می‌شوند.

اعتبارسنجی ورودی شامل فیلدهای اجباری، حداکثر طول، شناسه‌های غیرخالی، شماره موبایل معتبر و بازه‌های صفحه‌بندی است. invariantهای کسب‌وکار همچنان در `Domain` enforce می‌شوند و یکتایی atomic در repository enforce می‌شود، چون باید در همان lock بررسی و نوشته شود.

## نگاشت

کتابخانه `Mapster` برای نگاشت object-to-object بین قراردادهای API، پیام‌های `Application`، پاسخ‌های صفحه‌بندی‌شده، objectهای domain و DTOهای پاسخ استفاده می‌شود. قوانین mapping با کلاس‌های `IRegister` پیاده‌سازی شده‌اند:

- `PhoneBook.Application.Common.Mappings.ContactMappingConfig`
- `PhoneBook.Api.Mappings.ApiMappingConfig`

پیکربندی dependency injection در لایه `Application`، assemblyهای mapping را در `TypeAdapterConfig.GlobalSettings` اسکن می‌کند و کد از سبک مستقیم `Mapster` استفاده می‌کند:

<div dir="ltr" align="left">

```csharp
contact.Adapt<ContactResponse>();
```

</div>

## مدیریت خطا

پروژه `PhoneBook.Api` از `GlobalExceptionHandler` برای تبدیل failureهای مورد انتظار به پاسخ‌های HTTP یکپارچه استفاده می‌کند.

- `ValidationException` به `400 Bad Request` همراه با `ValidationApiResponse` تبدیل می‌شود.
- `NotFoundException` به `404 Not Found` تبدیل می‌شود.
- `ConflictException` به `409 Conflict` تبدیل می‌شود.
- `BusinessRuleException` به `422 Unprocessable Entity` تبدیل می‌شود.
- exceptionهای غیرمنتظره log می‌شوند و به `500 Internal Server Error` تبدیل می‌شوند.

پاسخ‌های موفق، به‌جز حذف موفق که `204 No Content` برمی‌گرداند، از `ApiResponse<T>` استفاده می‌کنند. پاسخ‌های خطا از `ApiResponse` یا `ValidationApiResponse` استفاده می‌کنند.

<div dir="ltr" align="left">

```mermaid
flowchart TD
    Request[HTTP Request] --> Controller
    Controller --> MediatR
    MediatR --> Validation{Valid?}
    Validation -- No --> ValidationException[ValidationException]
    Validation -- Yes --> Handler
    Handler --> Expected{Expected failure?}
    Expected -- Not found --> NotFound[NotFoundException]
    Expected -- Duplicate phone --> Conflict[ConflictException]
    Expected -- Other business rule --> Business[BusinessRuleException]
    Expected -- No --> Success[ApiResponse or 204]
    ValidationException --> ExceptionHandler[GlobalExceptionHandler]
    NotFound --> ExceptionHandler
    Conflict --> ExceptionHandler
    Business --> ExceptionHandler
    ExceptionHandler --> ErrorResponse[Stable error response]
```

</div>

## Dependency Injection

پروژه `PhoneBook.Api` نقش composition root را دارد. این پروژه controllerها، `Swagger`، سرویس‌های `Application`، سرویس‌های `Infrastructure` و exception handling را ثبت می‌کند.

ثبت `Application` شامل handlerهای `MediatR`، validatorهای `FluentValidation`، validation pipeline behavior و پیکربندی `Mapster` است. ثبت `Infrastructure`، رابط `IContactRepository` را به `InMemoryContactRepository` وصل می‌کند.

کد به‌جای پیاده‌سازی‌های concrete، abstractionهایی مثل `ISender` و `IContactRepository` را inject می‌کند. این کار handlerها را قابل تست نگه می‌دارد و مانع وابستگی `Application` به `Infrastructure` می‌شود.

## صفحه‌بندی

endpointهای لیست، `pageNumber` و `pageSize` را دریافت می‌کنند. اگر query valueها ارسال نشوند، mapping در API مقدارهای پیش‌فرض page `1` و size `20` را اعمال می‌کند. اعتبارسنجی الزام می‌کند page number حداقل `1` باشد و page size بین `1` و `100` قرار بگیرد.

نتایج repository ابتدا بر اساس `CreatedAtUtc` و سپس بر اساس `ContactId` مرتب می‌شوند؛ این کار صفحه‌بندی را deterministic نگه می‌دارد.

## تست‌ها

solution برای هر لایه تست‌های متمرکز دارد:

- تست‌های unit در `PhoneBook.Domain.UnitTests`: رفتار value objectها، اعتبارسنجی نام‌های primitive داخل `Contact`، ایجاد و به‌روزرسانی `Contact`، auditing، قوانین timestamp و normalization را بررسی می‌کنند.
- تست‌های unit در `PhoneBook.Application.UnitTests`: validatorها، رفتار validation در `MediatR`، handlerها، forwarding مربوط به cancellation token، mapping و exceptionهای application را بررسی می‌کنند.
- تست‌های unit در `PhoneBook.Infrastructure.UnitTests`: رفتار CRUD در repository، مدیریت شماره تکراری، snapshot isolation، صفحه‌بندی deterministic، فیلتر tag و concurrency را بررسی می‌کنند.
- تست‌های integration در `PhoneBook.Api.IntegrationTests`: endpointهای HTTP، envelopeهای پاسخ، خطاهای validation، exception handling و integration با host واقعی `ASP.NET Core` را بررسی می‌کنند.

## اجرای پروژه

پیش‌نیاز: .NET SDK نسخه `9.0.102` یا latest patch سازگار، مطابق `global.json`.

<div dir="ltr" align="left">

```bash
dotnet restore
dotnet build
dotnet run --project src/PhoneBook.Api
dotnet test
```

</div>

`Swagger` در مسیر `/swagger` و فقط در محیط `Development` در دسترس است.

فایل `Dockerfile` موجود نیز می‌تواند API را build و اجرا کند:

<div dir="ltr" align="left">

```bash
docker build -t phonebook-api .
docker run --rm -p 8080:8080 phonebook-api
```

</div>

## تصمیم‌های طراحی

- معماری <span dir="ltr">Clean Architecture</span>: قوانین کسب‌وکار را از جزئیات HTTP و persistence مستقل نگه می‌دارد.
- طراحی دامنه‌محور: رفتار مخاطب را با aggregate root و value objectها در جاهایی که رفتار دامنه‌ای مستقل دارند مدل می‌کند، در حالی که نام‌های primitive داخل خود aggregate اعتبارسنجی می‌شوند.
- الگوی `CQRS`: use caseهای خواندن و نوشتن را صریح و مستقل از هم قابل تست می‌کند.
- کتابخانه `MediatR`: dispatch کردن command/queryها را متمرکز می‌کند و pipeline behaviorهایی مثل validation را ممکن می‌سازد.
- کتابخانه `FluentValidation`: اعتبارسنجی ورودی را declarative و جدا از handlerها نگه می‌دارد.
- کتابخانه `Mapster`: mapping سبک و صریح با پیکربندی `IRegister` و مصرف `Adapt<T>()` فراهم می‌کند.
- پیاده‌سازی in-memory repository: نیازهای محدوده مصاحبه را بدون راه‌اندازی دیتابیس پوشش می‌دهد.
- قرار گرفتن repository abstraction در `Application`: اجازه می‌دهد use caseها نیاز persistence خود را تعریف کنند و `Infrastructure` فقط پیاده‌سازی را فراهم کند.
- استفاده از global exception handler: controllerها را روی orchestration HTTP متمرکز نگه می‌دارد و قرارداد خطا را مرکزی می‌کند.
- خواندن snapshot-based از repository: جلوی mutation تصادفی state ذخیره‌شده را می‌گیرد.
- یکتایی atomic شماره موبایل: از ثبت شماره موبایل canonical تکراری داخل process جلوگیری می‌کند.

## بهبودهای آینده

برای آماده‌سازی production می‌توان موارد زیر را اضافه کرد:

- persistence با `EF Core`.
- دیتابیس `PostgreSQL` همراه با migration و unique constraint در سطح دیتابیس.
- Authentication.
- Authorization policyها.
- `Docker Compose` یا پیکربندی deployment آماده برای orchestration.
- `Redis` یا cache توزیع‌شده دیگر.
- Structured logging.
- tracing و metrics با `OpenTelemetry`.
- Health checks.
- مثال‌ها و metadata کامل‌تر برای `Swagger`.
- Background jobs.
- Rate limiting.
- Cursor-based pagination برای datasetهای بزرگ‌تر.
- Audit trail پایدار.
- گزارش coverage در CI و quality gateها.

## Trade-offs

این پروژه عمداً persistence را درون حافظه نگه می‌دارد تا تمرکز روی معماری و رفتار use caseها بماند. داده‌ها process-local هستند و با restart برنامه از بین می‌روند. repository داخل یک process thread-safe است، اما distributed consistency فراهم نمی‌کند.

Authentication، authorization، storage پایدار، caching، observability و orchestration برای deployment عمداً محدود یا حذف شده‌اند، چون خارج از محدوده فعلی تمرین مصاحبه هستند. طراحی فعلی برای این concernها extension point واضح باقی می‌گذارد، بدون اینکه آن‌ها را به `Domain` یا `Application` couple کند.

</div>
