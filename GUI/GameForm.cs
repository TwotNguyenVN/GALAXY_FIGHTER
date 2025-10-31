using BUS.Services;
using DAL.Model;
using GUI.GraphicsCore;
using NAudio.Gui;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO; // nếu cần
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gfx = System.Drawing.Graphics;

namespace GUI
{
    public partial class GameForm : Form
    {

        // Chuẩn homing của đạn xanh Lv3 (đang dùng trong FireGreen tier 3)
        private const float GREEN3_HOMING_RANGE = 700f;
        private const float GREEN3_HOMING_FOV_DEG = 100f;
        private const int GREEN3_HOMING_EXPIRE_MS = 3000;
        private const float GREEN3_LERP = 0.18f;

        // Hệ số homing cho tên lửa so với đạn xanh Lv3 (có thể chỉnh)
        private float MissileHomingFactor = 2.0f;   // 2.0 = gấp đôi xanh Lv3


        // Scale riêng cho tên lửa (có thể chỉnh về sau)
        private int MissileScalePct = 500; // 100 = đúng kích thước ảnh, 300 = phóng 3x

        // ===== Enemy movement tuning =====
        private float SeedEnemy = 1.00f;
        // 0.75f = chậm, 1.00f = chuẩn, 1.25f = nhanh nhẹ, 1.50f = nhanh hơn

        private float ShieldScale = 1.50f; // 1.0 = bằng tàu; 1.3 = to hơn 30%

        // ===== Global size knobs =====
        private int BulletScalePct = 300; // x — phóng to đạn người chơi (%). VD 100 = giữ nguyên, 135 = to 35%
        private int PickupScalePct = 200; // y — phóng to pickup/coin/heart (drop) (%)
        private int HudIconSize = 20;  // z — kích thước icon HUD (góc trên trái), nếu muốn to hơn thì tăng số này

        private static int ScalePx(int px, int pct) => (int)Math.Round(px * pct / 100.0);

        private bool _specialInFlight = false;


        // ===== Audio — throttle & state =====
        private int _lastGreenLockTick = -999999;
        private const int GREEN_LOCK_COOLDOWN_MS = 220;
        private bool _wasOverheated = false;


        // ======= Boss HUD (vẽ GDI+) =======
        private bool _showBossHud = false;
        private int _bossHp = 0, _bossHpMax = 0;
        private string _bossName = string.Empty;

        private const int SCALE2 = 2; // x2 size cho ship + enemy + boss

        // ===== Missile sprite =====
        private Image _missileSprite;


        // ===== Background (parallax) =====
        private Image _bgFar, _bgMid, _bgNear;
        private float _bgOffFar = 0f, _bgOffMid = 0f, _bgOffNear = 0f;
        private float _bgSpdFar = -100f, _bgSpdMid = -100f, _bgSpdNear = -100f;

        // ===== Player ship =====
        private Image _shipImg;

        // ===== Enemy & Boss sprites =====
        private Image[] _enemySprites;   // theo Enemy.TypeIndex: 0..3
        private Image[] _bossSprites;    // theo _bossTypeIndex: 0..2

        // ===== Default Explosion spritesheet (fallback) =====
        private Image _explosionSheet;
        private const int EXP_FRAME_W = 64, EXP_FRAME_H = 64, EXP_FRAMES = 16;
        private const int EXP_LIFE_MS = 180;

        // ===== HUD icons =====
        private Image _icoCoin, _icoHeart, _icoHeat;

        // ===== Bullet sprites (player) =====
        private Image _bulletRed_10x14, _bulletRed_12x16;
        private Image _bulletYellow_6x12;
        private Image _bulletGreen_6x12;
        private Image _bulletBlue_8x14, _bulletBlue_12x18, _bulletBlueBeam_12x60;

        // ===== Pickup sprites (drops) =====
        private Image _pickupColorRed_18, _pickupColorYellow_18, _pickupColorGreen_18, _pickupColorBlue_18;
        private Image _pickupUpgrade_18;

        // ====== DI ======
        private readonly Player _player;
        private readonly IPlayerService _playerService;
        private readonly IGameService _gameService;
        private readonly ISettingsService _settingsService;

        // ====== Game State ======
        private enum GameState { Loading, Playing, Paused, BossFight, GameOver, Countdown }
        private GameState _state = GameState.Loading;
        private int _sessionId = 0;

        // Score/Coins
        private int _score = 0;
        private int _walletCoins = 0;



        // ====== Player ======
        private Rectangle _ship = new Rectangle(260, 540, 36 * SCALE2, 36 * SCALE2);
        private Point _mousePos;
        private bool _mouseDown = false;
        private int _hp = 5, _hpMax = 8;


        // Sprites
        private Image _pickupShield_18;   // ảnh item khiêng rơi
        private Image _shieldAura_96;     // ảnh vòng chắn vẽ quanh máy bay

        // Shield state
        private bool _shieldOn = false;
        private int _shieldHitsLeft = 0;
        private int _shieldExpireTick = 0;
        private const int SHIELD_DURATION_MS = 10_000;
        private const int SHIELD_MAX_HITS = 2;
        // === NEW: Kho khiêng theo từng màn chơi ===
        private int _shieldStock = 0;                 // số khiêng đang có (không giới hạn)
        private const int SHIELD_FLASH_REMAIN_MS = 2500;   // còn <2.5s thì nháy
        private const int SHIELD_FLASH_INTERVAL_MS = 160;  // tần số nhấp nháy (ms)

        // ====== Overheat (chuột trái) ======
        private int _fireIntervalMs = 160, _baseFireIntervalMs = 160, _lastFireTick = -999999;
        private int _heat = 0, _heatMax = 110, _coolPerStep = 1, _recoverThres = 45;
        private bool _overheated = false;

        // ====== Special missile (Space) ======
        private int _specialCooldownMs = 2500;
        private int _lastSpecialTick = -999999;
        private const int SPECIAL_COST = 20;

        // ====== Weapon (màu × cấp) ======
        private enum WeaponColor { Yellow, Blue, Green, Red }
        private WeaponColor _weaponColor = WeaponColor.Yellow;
        private int _weaponLevel = 1; // 1..3

        bool foundAnyColor = false;

        // ====== Bullet owner ======
        private enum BulletOwner { Player, Enemy, Boss }



        // ====== Session / Coins-Only-Increment ======
        private int _coinsEarnedSession = 0; // chỉ cộng khi NHẬN coin trong phiên
        private bool _sessionActive = false;
        private bool _ledgerFlushed = false;


        // ====== Explosion types (NEW) ======
        private enum ExplosionType { EnemyDeath, BossDeath, MissileHit, RedAoE, DamageOnly }

        private void AddDamageOnlyAoE(PointF pos, float radius, int dmg, int lifeMs = 80)
        {
            _explosions.Add(new Explosion
            {
                Pos = pos,
                Radius = radius,
                Dmg = dmg,
                HasAppliedDamage = false,
                LifeMs = lifeMs,
                TotalLifeMs = lifeMs,
                Type = ExplosionType.DamageOnly,
                FrameCount = 0,
                FrameW = 0,
                FrameH = 0,
                DrawScale = 1f,
                UseZoom = false,
                ZoomBox = 0f
            });
        }

        private const float MISSILE_VFX_ZOOMBOX = 100f * 5.0f; // khớp AddExplosionVfx(MissileHit) kích cỡ vụ nổ tên lửa
        private const int MISSILE_AOE_DMG = 5;            // sát thương AoE ẩn (tùy chỉnh) dame tên lửa
        private static float MissileVisualRadius() => MISSILE_VFX_ZOOMBOX * 0.5f;


        private static void DrawImageRotated(Graphics g, Image img, Rectangle dst, float angleDeg)
        {
            var state = g.Save();
            try
            {
                // tâm của viên đạn
                float cx = dst.X + dst.Width / 2f;
                float cy = dst.Y + dst.Height / 2f;

                g.TranslateTransform(cx, cy);
                g.RotateTransform(angleDeg); // GDI+ xoay thuận chiều kim đồng hồ
                g.TranslateTransform(-cx, -cy);

                g.DrawImage(img, dst);
            }
            finally { g.Restore(state); }
        }


        // ====== Bullets ======
        private struct Bullet
        {
            public float X, Y;
            public int W, H;
            public float Vx, Vy;
            public int Dmg;
            public BulletOwner Owner;
            public int PierceLeft;
            public bool Homing;
            public float Lerp;
            public Color ColorHint;
            public bool IsMissile;

            // BLUE-only
            public HashSet<int> HitIds;

            // --- GREEN homing limits ---
            public float HomingRange;       // px – chỉ homing trong bán kính này
            public float HomingFovDeg;      // độ – nón góc phía trước (ví dụ 70°)
            public int HomingExpireTick;    // ms (Environment.TickCount) – hết giờ thì ngừng homing
            public float InitDx, InitDy;    // hướng gốc khi sinh ra (chuẩn hoá)

            public Sfx.ISfxHandle ShotSfx;  // handle âm thanh riêng cho viên đạn (dùng cho Green)

        }


        private readonly List<Bullet> _bullets = new List<Bullet>();

        // ====== Explosions (VFX & AoE) ======
        private struct Explosion
        {
            public PointF Pos;
            public float Radius;
            public int Dmg;
            public int LifeMs;
            public bool HasAppliedDamage;

            // VFX
            public ExplosionType Type;
            public int FrameCount;
            public int FrameW, FrameH;
            public int TotalLifeMs;
            public float DrawScale;

            // NEW: Zoom-to-fit (đảm bảo thấy toàn bộ frame)
            public bool UseZoom;      // bật/tắt zoom-to-fit
            public float ZoomBox;     // kích thước hộp đích (px) – cạnh dài nhất của frame sau khi fit


        }
        private readonly List<Explosion> _explosions = new List<Explosion>();

        // ====== Items ======
        private enum DropType { Heal, Upgrade, ColorYellow, ColorBlue, ColorGreen, ColorRed, Coin, Shield }

        private struct Drop
        {
            public Rectangle R;
            public DropType Type;
            public int Vy;
            public int Value;
        }
        private readonly List<Drop> _drops = new List<Drop>();
        private readonly Random _rng = new Random();

        // ====== Enemy archetypes ======
        private enum EnemyGunPattern { Straight, Spread2, Spread3, Burst }

        private struct EnemyArchetype
        {
            public string Name;
            public Size Size;
            public int HP;
            public int SpeedYMin, SpeedYMax;
            public int FireCdMin, FireCdMax;
            public int BulletSpeed;
            public EnemyGunPattern Pattern;
            public Color BodyColor;
            public Color BulletColor;
            public double FireChance;
        }
        private EnemyArchetype[] _enemyTypes;

        // ====== Enemy state ======

        private enum MovementStyle
        {
            Stationary,     // đứng yên tại vị trí hover
            DriftEase,      // trôi êm giữa 2 mốc (ease-in-out)
            SineBob,        // sin ngang + nhún dọc rất nhỏ
            LaneStrafe,     // đổi làn mượt (5 làn)
            EllipsePatrol,  // tuần tra elip nhỏ quanh anchor
            TwoStopHover,   // dừng 2–3 trạm
            NoiseSmoothed,  // nhiễu ngẫu nhiên có lọc (mượt)
            ColumnSweep     // quét cột (dao động dọc vừa phải)
        }

        private enum EnemyState { GlideToHover, Hovering, Descending }
        private struct Enemy
        {
            public int Id;
            public Rectangle R;
            public int HP;
            public EnemyState State;
            public int HoverTimeMs;
            public int HoverTargetMs;
            public int FireCooldownMs;
            public int LastFireTick;
            public int SpeedY;
            public int TypeIndex;
            public int TargetY;

            // ===== Movement (NEW) =====
            // ===== Movement (NEW/UPDATED) =====
            public MovementStyle Style;

            public PointF Anchor;         // mốc hover (vào hover là chốt)
            public PointF Anchor2;        // mốc phụ cho DriftEase (điểm còn lại)

            public float AmpX, AmpY;      // biên độ (Sine+Bob / Ellipse / Column wiggle)
            public float Phase;           // pha sin

            // Pha lerp (DriftEase / LaneStrafe / TwoStop / ColumnSweep)
            public int MoveStartTick, MoveEndTick, MoveDurMs;
            public PointF MoveFrom, MoveTo;

            // Dừng nghỉ (LaneStrafe / TwoStopHover)
            public int DwellUntilTick;    // thời điểm hết dừng (ms) khi đã tới nơi

            // LaneStrafe
            public int LaneIndex, LaneCount;

            // Noise (giữ vận tốc cố định 1 khoảng)
            public float FixedVx, FixedVy;
            public int LastChangeTick, ChangeEveryMs;

            // ColumnSweep (quét cả cột)
            public float SweepTopY, SweepBottomY; // ranh dọc
            public bool SweepForward;             // hướng (đi xuống hay đi lên)


            // NoiseSmoothed
            public float SmVx, SmVy;      // vận tốc mượt đã lọc
        }


        private int _enemyIdSeed = 0;   // cấp phát Id tăng dần


        private readonly List<Enemy> _enemies = new List<Enemy>();

        // ====== Boss archetypes ======
        private enum BossPattern { WideSpread, TripleStream, Ring }
        private struct BossArchetype
        {
            public string Name;
            public Size Size;
            public int HPBase;
            public int FireCd;
            public int BulletSpeed;
            public BossPattern Pattern;
            public Color BodyColor;
            public Color[] BulletPalette;
        }
        private BossArchetype[] _bossTypes;

        private int _waveIndex = 0;
        private bool _bossSpawned = false;
        private Enemy _boss;
        private bool _bossAlive = false;
        private int _bossTypeIndex = 0;

        // ====== Loop ======
        private readonly Timer _loop = new Timer { Interval = 16 }; // ~60fps

        // ====== Pickup lock ======
        private int _pickupLockUntilTick = -999999;
        private const int PICKUP_LOCK_MS = 140;

        // ====== Enemy cap ======
        private const int ENEMY_MAX_ON_SCREEN = 14;
        private const int ENEMY_MIN_GAP = 18 * 3; //SCALE2

        // ====== Resume countdown (vẽ GDI+) ======
        private readonly Timer _countdownTimer = new Timer { Interval = 1000 };
        private int _countdownLeft = 0;

        // ====== Explosion sprites per type (NEW) ======
        private Image _expEnemySheet, _expBossSheet, _expMissileSheet, _expRedSheet;

        public GameForm(Player player,
                        IPlayerService playerService,
                        IGameService gameService,
                        ISettingsService settingsService)
        {


            InitializeComponent();
            DoubleBuffered = true;
            KeyPreview = true;
            Cursor = Cursors.Cross;
            _countdownTimer.Tick -= CountdownTimer_Tick;
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Interval = 1000;

            _player = player ?? throw new ArgumentNullException(nameof(player));
            _playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
            _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            SeedArchetypes();

            Load += async (_, __) => await InitAsync();
            FormClosed += async (_, __) => await CleanupAsync();

            _loop.Tick += (s, e) =>
            {
                if (_state == GameState.Playing || _state == GameState.BossFight) Step();
                Invalidate();
            };



            // Input
            KeyDown += GameForm_KeyDown;
            MouseMove += (s, e) => _mousePos = e.Location;
            MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { _mouseDown = true; TryFire(); } };
            MouseUp += (s, e) => { if (e.Button == MouseButtons.Left) _mouseDown = false; };

            // Countdown
            
            //_countdownTimer.Tick += (_, __) =>
            //{
            //    Sfx.Play("ui_countdown_tick.wav", 0.7f);
            //    if (_countdownLeft <= 1)
            //    {
            //        _countdownTimer.Stop();
            //        _state = _bossAlive ? GameState.BossFight : GameState.Playing;
            //        return;
            //    }
            //    _countdownLeft--;
            //};
        }



