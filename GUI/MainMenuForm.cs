using BUS.Services;
using DAL.Model;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GUI
{
    public partial class MainMenuForm : Form
    {
        private readonly IPlayerService _playerService;
        private readonly IGameService _gameService;
        private readonly ISettingsService _settingsService;

        private Player _player;

        public MainMenuForm(Player player, IPlayerService ps, IGameService gs, ISettingsService ss)
        {
            InitializeComponent();
            _player = player;
            _playerService = ps;
            _gameService = gs;
            _settingsService = ss;

            // hiển thị thông tin ban đầu
            lblPlayer.Text = $"Player: {_player.Username}";
            lblCoins.Text = $"Coins: {_player.Coins}";
            txtPlayerName.Text = _player.Username;
        }

        // ==== Nút 1: ĐỔI TÊN (đổi username của tài khoản hiện tại, giữ nguyên dữ liệu) ====
        private async void btnRename_Click(object sender, EventArgs e)
        {
            var newName = (txtPlayerName.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("Tên không được để trống.", "Lưu ý", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (newName.Equals(_player.Username, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Tên không thay đổi.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // trùng tên với người khác?
                if (await _playerService.UsernameExistsAsync(newName))
                {
                    MessageBox.Show("Tên này đã tồn tại. Vui lòng chọn tên khác.", "Trùng tên",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Đổi tên trên chính record hiện tại
                var ok = await _playerService.UpdateUsernameAsync(_player.Id, newName);
                if (!ok)
                {
                    MessageBox.Show("Không thể đổi tên. Vui lòng thử lại.", "Lỗi",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _player.Username = newName;
                lblPlayer.Text = $"Player: {_player.Username}";
                txtPlayerName.Text = _player.Username;

                try { await _settingsService.UpsertAsync("LAST_USERNAME", _player.Username); } catch { }

                MessageBox.Show("Đổi tên thành công.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi đổi tên:\n" + ex.Message, "Lỗi",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==== Nút 2: ĐĂNG NHẬP / CHUYỂN TÀI KHOẢN ====
        private async void btnLoginSwitch_Click(object sender, EventArgs e)
        {
            var name = (txtPlayerName.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Vui lòng nhập tên.", "Lưu ý", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (await _playerService.UsernameExistsAsync(name))
                {
                    // Đăng nhập vào tài khoản có sẵn
                    var p2 = await _playerService.GetByUsernameAsync(name);
                    if (p2 == null)
                    {
                        MessageBox.Show("Không thể tải tài khoản. Vui lòng thử lại.", "Lỗi",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    _player = p2;

                    lblPlayer.Text = $"Player: {_player.Username}";
                    lblCoins.Text = $"Coins: {_player.Coins}";
                    try { await _settingsService.UpsertAsync("LAST_USERNAME", _player.Username); } catch { }

                    MessageBox.Show("Đăng nhập thành công.", "OK",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // Chưa có → hỏi tạo mới
                    var confirm = MessageBox.Show(
                        $"Không tìm thấy “{name}”.\nBạn có muốn tạo tài khoản mới với tên này không?",
                        "Tạo tài khoản mới?",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (confirm == DialogResult.Yes)
                    {
                        var p2 = await _playerService.CreateAsync(name);
                        _player = p2;

                        lblPlayer.Text = $"Player: {_player.Username}";
                        lblCoins.Text = $"Coins: {_player.Coins}";
                        try { await _settingsService.UpsertAsync("LAST_USERNAME", _player.Username); } catch { }

                        MessageBox.Show("Đã tạo và đăng nhập tài khoản mới.", "OK",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }

                // đồng bộ textbox
                txtPlayerName.Text = _player.Username;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi đăng nhập/tạo tài khoản:\n" + ex.Message,
                                "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==== Play ====
        private async void btnPlay_Click(object sender, EventArgs e)
        {
            using (var game = new GameForm(_player, _playerService, _gameService, _settingsService))
            {
                this.Hide();
                game.ShowDialog(this);
                this.Show();
                await BindPlayerAsync(); // cập nhật coin sau khi chơi
            }
        }

        // ==== Settings ====
        private void btnSettings_Click(object sender, EventArgs e)
        {
            using (var f = new SettingsForm(_settingsService))
                f.ShowDialog(this);
        }

        // ==== Leaderboard ====
        private void btnLeaderboard_Click(object sender, EventArgs e)
        {
            using (var f = new LeaderboardForm())
                f.ShowDialog(this);
        }

        // ==== Profile ====
        private void btnProfile_Click(object sender, EventArgs e)
        {
            using (var f = new ProfileForm(_player))
                f.ShowDialog(this);
        }

        // ==== About ====
        private void btnAbout_Click(object sender, EventArgs e)
        {
            using (var f = new AboutForm())
            {
                f.StartPosition = FormStartPosition.CenterParent; // cho đẹp, mở giữa menu
                f.ShowDialog(this);                                // modal, block MainMenuForm cho đến khi đóng
            }
        }


        // ==== Exit ====
        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // ==== Helper: load lại player ====
        private async Task BindPlayerAsync()
        {
            var latest = await _playerService.GetByUsernameAsync(_player.Username) ?? _player;
            _player = latest;

            lblPlayer.Text = $"Player: {_player.Username}";
            lblCoins.Text = $"Coins: {_player.Coins}";
        }
    }
}
