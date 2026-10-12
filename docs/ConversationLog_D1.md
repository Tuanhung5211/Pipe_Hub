# LOG TỔNG HỢP HỘI THOẠI — PIPE_HUB (D1)

> Xuất ngày: 2026-10-11. Phạm vi: `C:\Code_Projects\VS\BTL_LTTQ\Pipe_Hub`

## 1. Mở đầu phiên
- User yêu cầu liệt kê file thư mục hiện tại (lúc đó session ở `C:\Code_Projects\VS`).
- User giới hạn phạm vi làm việc trong `C:\Code_Projects\VS\BTL_LTTQ\Pipe_Hub` → session đã chuyển về đó và chỉ làm việc trong đó từ lúc này.
- User giao vai trò AI agent đồ án WinForms .NET 10 tên Pipe_Hub + quy tắc làm việc (task nhỏ, dừng báo cáo, đọc trước sửa, verify 4 bước).

## 2. Kết quả từng task D1

| Task | Nội dung | Kết quả |
|------|----------|---------|
| D1.1 | Kiểm tra cấu trúc solution | Chỉ có `Pipe_Hub.UI` (net10.0-windows, chưa LangVersion). Chưa có BLL/DAL |
| D1.2 | Tạo BLL + DAL, thêm LangVersion 13.0 | OK. Build pass, 0 warning. `Class1.cs` do user xóa tay (shell bị chặn xóa) |
| D1.3 | Reference UI→BLL→DAL | OK. DAL không ref ai. Build pass |
| D1.4 | NuGet DAL (2 gói) | EF Core SqlServer 10.0.12, SqlClient 7.1.1. Build pass |
| D1.5 | NuGet BLL (11 gói) | BCrypt 4.2.0, ClosedXML 0.105.1, QuestPDF 2026.9.1, CsvHelper 33.1.0, QRCoder 1.8.0, MailKit 4.18.1, RestSharp 114.0.0, Extensions.* 10.0.12. Build pass |
| D1.6 | NuGet UI (8 gói) | LiveCharts 2.0.5, GMap 2.1.7, Serilog 4.4.0, Sinks.File 7.0.0, Ext.Hosting 10.0.0, Hosting 10.0.12, Config.Json 10.0.12, EF Design 10.0.12 (PrivateAssets=all tự sinh). Build pass, 10 warning (NU1902/1903 + NU1701) |
| D1.7 | .gitignore + suppress NU1902/1903 | OK. Warning còn 6 (chỉ NU1701) |
| D1.8 | BaseEntity.cs | OK |
| D1.9 | 7 enum riêng biệt | OK |
| D1.10 | User.cs, Role.cs (không build) | OK |
| Fix nhỏ | Xóa `using Pipe_Hub.DAL.Enums;` thừa trong User.cs | OK |
| D1.11 | 6 entity nhóm Game (không build) | OK |
| D1.12 | 5 entity Order/Wallet/Transaction (không build) | OK |
| D1.13 | 4 entity Review/Comment/Achievement (không build) | OK |
| D1.14 | 4 entity Misc (không build) | OK. Tổng 22 file Entities (BaseEntity + 21 entity, gồm TopUpRequest bổ sung) |
| D1.15 | Build lần đầu | succeeded, 6 warning NU1701, 0 error, 0 CS8019 |
| D1.16 | Program.cs (DI+Serilog+3 handler) + appsettings.json + csproj copy | OK. Build pass |
| D1.17 | DecisionLog.md + git commit D1 | Commit `07bc406` (41 file) |
| D1.18 P1 | Form test LiveCharts tạm | Build pass. User tự F5 test runtime |
| D1.18 P2 | Revert Program.cs, xóa _Test, DecisionLog LiveCharts | Program.cs revert OK; _Test do user xóa tay (đã xác nhận); commit `c01ae64` (DecisionLog). Không có commit cleanup vì working tree sạch |

## 3. Trạng thái cuối D1
- 3 project: UI (net10.0-windows) / BLL (net10.0) / DAL (net10.0); Nullable enable + LangVersion 13.0 cả 3.
- Reference một chiều UI→BLL→DAL. UI không ref DAL.
- NuGet: DAL 2 gói, BLL 11 gói, UI 8 gói (chi tiết ở các báo cáo task).
- DAL/Entities: 22 file (BaseEntity + 21 entity). DAL/Enums: 7 file.
- Program.cs: Serilog file `%LocalAppData%\Pipe_Hub\logs`, 3 global handler, Host DI (TODO D2/D3).
- appsettings.json: connection string LocalDB mẫu + Serilog Information.
- .gitignore: bin/obj/publish, *.user/*.suo/.vs, appsettings.Local.json, .idea, TestResults, *.nupkg.
- Warning: 6 NU1701 (LiveCharts2) — chấp nhận, đã verify runtime OK. NU1902/1903 đã suppress.
- Git: repo riêng `C:\Code_Projects\VS\BTL_LTTQ\Pipe_Hub\.git`, remote `https://github.com/Tuanhung5211/Pipe_Hub.git`. Commit: `07bc406` (D1), `c01ae64` (D1.18 DecisionLog). Đã push.
- DecisionLog: 6 quyết định D1 (.NET 10, LangVersion 13.0, suppress NU1902/1903, 3 tầng, TopUpRequest, LiveCharts2) + note kỹ thuật D2.

## 4. Quyết định đã chốt cho D2 (Fluent API)
- `DeleteBehavior.Restrict` cho Comment/Review (tránh multiple cascade paths).
- `HasQueryFilter` soft delete cho Review/Comment.
- Unique index: User.Username, User.Email, Role.Name, GameKey.Key, Wallet.UserId (1-1), SystemConfig.Key.
- Unique composite: Review (UserId, GameId), UserAchievement (UserId, AchievementId).
- Check constraint Rating 1-5.
- Transaction có 2 navigation (User + Wallet) → cấu hình FK rõ ràng.
- Lộ trình: D2.1 DbContext khung + Factory → D2.2→D2.6 Fluent API từng nhóm → D2.7 Migration → D2.8 Seeder → D2.9 DI → D2.10 test kết nối/migrate/seed.

## 5. Ghi chú vận hành
- `git rev-parse --show-toplevel` = `C:/Code_Projects/VS/BTL_LTTQ/Pipe_Hub` (repo riêng đã hoạt động).
- Mọi lệnh git chạy trong Pipe_Hub, không chạy ở VS.
- Shell bị chặn lệnh xóa (`rm`/`del`/`Remove-Item`) — file xóa tay do user thực hiện (Class1.cs, folder _Test).
- File log này chỉ để lưu trữ, không thuộc source đồ án.
