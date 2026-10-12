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
