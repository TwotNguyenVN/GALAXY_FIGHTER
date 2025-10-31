using DAL.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GUI
{
    public partial class ProfileForm : Form
    {
        private readonly Player _player;
        private GameDbContext _db;

        // Trạng thái sort cho DataView (áp dụng cho dgvSessions)
        private string _currentSort = string.Empty; // ví dụ: "Lần chơi DESC, Score DESC"
        private readonly Dictionary<string, string> _colDir = new Dictionary<string, string>();

        public ProfileForm(Player player)
        {
            InitializeComponent();
            _player = player ?? throw new ArgumentNullException(nameof(player));

            _db = new GameDbContext();

            this.Load += async (_, __) => await LoadDataAsync();
            btnRefresh.Click += async (_, __) => await LoadDataAsync();
            btnClose.Click += (_, __) => this.Close();

            dgvSessions.ColumnHeaderMouseClick += DgvSessions_ColumnHeaderMouseClick;

            this.FormClosed += (_, __) =>
            {
                if (_db != null) _db.Dispose();
                _db = null;
            };
        }

        private async Task LoadDataAsync()
        {
            try
            {
                if (_db == null) return;

                var p = await _db.Players
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(x => x.Id == _player.Id);
                if (p == null)
                {
                    MessageBox.Show("Không tìm thấy người chơi.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                    return;
                }

                // Header
                lblUsername.Text = p.Username;
                lblCoins.Text = "Coins: " + p.Coins;
                lblStreak.Text = "Streak: " + p.DailyStreak + "/7";
                lblLastLogin.Text = "Last login: " +
                    (p.LastLoginDate.HasValue ? p.LastLoginDate.Value.ToString("yyyy-MM-dd") : "N/A");

                // Sessions (mới nhất trước)
                var sessions = await _db.GameSessions
                    .Where(s => s.PlayerId == p.Id)
                    .OrderByDescending(s => s.StartedAt)
                    .AsNoTracking()
                    .ToListAsync();

                // Tổng quan
                int totalSessions = sessions.Count;
                long totalScore = sessions.Sum(s => (long)s.Score);
                long totalCoinsEarned = sessions.Sum(s => (long)s.CoinsEarned);
                int bossKills = sessions.Count(s => s.BossKilled);
                int noHitBossKills = sessions.Count(s => s.NoHitBossKill);
                DateTime? lastPlayedAt = sessions.Count > 0 ? (DateTime?)sessions[0].StartedAt : null;

                lblTotals.Text =
                    "Sessions: " + totalSessions +
                    " | Score: " + totalScore +
                    " | Coins Earned: " + totalCoinsEarned +
                    " | BossKills: " + bossKills +
                    " | No-hit: " + noHitBossKills;

                lblLastPlayed.Text = "Last played: " +
                    (lastPlayedAt.HasValue ? lastPlayedAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "N/A");

                // DataTable cho lưới
                var dt = new DataTable();
                dt.Columns.Add("Lần chơi", typeof(int));
                dt.Columns.Add("Bắt đầu", typeof(string));
                dt.Columns.Add("Thời lượng", typeof(string));  // <- thay thế "Kết thúc"
                dt.Columns.Add("Score", typeof(int));
                dt.Columns.Add("Coins Earned", typeof(int));
                dt.Columns.Add("BossKilled", typeof(bool));
                dt.Columns.Add("NoHitBoss", typeof(bool));

                // Cột ẩn: khóa sort số học cho "Thời lượng"
                dt.Columns.Add("DurationSec", typeof(int));

                int n = sessions.Count;
                for (int i = 0; i < n; i++)
                {
                    var s = sessions[i];
                    int playNo = n - i; // 1..n; n = phiên mới nhất

                    // Tính duration
                    TimeSpan dur = TimeSpan.Zero;
                    if (s.EndedAt.HasValue && s.EndedAt.Value >= s.StartedAt)
                        dur = s.EndedAt.Value - s.StartedAt;

                    string durStr = FormatDurationMmSs(dur);
                    int durSec = (int)Math.Max(0, dur.TotalSeconds);

                    dt.Rows.Add(
                        playNo,
                        s.StartedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                        durStr,
                        s.Score,
                        s.CoinsEarned,
                        s.BossKilled,
                        s.NoHitBossKill,
                        durSec          // DurationSec (ẩn)
                    );
                }

                dgvSessions.AutoGenerateColumns = true;
                var dv = dt.DefaultView;

                // Áp sort đang nhớ (nếu có)
                dv.Sort = _currentSort;
                dgvSessions.DataSource = dv;

                // Ẩn cột DurationSec (chỉ dùng để sort)
                if (dgvSessions.Columns["DurationSec"] != null)
                    dgvSessions.Columns["DurationSec"].Visible = false;

                // Enable sort icon & autosize
                foreach (DataGridViewColumn col in dgvSessions.Columns)
                    col.SortMode = DataGridViewColumnSortMode.Automatic;

                AutoSizeGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không tải được hồ sơ người chơi:\n" + ex.Message,
                                "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // map tiêu đề hiển thị -> tên cột sort trong DataView
        private string MapSortColumn(string headerText)
        {
            // Bấm "Thời lượng" thì sort theo DurationSec (số)
            if (string.Equals(headerText, "Thời lượng", StringComparison.OrdinalIgnoreCase))
                return "DurationSec";
            return headerText;
        }

        private void DgvSessions_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            var dv = dgvSessions.DataSource as DataView;
            if (dv == null) return;

            var col = dgvSessions.Columns[e.ColumnIndex];
            string header = col.HeaderText;
            string sortCol = MapSortColumn(header);

            string dir;
            if (!_colDir.TryGetValue(header, out dir))
                dir = "ASC";
            else
                dir = string.Equals(dir, "ASC", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";

            bool addSecondary = (ModifierKeys & Keys.Shift) == Keys.Shift;

            if (!addSecondary || string.IsNullOrWhiteSpace(_currentSort))
            {
                _currentSort = sortCol + " " + dir;
            }
            else
            {
                // loại bỏ sort cũ trên cùng header (dù map sang tên cột khác)
                var tokens = _currentSort
                    .Split(',')
                    .Select(s => s.Trim())
                    .Where(s =>
                    {
                        // s có dạng "ColName DIR"
                        int sp = s.LastIndexOf(' ');
                        if (sp <= 0) return true;
                        string colName = s.Substring(0, sp).Trim();
                        // nếu trước đó từng map "Thời lượng" -> "DurationSec", thì colName có thể là "DurationSec"
                        if (string.Equals(colName, sortCol, StringComparison.OrdinalIgnoreCase)) return false;
                        if (string.Equals(colName, header, StringComparison.OrdinalIgnoreCase)) return false;
                        return true;
                    })
                    .ToList();

                tokens.Add(sortCol + " " + dir);
                _currentSort = string.Join(", ", tokens);
            }

            _colDir[header] = dir;
            dv.Sort = _currentSort;
        }

        private static string FormatDurationMmSs(TimeSpan dur)
        {
            // Nếu dài hơn 60 phút, hiển thị HH:mm:ss; còn lại mm:ss
            if (dur.TotalHours >= 1.0)
                return string.Format("{0:D2}:{1:D2}:{2:D2}", (int)dur.TotalHours, dur.Minutes, dur.Seconds);
            return string.Format("{0:D2}:{1:D2}", (int)dur.TotalMinutes, dur.Seconds);
        }

        private void AutoSizeGrid()
        {
            if (dgvSessions.Columns.Count == 0) return;

            dgvSessions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;

            var col = dgvSessions.Columns["Bắt đầu"];
            if (col != null)
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }
    }
}
