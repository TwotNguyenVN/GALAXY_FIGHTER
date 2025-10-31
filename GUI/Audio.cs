using System;
using System.IO;
using NAudio.Wave;

namespace GUI
{
    // ===========================================================
    // SFX PLAYER
    // - Play(...)     : one-shot như cũ (không trả handle)
    // - PlayScoped(...) : trả handle để Stop/Dispose đúng “lần bắn”
    // ===========================================================
    static class Sfx
    {
        private static readonly object _cleanupLock = new object();

        public static string BaseDir =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio");

        // ---- One-shot (không handle) ----
        public static void Play(string fileName, float volume = 0.9f)
        {
            try
            {
                var path = Path.Combine(BaseDir, fileName);
                if (!File.Exists(path)) return;

                var afr = new AudioFileReader(path) { Volume = volume };
                var wo = new WaveOutEvent();
                wo.Init(afr);
                wo.Play();

                wo.PlaybackStopped += (_, __) =>
                {
                    lock (_cleanupLock)
                    {
                        try { afr.Dispose(); } catch { }
                        try { wo.Dispose(); } catch { }
                    }
                };
            }
            catch { /* swallow SFX errors */ }
        }

        // ---- Có handle để dừng theo đời sống đối tượng ----
        public static ISfxHandle PlayScoped(string fileName, float volume = 0.9f)
        {
            try
            {
                var path = Path.Combine(BaseDir, fileName);
                if (!File.Exists(path)) return SfxHandle.Noop;

                var afr = new AudioFileReader(path) { Volume = volume };
                var wo = new WaveOutEvent();
                wo.Init(afr);
                wo.Play();

                return new SfxHandle(wo, afr);
            }
            catch
            {
                return SfxHandle.Noop;
            }
        }

        // ===== Handle interface & implementation =====
        public interface ISfxHandle : IDisposable
        {
            void Stop();
            bool IsValid { get; }
        }

        private sealed class SfxHandle : ISfxHandle
        {
            public static readonly ISfxHandle Noop = new NoopHandle();

            private WaveOutEvent _wo;
            private AudioFileReader _afr;
            private bool _disposed;

            public bool IsValid { get { return _wo != null && _afr != null; } }

            public SfxHandle(WaveOutEvent wo, AudioFileReader afr)
            {
                _wo = wo; _afr = afr;
            }

            public void Stop()
            {
                if (_disposed) return;
                try { _wo.Stop(); } catch { }
                Dispose();
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                try { _wo.Dispose(); } catch { }
                try { _afr.Dispose(); } catch { }
                _wo = null; _afr = null;
            }

            private sealed class NoopHandle : ISfxHandle
            {
                public bool IsValid { get { return false; } }
                public void Stop() { }
                public void Dispose() { }
            }
        }
    }

    // ===========================================================
    // LOOPING PLAYER (cho BGM/loop dài)
    // ===========================================================
    sealed class LoopPlayer : IDisposable
    {
        private readonly WaveOutEvent _wo;
        private readonly AudioFileReader _afr;
        private readonly bool _loop;
        private bool _disposed;

        public LoopPlayer(string path, float volume = 0.6f, bool loop = true)
        {
            _afr = new AudioFileReader(path) { Volume = volume };
            _wo = new WaveOutEvent();
            _wo.Init(_afr);
            _loop = loop;
            _wo.PlaybackStopped += (_, __) =>
            {
                if (_disposed) return;
                if (_loop)
                {
                    try { _afr.Position = 0; _wo.Play(); } catch { }
                }
            };
        }

        public float Volume { get { return _afr.Volume; } set { _afr.Volume = Math.Max(0f, Math.Min(1f, value)); } }
        public void Play() { _wo.Play(); }
        public void Pause() { _wo.Pause(); }
        public void Stop() { _wo.Stop(); try { _afr.Position = 0; } catch { } }
        public PlaybackState State { get { return _wo.PlaybackState; } }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { _wo.Stop(); } catch { }
            _wo.Dispose();
            _afr.Dispose();
        }
    }

    // ===========================================================
    // AUDIO HUB (BGM + loop dài khác)
    // ===========================================================
    static class AudioHub
    {
        public static string BaseDir =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio");

        private static LoopPlayer _bgm;
        private static LoopPlayer _blueBeamLoop;   // loop nhỏ cho beam xanh dương

        public static void StopBgm()
        {
            if (_bgm != null) { _bgm.Dispose(); _bgm = null; }
        }

        public static void DuckBgm(bool duck)
        {
            if (_bgm != null) _bgm.Volume = duck ? 0.18f : 0.55f;
        }

        public static void PauseBgm(bool pause)
        {
            if (_bgm == null) return;
            if (pause) _bgm.Pause(); else _bgm.Play();
        }

        private static void SwapBgm(string path, float vol)
        {
            try
            {
                if (_bgm != null) { _bgm.Dispose(); _bgm = null; }
                if (!File.Exists(path)) return;
                _bgm = new LoopPlayer(path, vol, true);
                _bgm.Play();
            }
            catch { }
        }

        // ---- Blue beam tiny loop ----
        public static void StartBlueBeamLoop()
        {
            try
            {
                if (_blueBeamLoop != null) return;
                string p = Path.Combine(BaseDir, "sfx_blue_beam_loop.wav");
                if (!File.Exists(p)) return;
                _blueBeamLoop = new LoopPlayer(p, 0.18f, true);
                _blueBeamLoop.Play();
            }
            catch { }
        }

        public static void StopBlueBeamLoop()
        {
            try { if (_blueBeamLoop != null) _blueBeamLoop.Dispose(); } catch { }
            _blueBeamLoop = null;
        }

        private static string FirstExisting(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                var p = Path.Combine(BaseDir, names[i]);
                if (File.Exists(p)) return p;
            }
            return null;
        }

        // ---- Stage/Boss BGMs ----
        public static void PlayStageBgm()
        {
            var p = FirstExisting("bgm_stage.mp3", "bgm_stage.ogg", "bgm_stage.wav");
            if (p != null) SwapBgm(p, 0.55f);
        }

        public static void PlayBossBgm()
        {
            var p = FirstExisting("bgm_boss.mp3", "bgm_boss.ogg", "bgm_boss.wav");
            if (p != null) SwapBgm(p, 0.60f);
        }
    }
}