        private void SeedArchetypes()
        {
            _enemyTypes = new[]
            {
                new EnemyArchetype { Name="Scout",   Size=new Size(24,24),  HP=3, SpeedYMin=3, SpeedYMax=5, FireCdMin=1200, FireCdMax=1800, BulletSpeed=7, Pattern=EnemyGunPattern.Straight, BodyColor=Color.OrangeRed,   BulletColor=Color.Purple, FireChance=0.25 },
                new EnemyArchetype { Name="Striker", Size=new Size(28,28),  HP=4, SpeedYMin=3, SpeedYMax=5, FireCdMin=1500, FireCdMax=2100, BulletSpeed=7, Pattern=EnemyGunPattern.Spread2,  BodyColor=Color.LimeGreen,  BulletColor=Color.Purple, FireChance=0.20 },
                new EnemyArchetype { Name="Bulky",   Size=new Size(32,32),  HP=6, SpeedYMin=2, SpeedYMax=4, FireCdMin=1700, FireCdMax=2400, BulletSpeed=6, Pattern=EnemyGunPattern.Spread3,  BodyColor=Color.DeepSkyBlue,BulletColor=Color.Purple, FireChance=0.18 },
                new EnemyArchetype { Name="Burst",   Size=new Size(26,26),  HP=4, SpeedYMin=3, SpeedYMax=5, FireCdMin=1800, FireCdMax=2600, BulletSpeed=8, Pattern=EnemyGunPattern.Burst,   BodyColor=Color.Gold,       BulletColor=Color.Purple, FireChance=0.16 }
            };
            _bossTypes = new[]
            {
                new BossArchetype { Name="Hydra",    Size=new Size(90,64),  HPBase=70, FireCd=500, BulletSpeed=9,  Pattern=BossPattern.WideSpread,  BodyColor=Color.Purple,          BulletPalette=new []{ Color.Cyan, Color.Magenta, Color.Yellow } },
                new BossArchetype { Name="Behemoth", Size=new Size(110,72), HPBase=90, FireCd=450, BulletSpeed=8,  Pattern=BossPattern.TripleStream,BodyColor=Color.DarkRed,         BulletPalette=new []{ Color.Orange, Color.LightGreen, Color.LightBlue } },
                new BossArchetype { Name="Azura",    Size=new Size(100,70), HPBase=80, FireCd=420, BulletSpeed=10, Pattern=BossPattern.Ring,        BodyColor=Color.MediumSlateBlue, BulletPalette=new []{ Color.White, Color.SkyBlue, Color.Violet } }
            };

            // Scale up enemy/boss
            for (int i = 0; i < _enemyTypes.Length; i++)
            {
                var t = _enemyTypes[i];
                t.Size = new Size(t.Size.Width * 3, t.Size.Height * 3);
                _enemyTypes[i] = t;
            }
            for (int i = 0; i < _bossTypes.Length; i++)
            {
                var t = _bossTypes[i];
                t.Size = new Size(t.Size.Width * 3, t.Size.Height * 3);
                _bossTypes[i] = t;
            }
        }

        // ====== Init / Reset ======
        private async Task InitAsync()
        {
            try
            {
                _specialCooldownMs = await _settingsService.GetMissileCooldownAsync();
                _baseFireIntervalMs = Math.Max(80, Math.Min(400, _specialCooldownMs / 10));
            }
            catch { _specialCooldownMs = 2500; _baseFireIntervalMs = 160; }

            try { var latest = await _playerService.LoginOrCreateAsync(_player.Username); _walletCoins = latest.Coins; }
            catch { _walletCoins = _player.Coins; }

            ResetWeaponToBaseRandom();

            await StartNewSessionAndWave1Async();

            _state = GameState.Playing;
            AudioHub.PlayStageBgm();
            _loop.Start();
        }

        private void AddCoinsNow(int delta)
        {
            if (delta <= 0) return; // không cộng âm vào earned-session
            _walletCoins += delta;   // ví hiện tại (DB) – logic sẵn có của bạn
            _coinsEarnedSession += delta; // chỉ cộng dồn để hiển thị lifetime
        }

        private async Task StartNewSessionAndWave1Async()
        {
            _sessionActive = true;
            _ledgerFlushed = false;
            _coinsEarnedSession = 0;
            _planeLevel = 1;
            _sessionId = await _gameService.StartSessionAsync(_player.Id);

            _score = 0;
            _hp = _hpMax;
            _heat = 0; _overheated = false;
            StopAllBulletSfx(); _drops.Clear(); _explosions.Clear();
            _enemies.Clear(); _bossAlive = false; _bossSpawned = false;
            _shieldStock = 0;

            ResetWeaponToBaseRandom();

            _showBossHud = false;
            _bossHp = _bossHpMax = 0;
            _bossName = string.Empty;

            UpdateHud();

            _waveIndex = 0;
            StartNextWaveImmediate();
        }

        private void ResetWeaponToBaseRandom()
        {
            _weaponColor = (WeaponColor)_rng.Next(0, 4);
            AudioHub.StopBlueBeamLoop();
            _weaponLevel = 1;
            ApplyWeaponTuning();
        }

        private async Task CleanupAsync()
        {
            if (_state != GameState.GameOver && _sessionId > 0)
                await DoGameOverAsync(true);
        }

        // ====== Waves / Boss ======
        private void StartNextWaveImmediate()
        {
            Sfx.Play("sfx_wave_start.wav", 0.85f);
            _waveIndex++;
            SpawnWave(_waveIndex);
            _bossSpawned = false;
            _bossAlive = false;
        }

        private void SpawnWave(int index)
        {
            _enemies.Clear();

            int baseCount = 6 + (index - 1) * 3;
            baseCount = Math.Max(4, baseCount);

            int canAdd = Math.Max(0, ENEMY_MAX_ON_SCREEN - _enemies.Count);
            int count = Math.Min(baseCount, canAdd);

            int hoverMs = 20_000;

            for (int i = 0; i < count; i++)
            {
                int tries = 0;
                int typeIdx = _rng.Next(0, _enemyTypes.Length);
                var t = _enemyTypes[typeIdx];

                int w = t.Size.Width, h = t.Size.Height;

                int targetY = _rng.Next(30, Math.Max(32, ClientSize.Height / 3));

                Rectangle r;
                do
                {
                    int x = _rng.Next(0, Math.Max(1, ClientSize.Width - w));
                    int y = -h - _rng.Next(0, 60);
                    r = new Rectangle(x, y, w, h);
                    tries++;
                }
                while (tries < 40 && _enemies.Any(e => Inflated(e.R, ENEMY_MIN_GAP).IntersectsWith(Inflated(r, ENEMY_MIN_GAP))));

                var e = new Enemy
                {
                    Id = ++_enemyIdSeed,
                    R = r,
                    HP = t.HP + index / 3,
                    State = EnemyState.GlideToHover,
                    HoverTimeMs = 0,
                    HoverTargetMs = hoverMs + _rng.Next(-1500, 1500),
                    FireCooldownMs = _rng.Next(t.FireCdMin, t.FireCdMax),
                    LastFireTick = Environment.TickCount + _rng.Next(0, 600),
                    SpeedY = _rng.Next(t.SpeedYMin, t.SpeedYMax),
                    TypeIndex = typeIdx,
                    TargetY = targetY
                    


                };
                // Chọn ngẫu nhiên tỉ lệ bằng nhau giữa 8 kiểu (kể cả đứng yên)
                e.Style = (MovementStyle)_rng.Next(0, 8); // 0..7

                _enemies.Add(e);
            }
        }

        private void SetupHoverMovement(ref Enemy e)
        {
            int now = Environment.TickCount;

            // ANCHOR = đúng vị trí khi vừa vào hover (giữ nguyên "ngay tại ấy")
            e.Anchor = new PointF(e.R.X, e.R.Y);

            // Biên an toàn màn hình
            float leftBound = 0f;
            float rightBound = Math.Max(0, ClientSize.Width - e.R.Width);
            float topBound = 0f;
            float botBound = Math.Max(0, ClientSize.Height - e.R.Height);

            // Khoảng trống còn lại tính từ anchor để không vượt biên
            float roomLeft = Math.Max(0f, e.Anchor.X - leftBound);
            float roomRight = Math.Max(0f, rightBound - e.Anchor.X);
            float roomUp = Math.Max(0f, e.Anchor.Y - topBound);
            float roomDown = Math.Max(0f, botBound - e.Anchor.Y);

            switch (e.Style)
            {
                case MovementStyle.Stationary:
                    // Đứng yên tại Anchor
                    break;

                case MovementStyle.DriftEase:
                    {
                        // Tâm gốc = đúng vị trí vừa vào hover (Anchor giữ nguyên)
                        // (cx không dùng nữa, có thể bỏ)
                        // float cx = e.Anchor.X + e.R.Width / 2f;

                        // Biên trái/phải khả dụng tính từ vị trí hiện tại
                        float lb = 0f; // left bound
                        float rb = Math.Max(0, ClientSize.Width - e.R.Width); // right bound

                        float rmL = Math.Max(0f, (e.R.X - lb));   // room left
                        float rmR = Math.Max(0f, (rb - e.R.X)); // room right

                        // Khoảng lệch trái/phải nho nhỏ, không vượt biên
                        float offL = Math.Min(rmL, RangeF(_rng, 30f, 80f));
                        float offR = Math.Min(rmR, RangeF(_rng, 30f, 80f));

                        // Đi qua lại quanh VỊ TRÍ HIỆN TẠI
                        e.MoveFrom = new PointF(e.R.X - offL, e.R.Y);
                        e.MoveTo = new PointF(e.R.X + offR, e.R.Y);

                        e.MoveDurMs = RangeInt(_rng, 1600, 2400);
                        e.MoveStartTick = now;
                        e.MoveEndTick = now + (int)(e.MoveDurMs / SeedEnemy);

                        // Không đụng tới e.Anchor / e.Anchor2 nữa để tránh nhảy
                    }
                    break;



                case MovementStyle.SineBob:
                    {
                        // Lướt sin quanh ANCHOR: AmpX/AmpY không vượt biên quanh anchor
                        e.AmpX = Math.Max(6f, Math.Min(roomLeft, roomRight) * 0.9f);
                        // Giới hạn dọc mặc định chỉ ở 1/3 màn hình phía trên, nhưng tính quanh anchor
                        float topBand = topBound;
                        float botBand = Math.Min(botBound, ClientSize.Height / 3f);
                        float upRoom = Math.Max(0f, e.Anchor.Y - topBand);
                        float dnRoom = Math.Max(0f, botBand - e.Anchor.Y);
                        e.AmpY = Math.Max(4f, Math.Min(upRoom, dnRoom) * 0.9f);

                        e.Phase = RangeF(_rng, 0f, (float)(Math.PI * 2));
                    }
                    break;

                case MovementStyle.LaneStrafe:
                    {
                        // Đổi làn nhưng xuất phát từ vị trí hiện tại
                        e.LaneCount = 5;
                        int laneW = Math.Max(1, ClientSize.Width / e.LaneCount);
                        int cx = e.R.X + e.R.Width / 2;
                        e.LaneIndex = ClampI(cx / laneW, 0, e.LaneCount - 1);

                        int targetLane;
                        do { targetLane = RangeInt(_rng, 0, e.LaneCount - 1); }
                        while (targetLane == e.LaneIndex);

                        float laneX = targetLane * laneW + (laneW - e.R.Width) / 2f;

                        e.MoveFrom = new PointF(e.R.X, e.R.Y);
                        e.MoveTo = new PointF(ClampF(laneX, leftBound, rightBound), e.R.Y);

                        float dist = Math.Abs(e.MoveTo.X - e.MoveFrom.X);
                        float speed = 90f * SeedEnemy;
                        e.MoveDurMs = (int)Math.Max(600, (dist / Math.Max(10f, speed)) * 1000f);
                        e.MoveStartTick = now;
                        e.MoveEndTick = now + e.MoveDurMs;

                        e.DwellUntilTick = e.MoveEndTick + RangeInt(_rng, 3000, 5000);
                        e.LaneIndex = targetLane;
                    }
                    break;

                case MovementStyle.EllipsePatrol:
                    {
                        // Anchor đã là vị trí hiện tại (đặt ở đầu hàm)
                        // Tính room hai bên quanh anchor để không vượt biên
                        float lb = 0f, rb = Math.Max(0, ClientSize.Width - e.R.Width);
                        float tb = 0f, bb = Math.Max(0, ClientSize.Height - e.R.Height);

                        float roomL = Math.Max(0f, e.Anchor.X - lb);
                        float roomR = Math.Max(0f, rb - e.Anchor.X);
                        float roomU = Math.Max(0f, e.Anchor.Y - tb);
                        float roomD = Math.Max(0f, bb - e.Anchor.Y);

                        // Biên độ MỤC TIÊU (đủ rộng nhưng không vượt biên)
                        float tgtAx = Math.Max(10f, Math.Min(roomL, roomR) * RangeF(_rng, 0.6f, 0.9f));
                        float tgtAy = Math.Max(8f, Math.Min(roomU, roomD) * RangeF(_rng, 0.6f, 0.9f));

                        e.AmpX = tgtAx;   // lưu biên độ mục tiêu
                        e.AmpY = tgtAy;

                        // Pha ngẫu nhiên để mỗi con lệch nhịp chút cho sinh động
                        e.Phase = RangeF(_rng, 0f, (float)(Math.PI * 2));

                        // KHÔNG đổi e.R.X/Y ở đây → không bật vị trí
                    }
                    break;


                case MovementStyle.TwoStopHover:
                    {
                        // Điểm dừng gần ANCHOR (bán kính nhỏ), không “dịch tâm”
                        float dx = RangeF(_rng, -Math.Min(110f, roomLeft), Math.Min(110f, roomRight));
                        float dy = RangeF(_rng, -Math.Min(28f, roomUp), Math.Min(28f, roomDown));
                        PointF stopA = new PointF(e.Anchor.X + dx, e.Anchor.Y + dy);

                        e.MoveFrom = new PointF(e.R.X, e.R.Y);
                        e.MoveTo = new PointF(ClampF(stopA.X, leftBound, rightBound),
                                                ClampF(stopA.Y, topBound, botBound));

                        float dist = (float)Math.Sqrt((e.MoveTo.X - e.MoveFrom.X) * (e.MoveTo.X - e.MoveFrom.X)
                                                      + (e.MoveTo.Y - e.MoveFrom.Y) * (e.MoveTo.Y - e.MoveFrom.Y));
                        float speed = 70f * SeedEnemy;
                        e.MoveDurMs = (int)Math.Max(800, (dist / Math.Max(8f, speed)) * 1000f);
                        e.MoveStartTick = now;
                        e.MoveEndTick = now + e.MoveDurMs;
                        e.DwellUntilTick = e.MoveEndTick + RangeInt(_rng, 1000, 3000);
                    }
                    break;

                case MovementStyle.NoiseSmoothed:
                    {
                        // Vx/Vy giữ trong 2–4s, nhưng ràng buộc biên tương đối quanh anchor (±40px dọc)
                        e.FixedVx = RangeF(_rng, -45f, 45f) * SeedEnemy;
                        e.FixedVy = RangeF(_rng, -18f, 18f) * SeedEnemy;
                        e.ChangeEveryMs = RangeInt(_rng, 2000, 4000);
                        e.LastChangeTick = now;
                    }
                    break;

                case MovementStyle.ColumnSweep:
                    {
                        // Quét dọc quanh X hiện tại; không đẩy lên giữa; giới hạn top/bottom theo anchor
                        float marginTop = 8f, marginBottom = 60f;
                        float topY = topBound + marginTop;
                        float botY = botBound - marginBottom;

                        // Không “bắt” anchor vào 1/3 trên; quét trong [topY .. botY] nhưng vẫn tôn trọng anchor
                        e.SweepTopY = Math.Min(e.Anchor.Y, topY);
                        e.SweepBottomY = Math.Max(e.Anchor.Y, botY);
                        // Nếu anchor nằm giữa, cứ dùng dải [topY..botY]
                        e.SweepTopY = topY;
                        e.SweepBottomY = botY;

                        e.SweepBottomY = Math.Max(e.SweepTopY + 40f, e.SweepBottomY);

                        e.SweepForward = true;

                        e.MoveFrom = new PointF(e.R.X, e.R.Y);
                        e.MoveTo = new PointF(e.Anchor.X, e.SweepBottomY);

                        float dist = Math.Abs(e.MoveTo.Y - e.MoveFrom.Y);
                        float speed = 110f * SeedEnemy;
                        e.MoveDurMs = (int)Math.Max(1200, (dist / Math.Max(12f, speed)) * 1000f);
                        e.MoveStartTick = now;
                        e.MoveEndTick = now + e.MoveDurMs;

                        e.AmpX = RangeF(_rng, 6f, 12f);
                        e.Phase = RangeF(_rng, 0f, (float)(Math.PI * 2));
                    }
                    break;
            }
        }



