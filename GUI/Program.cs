using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using BUS.Services;
using DAL.Model;

namespace GUI
{
    internal static class Program
    {
        [STAThread]
        private static async Task Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 1 DbContext chia sẻ cho các service (đủ đơn giản cho WinForms)
            var db = new GameDbContext();

            var playerSvc = new PlayerService(db);
            var gameSvc = new GameService(db);
            var settingsSvc = new SettingsService(db);

            // Lấy/tạo player khởi động
            var player = await GetStartupPlayerAsync(playerSvc, settingsSvc);

            Application.Run(new MainMenuForm(player, playerSvc, gameSvc, settingsSvc));



        }

        private static async Task<Player> GetStartupPlayerAsync(
            IPlayerService playerSvc, ISettingsService settingsSvc)
        {
            string last = null;
            try { last = await settingsSvc.GetStringAsync("LAST_USERNAME"); } catch { }

            if (string.IsNullOrWhiteSpace(last))
            {
                last = "Pilot" + new Random().Next(1000, 9999);
                try { await settingsSvc.UpsertAsync("LAST_USERNAME", last); } catch { }
            }

            return await playerSvc.LoginOrCreateAsync(last);
        }
    }
}
