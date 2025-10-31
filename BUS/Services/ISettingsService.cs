using System.Threading.Tasks;

namespace BUS.Services
{
    public interface ISettingsService
    {
        Task<(int baseBonus, int day7Bonus)> GetDailyBonusAsync();
        Task<int> GetMissileCooldownAsync();
        Task UpsertAsync(string key, string value);

        Task<string> GetStringAsync(string key);
    }
}
