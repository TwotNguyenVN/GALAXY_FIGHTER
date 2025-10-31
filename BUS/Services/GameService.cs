using System;
using System.Data.Entity;
using System.Threading.Tasks;
using DAL.Model;

namespace BUS.Services
{
    public class GameService : IGameService
    {
        // KHÔNG có _db field, KHÔNG có constructor khởi tạo _db
        private readonly GameDbContext _db;
        public GameService(GameDbContext db) { _db = db; }
        public async Task<int> StartSessionAsync(int playerId)
        {
            using (var db = new GameDbContext())
            {
                var s = new GameSession
                {
                    PlayerId = playerId,
                    StartedAt = DateTime.UtcNow,
                    Score = 0,
                    CoinsEarned = 0,
                    BossKilled = false,
                    NoHitBossKill = false
                };
                db.GameSessions.Add(s);
                await db.SaveChangesAsync();
                return s.Id;
            }
        }

        public async Task<int> EndSessionAsync(int sessionId, int finalScore, bool bossKilled, bool noHitBossKill)
        {
            using (var db = new GameDbContext())
            {
                var s = await db.GameSessions.FirstOrDefaultAsync(x => x.Id == sessionId);
                if (s == null) throw new InvalidOperationException("GameSession not found.");

                s.EndedAt = DateTime.UtcNow;
                s.Score = finalScore;
                s.BossKilled = bossKilled;
                s.NoHitBossKill = noHitBossKill;

                // Tính coins từ score (ví dụ): 1 coin mỗi 200 điểm
                var coins = Math.Max(0, finalScore / 200);
                s.CoinsEarned = coins;

                // Cộng coin cho player
                var p = await db.Players.FirstOrDefaultAsync(x => x.Id == s.PlayerId);
                if (p != null)
                {
                    p.Coins += coins;
                    p.UpdatedAt = DateTime.UtcNow;
                }

                await db.SaveChangesAsync();
                return coins; // trả về số coin kiếm được ở session này
            }
        }
    }
}