        private void UpdateHoverMovement(ref Enemy e)
        {
            int now = Environment.TickCount;
            float dt = _loop.Interval / 1000f;

            // Nếu vừa vào Hover (< 1 tick), giữ nguyên vị trí hiện tại
            if (e.HoverTimeMs <= _loop.Interval)
            {
                e.R.X = (int)e.R.X;
                e.R.Y = (int)e.R.Y;
                // không return; vẫn cho style khác update nhẹ nếu cần
            }


            switch (e.Style)
            {
                case MovementStyle.Stationary:
                    e.R.X = (int)e.Anchor.X;
                    e.R.Y = (int)e.Anchor.Y;
                    break;

                case MovementStyle.DriftEase:
                    {
                        float denom = Math.Max(1, e.MoveEndTick - e.MoveStartTick);
                        float t = SmoothStep01((now - e.MoveStartTick) / denom);

                        float nx = Lerp(e.MoveFrom.X, e.MoveTo.X, t);
                        float ny = e.MoveFrom.Y; // giữ Y
                        e.R.X = (int)ClampF(nx, 0, ClientSize.Width - e.R.Width);
                        e.R.Y = (int)ClampF(ny, 0, ClientSize.Height - e.R.Height);

                        if (now >= e.MoveEndTick)
                        {
                            // Đảo chiều qua lại quanh vị trí hiện tại
                            var cur = new PointF(e.R.X, e.R.Y);
                            var span = Math.Abs(e.MoveTo.X - e.MoveFrom.X);

                            // đổi hướng: nếu MoveTo ở bên phải cur thì đẩy sang trái và ngược lại
                            bool toRight = e.MoveTo.X > e.MoveFrom.X;
                            e.MoveFrom = cur;
                            e.MoveTo = new PointF(cur.X + (toRight ? -span : +span), cur.Y);

                            e.MoveDurMs = RangeInt(_rng, 1600, 2400);
                            e.MoveStartTick = now;
                            e.MoveEndTick = now + (int)(e.MoveDurMs / SeedEnemy);
                        }
                    }
                    break;



                case MovementStyle.SineBob:
                    {
                        // tần số thấp → chuyển động gợn sóng êm; SeedEnemy ↑ => nhịp nhanh hơn
                        float wx = 0.6f * SeedEnemy;   // rad/s cho trục X (lướt dài)
                        float wy = 0.9f * SeedEnemy;   // rad/s cho trục Y (nhịp dọc hơi nhanh hơn chút)
                        e.Phase += wx * dt;

                        float nx = e.Anchor.X + (float)Math.Sin(e.Phase) * e.AmpX;
                        float ny = e.Anchor.Y + (float)Math.Sin(e.Phase * (wy / wx)) * e.AmpY;

                        e.R.X = (int)ClampF(nx, 0, ClientSize.Width - e.R.Width);
                        e.R.Y = (int)ClampF(ny, 0, ClientSize.Height - e.R.Height);
                    }
                    break;


                case MovementStyle.LaneStrafe:
                    {
                        if (now < e.MoveEndTick)
                        {
                            float t = SmoothStep01((now - e.MoveStartTick) / (float)Math.Max(1, e.MoveEndTick - e.MoveStartTick));
                            float nx = Lerp(e.MoveFrom.X, e.MoveTo.X, t);
                            e.R.X = (int)ClampF(nx, 0, ClientSize.Width - e.R.Width);
                            e.R.Y = (int)e.MoveFrom.Y;
                        }
                        else
                        {
                            // Đang dừng trên làn
                            e.R.X = (int)e.MoveTo.X;
                            e.R.Y = (int)e.MoveTo.Y;

                            if (now >= e.DwellUntilTick)
                            {
                                // Chọn làn hợp lệ mới (khác làn hiện tại)
                                int laneW = Math.Max(1, ClientSize.Width / Math.Max(1, e.LaneCount));
                                int targetLane;
                                do { targetLane = RangeInt(_rng, 0, e.LaneCount - 1); }
                                while (targetLane == e.LaneIndex);

                                float laneX = targetLane * laneW + (laneW - e.R.Width) / 2f;

                                e.MoveFrom = new PointF(e.R.X, e.R.Y);
                                e.MoveTo = new PointF(ClampF(laneX, 0, ClientSize.Width - e.R.Width), e.R.Y);

                                float dist = Math.Abs(e.MoveTo.X - e.MoveFrom.X);
                                float speed = 90f * SeedEnemy; // chậm
                                e.MoveDurMs = (int)Math.Max(600, (dist / Math.Max(10f, speed)) * 1000f);

                                e.MoveStartTick = now;
                                e.MoveEndTick = now + e.MoveDurMs;

                                // dừng 3–5s ở làn mới
                                e.DwellUntilTick = e.MoveEndTick + RangeInt(_rng, 3000, 5000);
                                e.LaneIndex = targetLane;
                            }
                        }
                    }
                    break;

                case MovementStyle.EllipsePatrol:
                    {
                        // Nhịp quay chậm để quỹ đạo lớn vẫn mượt
                        float w = 0.8f * SeedEnemy;
                        e.Phase += w * dt;

                        // Ramp-in biên độ 0→1 trong ~400ms đầu
                        const float GROW_MS = 400f;
                        float grow = SmoothStep01(Math.Min(1f, e.HoverTimeMs / GROW_MS));

                        float ax = e.AmpX * grow;
                        float ay = e.AmpY * grow;

                        float nx = e.Anchor.X + (float)Math.Cos(e.Phase) * ax;
                        float ny = e.Anchor.Y + (float)Math.Sin(e.Phase) * ay;

                        e.R.X = (int)ClampF(nx, 0, ClientSize.Width - e.R.Width);
                        e.R.Y = (int)ClampF(ny, 0, ClientSize.Height - e.R.Height);
                    }
                    break;



                case MovementStyle.TwoStopHover:
                    {
                        if (now < e.MoveEndTick)
                        {
                            float t = SmoothStep01((now - e.MoveStartTick) / (float)Math.Max(1, e.MoveEndTick - e.MoveStartTick));
                            float nx = Lerp(e.MoveFrom.X, e.MoveTo.X, t);
                            float ny = Lerp(e.MoveFrom.Y, e.MoveTo.Y, t);
                            e.R.X = (int)ClampF(nx, 0, ClientSize.Width - e.R.Width);
                            e.R.Y = (int)ClampF(ny, 0, ClientSize.Height - e.R.Height);
                        }
                        else
                        {
                            // Dừng tại trạm 1–3s
                            e.R.X = (int)e.MoveTo.X;
                            e.R.Y = (int)e.MoveTo.Y;

                            if (now >= e.DwellUntilTick)
                            {
                                // Chọn trạm mới quanh Anchor
                                PointF nxt = new PointF(
                                    ClampF(e.Anchor.X + RangeF(_rng, -110f, +110f), 0, ClientSize.Width - e.R.Width),
                                    ClampF(e.Anchor.Y + RangeF(_rng, -28f, +28f), 0, ClientSize.Height - e.R.Height)
                                );
                                e.MoveFrom = new PointF(e.R.X, e.R.Y);
                                e.MoveTo = nxt;

                                float dist = (float)Math.Sqrt((e.MoveTo.X - e.MoveFrom.X) * (e.MoveTo.X - e.MoveFrom.X)
                                                            + (e.MoveTo.Y - e.MoveFrom.Y) * (e.MoveTo.Y - e.MoveFrom.Y));
                                float speed = 70f * SeedEnemy; // chậm rãi
                                e.MoveDurMs = (int)Math.Max(800, (dist / Math.Max(8f, speed)) * 1000f);

                                e.MoveStartTick = now;
                                e.MoveEndTick = now + e.MoveDurMs;
                                e.DwellUntilTick = e.MoveEndTick + RangeInt(_rng, 1000, 3000);
                            }
                        }
                    }
                    break;

                case MovementStyle.NoiseSmoothed:
                    {
                        // Giữ vận tốc cố định 2–4s rồi đổi
                        if (now - e.LastChangeTick >= e.ChangeEveryMs)
                        {
                            e.FixedVx = RangeF(_rng, -45f, 45f) * SeedEnemy;
                            e.FixedVy = RangeF(_rng, -18f, 18f) * SeedEnemy;
                            e.ChangeEveryMs = RangeInt(_rng, 2000, 4000);
                            e.LastChangeTick = now;
                        }

                        float nx = e.R.X + e.FixedVx * dt;
                        float ny = e.R.Y + e.FixedVy * dt;

                        // giữ kề Anchor theo trục dọc (±40px)
                        ny = ClampF(ny, e.Anchor.Y - 40f, e.Anchor.Y + 40f);

                        e.R.X = (int)ClampF(nx, 0, ClientSize.Width - e.R.Width);
                        e.R.Y = (int)ClampF(ny, 0, ClientSize.Height - e.R.Height);
                    }
                    break;

                case MovementStyle.ColumnSweep:
                    {
                        // rung ngang rất nhẹ
                        e.Phase += (0.8f * SeedEnemy) * dt;
                        float wiggleX = (float)Math.Sin(e.Phase * 1.3f) * e.AmpX;

                        if (now < e.MoveEndTick)
                        {
                            float t = SmoothStep01((now - e.MoveStartTick) / (float)Math.Max(1, e.MoveEndTick - e.MoveStartTick));
                            float ny = Lerp(e.MoveFrom.Y, e.MoveTo.Y, t);
                            float nx = e.Anchor.X + wiggleX;

                            e.R.X = (int)ClampF(nx, 0, ClientSize.Width - e.R.Width);
                            e.R.Y = (int)ClampF(ny, e.SweepTopY, e.SweepBottomY);
                        }
                        else
                        {
                            // Đảo chiều: quét cả cột (có thể xuống gần vùng người chơi)
                            e.SweepForward = !e.SweepForward;
                            float nextY = e.SweepForward ? e.SweepBottomY : e.SweepTopY;

                            e.MoveFrom = new PointF(e.R.X, e.R.Y);
                            e.MoveTo = new PointF(e.Anchor.X, nextY);

                            float dist = Math.Abs(e.MoveTo.Y - e.MoveFrom.Y);
                            float speed = 110f * SeedEnemy; // chậm vừa
                            e.MoveDurMs = (int)Math.Max(1200, (dist / Math.Max(12f, speed)) * 1000f);

                            e.MoveStartTick = now;
                            e.MoveEndTick = now + e.MoveDurMs;
                        }
                    }
                    break;
            }
        }



        private static Rectangle Inflated(Rectangle r, int pad) => Rectangle.Inflate(r, pad, pad);

        private void SpawnBoss()
        {
            _bossTypeIndex = _rng.Next(0, _bossTypes.Length);
            var bt = _bossTypes[_bossTypeIndex];

            int w = bt.Size.Width, h = bt.Size.Height;
            int x = (ClientSize.Width - w) / 2, y = 40;
            _boss = new Enemy
            {
                Id = ++_enemyIdSeed,
                R = new Rectangle(x, y, w, h),
                HP = bt.HPBase + _waveIndex * 10,
                State = EnemyState.Hovering,
                HoverTargetMs = 25_000,
                HoverTimeMs = 0,
                FireCooldownMs = bt.FireCd,
                LastFireTick = Environment.TickCount,
                SpeedY = 4,
                TypeIndex = -1,
                TargetY = y
            };

            _bossAlive = true;
            Sfx.Play("sfx_boss_warning.wav", 0.9f);
            AudioHub.PlayBossBgm();
            _showBossHud = true;
            _bossName = bt.Name;
            _bossHpMax = _boss.HP;
            _bossHp = _boss.HP;
        }

        private void StopAllBulletSfx()
        {
            for (int i = 0; i < _bullets.Count; i++)
                StopBulletSfx(_bullets[i]);
            _bullets.Clear();
        }


        // ====== Main Step ======
        private void Step()
        {
            // Parallax scroll
            float dt = _loop.Interval / 1000f;
            if (_bgFar != null) { _bgOffFar += _bgSpdFar * dt; if (_bgOffFar >= _bgFar.Height) _bgOffFar -= _bgFar.Height; }
            if (_bgMid != null) { _bgOffMid += _bgSpdMid * dt; if (_bgOffMid >= _bgMid.Height) _bgOffMid -= _bgMid.Height; }
            if (_bgNear != null) { _bgOffNear += _bgSpdNear * dt; if (_bgOffNear >= _bgNear.Height) _bgOffNear -= _bgNear.Height; }

            // Move by mouse
            _ship.X = Math.Max(0, Math.Min(ClientSize.Width - _ship.Width, _mousePos.X - _ship.Width / 2));
            _ship.Y = Math.Max(0, Math.Min(ClientSize.Height - _ship.Height, _mousePos.Y - _ship.Height / 2));
            if (_mouseDown) TryFire();

            // Enemies tick
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                var e = _enemies[i];
                var t = _enemyTypes[e.TypeIndex];

                if (e.State == EnemyState.GlideToHover)
                {
                    int glideSpeed = Math.Max(2, e.SpeedY);
                    e.R.Y += glideSpeed;
                    if (e.R.Y >= e.TargetY)
                    {
                        e.R.Y = e.TargetY;
                        e.State = EnemyState.Hovering;
                        e.HoverTimeMs = 0;

                        // >>> ADD: setup chuyển động theo style
                        SetupHoverMovement(ref e);
                    }
                }
                else if (e.State == EnemyState.Hovering)
                {
                    e.HoverTimeMs += _loop.Interval;

                    //int dx = (int)(Math.Sin(Environment.TickCount / 220.0 + i) * 1.8);
                    //e.R.X = Math.Max(0, Math.Min(ClientSize.Width - e.R.Width, e.R.X + dx));
                    UpdateHoverMovement(ref e);


                    if (Environment.TickCount - e.LastFireTick >= e.FireCooldownMs)
                    {
                        e.LastFireTick = Environment.TickCount;
                        if (_rng.NextDouble() < t.FireChance) EnemyFire(e, t);
                        e.FireCooldownMs = _rng.Next(t.FireCdMin, t.FireCdMax);
                    }
                    if (e.HoverTimeMs >= e.HoverTargetMs) e.State = EnemyState.Descending;
                }
                else
                {
                    e.R.Y += e.SpeedY + 1;
                    if (e.R.Top > ClientSize.Height) { _enemies.RemoveAt(i); continue; }
                }

                _enemies[i] = e;
            }

            // Boss spawn mỗi 4 wave
            if (!_bossSpawned && (_waveIndex % 4 == 0) && _enemies.Count == 0)
            {
                _bossSpawned = true;
                SpawnBoss();
                _state = GameState.BossFight;
            }

            // Boss tick
            if (_bossAlive)
            {
                var bt = _bossTypes[_bossTypeIndex];
                int dx = (int)(Math.Sin(Environment.TickCount / 350.0) * 3.5);
                _boss.R.X = Math.Max(0, Math.Min(ClientSize.Width - _boss.R.Width, _boss.R.X + dx));
                if (Environment.TickCount - _boss.LastFireTick >= _boss.FireCooldownMs)
                {
                    _boss.LastFireTick = Environment.TickCount;
                    BossFire(_boss, bt);
                }
            }

            // Bullets move + homing
            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                var b = _bullets[i];

