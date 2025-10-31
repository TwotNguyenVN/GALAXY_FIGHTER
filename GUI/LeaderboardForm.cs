using BUS.Services;
using DAL.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GUI
{
    public partial class LeaderboardForm : Form
    {
        private GameDbContext _db;

        public LeaderboardForm()
        {
            InitializeComponent();
            _db = new GameDbContext();

            this.Load += async (_, __) => await LoadDataAsync();
            btnRefresh.Click += async (_, __) => await LoadDataAsync();
            cboSort.SelectedIndexChanged += async (_, __) => await LoadDataAsync();
            nudTop.ValueChanged += async (_, __) => await LoadDataAsync();
            txtSearch.TextChanged += async (_, __) => await LoadDataAsync();

            this.FormClosed += (_, __) => { _db?.Dispose(); _db = null; };
        }

        private async Task LoadDataAsync()
        {
            try
            {
                if (_db == null) return;

                string search = (txtSearch.Text ?? "").Trim();
                int take = (int)nudTop.Value;
                string sort = cboSort.SelectedItem?.ToString() ?? "TotalScore";

                //var baseQuery = _db.v_PlayerSummary.AsNoTracking();
                IQueryable<v_PlayerSummary> baseQuery = _db.v_PlayerSummary.AsNoTracking();

                if (!string.IsNullOrEmpty(search))
                    baseQuery = baseQuery.Where(x => x.Username.Contains(search));

                // Với các sort thuần DB, cứ sắp xếp trước rồi ToListAsync
                switch (sort)
                {
                    case "TotalScore":
                        baseQuery = baseQuery.OrderByDescending(x => x.TotalScore).ThenBy(x => x.Username);
                        break;
                    case "TotalSessions":
                        baseQuery = baseQuery.OrderByDescending(x => x.TotalSessions).ThenBy(x => x.Username);
                        break;
                    case "LastPlayedAt":
                        baseQuery = baseQuery.OrderByDescending(x => x.LastPlayedAt).ThenBy(x => x.Username);
                        break;
                    case "BossKills":
                        baseQuery = baseQuery.OrderByDescending(x => x.BossKills).ThenBy(x => x.Username);
                        break;
                    case "TotalCoins":
                        // sẽ sort ở bước 2 (in-memory) vì cần ledger
                        break;
                    default:
                        baseQuery = baseQuery.OrderByDescending(x => x.TotalScore).ThenBy(x => x.Username);
                        break;
                }

                var raw = await baseQuery.Take(take * 2).ToListAsync(); // lấy dư 1 chút để sort lại in-memory
                var earned = CoinsLedgerStore.LoadTotals();             // username -> lifetime earned

                // Gộp và tính TotalCoins (chỉ cộng, không bao giờ giảm)
                var list = raw.Select(x => new
                {
                    x.Username,
                    x.TotalScore,
                    x.TotalSessions,
                    x.BossKills,
                    x.NoHitBossKills,
                    CoinsNow = x.Coins, // vẫn có nếu cần debug, nhưng không dùng cho TotalCoins
                    LifetimeEarned = earned.TryGetValue(x.Username, out var v) ? v : 0L,
                    TotalCoins = earned.TryGetValue(x.Username, out var v2) ? v2 : 0L,
                    LastPlayedAt = x.LastPlayedAt
                });

                // Sort in-memory nếu người dùng chọn TotalCoins
                if (sort == "TotalCoins")
                {
                    list = list
                        .OrderByDescending(z => z.TotalCoins)
                        .ThenBy(z => z.Username);
                }

                // cắt top N sau khi sort in-memory
                var data = list.Take(take)
               .Select((z, idx) => new
               {
                   Rank = idx + 1,
                   z.Username,
                   z.TotalScore,
                   z.TotalSessions,
                   z.BossKills,
                   z.NoHitBossKills,
                   TotalCoins = z.TotalCoins, // giờ là lifetime-only
                   LastPlayedAt = z.LastPlayedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
               })
               .ToList();


                dgv.DataSource = data;
                AutoSizeGrid();
                if (dgv.Columns["TotalCoins"] != null)
                    dgv.Columns["TotalCoins"].HeaderText = "Total Coins";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không tải được Leaderboard:\n" + ex.Message,
                                "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void AutoSizeGrid()
        {
            if (dgv.Columns.Count == 0) return;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            if (dgv.Columns["Username"] != null)
                dgv.Columns["Username"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }
    }
}
