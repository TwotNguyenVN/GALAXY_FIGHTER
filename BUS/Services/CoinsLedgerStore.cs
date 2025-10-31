using Newtonsoft.Json;   // <- dùng Json.NET
using System;
using System.Collections.Generic;
using System.IO;


namespace BUS.Services
{
    /// <summary>
    /// Lưu "lifetime earned coins" (chỉ cộng, không trừ) ngoài DB.
    /// File JSON: %AppData%\GalaxyFighter\coins_ledger.json
    /// </summary>
    public static class CoinsLedgerStore
    {
        private static readonly object _lock = new object();
        private static readonly string _dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GalaxyFighter");
        private static readonly string _path = Path.Combine(_dir, "coins_ledger.json");

        private class Ledger
        {
            public Dictionary<string, long> Totals { get; set; } =
                new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        }

        private static Ledger LoadInternal()
        {
            try
            {
                if (!File.Exists(_path)) return new Ledger();
                var json = File.ReadAllText(_path);
                var data = JsonConvert.DeserializeObject<Ledger>(json);
                return data ?? new Ledger();
            }
            catch
            {
                return new Ledger(); // không crash game vì lỗi I/O/parsing
            }
        }

        private static void SaveInternal(Ledger lgr)
        {
            try
            {
                Directory.CreateDirectory(_dir);
                var json = JsonConvert.SerializeObject(lgr, Formatting.Indented);
                File.WriteAllText(_path, json);
            }
            catch
            {
                // nuốt lỗi I/O
            }
        }

        /// <summary> Cộng dồn coin cho user (earned trong 1 phiên). Bỏ qua nếu earned <= 0. </summary>
        public static void Append(string username, int earned)
        {
            if (string.IsNullOrWhiteSpace(username) || earned <= 0) return;

            lock (_lock)
            {
                var lgr = LoadInternal();
                if (!lgr.Totals.ContainsKey(username)) lgr.Totals[username] = 0;
                lgr.Totals[username] += earned; // chỉ cộng, không trừ
                SaveInternal(lgr);
            }
        }

        /// <summary> Lấy lifetime earned của 1 user (0 nếu chưa có). </summary>
        public static long GetTotal(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return 0;
            lock (_lock)
            {
                var lgr = LoadInternal();
                return lgr.Totals.TryGetValue(username, out var v) ? v : 0L;
            }
        }

        /// <summary> Lấy toàn bộ map {username -> lifetime earned}. </summary>
        public static Dictionary<string, long> LoadTotals()
        {
            lock (_lock)
            {
                var lgr = LoadInternal();
                return new Dictionary<string, long>(lgr.Totals, StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
