// Tái hiện "double edit PUT /food-logs/entries/{id}" ở mức EF trên một DB LocalDB RIÊNG, TẠO MỚI rồi XOÁ trong chính lần chạy này (MacroOS_ConcRepro; không đụng DB nào khác), rồi chứng minh cách sửa
// (IdempotentWrite: bỏ phần chưa lưu, đọc lại, áp lại, lưu). Migration S0..mới nhất chỉ áp lên DB tạm này.
using Application;
using Contract;
using Domain;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

const string server = "Server=(localdb)\\MSSQLLocalDB;";
const string db = "MacroOS_ConcRepro";
var cs = $"{server}Database={db};Integrated Security=true;TrustServerCertificate=True";
if (!cs.StartsWith("Server=(localdb)\\MSSQLLocalDB;Database=MacroOS_ConcRepro;")) throw new InvalidOperationException("LocalDB MacroOS_ConcRepro only");
var opts = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).Options;
ApplicationDbContext NewCtx() => new(opts);

async Task Drop()
{
    await using var m = new Microsoft.Data.SqlClient.SqlConnection($"{server}Database=master;Integrated Security=true;TrustServerCertificate=True");
    await m.OpenAsync();
    await using var cmd = m.CreateCommand();
    cmd.CommandText = $"IF DB_ID('{db}') IS NOT NULL BEGIN ALTER DATABASE [{db}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{db}]; END";
    await cmd.ExecuteNonQueryAsync();
}

async Task<FoodLogEntry> Load(ApplicationDbContext c, Guid id)   // y hệt FoodLogSupport.GetOwnedEntryAsync (tracking, kèm FoodLog + Entries)
    => await c.Set<FoodLogEntry>().Include(e => e.FoodLog).ThenInclude(l => l.Entries).FirstAsync(e => e.Id == id);

const decimal q0 = 100m, cal0 = 165m;
var logId = Guid.NewGuid(); var id = Guid.NewGuid();
void Edit(FoodLogEntry e, decimal quantity) { e.Quantity = quantity; e.Calories = cal0 * quantity / q0; FoodLogSupport.RecalcTotals(e.FoodLog); }   // như handler: đổi entry + tính lại tổng ngày

await Drop();
try
{
    using (var setup = NewCtx())
    {
        await setup.Database.MigrateAsync();   // S0..mới nhất, chỉ trên DB tạm
        await setup.Database.ExecuteSqlRawAsync("ALTER TABLE [FoodLogEntries] NOCHECK CONSTRAINT ALL; ALTER TABLE [FoodLogs] NOCHECK CONSTRAINT ALL");   // dữ liệu thử, không cần user/food thật
        setup.Add(new FoodLog { Id = logId, UserId = Guid.NewGuid(), LogDate = new DateOnly(2026, 10, 23), TotalCalories = cal0 });
        setup.Add(new FoodLogEntry { Id = id, FoodLogId = logId, MealType = MealType.Lunch, FoodItemId = Guid.NewGuid(), Quantity = q0, Calories = cal0, Protein = 31, Carbs = 0, Fat = 3.6m });
        await setup.SaveChangesAsync();
    }
    Console.WriteLine($"setup: entry quantity {q0}, calories {cal0}");

    // 1) HAI REQUEST ĐỒNG THỜI: cùng đọc, A lưu trước, B lưu sau ⇒ B hỏng
    using (var a = NewCtx()) using (var b = NewCtx())
    {
        var ea = await Load(a, id); var eb = await Load(b, id);
        Edit(ea, q0 + 1); Edit(eb, q0 + 2);
        await a.SaveChangesAsync();
        try { await b.SaveChangesAsync(); Console.WriteLine("1) UNEXPECTED: second save succeeded"); }
        catch (DbUpdateConcurrencyException ex) { Console.WriteLine($"1) REPRODUCED: second concurrent PUT => {ex.GetType().Name} ({ex.Entries.Count} entries: {string.Join(", ", ex.Entries.Select(e => e.Metadata.ClrType.Name))}). Nothing catches it => HTTP 500."); }
    }

    // 2) CÁCH SỬA: IdempotentWrite.RunAsync: bên thua bỏ phần chưa lưu, ĐỌC LẠI, áp lại, lưu (last-writer-wins, không 500)
    using (var a = NewCtx())
    {
        var ea = await Load(a, id);
        var attempts = 0;
        using var b = NewCtx();
        var result = await IdempotentWrite.RunAsync(async () =>
        {
            attempts++;
            var eb = await Load(b, id);                 // lần chạy lại đọc lại dữ liệu mới
            if (attempts == 1) { Edit(ea, q0 + 5); await a.SaveChangesAsync(); }   // A thắng giữa lúc B đã đọc xong
            Edit(eb, q0 + 6);
            await b.SaveChangesAsync();
            return eb.Quantity;
        }, b.ChangeTracker.Clear);
        using var verify = NewCtx();
        var finalQ = await verify.Set<FoodLogEntry>().Where(x => x.Id == id).Select(x => x.Quantity).FirstAsync();
        var finalLog = await verify.Set<FoodLog>().Where(l => l.Id == logId).Select(l => l.TotalCalories).FirstAsync();
        Console.WriteLine($"2) FIX: attempts = {attempts}, stored quantity {finalQ} (last writer wins), no exception; day total calories {finalLog} (= entry {cal0 * (q0 + 6) / q0})");
    }

    // 3) Vẫn lỗi sau lần chạy lại ⇒ ConflictException (409), không 500
    using (var a = NewCtx())
    {
        var attempts = 0;
        try
        {
            await IdempotentWrite.RunAsync(async () =>
            {
                attempts++;
                var ea = await Load(a, id);
                using var other = NewCtx(); var eo = await Load(other, id); Edit(eo, q0 + 20 + attempts); await other.SaveChangesAsync();   // luôn có người ghi chen, mỗi lần một giá trị khác
                Edit(ea, q0 + 10); await a.SaveChangesAsync();
            }, a.ChangeTracker.Clear);
        }
        catch (ConflictException ex) { Console.WriteLine($"3) persistent conflict after {attempts} attempts => ConflictException (HTTP 409): {ex.Message}"); }
    }
}
finally { await Drop(); Console.WriteLine($"dropped temporary database {db}"); }
