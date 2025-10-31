using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BUS.Services
{
    public interface IGameService
    {
        Task<int> StartSessionAsync(int playerId);
        Task<int> EndSessionAsync(int sessionId, int finalScore, bool bossKilled, bool noHitBossKill);
    }
}

