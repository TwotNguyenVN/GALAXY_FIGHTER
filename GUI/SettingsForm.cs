using BUS.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GUI
{
    public partial class SettingsForm : Form
    {
        private readonly ISettingsService _svc;

        // Constructor mặc định cho Designer
        public SettingsForm()
        {
            InitializeComponent();
        }

        // Constructor dùng thật khi mở từ MainMenu
        public SettingsForm(ISettingsService svc) : this()
        {
            _svc = svc ?? throw new ArgumentNullException(nameof(svc));
            this.Load += async (_, __) => await LoadDataAsync();

            btnReload.Click += async (_, __) => await LoadDataAsync();
            btnSave.Click += async (_, __) => await SaveAsync();
            btnClose.Click += (_, __) => this.Close();
        }

        private async Task LoadDataAsync()
        {
            if (_svc == null) return; // mở từ Designer
            try
            {
                // Missile cooldown
                var ms = await _svc.GetMissileCooldownAsync();
                if (ms < (int)nudMissile.Minimum) ms = (int)nudMissile.Minimum;
                if (ms > (int)nudMissile.Maximum) ms = (int)nudMissile.Maximum;
                nudMissile.Value = ms;

                // Daily bonus
                var (baseBonus, day7Bonus) = await _svc.GetDailyBonusAsync();
                baseBonus = Math.Max((int)nudBase.Minimum, Math.Min((int)nudBase.Maximum, baseBonus));
                day7Bonus = Math.Max((int)nudDay7.Minimum, Math.Min((int)nudDay7.Maximum, day7Bonus));
                nudBase.Value = baseBonus;
                nudDay7.Value = day7Bonus;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không tải được cài đặt:\n" + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SaveAsync()
        {
            if (_svc == null) return;
            try
            {
                // Ghi 3 khóa settings
                await _svc.UpsertAsync("MISSILE_COOLDOWN_MS", nudMissile.Value.ToString());
                await _svc.UpsertAsync("DAILY_BASE", nudBase.Value.ToString());
                await _svc.UpsertAsync("DAILY_DAY7", nudDay7.Value.ToString());

                MessageBox.Show("Đã lưu cài đặt!", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lưu thất bại:\n" + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
