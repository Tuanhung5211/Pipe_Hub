# DECISION LOG — PIPE_HUB

> Nhật ký các quyết định thiết kế quan trọng.
> Ghi mỗi khi có quyết định ảnh hưởng nhiều chỗ hoặc cần giải thích khi bảo vệ.

---

## D1 — Setup solution và công nghệ nền

### 2026-10-11 — Chọn .NET 10 thay .NET 9

**Bối cảnh:** Đề đồ án ghi ".NET 9 trở lên" (C# 12/13). Máy đã có SDK 10.0.401, không có .NET 9.

**Quyết định:** Dùng .NET 10 cho cả 3 project.

**Lý do:**
- .NET 10 là LTS (hỗ trợ dài hạn), trong khi .NET 9 là STS (kết thúc hỗ trợ 11/2026)
- EF Core 10.0.12 tương thích hoàn toàn, không phải hạ version
- Đề cho phép ".NET 9 trở lên" nên .NET 10 hợp lệ
- Bản publish self-contained không cần cài .NET trên máy đích

### 2026-10-11 — Ép LangVersion=13.0

**Bối cảnh:** Đề ghi "C# 12/13" (không ghi "trở lên"). .NET 10 mặc định dùng C# 14.

**Quyết định:** Thêm `<LangVersion>13.0</LangVersion>` vào cả 3 file .csproj.

**Lý do:** Đảm bảo khớp đúng yêu cầu PDF, không bị hỏi khi bảo vệ.

### 2026-10-11 — Suppress warning NU1902/NU1903

**Bối cảnh:** Gói `GMap.NET.WinForms 2.1.7` kéo theo `System.Data.SqlClient 4.8.3` — gói cũ có lỗ hổng bảo mật mức moderate + high.

**Quyết định:** Thêm `<NoWarn>$(NoWarn);NU1902;NU1903</NoWarn>` vào Pipe_Hub.UI.csproj.

**Lý do:**
- Dự án KHÔNG dùng trực tiếp System.Data.SqlClient (đã dùng Microsoft.Data.SqlClient ở DAL)
- Đây là transitive dependency của GMap.NET
- Warning NU1701 (từ LiveCharts2) thì GIỮ — vì là do vendor chưa update

### 2026-10-11 — Kiến trúc 3 tầng

**Quyết định:** UI → BLL → DAL, một chiều. UI không tham chiếu DAL trực tiếp. DbContext do BLL đăng ký qua AddPipeHubServices, UI chỉ gọi một dòng.

**Ngoại lệ duy nhất:** Gói Microsoft.EntityFrameworkCore.Design trong UI với PrivateAssets="all" (chỉ dùng cho công cụ `dotnet ef`, không đi theo output/publish).

**Lý do:** Đúng yêu cầu PDF mục II.4: "Không chấp nhận toàn bộ logic viết trực tiếp trong code-behind của Form".

### 2026-10-11 — Bổ sung entity TopUpRequest (21 entity thay vì 20)

**Bối cảnh:** Kế hoạch ban đầu ghi 20 entity. Trong quá trình làm D1.12, cần thêm entity để lưu yêu cầu nạp tiền chờ duyệt.

**Quyết định:** Thêm `TopUpRequest` — tổng thành 21 entity.

**Lý do:** Luồng Admin duyệt topup cần bảng riêng (không dùng Transaction vì Transaction là giao dịch đã xảy ra, TopUpRequest là yêu cầu chờ duyệt).

### 2026-10-11 — LiveCharts2 chạy được trên .NET 10

**Bối cảnh:** NuGet `LiveChartsCore.SkiaSharpView.WinForms 2.0.5` có 3 warning NU1701 (OpenTK, OpenTK.GLControl, SkiaSharp.Views.WindowsForms restore bằng .NET Framework thay vì net10). Cần verify runtime.

**Quyết định:** Giữ LiveCharts2 — không đổi sang OxyPlot.

**Lý do:**
- Test form với LineSeries<double> 5 điểm dữ liệu vẽ thành công
- Trục X, Y, legend, tooltip hoạt động
- Warning NU1701 là cảnh báo build-time, không ảnh hưởng runtime
- Vendor chưa update build cho net10 nhưng binary vẫn tương thích

**Rủi ro còn lại:** Nếu sau này LiveCharts2 gặp lỗi cụ thể (ví dụ với PieChart, CartesianChart phức tạp), sẽ xem xét phương án thay thế.

---

## Ghi chú kỹ thuật cho D2 (chưa quyết định, chỉ note)

- Cần `DeleteBehavior.Restrict` cho Comment/Review để tránh lỗi multiple cascade paths trên SQL Server
- Cần `HasQueryFilter` cho Review/Comment (soft delete)
- Cần unique index cho: User.Username, User.Email, Role.Name, GameKey.Key, Wallet.UserId (1-1), SystemConfig.Key
- Cần unique composite index cho: Review (UserId, GameId), UserAchievement (UserId, AchievementId)
- Cần check constraint Rating 1-5

---

## D2 — Fluent API và Migration Init

### 2026-10-12 — Tách Fluent API ra thư mục Configurations/

**Quyết định:** Mỗi entity 1 file `XxxConfiguration.cs` trong `Pipe_Hub.DAL/Configurations/`. DbContext gọi `modelBuilder.ApplyConfigurationsFromAssembly(typeof(PipeHubDbContext).Assembly)`.

**Lý do:** Config tách rời giúp đọc/diff dễ hơn; tránh DbContext phình to 500+ dòng. 21 file config cho 21 entity.

### 2026-10-12 — Bảng nối N-N đặt tên cụ thể

**Quyết định:** Game-Genre → `GameGenres`; Game-Tag → `GameTags`; Game-Language → `GameLanguages`.

**Lý do:** Mặc định EF Core tự sinh tên như `GameGenre` — nhưng đặt rõ ràng để dễ đọc ERD và báo cáo.

### 2026-10-12 — GameKey → Order dùng DeleteBehavior.SetNull

**Quyết định:** `GameKey.OrderId` là nullable → khi xóa Order, GameKey vẫn giữ, chỉ set OrderId = null.

**Lý do:** Key đã bán không nên bị xóa khi đơn hàng bị hủy/xóa — giữ audit trail.

### 2026-10-12 — Wallet.RowVersion dùng IsRowVersion()

**Quyết định:** `builder.Property(w => w.RowVersion).IsRowVersion().IsConcurrencyToken();`

**Lý do:** PurchaseAsync (D4) cần chống double-spend. SQL Server dùng rowversion column để detect concurrent update. Không có config này, EF Core coi RowVersion là byte[] thường.

### 2026-10-12 — Soft delete Review/Comment + Check Constraint Rating

**Quyết định:**
- `HasQueryFilter(r => !r.IsDeleted)` cho Review + Comment
- `CheckConstraint("CK_Review_Rating", "[Rating] >= 1 AND [Rating] <= 5")`

**Lý do:** Review/Comment xóa mềm — không xuất hiện trong query mặc định nhưng vẫn giữ trong DB. Rating chỉ nhận 1-5, không cho insert bừa.

### 2026-10-12 — Thêm BCrypt vào DAL cho DbSeeder

**Bối cảnh:** DbSeeder cần hash password Admin mặc định. BLL đã có BCrypt.Net-Next nhưng DAL chưa.

**Quyết định:** Thêm gói BCrypt.Net-Next 4.2.0 vào Pipe_Hub.DAL.

**Lý do:**
- BCrypt là utility library, không phải business logic — thêm vào DAL KHÔNG vi phạm kiến trúc 3 tầng
- Tránh hard-code hash password trong Seeder (rủi ro sai → login không được)
- DbSeeder nằm ở DAL → cần HashPassword() ngay tại tầng đó

**Ngoại lệ:** Quy tắc "NuGet đúng tầng" đã có 1 ngoại lệ ở UI (Microsoft.EntityFrameworkCore.Design). Đây là ngoại lệ thứ 2 — BCrypt trong DAL.

### 2026-10-12 — Chuyển MigrateAndSeed vào BLL, UI không chạm DAL

**Bối cảnh:** Program.cs (UI) cần gọi migrate + seed khi khởi động. Nếu UI gọi trực tiếp `IDbContextFactory` hoặc `DbSeeder` → vi phạm kiến trúc "UI không tham chiếu DAL".

**Quyết định:** Tạo 2 method trong `Pipe_Hub.BLL/Extensions/`:
- `ServiceCollectionExtensions.AddPipeHubServices(services, configuration)` — đăng ký DbContextFactory
- `DatabaseInitializer.MigrateAndSeed(services)` — gọi migrate + seed

Program.cs CHỈ gọi 2 method này. KHÔNG có `using Pipe_Hub.DAL` trong Pipe_Hub.UI.

**Lý do:**
- Tuân thủ quy tắc 3 tầng: UI → BLL → DAL một chiều
- Verify bằng `Select-String -Path Pipe_Hub.UI\*.cs -Pattern "using Pipe_Hub.DAL"` → 0 kết quả
- Khi bảo vệ, hội đồng có thể hỏi "UI có gọi DbContext không?" → trả lời chắc chắn "Không"

### 2026-10-12 — Bọc DbSeeder trong transaction

**Bối cảnh:** DbSeeder cũ có hơn 10 lần SaveChanges rời nhau. Nếu lỗi giữa chừng (VD: unique constraint sau khi đã insert Role), lần chạy sau thấy Roles.AnyAsync() = true → bỏ qua toàn bộ → DB thiếu data vĩnh viễn.

**Quyết định:** Bọc toàn bộ seed trong `BeginTransactionAsync` + `CommitAsync` cuối + `RollbackAsync` trong catch. Đổi idempotent check từ Roles.AnyAsync() sang Orders.AnyAsync() (bảng cuối cùng được seed).

**Lý do:** Transaction đảm bảo all-or-nothing. Idempotent check dựa trên bảng cuối giúp phát hiện đúng trạng thái seed hoàn chỉnh.

### 2026-10-12 — Seed bổ sung Order/Transaction/GameBuild/TopUpRequest/Achievement

**Bối cảnh:** DbSeeder cũ chỉ seed User/Game/Wallet/Review — thiếu Order, Transaction, GameBuild. Dashboard và Report sẽ trống hoàn toàn khi demo.

**Quyết định:** Bổ sung seed data:
- 2 Order (player1 mua Sky Adventure, player2 mua Dungeon Master)
- 2 OrderItem tương ứng
- 5 Transaction (3 TopUp ban đầu + 2 Purchase)
- 6 GameBuild (Stable + Beta)
- 3 TopUpRequest (2 Pending + 1 Approved)
- 3 Achievement + 3 UserAchievement

**Lý do:** Đủ data để biểu đồ doanh thu/CCU có ý nghĩa. Wallet Balance đồng bộ với tổng Transaction (player1 = 301k sau khi mua, player2 = 101k, player3 = 1M chưa mua).

### 2026-10-12 — MessageBox khi lỗi khởi động

**Bối cảnh:** Program.cs cũ chỉ ghi log trong catch, không hiện MessageBox. Máy bảo vệ không có LocalDB → app tắt im lặng.

**Quyết định:** Catch block hiện MessageBox với 2 case:
- SqlException → gợi ý kiểm tra LocalDB + connection string
- Exception khác → thông báo chung + chỉ đến file log

Dùng fully-qualified `Microsoft.Data.SqlClient.SqlException` (không thêm using — UI.csproj không cài SqlClient trực tiếp).

**Lý do:** Người dùng cuối không đọc log file → cần thông báo trực quan. Đặc biệt quan trọng khi demo trên máy khác.

### 2026-10-12 — Không dùng HasQueryFilter cho Game.IsDeleted

**Bối cảnh:** Game có property IsDeleted. Reviewer đề xuất thêm HasQueryFilter như Review/Comment.

**Quyết định:** KHÔNG dùng query filter cho Game. Lọc thủ công trong Service khi query cho Player.

**Lý do:**
- Publisher cần xem game đã xóa để restore hoặc audit
- Admin cần xem game đã xóa để quản lý
- Query filter áp dụng MỌI query → sẽ ẩn game với cả Admin/Publisher
- Player query sẽ tự thêm `Where(g => !g.IsDeleted)` trong GameService

### 2026-10-12 — Quy ước dấu số tiền Transaction.Amount

**Bối cảnh:** Transaction ghi mọi biến động ví (TopUp, Purchase, Refund, Commission). Cần quy ước thống nhất để tính Balance = SUM(Amount).

**Quyết định:** Amount dương cho tiền vào ví (TopUp, Refund), âm cho tiền ra (Purchase). Commission chốt khi làm OrderService (D4).

**Lý do:** Balance ví luôn bằng tổng Amount các Transaction Success → kiểm tra đồng bộ dễ dàng bằng 1 query GROUP BY.

### 2026-10-12 — Migration ReviewIndexFilter cho index Review

**Bối cảnh:** ReviewConfiguration thêm `.HasFilter("[IsDeleted] = 0")` cho unique index (UserId, GameId) nhưng migration Init chưa có → model lệch snapshot, EF Core 9+ ném lỗi khi Migrate().

**Quyết định:** Tạo migration riêng `ReviewIndexFilter` (DROP index cũ + CREATE lại có filter), KHÔNG xóa/sửa migration Init.

**Lý do:** Migration đã apply lên DB không được sửa tay. Soft delete không chặn user viết lại review sau khi xóa.

### 2026-10-12 — Idempotent seed dùng Roles.AnyAsync()

**Bối cảnh:** DbSeeder từng đổi idempotent check sang Orders.AnyAsync() (bảng cuối). Nhưng toàn bộ seed đã bọc transaction all-or-nothing nên DB chỉ ở 2 trạng thái: trống hoặc đầy đủ.

**Quyết định:** Dùng lại `Roles.AnyAsync()` (bảng đầu tiên được seed).

**Lý do:** Với transaction, check bảng đầu là đủ và rẻ nhất. Tránh phụ thuộc vào thứ tự seed các bảng sau.

---

## D3 — Auth, MainForm, validate và DPI

### 2026-10-12 — Vòng đời service/context trong DI

**Quyết định:** `CurrentUserContext` + `ISystemConfigService` = Singleton (giữ trạng thái user/cache config); `IAuthService` + `ILogService` = Transient (không state); DbContext tạo ngắn hạn qua `IDbContextFactory`, dùng `await using` rồi bỏ.

**Lý do:** Singleton cho state/cache để dùng chung xuyên form; Transient cho service không state để thread-safe; không giữ 1 DbContext suốt app (WinForms pattern) tránh tracking rác và lỗi concurrency.

### 2026-10-12 — Ghi log nghiệp vụ fire-and-forget

**Quyết định:** AuthService gọi `_ = _logService.WriteAsync(...)` không await.

**Lý do:** `WriteAsync` nuốt mọi exception bên trong nên fire-and-forget an toàn — login/register không bị block bởi ghi log, app không crash nếu bảng LogEntries lỗi.

### 2026-10-12 — Luồng đăng xuất quay lại LoginForm

**Quyết định:** `MainForm.LogoutRequested` phân biệt Đăng xuất (LoginForm hiện lại, clear ô nhập) và Thoát/nút X (đóng app). LoginForm bỏ `FormClosed += Close` cũ và bỏ `CancelButton` trỏ vào nút Đăng ký (Esc không mở Register nữa).

**Lý do:** Đúng yêu cầu đề (đăng nhập/đăng xuất); tránh thoát app nhầm khi user chỉ muốn đăng xuất hoặc bấm Esc.

### 2026-10-12 — UI chỉ dùng DTO, không lộ entity DAL

**Quyết định:** `ILogService` trả `List<LogEntryDto>` (map từ entity + `Include(User)` lấy Username); UI không có `using Pipe_Hub.DAL` (verify bằng Select-String = 0 kết quả).

**Lý do:** Giữ đúng tầng UI → BLL → DAL; `UserDto`/`LogEntryDto` không chứa PasswordHash; khi bảo vệ trả lời được "UI có gọi DbContext không — Không".

### 2026-10-12 — Validate 2 lớp (UI + BLL)

**Quyết định:** UI dùng Validating/ErrorProvider + `AutoValidate.EnableAllowFocusChange` cho trải nghiệm gõ-phản hồi ngay; BLL (AuthService) validate lại toàn bộ cho bảo mật. Helper dùng chung: `UiExtensions.RunSafeAsync` (AppException → MessageBox, lỗi lạ → log + message chung, không lộ ex.Message) và `ErrorProviderHelper` (email qua `MailAddress.TryCreate`).

**Lý do:** Validate UI có thể bị bypass; BLL là chốt chặn cuối. Message lỗi không lường trước không được lộ stack trace/chi tiết kỹ thuật.

### 2026-10-12 — BCrypt chạy ngoài luồng UI

**Quyết định:** Mọi gọi `PasswordHasher.Hash/Verify` trong AuthService bọc `await Task.Run(...)`; PasswordHasher.cs giữ nguyên (static, không biết gì về UI thread).

**Lý do:** BCrypt tốn ~100–300ms — chạy trực tiếp sau await sẽ giật UI khi đăng nhập/đăng ký. Task.Run đẩy CPU-bound work ra thread pool.

### 2026-10-12 — Repository Hybrid

**Bối cảnh:** Đề PDF Ch.7 ghi "tầng truy xuất dữ liệu (Repository/DbContext)" — dấu / = HOẶC. Checklist ban đầu có IRepository<T> generic + UnitOfWork.

**Quyết định:** Dùng HYBRID:
- **IDbContextFactory trực tiếp:** AuthService, LogService, SystemConfigService, GameService, WalletService, KeyService — cho CRUD đơn giản.
- **IUnitOfWork + Repos chuyên biệt:** OrderService.PurchaseAsync — cần transaction + RowVersion + atomic key assignment.
- **IReportRepository:** ReportService — LINQ nặng (group-by, join).

**Lý do:**
- Đề cho phép cả Repository và DbContext (dấu /).
- Không dùng IRepository<T> generic — thay bằng interface service (IAuthService, ILogService, ISystemConfigService) làm minh chứng Ch.2.
- Tránh over-engineering: CRUD đơn giản không cần bọc Repository.
- Vẫn có Repository pattern ở những chỗ cần (concurrency, LINQ nặng).

**Minh chứng Ch.2:** IAuthService, ILogService, ISystemConfigService (đã có). Sẽ thêm IOrderService, IReportRepository, IReportBuilder, IPaymentGateway trong các task sau.

---

## D4 — Repository và Services

### 2026-10-13 — Chia D4 thành 10 task nhỏ

**Bối cảnh:** PurchaseAsync là method phức tạp nhất dự án (transaction + RowVersion + gán key atomic). Làm gộp dễ sai và khó review.

**Quyết định:** Chia D4 thành D4.1→D4.10 (repos → từng service → DI → test → commit).

**Lý do:** Tách task nhỏ giúp verify từng bước, lỗi lộ sớm ở đúng chỗ thay vì dồn cuối.

### 2026-10-13 — Generic constraint `where T : BaseEntity` cho IRepository

**Bối cảnh:** `GetByIdAsync` truy cập `e.Id` nên cần đảm bảo mọi T đều có Id.

**Quyết định:** Constraint bắt buộc `where T : BaseEntity` trên cả interface và implementation.

**Lý do:** BaseEntity cung cấp Id cho mọi entity, compiler bắt lỗi ngay nếu dùng sai kiểu.

### 2026-10-13 — UnitOfWork chia sẻ 1 DbContext cho 3 repo

**Bối cảnh:** PurchaseAsync cần atomic trên Order + Wallet + GameKey — mỗi repo một context riêng thì không share transaction được.

**Quyết định:** 1 context dùng chung, 3 repo lazy-init qua `??=`, transaction wrap ở UoW với null-check.

**Lý do:** Đảm bảo all-or-nothing cho nghiệp vụ mua game; repo chỉ tạo khi dùng tới.

### 2026-10-13 — PurchaseAsync dùng IDbContextFactory + UnitOfWork kết hợp

**Bối cảnh:** Cần theo Hybrid đã chốt (CRUD đơn giản dùng context, phần atomic dùng repo) mà không đăng ký UoW vào DI.

**Quyết định:** Tạo context từ factory → `new UnitOfWork(context)` thủ công → BeginTransaction; key lấy qua `uow.GameKeys`.

**Lý do:** Giữ đúng Hybrid; RowVersion tự check ở SaveChanges, catch DbUpdateConcurrencyException trả Failure thay vì throw.

### 2026-10-13 — SQLite override RowVersion trong DbContext

**Bối cảnh:** Wallet dùng `IsRowVersion()` của SQL Server nhưng SQLite không hỗ trợ → test seed fail NOT NULL.

**Quyết định:** Trong OnModelCreating, nếu ProviderName chứa "Sqlite" thì override thành byte[] thường (`IsConcurrencyToken(false)`, `ValueGeneratedNever()`, default rỗng).

**Lý do:** Test chạy SQLite in-memory; không dùng `Database.IsSqlite()` vì DAL không có package Sqlite. Production SQL Server không ảnh hưởng.

### 2026-10-13 — Test PurchaseAsync với 3 case + SQLite in-memory

**Bối cảnh:** PurchaseAsync là lõi tiền bạc, cần chứng minh thiếu tiền/hết key/thành công đều đúng.

**Quyết định:** 3 test xUnit trên SQLite in-memory (giữ connection mở, EnsureCreated): thiếu tiền, hết key, thành công kèm verify Balance/key/Order/Transaction. Mock ILogService bằng NSubstitute, CurrentUserContext dùng thật.

**Lý do:** SQLite hỗ trợ transaction thật (EF InMemory không có); mỗi test connection riêng nên isolated.

### 2026-10-13 — DI lifetime Transient cho 4 service D4

**Bối cảnh:** Cần chốt lifetime cho Game/Wallet/Order/KeyService khi wiring DI.

**Quyết định:** Cả 4 = Transient. Không đăng ký UnitOfWork vào DI (tạo thủ công trong PurchaseAsync).

**Lý do:** Cả 4 stateless, tạo context mới mỗi call qua factory nên thread-safe.

---

## D5 — Player Forms và MDI

### 2026-10-13 — DebounceTimer cho tìm kiếm realtime (300ms)

**Bối cảnh:** Tìm kiếm gõ từng ký tự mà query mỗi lần sẽ spam DB và giật UI.

**Quyết định:** Helper DebounceTimer dùng WinForms Timer (đúng UI thread): chờ 300ms sau lần gõ cuối mới gọi OnSearchChanged.

**Lý do:** Gõ liên tục chỉ trigger 1 lần filter; dùng Timer của WinForms nên không lỗi cross-thread.

### 2026-10-13 — BaseCrudForm abstract + 5 abstract method

**Bối cảnh:** Nhiều form CRUD có layout giống nhau (toolbar + lưới + đếm dòng) nhưng data/handler khác nhau.

**Quyết định:** Form cha abstract với ToolStrip (Thêm/Sửa/Xóa/Làm mới/Tìm) + DataGridView + StatusStrip; 5 abstract method bắt subclass override; Ctrl+F focus ô tìm, F5 refresh.

**Lý do:** Thống nhất UX các form list; debounce Dispose đúng để không leak Timer.

### 2026-10-13 — StoreForm ẩn 3 nút CRUD

**Bối cảnh:** Player chỉ được xem/mua game, không có quyền thêm/sửa/xóa.

**Quyết định:** StoreForm kế thừa BaseCrudForm nhưng ẩn `_btnAdd/_btnEdit/_btnDelete`; double-click mở chi tiết; search lọc client-side theo tên/NPH/thể loại; giá format N0.

**Lý do:** Tái dùng layout cha mà vẫn đúng phân quyền; filter client-side đủ nhanh, không cần query lại.

### 2026-10-13 — WalletForm thêm panel Balance 100px

**Bối cảnh:** Player cần thấy số dư nổi bật + nút nạp ví + lịch sử yêu cầu nạp cùng một chỗ.

**Quyết định:** Panel top 100px hiển thị Balance cỡ chữ 22 + nút Nạp ví; dialog nhập tiền + ComboBox phương thức; grid lịch sử TopUpRequest.

**Lý do:** Số dư là thông tin quan trọng nhất của ví; gom nạp + lịch sử vào một form cho tiện.

### 2026-10-13 — GameDetailForm không kế thừa BaseCrudForm

**Bối cảnh:** Form chi tiết game là layout đọc + 1 nút Mua, không phải danh sách CRUD.

**Quyết định:** Kế thừa Form thường với panel scroll; hiển thị giá gốc gạch ngang khi sale; disable nút Mua nếu game chưa Approved; mua xong hiện key + reload balance.

**Lý do:** Ép vào BaseCrudForm sẽ thừa toolbar/lưới; form chi tiết cần layout tự do.

### 2026-10-13 — ActivatorUtilities cho form có param gameId

**Bối cảnh:** GameDetailForm cần `int gameId` qua constructor nên không đăng ký DI thường được.

**Quyết định:** StoreForm dùng `ActivatorUtilities.CreateInstance<GameDetailForm>(_serviceProvider, selected.Id)` — DI tự inject service, truyền tay gameId.

**Lý do:** Vừa dùng được DI cho service, vừa truyền được tham số runtime; không cần factory class riêng.

### 2026-10-13 — Chuyển từ ShowDialog sang MDI Child

**Bối cảnh:** Đề yêu cầu MainForm MDI Parent nhưng các form mở modal thì không đúng chuẩn desktop.

**Quyết định:** `OpenMdiChild<TForm>()` generic: đã mở thì activate (bỏ minimize nếu cần), chưa thì resolve DI + `MdiParent = this` + `Show()`. StoreForm/WalletForm mở qua menu Ctrl+S/Ctrl+W; GameDetailForm và ChangePasswordForm giữ modal vì là dialog ngắn.

**Lý do:** Đúng yêu cầu MDI của đề; tránh mở trùng nhiều cửa sổ cùng form.

### 2026-10-13 — Shortcut Ctrl+S/Ctrl+W trong MainForm

**Bối cảnh:** Đề Ch.4 yêu cầu phím tắt thực tế; Player mở 2 form dùng nhiều nhất là Store và Wallet.

**Quyết định:** Ctrl+S mở Cửa hàng, Ctrl+W mở Ví (cùng hệ Ctrl+P đổi MK, Ctrl+Q đăng xuất đã có).

**Lý do:** Thao tác nhanh không cần chuột; dùng chung helper RegisterShortcut.

### 2026-10-13 — Fix OnFormClosing cho form Auth

**Bối cảnh:** AutoValidate + Validating cancel khiến bấm X không đóng được form khi có lỗi đỏ.

**Quyết định:** Override OnFormClosing đặt `AutoValidate = Disable` trước `base.OnFormClosing` ở cả 3 form Auth (pattern chuẩn WinForms).

**Lý do:** Nút Hủy/Thoát phải bấm được mọi lúc; `ValidateChildren()` ở nút chính vẫn chạy khi submit nên không mất validate.