                if (b.Owner == BulletOwner.Player && b.Homing)
                {
                    // Hết thời gian homing?
                    if (Environment.TickCount > b.HomingExpireTick)
                    {
                        b.Homing = false;
                    }
                    else
                    {
                        var target = FindNearestEnemyCenter(new Point((int)b.X, (int)b.Y));
                        if (target.HasValue)
                        {
                            // Vector hướng đến mục tiêu
                            float tx = target.Value.X - (b.X + b.W / 2f);
                            float ty = target.Value.Y - (b.Y + b.H / 2f);
                            float dist = (float)Math.Sqrt(tx * tx + ty * ty);

                            // 1) Phải trong RANGE
                            if (dist <= b.HomingRange && dist > 0.0001f)
                            {
                                tx /= dist; ty /= dist;

                                // 2) Phải ở PHÍA TRƯỚC (đạn bắn lên: địch phải nằm phía trên)
                                bool ahead = (ty < 0f);

                                // 3) Phải trong FOV quanh hướng ban đầu
                                // cos(theta) = dot(init, targetDir)
                                float cosAng = b.InitDx * tx + b.InitDy * ty; // init: (0,-1) => cosAng ~1 khi cùng hướng
                                                                              // FOV/2 vì ta xét lệch 2 bên
                                float maxAngleRad = (b.HomingFovDeg * 0.5f) * (float)Math.PI / 180f;
                                float cosMax = (float)Math.Cos(maxAngleRad);

                                if (ahead && cosAng >= cosMax)
                                {
                                    // LOCK HINT cho Green homing (throttle toàn cục 220ms)
                                    if (b.ColorHint == Color.Lime) // chỉ xanh lá
                                    {
                                        int nowTick = Environment.TickCount;
                                        if (nowTick - _lastGreenLockTick >= GREEN_LOCK_COOLDOWN_MS)
                                        {
                                            _lastGreenLockTick = nowTick;
                                            Sfx.Play("sfx_green_lock.wav", 0.40f); // rất nhỏ
                                        }
                                    }

                                    // Cho phép homing (giữ nguyên công thức cũ)
                                    var spd = (float)Math.Sqrt(b.Vx * b.Vx + b.Vy * b.Vy);
                                    if (spd <= 0) spd = 12f;
                                    float cx = b.Vx / spd, cy = b.Vy / spd;

                                    cx = cx + (tx - cx) * b.Lerp;
                                    cy = cy + (ty - cy) * b.Lerp;

                                    var n = (float)Math.Sqrt(cx * cx + cy * cy);
                                    if (n > 0) { cx /= n; cy /= n; }
                                    b.Vx = cx * spd;
                                    b.Vy = cy * spd;
                                }
                                else
                                {
                                    // Mục tiêu ngoài FOV hoặc không ở phía trước -> KHÔNG homing lần này
                                    // (tuỳ chọn) nếu bạn muốn tắt homing luôn khi ra ngoài nón:
                                    // b.Homing = false;
                                }
                            }
                            else
                            {
                                // Ngoài RANGE -> không homing (có thể tắt hẳn nếu muốn)
                                // b.Homing = false;
                            }
                        }
                        else
                        {
                            // Không có mục tiêu -> giữ hướng bay, tiếp tục chờ đến khi hết hạn
                            // (tuỳ chọn) b.Homing = false; // nếu muốn bỏ homing ngay khi không tìm được gì
                        }
                    }
                }


                b.X += b.Vx;
                b.Y += b.Vy;

                if (b.Y + b.H < 0 || b.Y > ClientSize.Height || b.X + b.W < 0 || b.X > ClientSize.Width)
                {
                    StopBulletSfx(b);
                    _bullets.RemoveAt(i); 
                    continue; }

