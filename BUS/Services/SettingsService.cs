using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using DAL.Model;

namespace BUS.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly GameDbContext _db;
        public SettingsService(GameDbContext db) { _db = db; }
        public async Task<string> GetStringAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("key required", nameof(key));
            using (var db = new GameDbContext())
            {
                return await db.Settings
                               .Where(x => x.Key == key)
                               .Select(x => x.Value)
                               .FirstOrDefaultAsync();
            }
        }

        public async Task<(int baseBonus, int day7Bonus)> GetDailyBonusAsync()
        {
            // mặc định
            int baseBonus = 3, day7 = 6;

            using (var db = new GameDbContext())
            {
                // lấy value dạng string rồi TryParse an toàn
                var bVal = await db.Settings
                                   .Where(x => x.Key == "DAILY_BASE")
                                   .Select(x => x.Value)
                                   .FirstOrDefaultAsync();

                var dVal = await db.Settings
                                   .Where(x => x.Key == "DAILY_DAY7")
                                   .Select(x => x.Value)
                                   .FirstOrDefaultAsync();

                int tmp;
                if (int.TryParse(bVal, out tmp)) baseBonus = tmp;
                if (int.TryParse(dVal, out tmp)) day7 = tmp;
            }
            return (baseBonus, day7);
        }

        public async Task<int> GetMissileCooldownAsync()
        {
            // mặc định 2500 ms
            int cooldown = 2500;

            using (var db = new GameDbContext())
            {
                var s = await db.Settings
                                .Where(x => x.Key == "MISSILE_COOLDOWN_MS")
                                .Select(x => x.Value)
                                .FirstOrDefaultAsync();

                int tmp;
                if (int.TryParse(s, out tmp)) cooldown = tmp;
            }
            return cooldown;
        }

        public async Task UpsertAsync(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("key required", nameof(key));

            using (var db = new GameDbContext())
            {
                var s = await db.Settings.FirstOrDefaultAsync(x => x.Key == key);
                if (s == null)
                {
                    db.Settings.Add(new Setting { Key = key, Value = value });
                }
                else
                {
                    s.Value = value;
                }
                await db.SaveChangesAsync();
            }
        }
    }
}
