using DAL.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BUS.Services
{
    public interface IPlayerService
    {
        Task<Player> LoginOrCreateAsync(string username);
        Task<bool> UsernameExistsAsync(string username);
        Task<Player> GetByUsernameAsync(string username);
        Task<Player> CreateAsync(string username);

        Task<bool> TrySpendCoinsAsync(int playerId, int amount); // trừ coin (amount > 0)
        Task<bool> AddCoinsAsync(int playerId, int delta);       // cộng/trừ coin (delta +/-)

        Task<bool> UpdateUsernameAsync(int playerId, string newUsername);

        // Giữ đúng chữ ký bạn đang dùng
        Task<Player> ApplyDailyBonusAsync(Player player, int baseBonus, int day7Bonus);
    }
}
