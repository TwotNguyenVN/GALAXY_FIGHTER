using System;
using System.Data.Entity;          // EF6
using System.Threading.Tasks;
using DAL.Model;

namespace BUS.Services
{
    public class PlayerService : IPlayerService
    {
        private readonly GameDbContext _db;

        public PlayerService(GameDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        // ===== Auth / Lookup =====
        public async Task<Player> LoginOrCreateAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("username is required", nameof(username));

            var p = await _db.Players.FirstOrDefaultAsync(x => x.Username == username);
            if (p == null)
            {
                p = new Player
                {
                    Username = username,
                    Coins = 0,
                    DailyStreak = 0,                // byte
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.Players.Add(p);
                await _db.SaveChangesAsync();
            }
            return p;
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;
            return await _db.Players.AnyAsync(x => x.Username == username);
        }

        public async Task<Player> GetByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;
            return await _db.Players.FirstOrDefaultAsync(x => x.Username == username);
        }

        public async Task<Player> CreateAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("username is required", nameof(username));

            // Đã tồn tại -> trả luôn
            var exists = await _db.Players.FirstOrDefaultAsync(x => x.Username == username);
            if (exists != null) return exists;

            var p = new Player
            {
                Username = username,
                Coins = 0,
                DailyStreak = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Players.Add(p);
            await _db.SaveChangesAsync();
            return p;
        }

        // ===== Coins =====
        public async Task<bool> TrySpendCoinsAsync(int playerId, int amount)
        {
            if (amount <= 0) return true; // không trừ gì thì coi như OK

            var p = await _db.Players.FirstOrDefaultAsync(x => x.Id == playerId);
            if (p == null || p.Coins < amount) return false;

            p.Coins = p.Coins - amount;
            p.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddCoinsAsync(int playerId, int delta)
        {
            if (delta == 0) return true;

            var p = await _db.Players.FirstOrDefaultAsync(x => x.Id == playerId);
            if (p == null) return false;

            // chống overflow/âm
            long next = (long)p.Coins + delta;
            if (next < 0) next = 0;
            if (next > int.MaxValue) next = int.MaxValue;

            p.Coins = (int)next;
            p.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }

        // ===== Profile =====
        public async Task<bool> UpdateUsernameAsync(int playerId, string newUsername)
        {
            if (string.IsNullOrWhiteSpace(newUsername))
                throw new ArgumentException("newUsername required", nameof(newUsername));

            // chặn trùng tên với người khác
            bool taken = await _db.Players.AnyAsync(x => x.Username == newUsername && x.Id != playerId);
            if (taken) return false;

            var p = await _db.Players.FirstOrDefaultAsync(x => x.Id == playerId);
            if (p == null) throw new InvalidOperationException("Player not found");

            p.Username = newUsername;
            p.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }

        // ===== Daily Bonus =====
        public async Task<Player> ApplyDailyBonusAsync(Player player, int baseBonus, int day7Bonus)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));

            var p = await _db.Players.FirstOrDefaultAsync(x => x.Id == player.Id);
            if (p == null) throw new InvalidOperationException("Player not found");

            var today = DateTime.UtcNow.Date;

            // Đã nhận hôm nay → trả về
            if (p.LastLoginDate.HasValue && p.LastLoginDate.Value.Date == today)
                return p;

            // Reset streak nếu cách >1 ngày, ngược lại +1 (tối đa 7)
            if (!p.LastLoginDate.HasValue || (today - p.LastLoginDate.Value.Date).TotalDays > 1)
                p.DailyStreak = 1;
            else
                p.DailyStreak = (byte)Math.Min(7, p.DailyStreak + 1);

            // Thưởng
            int bonus = (p.DailyStreak == 7) ? day7Bonus : baseBonus;

            long next = (long)p.Coins + bonus;
            if (next > int.MaxValue) next = int.MaxValue;
            p.Coins = (int)next;
            p.LastLoginDate = today;
            p.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return p;
        }
    }
}