                _bullets[i] = b;
            }

            // Collisions: player bullets vs enemies/boss
            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                var b = _bullets[i];
                if (b.Owner != BulletOwner.Player) continue;

                bool consumed = false;

                // vs enemies
                for (int j = _enemies.Count - 1; j >= 0; j--)
                {
                    var e = _enemies[j];
                    if (!ToRect(b).IntersectsWith(e.R)) continue;

                    // --- Only-once damage per target for BLUE bullets ---
                    bool isBlue = (b.ColorHint == Color.DeepSkyBlue);
                    if (isBlue)
                    {
                        if (b.HitIds == null) b.HitIds = new HashSet<int>();
                        if (b.HitIds.Contains(e.Id))
                        {
                            // Đạn xanh đã gây dame enemy này rồi -> bỏ qua frame này
                            _bullets[i] = b; // ghi lại nếu đã gán HitIds ở trên
                            continue;        // không trừ HP, không consume
                        }
                        b.HitIds.Add(e.Id);  // đánh dấu đã trúng
                        _bullets[i] = b;     // ghi lại vì Bullet là struct
                    }

                    // Tiếp tục như cũ
                    e.HP -= b.Dmg;

                    // MissileHit VFX khi là tên lửa
                    // MissileHit VFX khi là tên lửa
                    if (b.IsMissile)
                    {
                        var mp = new PointF(
                            ClampF(b.X + b.W / 2f, e.R.Left, e.R.Right),
                            ClampF(b.Y + b.H / 2f, e.R.Top, e.R.Bottom));
                        AddExplosionVfx(ExplosionType.MissileHit, mp);
                        AddDamageOnlyAoE(mp, MissileVisualRadius(), MISSILE_AOE_DMG);

                    }

                    // >>> NEW: Đạn đỏ – luôn có VFX nổ khi TRÚNG (dù chưa chết)
                    // Đạn đỏ: nổ 1 vụ nổ (VFX + AoE) NGAY KHI TRÚNG Enemy
                    if (!b.IsMissile && _weaponColor == WeaponColor.Red)
                    {
                        var rp = new PointF(
                            ClampF(b.X + b.W / 2f, e.R.Left, e.R.Right),
                            ClampF(b.Y + b.H / 2f, e.R.Top, e.R.Bottom));
                        AddRedHitAoE(rp);
                        Sfx.Play("sfx_red_hit_pop.wav", 0.7f);

                    }




                    // nếu chết → VFX EnemyDeath + (nếu đạn Đỏ) AoE
                    if (e.HP <= 0)
                    {
                        Sfx.Play("sfx_enemy_explosion_small.wav", 0.85f);
                        var ep = new PointF(e.R.X + e.R.Width / 2f, e.R.Y + e.R.Height / 2f);
                        AddExplosionVfx(ExplosionType.EnemyDeath, ep);

                        //if (_weaponColor == WeaponColor.Red)
                        //{
                        //    float radius = (_weaponLevel == 1) ? 40f : (_weaponLevel == 2 ? 60f : 75f);
                        //    int dmg = (_weaponLevel == 3 ? 4 : (_weaponLevel == 2 ? 3 : 2));
                        //    AddRedAoE(ep, radius, dmg, EXP_LIFE_MS);
                        //}

                        _score += 100;
                        SpawnRandomDrop(e.R.Location, false);
                        StopBulletSfx(b);
                        _enemies.RemoveAt(j);
                    }
                    else
                    {
                        _enemies[j] = e;
                    }

                    // tiêu thụ viên đạn (Blue có xuyên)
                    if (_weaponColor == WeaponColor.Blue && b.PierceLeft > 0)
                    { b.PierceLeft--; consumed = false; _bullets[i] = b; }
                    else consumed = true;

                    break;
                }

                // vs boss
                if (!consumed && _bossAlive && ToRect(b).IntersectsWith(_boss.R))
                {
                    bool isBlue = (b.ColorHint == Color.DeepSkyBlue);
                    if (isBlue)
                    {
                        if (b.HitIds == null) b.HitIds = new HashSet<int>();
                        if (b.HitIds.Contains(_boss.Id))
                        {
                            _bullets[i] = b;
                            // không consume; beam/đạn vẫn bay, chỉ không gây dame lặp
                            continue;
                        }
                        b.HitIds.Add(_boss.Id);
                        _bullets[i] = b;
                    }

                    // rồi mới:
                    _boss.HP -= b.Dmg;
                    SetBossHpBar(_boss.HP);
                    Sfx.Play("sfx_impact_boss.wav", 0.6f);

                    // MissileHit VFX khi là tên lửa
                    if (b.IsMissile)
                    {
                        var mp = new PointF(
                            ClampF(b.X + b.W / 2f, _boss.R.Left, _boss.R.Right),
                            ClampF(b.Y + b.H / 2f, _boss.R.Top, _boss.R.Bottom));

                        AddExplosionVfx(ExplosionType.MissileHit, mp);
                        AddDamageOnlyAoE(mp, MissileVisualRadius(), MISSILE_AOE_DMG);

                    }

                    // >>> NEW: Đạn đỏ – VFX khi trúng boss
                    // Đạn đỏ: nổ 1 vụ nổ (VFX + AoE) NGAY KHI TRÚNG Boss
                    if (!b.IsMissile && _weaponColor == WeaponColor.Red)
                    {
                        var rp = new PointF(
                            ClampF(b.X + b.W / 2f, _boss.R.Left, _boss.R.Right),
                            ClampF(b.Y + b.H / 2f, _boss.R.Top, _boss.R.Bottom));
                        AddRedHitAoE(rp);
                        Sfx.Play("sfx_red_hit_pop.wav", 0.7f);
                    }



                    if (_boss.HP <= 0)
                    {
                        Sfx.Play("sfx_boss_explosion_big.wav", 1.0f);
                        var bp = new PointF(_boss.R.X + _boss.R.Width / 2f, _boss.R.Y + _boss.R.Height / 2f);
                        // BossDeath VFX
                        AddExplosionVfx(ExplosionType.BossDeath, bp);

                        // Đạn đỏ AoE
                        //if (_weaponColor == WeaponColor.Red)
                        //{
                        //    float radius = (_weaponLevel == 1) ? 40f : (_weaponLevel == 2 ? 60f : 75f);
                        //    int dmg = (_weaponLevel == 3 ? 4 : (_weaponLevel == 2 ? 3 : 2));
                        //    AddRedAoE(bp, radius, dmg, EXP_LIFE_MS);
                        //}

                        _bossAlive = false;
                        _showBossHud = false;
                        _score += 2000;
                        SpawnRandomDrop(new Point(_boss.R.X + _boss.R.Width / 2, _boss.R.Y + _boss.R.Height / 2), true);

                        // >>> THÊM DÒNG NÀY: ngừng BGM boss và mở lại BGM stage
                        AudioHub.PlayStageBgm();

                    }

                    if (_weaponColor == WeaponColor.Blue && b.PierceLeft > 0)
                    { b.PierceLeft--; consumed = false; _bullets[i] = b; }
                    else consumed = true;
                }

                if (consumed) { StopBulletSfx(b); _bullets.RemoveAt(i); continue; }
            }

            // Enemy/Boss bullets vs player
            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                var b = _bullets[i];
                if (b.Owner == BulletOwner.Player) continue;
                if (ToRect(b).IntersectsWith(_ship)) { _bullets.RemoveAt(i); TakeDamage(1); }
            }

            // Enemy body vs player
            foreach (var e in _enemies)
                if (e.R.IntersectsWith(_ship)) { TakeDamage(1); break; }
            if (_bossAlive && _boss.R.IntersectsWith(_ship)) TakeDamage(2);

            // Explosions tick (AoE đỏ áp dam 1 lần)
            for (int k = _explosions.Count - 1; k >= 0; k--)
            {
                var ex = _explosions[k];

                if (!ex.HasAppliedDamage && ex.Radius > 0 && ex.Dmg > 0
                    && (ex.Type == ExplosionType.RedAoE || ex.Type == ExplosionType.DamageOnly))
                {
                    // enemies
                    for (int j = _enemies.Count - 1; j >= 0; j--)
                    {
                        var en = _enemies[j];
                        var c = new PointF(en.R.X + en.R.Width / 2f, en.R.Y + en.R.Height / 2f);
                        if (Distance(c, ex.Pos) <= ex.Radius)
                        {
                            en.HP -= ex.Dmg;
                            if (en.HP <= 0)
                            {
                                var ep = new PointF(en.R.X + en.R.Width / 2f, en.R.Y + en.R.Height / 2f);
                                AddExplosionVfx(ExplosionType.EnemyDeath, ep);

                                _score += 100;
                                SpawnRandomDrop(en.R.Location, false);
                                _enemies.RemoveAt(j);
                                continue;
                            }
                            else _enemies[j] = en;
                        }
                    }
                    // boss
                    if (_bossAlive)
                    {
                        var bc = new PointF(_boss.R.X + _boss.R.Width / 2f, _boss.R.Y + _boss.R.Height / 2f);
                        if (Distance(bc, ex.Pos) <= ex.Radius)
                        {
                            _boss.HP -= ex.Dmg;
                            SetBossHpBar(_boss.HP);
                            if (_boss.HP <= 0)
                            {
                                var bp = new PointF(_boss.R.X + _boss.R.Width / 2f, _boss.R.Y + _boss.R.Height / 2f);
                                AddExplosionVfx(ExplosionType.BossDeath, bp);

                                _bossAlive = false;
                                _showBossHud = false;
                                _score += 2000;
                                SpawnRandomDrop(new Point(_boss.R.X + _boss.R.Width / 2, _boss.R.Y + _boss.R.Height / 2), true);
                            }
                        }
                    }

                    ex.HasAppliedDamage = true;
                }

                ex.LifeMs -= _loop.Interval;
                if (ex.LifeMs <= 0) _explosions.RemoveAt(k); else _explosions[k] = ex;
            }

            // Drops move & pick
            ProcessDrops();

            // Cool down & HUD
            CoolDownStep();
            TickShield();
            UpdateHud();

            // Wave → next
            if (!_bossAlive && _enemies.Count == 0 && _state != GameState.GameOver)
            {
                if ((_waveIndex % 4 == 0) && !_bossSpawned)
                {
                    _bossSpawned = true;
                    SpawnBoss();
                    _state = GameState.BossFight;
                }
                else
                {
                    Sfx.Play("sfx_wave_clear.wav", 0.85f);
                    StartNextWaveImmediate();
                    _state = GameState.Playing;
                }
            }
        }

        private static Rectangle ToRect(Bullet b) => new Rectangle((int)b.X, (int)b.Y, b.W, b.H);

        // ====== Enemy/Boss fire ======
        private void EnemyFire(Enemy e, EnemyArchetype t)
        {
            int cx = e.R.X + e.R.Width / 2;
            int y = e.R.Bottom;

            Action<float, float, float, float> Add = (offX, offY, vx, vy) =>
            {
                var r = new Rectangle((int)(cx + offX) - 3, (int)(y + offY), 6, 10);
                _bullets.Add(new Bullet { X = r.X, Y = r.Y, W = r.Width, H = r.Height, Vx = vx, Vy = vy, Dmg = 1, Owner = BulletOwner.Enemy, ColorHint = t.BulletColor });
            };

            switch (t.Pattern)
            {
                case EnemyGunPattern.Straight:
                    Add(0, 0, 0, +t.BulletSpeed);
                    break;
                case EnemyGunPattern.Spread2:
                    Add(-8, 0, -2, +t.BulletSpeed);
                    Add(+8, 0, +2, +t.BulletSpeed);
                    break;
                case EnemyGunPattern.Spread3:
                    Add(0, 0, 0, +t.BulletSpeed);
                    Add(-8, 0, -2.2f, +t.BulletSpeed);
                    Add(+8, 0, +2.2f, +t.BulletSpeed);
                    break;
                case EnemyGunPattern.Burst:
                    for (int k = -1; k <= 1; k++)
                        Add(k * 6, 0, k * 1.6f, +t.BulletSpeed + 1);
                    break;
            }
        }

        private void BossFire(Enemy b, BossArchetype t)
        {
            int cx = b.R.X + b.R.Width / 2;
            int y = b.R.Bottom;

            Func<Color> PickColor = () => t.BulletPalette[_rng.Next(0, t.BulletPalette.Length)];

            Action<float, float, float, float, Color> Add = (offX, offY, vx, vy, c) =>
            {
                var r = new Rectangle((int)(cx + offX) - 4, (int)(y + offY), 8, 12);
                _bullets.Add(new Bullet { X = r.X, Y = r.Y, W = r.Width, H = r.Height, Vx = vx, Vy = vy, Dmg = 1, Owner = BulletOwner.Boss, ColorHint = c });
            };

            switch (t.Pattern)
            {
                case BossPattern.WideSpread:
                    for (int i = -3; i <= 3; i++)
                        Add(i * 10, 0, i * 1.8f, +t.BulletSpeed, PickColor());
                    break;
                case BossPattern.TripleStream:
                    for (int lane = -1; lane <= 1; lane++)
                        Add(lane * 16, 0, lane * 0.8f, t.BulletSpeed + 1, PickColor());
                    break;
                case BossPattern.Ring:
                    int n = 12;
                    float spd = t.BulletSpeed - 2;
                    for (int k = 0; k < n; k++)
                    {
                        double ang = (Math.PI * 2) * (k / (double)n);
                        float vx = (float)Math.Cos(ang) * spd;
                        float vy = (float)Math.Sin(ang) * spd * 0.8f + 2.5f;
                        Add(0, 0, vx, vy, PickColor());
                    }
                    break;
            }
        }

        // ====== Weapon fire ======
        private void TryFire()
        {
            if (!(_state == GameState.Playing || _state == GameState.BossFight)) return;
            if (_overheated) return;
            int now = Environment.TickCount;
            if (now - _lastFireTick < _fireIntervalMs) return;
            _lastFireTick = now;

            switch (_weaponColor)
            {
                case WeaponColor.Yellow: FireYellow(); Sfx.Play("sfx_shot_yellow.wav", 0.7f); HeatShot(8); break;
                case WeaponColor.Blue: FireBlue(); Sfx.Play("sfx_shot_blue.wav", 0.6f); HeatShot(10); break;
                case WeaponColor.Green: FireGreen();  HeatShot(10); break;
                case WeaponColor.Red: FireRed(); Sfx.Play("sfx_shot_red.wav", 0.8f); HeatShot(10); break;
            }
        }

        private void FireYellow()
        {
            int tier = WeaponTier;
            int cx = _ship.X + _ship.Width / 2;

            int x = 1;
            // Chọn size theo tier
            int w = 2 * x, h = 4 * x;           // Lv1
            if (tier == 2) { w = 4 * x; h = 8 * x; }
            else if (tier >= 3) { w = 6 * x; h = 12 * x; }

            float speed = 12f;

            if (tier == 1)
            {
                // bắn 1 viên thẳng – dùng Ang để vẫn có góc (xoay = 0°)
                AddPlayerBulletAng(cx, _ship.Y, w, h, 0f, speed, BoostDmg(1));
            }
            else if (tier == 2)
            {
                // 3 viên: -12°, 0°, +12°
                AddPlayerBulletAng(cx, _ship.Y, w, h, -15f, speed, BoostDmg(1));
                AddPlayerBulletAng(cx, _ship.Y, w, h, 0f, speed, BoostDmg(1));
                AddPlayerBulletAng(cx, _ship.Y, w, h, +15f, speed, BoostDmg(1));
            }
            else // tier 3+
            {
                // 5 viên: -30°, -15°, 0°, +15°, +30°
                AddPlayerBulletAng(cx, _ship.Y, w, h, -30f, speed, BoostDmg(1));
                AddPlayerBulletAng(cx, _ship.Y, w, h, -15f, speed, BoostDmg(1));
                AddPlayerBulletAng(cx, _ship.Y, w, h, 0f, speed, BoostDmg(1));
                AddPlayerBulletAng(cx, _ship.Y, w, h, +15f, speed, BoostDmg(1));
                AddPlayerBulletAng(cx, _ship.Y, w, h, +30f, speed, BoostDmg(1));
            }
        }



        private void FireBlue()
        {
            int tier = WeaponTier;
            int cx = _ship.X + _ship.Width / 2, y = _ship.Y - 18;

            if (tier == 1)
            {
                var b = AddPlayerBullet(cx - 4, y, 8, 14, 0, -13, BoostDmg(2));
                b.PierceLeft = 1; // hoặc 3 ở tier 2 như cũ
                b.HitIds = new HashSet<int>();             // <-- NEW
                CommitLast(b);
                b.PierceLeft = 1; CommitLast(b);
            }
            else if (tier == 2)
            {
                var b = AddPlayerBullet(cx - 6, y, 12, 18, 0, -13, BoostDmg(3));
                b.PierceLeft = 1; // hoặc 3 ở tier 2 như cũ
                b.HitIds = new HashSet<int>();             // <-- NEW
                CommitLast(b);
                b.PierceLeft = 3; CommitLast(b);
            }
            else // tier 3+
            {
                int bw = ScalePx(12, BulletScalePct);
                int bh = ScalePx(60, BulletScalePct);
                var b = new Bullet
                {
                    HitIds = new HashSet<int>(),              // <-- NEW
                    X = cx - bw / 2,
                    Y = _ship.Y - bh,
                    W = bw,
                    H = bh,
                    Vx = 0,
                    Vy = -18,
                    Dmg = BoostDmg(2),
                    PierceLeft = 9999,
                    Owner = BulletOwner.Player,
                    ColorHint = Color.DeepSkyBlue
                };
                AudioHub.StartBlueBeamLoop();
                _bullets.Add(b);

            }
        }


        private void FireGreen()
        {
            int tier = WeaponTier;

            int count = tier; // 1..3 viên
            float speed = (tier == 1) ? 11f : (tier == 2 ? 12.5f : 14f);
            int baseDmg = (tier == 1) ? 1 : (tier == 2 ? 2 : 3);
            float lerp = (tier == 1) ? 0.12f : (tier == 2 ? 0.15f : 0.18f);

            // Kích thước thật sau scale để canh spawn và tính gap
            int sw = ScalePx(6, BulletScalePct);
            int sh = ScalePx(12, BulletScalePct);

            // --- Thông số homing theo cấp ---
            float homingRange, homingFovDeg; int homingExpireMs;
            if (tier == 1) { homingRange = 300f; homingFovDeg = 60f; homingExpireMs = 1000; }
            else if (tier == 2) { homingRange = 500f; homingFovDeg = 80f; homingExpireMs = 2000; }
            else { homingRange = 700f; homingFovDeg = 100f; homingExpireMs = 3000; } // nếu bạn muốn đúng “6700” thì đổi 700f -> 6700f

            // Khoảng cách ngang tối thiểu giữa các tia (≥ bề rộng viên + lề)
            int gap = Math.Max(10, sw + 6);

            for (int i = 0; i < count; i++)
            {
                int off;
                if (count == 1) off = 0;
                else if (count == 2) off = (i == 0 ? -gap : +gap);
                else /* count == 3 */  off = (i == 0 ? -gap : (i == 1 ? 0 : +gap));

                int spawnX = _ship.X + _ship.Width / 2 + off - sw / 2;
                int spawnY = _ship.Y - sh;

                var b = AddPlayerBullet(spawnX, spawnY, 6, 12, 0, -speed, BoostDmg(baseDmg), Color.Lime);
                b.Homing = true;
                b.Lerp = lerp;

                // Giới hạn homing theo yêu cầu
                b.HomingRange = homingRange;
                b.HomingFovDeg = homingFovDeg;
                b.HomingExpireTick = Environment.TickCount + homingExpireMs;
                b.InitDx = 0f; b.InitDy = -1f; // hướng ban đầu (thẳng lên)
                b.ShotSfx = Sfx.PlayScoped("sfx_shot_green.wav", 0.75f);
                CommitLast(b);
            }
        }





        private void FireRed()
        {
            int tier = WeaponTier;
            int cx = _ship.X + _ship.Width / 2, y = _ship.Y - 16;

            if (tier == 1)
                AddPlayerBullet(cx - 5, y, 10, 14, 0, -10, BoostDmg(2), Color.OrangeRed);
            else // tier 2,3+
                AddPlayerBullet(cx - 6, y, 12, 16, 0, -11, BoostDmg(3), Color.OrangeRed);
        }


        // ====== Bullet helpers ======

        private static void StopBulletSfx(Bullet b)
        {
            try { b.ShotSfx?.Stop(); } catch { }
            try { b.ShotSfx?.Dispose(); } catch { }
        }


        private void ActivateShield(bool refresh = true)
        {
            _shieldOn = true;
            Sfx.Play("sfx_shield_on.wav", 0.9f);
            _shieldHitsLeft = SHIELD_MAX_HITS;
            _shieldExpireTick = Environment.TickCount + SHIELD_DURATION_MS;
        }


        private void TickShield()
        {
            if (!_shieldOn) return;
            if (Environment.TickCount >= _shieldExpireTick)
            {
                Sfx.Play("sfx_shield_off.wav", 0.8f);
                _shieldOn = false;
                _shieldHitsLeft = 0;
            }
        }


        private Image GetPlayerBulletSprite(Bullet b)
        {
            // MISSILE ưu tiên riêng
            if (b.IsMissile && _missileSprite != null)
                return _missileSprite;

            // Nhận diện beam xanh dương bằng tỉ lệ
            if (b.ColorHint == Color.DeepSkyBlue)
            {
                if (b.H >= b.W * 4) return _bulletBlueBeam_12x60; // beam
                                                                  // viên thường: ưu tiên sprite lớn nếu viên "to"
                return (b.W >= ScalePx(11, BulletScalePct) || b.H >= ScalePx(17, BulletScalePct))
                     ? _bulletBlue_12x18
                     : _bulletBlue_8x14;
            }

            if (b.ColorHint == Color.OrangeRed) // Red
            {
                return (b.W >= ScalePx(11, BulletScalePct) || b.H >= ScalePx(15, BulletScalePct))
                     ? _bulletRed_12x16
                     : _bulletRed_10x14;
            }

            if (b.ColorHint == Color.Gold) return _bulletYellow_6x12; // Yellow
            if (b.ColorHint == Color.Lime) return _bulletGreen_6x12;  // Green

            return null;
        }



        private void AddRedHitAoE(PointF pos)
        {


            // ===== Base theo cấp =====
            float baseRadius = (_weaponLevel == 1) ? 40f
                              : (_weaponLevel == 2) ? 60f
                              : 75f;        // bán kính gốc (chưa nhân x)
            int tier = WeaponTier;
            int baseDmg = (tier == 1) ? 2 : (tier == 2 ? 3 : 4);
            int dmg = BoostDmg(baseDmg); // tăng theo cấp >3

            int lifeMs = EXP_LIFE_MS;

            // ===== Sprite-sheet (VFX) =====
            Image sheet = _expRedSheet ?? _explosionSheet;
            int frameW = (sheet == _expRedSheet ? 64 : EXP_FRAME_W);
            int frameH = (sheet == _expRedSheet ? 64 : EXP_FRAME_H);
            int frames = (sheet == _expRedSheet ? 16 : EXP_FRAMES);
            NormalizeExplosionSheet(sheet, ref frameW, ref frameH, ref frames);

            // ===== Núm vặn kích thước hiển thị ===== kích thức vụ nổ đạn đỏ
            float x = 3f; // chỉnh to/nhỏ VFX; AoE sẽ khớp theo VFX

            // VFX target diameter (px) và scale
            float radius = baseRadius * x;     // AoE RADIUS = VFX RADIUS  <<< quan trọng
            float targetPx = radius * 2f;        // đường kính hiển thị mong muốn
            float baseSize = Math.Max(frameW, frameH);
            float levelScale = (baseSize > 0f) ? (targetPx / baseSize) : 1f;

            _explosions.Add(new Explosion
            {
                Pos = pos,

                // AoE (logic) — khớp 1:1 với VFX
                Radius = radius,
                Dmg = dmg,
                HasAppliedDamage = false,

                // Life
                LifeMs = lifeMs,
                TotalLifeMs = lifeMs,

                // VFX (sprite-sheet)
                Type = ExplosionType.RedAoE,
                FrameCount = frames,
                FrameW = frameW,
                FrameH = frameH,
                DrawScale = levelScale,  // vẽ đúng đường kính targetPx
                UseZoom = false,         // không auto-fit
                ZoomBox = 0f
            });
        }




        private static void NormalizeExplosionSheet(Image sheet, ref int frameW, ref int frameH, ref int frames)
        {
            if (sheet == null) return;

            // Nếu không chia hết hoặc frames < 2 → coi như ảnh đơn, vẽ full
            bool notSheet = (frameW <= 0 || frameH <= 0 || frames < 2
                             || (sheet.Width % frameW) != 0
                             || (sheet.Height % frameH) != 0);

            if (notSheet)
            {
                frameW = sheet.Width;
                frameH = sheet.Height;
                frames = 1;
            }
        }


        private static void DrawSpriteFrame(Graphics g, Image sheet, Rectangle src, RectangleF dst)
        {
            // Chặn vượt biên đề phòng sai frame
            if (src.X < 0) src.X = 0;
            if (src.Y < 0) src.Y = 0;
            if (src.X + src.Width > sheet.Width) src.Width = sheet.Width - src.X;
            if (src.Y + src.Height > sheet.Height) src.Height = sheet.Height - src.Y;

            var state = g.Save();
            try
            {
                // Quan trọng: tránh crop mép khi scale
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Default;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

                using (var ia = new ImageAttributes())
                {
                    ia.SetWrapMode(WrapMode.TileFlipXY); // <<< đặt ở đây
                    g.DrawImage(sheet,
                                Rectangle.Round(dst),
                                src.X, src.Y, src.Width, src.Height,
                                GraphicsUnit.Pixel,
                                ia);
                }
            }
            finally { g.Restore(state); }
        }


        private static int ClampI(int v, int min, int max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
        private static float SmoothStep01(float t) { t = ClampF(t, 0f, 1f); return t * t * (3f - 2f * t); }
        private static int RangeInt(Random rng, int a, int b) => a + rng.Next(b - a + 1);
        private static float RangeF(Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);



        private static float ClampF(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        private static float ZoomToFitScale(int frameW, int frameH, float box)
        {
            if (frameW <= 0 || frameH <= 0 || box <= 0) return 1f;
            float sx = box / frameW;
            float sy = box / frameH;
            return Math.Min(sx, sy); // đảm bảo không bị crop
        }


        private Bullet AddPlayerBullet(int x, int y, int w, int h, float vx, float vy, int dmg, Color? c = null)
        {
            // scale hitbox thật
            int sw = ScalePx(w, BulletScalePct);
            int sh = ScalePx(h, BulletScalePct);

            // canh giữa theo tâm hình gốc
            x -= (sw - w) / 2;
            y -= (sh - h) / 2;

            var b = new Bullet
            {
                X = x,
                Y = y,
                W = sw,
                H = sh,
                Vx = vx,
                Vy = vy,
                Dmg = dmg,
                Owner = BulletOwner.Player,
                ColorHint = c ?? (_weaponColor == WeaponColor.Yellow ? Color.Gold :
                                  _weaponColor == WeaponColor.Blue ? Color.DeepSkyBlue :
                                  _weaponColor == WeaponColor.Green ? Color.Lime :
                                                                       Color.OrangeRed),
                IsMissile = false
            };
            _bullets.Add(b);
            return b;
        }

        private Bullet AddPlayerBulletAng(int cx, int cy, int w, int h, float deg, float speed, int dmg)
        {
            // AddPlayerBulletAng(cx, _ship.Y, 6, 12, 0f, 12, BoostDmg(1));
            double rad = deg * Math.PI / 180.0;
            float vx = (float)Math.Sin(rad) * speed;
            float vy = -(float)Math.Cos(rad) * speed;

            int sw = ScalePx(w, BulletScalePct);
            int sh = ScalePx(h, BulletScalePct);

            return AddPlayerBullet(cx - sw / 2, cy - sh, sw, sh, vx, vy, dmg);
        }

        private void CommitLast(Bullet b) { if (_bullets.Count > 0) _bullets[_bullets.Count - 1] = b; }

        private void HeatShot(int heat)
        {
            _heat = Math.Min(_heatMax, _heat + heat);
            if (_heat >= _recoverThres + (_heatMax - _recoverThres))
                _overheated = true;
            if (!_wasOverheated && _overheated) Sfx.Play("sfx_overheat.wav", 1f);
            _wasOverheated = _overheated;
            UpdateHeatHud();
        }

        private void CoolDownStep()
        {
            bool was = _wasOverheated;
            _wasOverheated = _overheated;
            if (was && !_overheated) Sfx.Play("sfx_cooled.wav", 1f);
            if (_heat <= 0) return;
            _heat = Math.Max(0, _heat - _coolPerStep);
            if (_overheated && _heat <= _recoverThres) _overheated = false;
            UpdateHeatHud();
            // Stop beam loop nếu không còn điều kiện giữ
            bool keepBeamLoop = _weaponColor == WeaponColor.Blue && WeaponTier >= 3 && _mouseDown
                                && (_state == GameState.Playing || _state == GameState.BossFight) && !_overheated;
            if (!keepBeamLoop) AudioHub.StopBlueBeamLoop();

        }

        private void UpdateHeatHud() { /* HUD vẽ GDI+ */ }

        private void ApplyWeaponTuning()
        {
            // Dùng TIER (1..3) để quyết định nhịp bắn/heat; >3 coi như 3
            int tier = WeaponTier;
            double rate = 1.0;
            _coolPerStep = 1;
            _heatMax = 120;
            _recoverThres = 50;

            switch (_weaponColor)
            {
                case WeaponColor.Yellow:
                    rate = (tier == 1) ? 1.0 : (tier == 2 ? 0.85 : 0.7);
                    break;
                case WeaponColor.Blue:
                    rate = (tier == 1) ? 1.0 : (tier == 2 ? 0.9 : 0.8);
                    _heatMax = 130;
                    break;
                case WeaponColor.Green:
                    rate = (tier == 1) ? 1.0 : 0.9;
                    _heatMax = 135;
                    break;
                case WeaponColor.Red:
                    rate = 1.0;
                    _heatMax = 130;
                    break;
            }

            _fireIntervalMs = (int)Math.Max(60, _baseFireIntervalMs * rate);
        }


        // ====== Items ======
        private void SpawnRandomDrop(Point from, bool isBoss)
        {
            double baseChance = isBoss ? 0.70 : 0.28;
            if (_rng.NextDouble() > baseChance) return;

            double r = _rng.NextDouble();
            DropType t; int value = 0;

            if (r < 0.45) { t = DropType.Coin; value = _rng.Next(5, isBoss ? 21 : 11); }
            else if (r < 0.80)
            {
                double m = (r - 0.45) / 0.35;
                if (m < 0.25) t = DropType.ColorYellow;
                else if (m < 0.50) t = DropType.ColorBlue;
                else if (m < 0.75) t = DropType.ColorGreen;
                else t = DropType.ColorRed;
            }
            else if (r < 0.95) t = DropType.Upgrade;
            else if (r < 0.97) t = DropType.Heal;
            else t = DropType.Shield; // <<< Khiêng (khoảng 3%)


            int baseSize = 18;
            int sz = ScalePx(baseSize, PickupScalePct);
            _drops.Add(new Drop { R = new Rectangle(from.X, from.Y, sz, sz), Type = t, Vy = 3, Value = value });

        }

        private bool IsSameColorDrop(DropType t)
        {
            return (t == DropType.ColorYellow && _weaponColor == WeaponColor.Yellow)
                || (t == DropType.ColorBlue && _weaponColor == WeaponColor.Blue)
                || (t == DropType.ColorGreen && _weaponColor == WeaponColor.Green)
                || (t == DropType.ColorRed && _weaponColor == WeaponColor.Red);
        }
        private bool IsColorDrop(DropType t)
            => t == DropType.ColorYellow || t == DropType.ColorBlue || t == DropType.ColorGreen || t == DropType.ColorRed;

        private int ScoreDrop(DropType t)
        {
            if (t == DropType.Upgrade) return 100;
            if (IsSameColorDrop(t)) return 80;
            if (IsColorDrop(t)) return 60;
            if (t == DropType.Coin) return 50;
            if (t == DropType.Heal) return 10;
            return 0;
        }

        private void ApplyColorDrop(WeaponColor pickedColor)
        {
            if (pickedColor == _weaponColor)
            {
                // Cùng màu đang dùng: CHỈ tăng cấp đạn, KHÔNG đổi cấp máy bay
                _weaponLevel += 1;
            }
            else
            {
                // Đổi sang màu khác: set cấp đạn = cấp máy bay (không +1)
                _weaponColor = pickedColor;
                _weaponLevel = Math.Max(1, _planeLevel);
            }

            // Làm mát nhẹ nếu đang nóng
            _heat = Math.Min(_heat, _recoverThres);

            ApplyWeaponTuning();
            UpdateHeatHud();
        }



        private async Task<bool> TryAddCoinsServerAsync(int delta)
        {
            try { await _playerService.AddCoinsAsync(_player.Id, delta); return true; }
            catch { return false; }
        }

        private  void ApplyDrop(DropType t, int value)
        {
            switch (t)
            {
                case DropType.Heal: _hp = Math.Min(_hpMax, _hp + 1); Sfx.Play("sfx_pick_heal.wav", 0.8f); break; // hồi 1 máu
                case DropType.Upgrade:
                    _planeLevel += 1;       // máy bay +1
                    _weaponLevel += 1;      // loại đạn hiện tại +1
                    Sfx.Play("sfx_pick_upgrade.wav", 0.9f);
                    break;
                case DropType.ColorYellow: ApplyColorDrop(WeaponColor.Yellow); Sfx.Play("sfx_pick_color.wav", 0.8f); break; 
                case DropType.ColorBlue: ApplyColorDrop(WeaponColor.Blue); Sfx.Play("sfx_pick_color.wav", 0.8f); break; 
                case DropType.ColorGreen: ApplyColorDrop(WeaponColor.Green); Sfx.Play("sfx_pick_color.wav", 0.8f); break; 
                case DropType.ColorRed: ApplyColorDrop(WeaponColor.Red); Sfx.Play("sfx_pick_color.wav", 0.8f); break;
                case DropType.Coin:
                    if (value <= 0) value = 1;
                    AddCoinsNow(value);
                    _coinsEarnedSession += value;   // <<< cộng lifetime earned
                    _ = TryAddCoinsServerAsync(value);
                    Sfx.Play("sfx_pick_coin.wav", 0.8f);
                    break;
                case DropType.Shield:
                    Sfx.Play("sfx_pick_shield.wav", 0.8f);
                    _shieldStock = Math.Max(0, _shieldStock + 1);
                    break;

            }

            ApplyWeaponTuning();
            UpdateHud();
        }

        private void FlushLedgerOnce()
        {
            if (!_sessionActive || _ledgerFlushed) return;

            var earned = _coinsEarnedSession;
            if (earned > 0)
            {
                try
                {
                    CoinsLedgerStore.Append(_player.Username, earned); // chỉ-cộng
                }
                catch { /* nuốt lỗi I/O để không crash */ }
            }

            _ledgerFlushed = true;   // đánh dấu đã xả
            _sessionActive = false;  // phiên đã kết thúc
        }


        // ====== Special missile (Space) ======
        private async void TryFireSpecialAsync()
        {
            if (_specialInFlight) return;
            
            _specialInFlight = true;
            try
            {
                //Sfx.Play("sfx_missile_launch.wav", 0.9f);
                if (!(_state == GameState.Playing || _state == GameState.BossFight)) return;

                int now = Environment.TickCount;
                if (now - _lastSpecialTick < _specialCooldownMs) return;

                if (_walletCoins < SPECIAL_COST) { System.Media.SystemSounds.Beep.Play(); return; }

                var ok = await _playerService.TrySpendCoinsAsync(_player.Id, SPECIAL_COST);
                if (!ok)
                {
                    var latest = await _playerService.LoginOrCreateAsync(_player.Username);
                    _walletCoins = latest.Coins;
                    UpdateHud();
                    return;
                }

                _walletCoins = Math.Max(0, _walletCoins - SPECIAL_COST);
                _lastSpecialTick = now;
                UpdateHud();

                // spawn missiles (đánh dấu IsMissile = true)
                float cx = _ship.X + _ship.Width / 2f;
                float cy = _ship.Y - 12f;

                // core hiển thị tên lửa
                Sfx.Play("sfx_missile_launch.wav", 0.9f);
                AddMissile(cx, cy, 0f, -14f, 15);

                // 2 quả phụ lệch xíu sang trái/phải số lượng tên lửa tùy theo cấp
                //AddMissile(cx - 14f, cy + 8f, -1.2f, -13f, 8);
                //AddMissile(cx + 14f, cy + 8f, +1.2f, -13f, 8);

            }
            finally
            {
                _specialInFlight = false;
            }
        }

        // ====== HUD & Damage / End ======
        private void UpdateHud() { /* HUD vẽ GDI+ */ }

        private void TakeDamage(int dmg)
        {
            if (_shieldOn)
            {
                Sfx.Play("sfx_shield_block.wav", 0.9f);
                _shieldHitsLeft = Math.Max(0, _shieldHitsLeft - 1);
                if (_shieldHitsLeft <= 0) _shieldOn = false;
                return; // KHÔNG trừ HP
            }

            // Không có khiêng → phát tiếng bị trúng đạn
            Sfx.Play("sfx_player_hit.wav", 0.95f);

            _hp = Math.Max(0, _hp - dmg);
            if (_hp <= 0) _ = DoGameOverAsync(false);
        }



        private void SetBossHpBar(int hp)
        {
            _bossHp = Math.Max(0, Math.Min(hp, _bossHpMax));
        }

        private async Task DoGameOverAsync(bool silent)
        {
            //_bullets.Clear();
            if (_state == GameState.GameOver) return;
            _state = GameState.GameOver; FlushLedgerOnce();
            _loop.Stop();
            Sfx.Play("sfx_gameover.wav", 0.9f);
            AudioHub.StopBgm();
            AudioHub.StopBlueBeamLoop();

            bool bossKilled = !_bossAlive && _waveIndex > 0 && _waveIndex % 4 == 0;
            bool noHitBossKill = bossKilled && _hp == _hpMax;

            try {
                // ghi ledger: chỉ cộng, không trừ
                CoinsLedgerStore.Append(_player.Username, _coinsEarnedSession);
                await _gameService.EndSessionAsync(_sessionId, _score, bossKilled, noHitBossKill); 
            }
            catch (Exception ex)
            {
                if (!silent) MessageBox.Show("Lỗi khi kết thúc phiên chơi:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (silent) return;

            var result = MessageBox.Show(
                $"Game Over!\nScore: {_score}\nCoins Wallet: {_walletCoins}\n\nBạn muốn chơi lại?",
                "Kết thúc",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information
            );

            if (result == DialogResult.Yes)
            {
                try
                {
                    _state = GameState.Playing;
                    await StartNewSessionAndWave1Async();
                    _loop.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể bắt đầu ván mới:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                }
            }
            else
            {
                Close();
            }
            FlushLedgerOnce();
        }

        // ====== Pause & Keyboard (không dùng control) ======
        private void TogglePause()
        {
            if (_state == GameState.Playing || _state == GameState.BossFight)
            {
                _state = GameState.Paused;
                Sfx.Play("ui_pause.wav", 0.9f);
                AudioHub.PauseBgm(true);
            }
            else if (_state == GameState.Paused)
            {
                _state = _bossAlive ? GameState.BossFight : GameState.Playing;
                Sfx.Play("ui_pause.wav", 0.9f);
                AudioHub.PauseBgm(false);
            }
        }


        private void StartResumeCountdown()
        {
            if (_state != GameState.Paused) return;

            _state = GameState.Countdown;

            _countdownTimer.Stop();
            _countdownLeft = 4;                         // sentinel

            CountdownTimer_Tick(null, EventArgs.Empty); // nhảy ngay về 3 + phát tick
            _countdownTimer.Start();
        }


        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            _countdownLeft--;                // TRỪ TRƯỚC

            if (_countdownLeft <= 0)         // ĐẾN 0 -> tiếp tục chơi
            {
                _countdownTimer.Stop();
                _state = _bossAlive ? GameState.BossFight : GameState.Playing;

                // Mở lại BGM (nếu trước đó PauseBgm(true))
                AudioHub.PauseBgm(false);

                Invalidate();                // vẽ lại, xóa overlay
                return;
            }

            // Còn số để hiển thị: phát tick cho số mới (2, rồi 1)
            Sfx.Play("ui_countdown_tick.wav", 0.7f);
            Invalidate();                    // vẽ lại số
        }



        private void GameForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            FlushLedgerOnce();
        }

        private void GameForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) { TogglePause(); return; }

            if (_state == GameState.Paused)
            {
                if (e.KeyCode == Keys.Enter) { StartResumeCountdown(); return; }
                if (e.KeyCode == Keys.Q) {
                    FlushLedgerOnce();
                    Close(); return; }
                return;
            }

            if (e.KeyCode == Keys.Space)
            {
                e.Handled = true; e.SuppressKeyPress = true; TryFireSpecialAsync(); return;
            }

            // NEW: dùng khiêng bằng phím S
            if (e.KeyCode == Keys.S)
            {
                e.Handled = true; e.SuppressKeyPress = true;

                // Chỉ bật nếu đang không có khiêng hoạt động và còn hàng trong kho
                if (!_shieldOn && _shieldStock > 0)
                {
                    _shieldStock--;      // trừ kho
                    ActivateShield();    // bật lá chắn
                    UpdateHud();
                }
                return;
            }
        }


        // ====== Drops processing ======
        private void ProcessDrops()
        {
            // 1) Rơi & cull
            for (int i = _drops.Count - 1; i >= 0; i--)
            {
                var d = _drops[i];
                d.R.Y += d.Vy;
                if (d.R.Top > ClientSize.Height) { _drops.RemoveAt(i); continue; }
                _drops[i] = d;
            }

            // 2) Khoá nhặt cho item KHÔNG PHẢI MÀU
            int now = Environment.TickCount;
            if (now < _pickupLockUntilTick) return;

            if (_drops.Count == 0) return;

            // Tâm tàu
            float shipCx = _ship.X + _ship.Width / 2f;
            float shipCy = _ship.Y + _ship.Height / 2f;

            // Cho phép chain nhiều viên MÀU trong 1 frame
            const int COLOR_CHAIN_MAX = 8;   // giới hạn an toàn để tránh vòng lặp vô hạn
            int colorChainCount = 0;
            bool pickedNonColorThisFrame = false;

            // Hàm chọn item tốt nhất đang chạm
            int PickBestTouchingIndex()
            {
                int bestIdx = -1;
                
                bool foundAnyColorDifferent = false;
                double bestDist2ColorDifferent = double.MaxValue;
                double bestDist2ColorSame = double.MaxValue;

                int bestUpgradeIdx = -1, bestCoinIdx = -1, bestHealIdx = -1, bestShieldIdx = -1;
                double bestDist2Upgrade = double.MaxValue, bestDist2Coin = double.MaxValue, bestDist2Heal = double.MaxValue, bestDist2Shield = double.MaxValue;

                for (int i = _drops.Count - 1; i >= 0; i--)
                {
                    var d = _drops[i];
                    if (!d.R.IntersectsWith(_ship)) continue;

                    float cx = d.R.X + d.R.Width / 2f;
                    float cy = d.R.Y + d.R.Height / 2f;
                    double dx = cx - shipCx, dy = cy - shipCy;
                    double dist2 = dx * dx + dy * dy;

                    bool isColor =
                        d.Type == DropType.ColorYellow ||
                        d.Type == DropType.ColorBlue ||
                        d.Type == DropType.ColorGreen ||
                        d.Type == DropType.ColorRed;

                    if (isColor)
                    {
                        foundAnyColor = true;
                        WeaponColor dropColor =
                            d.Type == DropType.ColorYellow ? WeaponColor.Yellow :
                            d.Type == DropType.ColorBlue ? WeaponColor.Blue :
                            d.Type == DropType.ColorGreen ? WeaponColor.Green :
                                                            WeaponColor.Red;

                        bool different = dropColor != _weaponColor;

                        if (different)
                        {
                            foundAnyColorDifferent = true;
                            if (dist2 < bestDist2ColorDifferent)
                            {
                                bestDist2ColorDifferent = dist2;
                                bestIdx = i; // ưu tiên màu khác trước
                            }
                        }
                        else
                        {
                            if (!foundAnyColorDifferent && dist2 < bestDist2ColorSame)
                            {
                                bestDist2ColorSame = dist2;
                                bestIdx = i; // màu trùng (nếu chưa có màu khác)
                            }
                        }
                    }
                    else
                    {
                        // *** THÊM SHIELD Ở NHÓM NON-COLOR ***
                        if (d.Type == DropType.Shield)
                        {
                            if (dist2 < bestDist2Shield) { bestDist2Shield = dist2; bestShieldIdx = i; }
                        }
                        else if (d.Type == DropType.Upgrade)
                        {
                            if (dist2 < bestDist2Upgrade) { bestDist2Upgrade = dist2; bestUpgradeIdx = i; }
                        }
                        else if (d.Type == DropType.Coin)
                        {
                            if (dist2 < bestDist2Coin) { bestDist2Coin = dist2; bestCoinIdx = i; }
                        }
                        else if (d.Type == DropType.Heal)
                        {
                            if (dist2 < bestDist2Heal) { bestDist2Heal = dist2; bestHealIdx = i; }
                        }
                    }
                }

                if (bestIdx != -1) return bestIdx;           // 1) màu (ưu tiên tuyệt đối)
                if (bestShieldIdx != -1) return bestShieldIdx; // 2) khiêng  <<< THÊM DÒNG NÀY
                if (bestUpgradeIdx != -1) return bestUpgradeIdx;
                if (bestCoinIdx != -1) return bestCoinIdx;
                if (bestHealIdx != -1) return bestHealIdx;
                return -1;
            }


            // 3) Nhặt item — lặp để chain màu trong cùng 1 frame
            while (true)
            {
                int idx = PickBestTouchingIndex();
                if (idx == -1) break;

                var pick = _drops[idx];
                bool isColor =
                    pick.Type == DropType.ColorYellow ||
                    pick.Type == DropType.ColorBlue ||
                    pick.Type == DropType.ColorGreen ||
                    pick.Type == DropType.ColorRed;

                // Thực thi nhặt
                ApplyDrop(pick.Type, pick.Value);
                _drops.RemoveAt(idx);

                if (isColor)
                {
                    colorChainCount++;
                    if (colorChainCount >= COLOR_CHAIN_MAX) break; // an toàn
                                                                   // KHÔNG đặt lock hoặc lock cực ngắn cho màu để swap mượt
                                                                   // _pickupLockUntilTick = now + 0; // không lock
                    continue; // thử xem còn màu nào chạm tiếp không
                }
                else
                {
                    // Item không phải màu -> đặt lock chuẩn
                    pickedNonColorThisFrame = true;
                    break;
                }
            }

            // 4) Đặt lock cuối frame
            if (pickedNonColorThisFrame)
                _pickupLockUntilTick = now + PICKUP_LOCK_MS;   // vd: 140ms
            else
                _pickupLockUntilTick = now; // màu: không lock (hoặc đặt 5–10ms nếu bạn muốn rất nhẹ)
        }





        // ====== Tier & OverLevel helpers ======
        // Tier chỉ 1..3: ảnh hưởng "cách bắn"
        // OverLevel = max(0, level - 3): chỉ tăng dame
        private int WeaponTier => Math.Min(_weaponLevel, 3);
        private int OverLevel => Math.Max(0, _weaponLevel - 3);

        private int BoostDmg(int baseDmg)
        {
            // Tăng +1 damage cho mỗi cấp trên 3
            return baseDmg + OverLevel;
        }


        // ====== Helpers ======

        private int _planeLevel = 1;

        private float Distance(PointF a, PointF b)
        { float dx = a.X - b.X, dy = a.Y - b.Y; return (float)Math.Sqrt(dx * dx + dy * dy); }

        private void SpawnShard(Point loc, int vx, int vy)
        {
            var b = new Bullet { X = loc.X, Y = loc.Y, W = 6, H = 6, Vx = vx, Vy = vy, Dmg = 1, Owner = BulletOwner.Player, ColorHint = Color.OrangeRed };
            _bullets.Add(b);
        }

        private Bullet AddMissile(float cx, float cy, float vx, float vy, int dmg)
        {
            int baseW = 4, baseH = 8;
            int w = ScalePx(baseW, MissileScalePct);
            int h = ScalePx(baseH, MissileScalePct);

            var b = new Bullet
            {
                X = cx - w / 2f,
                Y = cy - h / 2f,
                W = w,
                H = h,
                Vx = vx,
                Vy = vy,
                Dmg = dmg,
                Owner = BulletOwner.Player,
                ColorHint = Color.White,
                IsMissile = true,

                // === Homing (mạnh hơn xanh Lv3 theo hệ số) ===
                Homing = true,
                // Tốc độ chuyển hướng (steer) – scale theo hệ số
                Lerp = GREEN3_LERP * MissileHomingFactor,
                // Tầm kiếm mục tiêu – scale theo hệ số
                HomingRange = GREEN3_HOMING_RANGE * MissileHomingFactor,
                // Giữ nguyên FOV của xanh Lv3 (cho ổn định), có thể scale nếu bạn muốn:
                HomingFovDeg = GREEN3_HOMING_FOV_DEG,
                // Thời gian homing (ms) – scale nhẹ theo hệ số
                HomingExpireTick = Environment.TickCount + (int)(GREEN3_HOMING_EXPIRE_MS * MissileHomingFactor),
            };

            // Hướng ban đầu (chuẩn hoá) để kiểm tra “phía trước + FOV”
            float spd = (float)Math.Sqrt(b.Vx * b.Vx + b.Vy * b.Vy);
            if (spd > 0.0001f) { b.InitDx = b.Vx / spd; b.InitDy = b.Vy / spd; }
            else { b.InitDx = 0f; b.InitDy = -1f; }

            _bullets.Add(b);
            return b;
        }



        private PointF? FindNearestEnemyCenter(Point p)
        {
            if (_enemies.Count == 0 && !_bossAlive) return null;
            PointF origin = new PointF(p.X, p.Y);
            PointF? best = null; float bestD = float.MaxValue;
            foreach (var e in _enemies)
            {
                var c = new PointF(e.R.X + e.R.Width / 2f, e.R.Y + e.R.Height / 2f);
                float d = Distance(c, origin); if (d < bestD) { bestD = d; best = c; }
            }
            if (_bossAlive)
            {
                var c = new PointF(_boss.R.X + _boss.R.Width / 2f, _boss.R.Y + _boss.R.Height / 2f);
                float d = Distance(c, origin); if (d < bestD) best = c;
            }
            return best;
        }

        // ====== VFX helpers (NEW) ======
        private void AddExplosionVfx(ExplosionType type, PointF pos)
        {
            Image sheet = null;
            int frameW = 64, frameH = 64, frames = 12, totalLife = 260;
            float scale = 1.0f;
            bool useZoom = true;
            float zoomBox = 96f; // mặc định

            switch (type)
            {
                case ExplosionType.EnemyDeath:
                    sheet = _expEnemySheet ?? _explosionSheet;
                    frameW = (sheet == _expEnemySheet ? 64 : EXP_FRAME_W);
                    frameH = (sheet == _expEnemySheet ? 64 : EXP_FRAME_H);
                    frames = (sheet == _expEnemySheet ? 12 : EXP_FRAMES);
                    totalLife = 260;
                    scale = 1.0f;
                    zoomBox = 90f;
                    break;

                case ExplosionType.BossDeath:
                    sheet = _expBossSheet ?? _explosionSheet;
                    frameW = (sheet == _expBossSheet ? 96 : EXP_FRAME_W);
                    frameH = (sheet == _expBossSheet ? 96 : EXP_FRAME_H);
                    frames = (sheet == _expBossSheet ? 20 : EXP_FRAMES);
                    totalLife = 420;
                    scale = 1.0f;
                    zoomBox = 160f * 5.0f;
                    break;

                case ExplosionType.MissileHit:
                    Sfx.Play("sfx_missile_explosion.wav", 0.95f);
                    sheet = _expMissileSheet ?? _explosionSheet;
                    frameW = (sheet == _expMissileSheet ? 64 : EXP_FRAME_W);
                    frameH = (sheet == _expMissileSheet ? 64 : EXP_FRAME_H);
                    frames = (sheet == _expMissileSheet ? 14 : EXP_FRAMES);
                    totalLife = 300;
                    scale = 1.0f;
                    zoomBox = 100f * 5.0f;
                    break;

                case ExplosionType.RedAoE:
                    sheet = _expRedSheet ?? _explosionSheet;
                    frameW = (sheet == _expRedSheet ? 64 : EXP_FRAME_W);
                    frameH = (sheet == _expRedSheet ? 64 : EXP_FRAME_H);
                    frames = (sheet == _expRedSheet ? 16 : EXP_FRAMES);
                    totalLife = EXP_LIFE_MS;
                    scale = 1.0f;
                    zoomBox = 110f;
                    break;
            }

            NormalizeExplosionSheet(sheet, ref frameW, ref frameH, ref frames);

            _explosions.Add(new Explosion
            {
                Pos = pos,
                Radius = 0f,
                Dmg = 0,
                LifeMs = totalLife,
                HasAppliedDamage = false,
                Type = type,
                FrameCount = frames,
                FrameW = frameW,
                FrameH = frameH,
                TotalLifeMs = totalLife,
                DrawScale = scale,
                UseZoom = useZoom,
                ZoomBox = zoomBox
            });
        }

        // ====== Rendering ======
        private void DrawGame(System.Drawing.Graphics g)
        {
            // Background parallax
            if (_bgFar != null || _bgMid != null || _bgNear != null)
            {
                g.Clear(Color.Black);
                DrawTiledY(g, _bgFar, _bgOffFar);
                DrawTiledY(g, _bgMid, _bgOffMid);
                DrawTiledY(g, _bgNear, _bgOffNear);
            }
            else g.Clear(Color.Black);

            // Enemies
            if (_enemySprites != null)
            {
                for (int i = 0; i < _enemies.Count; i++)
                {
                    var eEnemy = _enemies[i];
                    int t = eEnemy.TypeIndex;
                    if (t >= 0 && t < _enemySprites.Length && _enemySprites[t] != null)
                        g.DrawImage(_enemySprites[t], eEnemy.R);
                    else
                        using (var b = new SolidBrush(_enemyTypes[t].BodyColor))
                            g.FillRectangle(b, eEnemy.R);
                }
            }

            // Boss
            if (_bossAlive)
            {
                int bi = _bossTypeIndex;
                if (bi >= 0 && bi < _bossSprites.Length && _bossSprites[bi] != null)
                    g.DrawImage(_bossSprites[bi], _boss.R);
                else
                    using (var b = new SolidBrush(_bossTypes[bi].BodyColor))
                        g.FillRectangle(b, _boss.R);
            }

            // Bullets
            // Bullets (player)
            // Bullets (player)
            foreach (var x in _bullets.Where(z => z.Owner == BulletOwner.Player))
            {
                var r = ToRect(x);
                var img = GetPlayerBulletSprite(x);
                if (img != null)
                {
                    // Xoay nếu viên đạn đang bay lệch (|Vx|>0) hoặc là homing
                    bool needRotate = x.Homing || Math.Abs(x.Vx) > 0.01f || Math.Abs(x.Vy + 12f) > 0.01f;
                    if (needRotate)
                    {
                        float angle = (float)(Math.Atan2(x.Vy, x.Vx) * 180.0 / Math.PI) + 90f;
                        DrawImageRotated(g, img, r, angle);
                    }
                    else
                    {
                        g.DrawImage(img, r);
                    }
                }
                else
                {
                    using (var b = new SolidBrush(
                        x.ColorHint == Color.Empty ? (_weaponColor == WeaponColor.Yellow ? Color.Gold
                            : _weaponColor == WeaponColor.Blue ? Color.DeepSkyBlue
                            : _weaponColor == WeaponColor.Green ? Color.Lime : Color.OrangeRed)
                            : x.ColorHint))
                    {
                        g.FillRectangle(b, r);
                    }
                }
            }


            // Bullets (enemy)
            foreach (var x in _bullets.Where(z => z.Owner == BulletOwner.Enemy))
                using (var b = new SolidBrush(x.ColorHint == Color.Empty ? Color.Purple : x.ColorHint))
                    g.FillRectangle(b, ToRect(x));

            // Bullets (boss)
            foreach (var x in _bullets.Where(z => z.Owner == BulletOwner.Boss))
                using (var b = new SolidBrush(x.ColorHint == Color.Empty ? Color.White : x.ColorHint))
                    g.FillRectangle(b, ToRect(x));

            foreach (var x in _bullets.Where(z => z.Owner == BulletOwner.Enemy))
                using (var b = new SolidBrush(x.ColorHint == Color.Empty ? Color.Purple : x.ColorHint))
                    g.FillRectangle(b, ToRect(x));
            foreach (var x in _bullets.Where(z => z.Owner == BulletOwner.Boss))
                using (var b = new SolidBrush(x.ColorHint == Color.Empty ? Color.White : x.ColorHint))
                    g.FillRectangle(b, ToRect(x));

            // Drops
            foreach (var d in _drops)
            {
                // Coin
                if (d.Type == DropType.Coin && _icoCoin != null)
                { g.DrawImage(_icoCoin, d.R); continue; }

                // Heal
                if (d.Type == DropType.Heal && _icoHeart != null)
                { g.DrawImage(_icoHeart, d.R); continue; }

                // Upgrade (máy bay + đạn)
                if (d.Type == DropType.Upgrade && _pickupUpgrade_18 != null)
                { g.DrawImage(_pickupUpgrade_18, d.R); continue; }

                // Color switches
                if (d.Type == DropType.ColorRed && _pickupColorRed_18 != null)
                { g.DrawImage(_pickupColorRed_18, d.R); continue; }

                if (d.Type == DropType.ColorYellow && _pickupColorYellow_18 != null)
                { g.DrawImage(_pickupColorYellow_18, d.R); continue; }

                if (d.Type == DropType.ColorGreen && _pickupColorGreen_18 != null)
                { g.DrawImage(_pickupColorGreen_18, d.R); continue; }

                if (d.Type == DropType.ColorBlue && _pickupColorBlue_18 != null)
                { g.DrawImage(_pickupColorBlue_18, d.R); continue; }

                if (d.Type == DropType.Shield && _pickupShield_18 != null)
                { g.DrawImage(_pickupShield_18, d.R); continue; }


                // Fallback nếu thiếu ảnh: giữ kiểu vẽ cũ
                bool isColor = d.Type == DropType.ColorYellow || d.Type == DropType.ColorBlue
                            || d.Type == DropType.ColorGreen || d.Type == DropType.ColorRed;

                Color c = d.Type == DropType.Upgrade ? Color.White :
                          d.Type == DropType.ColorYellow ? Color.Gold :
                          d.Type == DropType.ColorBlue ? Color.DeepSkyBlue :
                          d.Type == DropType.ColorGreen ? Color.Lime :
                          d.Type == DropType.ColorRed ? Color.OrangeRed :
                          Color.Gold;

                if (isColor)
                {
                    using (var brush = new SolidBrush(c)) g.FillRectangle(brush, d.R);
                    using (var pen = new Pen(Color.FromArgb(200, 0, 0, 0), 1)) g.DrawRectangle(pen, d.R);
                }
                else
                {
                    using (var brush = new SolidBrush(c)) g.FillEllipse(brush, d.R);
                    using (var pen = new Pen(Color.FromArgb(200, 0, 0, 0), 1)) g.DrawEllipse(pen, d.R);
                }
            }




            // Explosions (VFX + AoE Red)
            // Explosions (VFX + AoE Red)
            for (int i = 0; i < _explosions.Count; i++)
            {
                var ex = _explosions[i];

                if (ex.Type == ExplosionType.DamageOnly)
                    continue; // không vẽ gì, chỉ tồn tại để áp dame 1 lần

                // Dùng 1 biến age duy nhất
                int age = ex.TotalLifeMs - ex.LifeMs;
                if (age < 0) age = 0;
                if (age > ex.TotalLifeMs) age = ex.TotalLifeMs;

                Image sheet = null;
                switch (ex.Type)
                {
                    case ExplosionType.EnemyDeath: sheet = _expEnemySheet ?? _explosionSheet; break;
                    case ExplosionType.BossDeath: sheet = _expBossSheet ?? _explosionSheet; break;
                    case ExplosionType.MissileHit: sheet = _expMissileSheet ?? _explosionSheet; break;
                    case ExplosionType.RedAoE: sheet = _expRedSheet ?? _explosionSheet; break;
                }

                if (sheet != null && ex.FrameW > 0 && ex.FrameH > 0)
                {
                    Rectangle src;

                    if (ex.FrameCount <= 1)
                    {
                        // Ảnh đơn: vẽ full như icon (coin/heart/heat)
                        src = new Rectangle(0, 0, sheet.Width, sheet.Height);
                    }
                    else
                    {
                        // Sprite-sheet: tính frame dựa trên age
                        int frameIndex = (int)((age / (float)ex.TotalLifeMs) * (ex.FrameCount - 1));
                        int cols = Math.Max(1, sheet.Width / ex.FrameW);
                        src = new Rectangle((frameIndex % cols) * ex.FrameW,
                                            (frameIndex / cols) * ex.FrameH,
                                            ex.FrameW, ex.FrameH);
                    }

                    // Zoom-to-fit theo ZoomBox để luôn thấy trọn frame
                    float scale = ex.DrawScale;
                    if (ex.UseZoom)
                        scale *= ZoomToFitScale(src.Width, src.Height, ex.ZoomBox);

                    float dw = src.Width * scale;
                    float dh = src.Height * scale;
                    var dst = new RectangleF(ex.Pos.X - dw / 2f, ex.Pos.Y - dh / 2f, dw, dh);

                    // Vẽ có ImageAttributes + WrapMode.TileFlipXY bên trong
                    DrawSpriteFrame(g, sheet, src, dst);
                }
                else
                {
                    // fallback: vòng tròn mờ
                    int alpha = Math.Max(30, Math.Min(200, ex.LifeMs + 20));
                    using (var pPen = new Pen(Color.FromArgb(alpha, 255, 120, 0), 2))
                        g.DrawEllipse(pPen, ex.Pos.X - 20, ex.Pos.Y - 20, 40, 40);
                }
            }


            // --- Shield aura: VẼ TRƯỚC SHIP để nó nằm dưới tàu ---
            if (_shieldOn)
            {
                // NEW: logic nhấp nháy khi sắp hết
                int now = Environment.TickCount;
                int remain = Math.Max(0, _shieldExpireTick - now);
                bool shouldBlink = remain <= SHIELD_FLASH_REMAIN_MS;

                // Tạo nhịp ON/OFF: OFF khi bậc chẵn
                bool blinkOff = shouldBlink && (((now / SHIELD_FLASH_INTERVAL_MS) % 2) == 0);

                if (!blinkOff)   // khi OFF thì bỏ qua vẽ để tạo hiệu ứng nháy
                {
                    if (_shieldAura_96 != null)
                    {
                        int w = (int)(_ship.Width * ShieldScale);
                        int h = (int)(_ship.Height * ShieldScale);

                        var box = new Rectangle(
                            _ship.X + (_ship.Width - w) / 2,
                            _ship.Y + (_ship.Height - h) / 2,
                            w, h
                        );

                        g.DrawImage(_shieldAura_96, box);
                    }
                    else
                    {
                        using (var pen = new Pen(Color.FromArgb(160, 100, 200, 255), 3))
                            g.DrawEllipse(pen,
                                _ship.X - (_ship.Width * (ShieldScale - 1f)) / 2f,
                                _ship.Y - (_ship.Height * (ShieldScale - 1f)) / 2f,
                                _ship.Width * ShieldScale,
                                _ship.Height * ShieldScale);
                    }
                }
            }



            // --- Player ship: VẼ SAU KHIÊNG để tàu nằm trên vòng chắn ---
            if (_shipImg != null) g.DrawImage(_shipImg, _ship);
            else g.FillRectangle(Brushes.DeepSkyBlue, _ship);



            // HUD
            DrawHudOverlay(g);

            // PAUSE / COUNTDOWN overlays (vẽ GDI+)
            if (_state == GameState.Paused) DrawPauseOverlay(g);
            else if (_state == GameState.Countdown) DrawCountdownOverlay(g, _countdownLeft);
        }


        // cuộn hình nền dọc
        private void DrawTiledY(System.Drawing.Graphics g, Image img, float offsetY)
        {
            //if (img == null) return;
            //int w = ClientSize.Width;
            //int h = ClientSize.Height;

            //float y = -offsetY;
            //while (y < h)
            //{
            //    g.DrawImage(img, new Rectangle(0, (int)y, w, img.Height));
            //    y += img.Height;
            //}

            if (img == null) return;
            int w = ClientSize.Width, h = ClientSize.Height;

            // đưa offset về [0, img.Height)
            float off = offsetY % img.Height;
            if (off < 0) off += img.Height;

            float y = -off;
            while (y < h)
            {
                g.DrawImage(img, new Rectangle(0, (int)y, w, img.Height));
                y += img.Height;
            }

        }



        private void DrawHudOverlay(System.Drawing.Graphics g)
        {
            int pad = 8, ico = HudIconSize; // dùng biến cấu hình

            var font = this.Font;

            Action<string, Point, Color> DrawShadowText = (text, pt, color) =>
            {
                var shadow = new Point(pt.X + 1, pt.Y + 1);
                TextRenderer.DrawText(g, text, font, shadow, Color.FromArgb(160, 0, 0, 0));
                TextRenderer.DrawText(g, text, font, pt, color);
            };



            // Player
            string playerLine = $"Player: {_player?.Username}";
            var playerPt = new Point(pad, pad);
            DrawShadowText(playerLine, playerPt, Color.White);

            int nameH = TextRenderer.MeasureText(playerLine, font).Height;
            int xLeft = pad;
            int yTop = pad + nameH + 6;

            // HP
            if (_icoHeart != null) g.DrawImage(_icoHeart, new Rectangle(xLeft, yTop, ico, ico));
            DrawShadowText($"HP: {_hp}/{_hpMax}", new Point(xLeft + ico + 6, yTop + 2), Color.White);

            // Heat
            int heatY = yTop + ico + 4;
            int heatX = xLeft;
            if (_icoHeat != null) g.DrawImage(_icoHeat, new Rectangle(heatX, heatY, ico, ico));
            heatX += ico + 6;

            int barW = 150, barH = 10;
            int barY = heatY + (ico - barH) / 2;

            using (var bg = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
                g.FillRectangle(bg, new Rectangle(heatX, barY, barW, barH));

            float pct = Math.Max(0, Math.Min(1f, _heat / (float)_heatMax));
            int fillW = (int)(barW * pct);
            Color fillColor = _overheated ? Color.OrangeRed : (pct > 0.7f ? Color.Orange : Color.LimeGreen);
            using (var fg = new SolidBrush(fillColor))
                g.FillRectangle(fg, new Rectangle(heatX, barY, fillW, barH));

            DrawShadowText($"Heat: {_heat}/{_heatMax}", new Point(heatX + barW + 8, heatY + 2),
                           _overheated ? Color.OrangeRed : Color.White);

            // Coin
            int coinY = heatY + ico + 6;
            if (_icoCoin != null) g.DrawImage(_icoCoin, new Rectangle(xLeft, coinY, ico, ico));
            DrawShadowText(_walletCoins.ToString(), new Point(xLeft + ico + 6, coinY + 2), Color.Gold);

            // NEW: Shield stock hiển thị cạnh Coin
            int coinTextW = TextRenderer.MeasureText(_walletCoins.ToString(), font).Width;
            int sx = xLeft + ico + 6 + coinTextW + 18; // cách coin một chút

            if (_pickupShield_18 != null)
                g.DrawImage(_pickupShield_18, new Rectangle(sx, coinY, ico, ico));
            else
            {
                using (var b = new SolidBrush(Color.SkyBlue))
                    g.FillEllipse(b, new Rectangle(sx, coinY, ico, ico));
                using (var p = new Pen(Color.FromArgb(200, 0, 0, 0), 1))
                    g.DrawEllipse(p, new Rectangle(sx, coinY, ico, ico));
            }

            DrawShadowText($"x{_shieldStock}", new Point(sx + ico + 6, coinY + 2), Color.SkyBlue);

            

            int wepY = coinY + ico + 6;
            int wepX = xLeft;
            int sw = 16, sh = 16;
            Color accent = GetWeaponUiColor();

            var swRect = new Rectangle(wepX, wepY, sw, sh);
            using (var swBrush = new SolidBrush(accent))
                g.FillRectangle(swBrush, swRect);
            using (var swPen = new Pen(Color.FromArgb(200, 0, 0, 0)))
                g.DrawRectangle(swPen, swRect);

            string weaponText = $"Đạn: {GetWeaponName()}   Lv.{_weaponLevel}";
            bool hot = _overheated;
            string text = hot ? weaponText + "  (Overheat!)" : weaponText;
            Color textColor = hot ? Color.OrangeRed : Color.White;

            var textPt = new Point(swRect.Right + 8, wepY - 2);
            TextRenderer.DrawText(g, text, font, new Point(textPt.X + 1, textPt.Y + 1), Color.FromArgb(160, 0, 0, 0));
            TextRenderer.DrawText(g, text, font, textPt, textColor);

            //if (_shieldStock > 0 && !_shieldOn)
            //{
            //    string tip = "Nhấn S để bật khiêng";
            //    var tipPt = new Point(sx + ico + 6, coinY + ico + 4);
            //    DrawShadowText(tip, tipPt, Color.Gainsboro);
            //}

            if (_shieldOn)
            {
                int tx = xLeft;
                int ty = coinY + ico + 6; // dưới dòng coin
                string label = $"Shield: {_shieldHitsLeft} hit(s) | {Math.Max(0, (_shieldExpireTick - Environment.TickCount) / 1000)}s";
                TextRenderer.DrawText(g, label, font, new Point(tx, ty), Color.SkyBlue);
            }


            // ===== CẤP MÁY BAY (hiển thị ngay dưới thông tin đạn) =====
            int planeLineY = wepY + sh + 4; // đặt ngay dưới dòng vũ khí
            string planeText = $"Cấp máy bay: Lv.{_planeLevel}";
            var planePtShadow = new Point(textPt.X + 1, planeLineY + 1);
            var planePt = new Point(textPt.X, planeLineY);
            TextRenderer.DrawText(g, planeText, font, planePtShadow, Color.FromArgb(160, 0, 0, 0));
            TextRenderer.DrawText(g, planeText, font, planePt, Color.White);


            // ===== (TÙY CHỌN) GỢI Ý KHI CẤP ĐẠN > 3 =====
            if (_weaponLevel > 3)
            {
                string hint = $"+{OverLevel} dame từ cấp >3";
                var hintSize = TextRenderer.MeasureText(hint, font);
                var hintPt = new Point(planePt.X, planePt.Y + hintSize.Height + 2);
                TextRenderer.DrawText(g, hint, font, new Point(hintPt.X + 1, hintPt.Y + 1), Color.FromArgb(160, 0, 0, 0));
                TextRenderer.DrawText(g, hint, font, hintPt, Color.Gold);
            }

            // Score/Wave (center top)
            string topCenter = $"Score: {_score}    Wave: {_waveIndex}";
            Size tcSize = TextRenderer.MeasureText(topCenter, font);
            var tcPt = new Point((ClientSize.Width - tcSize.Width) / 2, pad + 2);
            DrawShadowText(topCenter, tcPt, Color.White);

            // Boss bar
            if (_showBossHud && _bossAlive && _bossHpMax > 0)
            {
                int barWb = 320, barHb = 12;
                int bx = (ClientSize.Width - barWb) / 2;
                int by = pad + 24;

                using (var bg2 = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
                    g.FillRectangle(bg2, new Rectangle(bx, by, barWb, barHb));

                float pctB = Math.Max(0, Math.Min(1f, _bossHp / (float)_bossHpMax));
                int wFill = (int)(barWb * pctB);
                using (var fg2 = new SolidBrush(Color.OrangeRed))
                    g.FillRectangle(fg2, new Rectangle(bx, by, wFill, barHb));

                DrawShadowText($"BOSS: {_bossName}", new Point(bx, by - 16), Color.White);
            }
        }

        // ====== Pause / Countdown Overlays ======
        private void DrawPauseOverlay(Graphics g)
        {
            using (var dim = new SolidBrush(Color.FromArgb(140, 0, 0, 0)))
                g.FillRectangle(dim, ClientRectangle);

            var fontTitle = new Font(Font.FontFamily, 22, FontStyle.Bold);
            var fontHint = new Font(Font.FontFamily, 11, FontStyle.Regular);

            string title = "TẠM DỪNG";
            Size szT = TextRenderer.MeasureText(title, fontTitle);
            Point ptT = new Point((ClientSize.Width - szT.Width) / 2, (ClientSize.Height - szT.Height) / 2 - 30);
            TextRenderer.DrawText(g, title, fontTitle, ptT, Color.White);

            string hint1 = "Enter: Tiếp tục sau đếm ngược 3-2-1";
            string hint2 = "Esc: Tiếp tục ngay      •      Q: Thoát";
            Size sz1 = TextRenderer.MeasureText(hint1, fontHint);
            Size sz2 = TextRenderer.MeasureText(hint2, fontHint);
            Point pt1 = new Point((ClientSize.Width - sz1.Width) / 2, ptT.Y + szT.Height + 16);
            Point pt2 = new Point((ClientSize.Width - sz2.Width) / 2, pt1.Y + sz1.Height + 6);
            TextRenderer.DrawText(g, hint1, fontHint, pt1, Color.Gainsboro);
            TextRenderer.DrawText(g, hint2, fontHint, pt2, Color.Gainsboro);

            fontTitle.Dispose();
            fontHint.Dispose();
        }

        private void DrawCountdownOverlay(Graphics g, int secondsLeft)
        {
            using (var dim = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                g.FillRectangle(dim, ClientRectangle);

            var fontNum = new Font(Font.FontFamily, 48, FontStyle.Bold);
            var fontHint = new Font(Font.FontFamily, 12, FontStyle.Regular);

            string num = secondsLeft.ToString();
            Size szN = TextRenderer.MeasureText(num, fontNum);
            Point ptN = new Point((ClientSize.Width - szN.Width) / 2, (ClientSize.Height - szN.Height) / 2 - 10);
            TextRenderer.DrawText(g, num, fontNum, ptN, Color.White);

            string hint = "Sắp tiếp tục…";
            Size szH = TextRenderer.MeasureText(hint, fontHint);
            Point ptH = new Point((ClientSize.Width - szH.Width) / 2, ptN.Y + szN.Height + 8);
            TextRenderer.DrawText(g, hint, fontHint, ptH, Color.Gainsboro);

            fontNum.Dispose();
            fontHint.Dispose();
        }

        // ====== Render hooks ======
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            DrawGame(g);
            base.OnPaint(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            _missileSprite = AssetManager.Get("missile_4x8.png"); // load missile sprite


            // Backgrounds
            _bgFar = AssetManager.Get("bg_space_far.png");
            _bgMid = AssetManager.Get("bg_space_mid.png");
            _bgNear = AssetManager.Get("bg_space_near.png");

            // Player
            _shipImg = AssetManager.Get("ship_36.png");

            _pickupShield_18 = AssetManager.Get("pickup_shield_18.png");
            _shieldAura_96 = AssetManager.Get("aura_shield_96.png");


            // Enemies
            _enemySprites = new Image[]
            {
                AssetManager.Get("enemy_scout_24.png"),
                AssetManager.Get("enemy_striker_28.png"),
                AssetManager.Get("enemy_bulky_32.png"),
                AssetManager.Get("enemy_burst_26.png"),
            };

            // Boss
            _bossSprites = new Image[]
            {
                AssetManager.Get("boss_hydra_90x64.png"),
                AssetManager.Get("boss_behemoth_110x72.png"),
                AssetManager.Get("boss_azura_100x70.png"),
            };

            // Default fallback sheet (nếu thiếu sheet riêng)
            _explosionSheet = AssetManager.Get("exp_red_64x64_16.png");

            // VFX sheets cho 4 loại nổ (nếu chưa có file, AssetManager trả null → dùng fallback)
            _expEnemySheet = AssetManager.Get("exp_enemy_64x64_12.png");   // 64x64, 12f
            _expBossSheet = AssetManager.Get("exp_boss_96x96_20.png");    // 96x96, 20f
            _expMissileSheet = AssetManager.Get("exp_missile_64x64_14.png"); // 64x64, 14f
            _expRedSheet = AssetManager.Get("exp_red_64x64_16.png");     // 64x64, 16f

            // HUD icons
            _icoCoin = AssetManager.Get("ico_coin.png");
            _icoHeart = AssetManager.Get("ico_heart.png");
            _icoHeat = AssetManager.Get("ico_heat.png");

            // Double buffer
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
            this.FormClosing += GameForm_FormClosing;

            // Căn tàu gần đáy, giữa màn hình
            _ship.Width = 36 * SCALE2;
            _ship.Height = 36 * SCALE2;
            _ship.X = (ClientSize.Width - _ship.Width) / 2;
            _ship.Y = Math.Max(0, ClientSize.Height - _ship.Height - 20);

            // --- Player bullet sprites ---
            _bulletRed_10x14 = AssetManager.Get("bullet_red_10x14.png");
            _bulletRed_12x16 = AssetManager.Get("bullet_red_12x16.png");

            _bulletYellow_6x12 = AssetManager.Get("bullet_yellow_6x12.png");

            _bulletGreen_6x12 = AssetManager.Get("bullet_green_6x12.png");

            _bulletBlue_8x14 = AssetManager.Get("bullet_blue_8x14.png");
            _bulletBlue_12x18 = AssetManager.Get("bullet_blue_12x18.png");
            _bulletBlueBeam_12x60 = AssetManager.Get("bullet_blue_beam_12x60.png");

            // --- Pickups (color & upgrade) ---
            _pickupColorRed_18 = AssetManager.Get("pickup_color_red_18.png");
            _pickupColorYellow_18 = AssetManager.Get("pickup_color_yellow_18.png");
            _pickupColorGreen_18 = AssetManager.Get("pickup_color_green_18.png");
            _pickupColorBlue_18 = AssetManager.Get("pickup_color_blue_18.png");

            _pickupUpgrade_18 = AssetManager.Get("pickup_upgrade_18.png");

        }

        private string GetWeaponName()
        {
            switch (_weaponColor)
            {
                case WeaponColor.Yellow: return "Yellow";
                case WeaponColor.Blue: return "Blue";
                case WeaponColor.Green: return "Green";
                case WeaponColor.Red: return "Red";
                default: return "Unknown";
            }
        }

        private Color GetWeaponUiColor()
        {
            switch (_weaponColor)
            {
                case WeaponColor.Yellow: return Color.Gold;
                case WeaponColor.Blue: return Color.DeepSkyBlue;
                case WeaponColor.Green: return Color.Lime;
                case WeaponColor.Red: return Color.OrangeRed;
                default: return Color.White;
            }
        }
    }
}



