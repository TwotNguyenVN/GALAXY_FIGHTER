using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace GUI.GraphicsCore
{
    /// <summary>
    /// Quản lý tải & cache ảnh.
    /// - Thử Properties.Resources theo tên.
    /// - Nếu không có, tìm file dưới thư mục "Resources" (Copy to Output = Copy if newer).
    /// </summary>
    public static class AssetManager
    {
        private static readonly Dictionary<string, Image> _cache =
            new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);

        private static readonly string[] _extensions = new string[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };

        public static Image Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("key is null/empty", "key");

            Image img;
            if (_cache.TryGetValue(key, out img)) return img;

            // 1) thử Resources.resx
            img = TryFromResx(key);
            if (img != null) { _cache[key] = img; return img; }

            // 2) thử trong /Resources (file content)
            string path = ResolveContentPath(key, false);
            if (path != null)
            {
                img = Image.FromFile(path);
                _cache[key] = img;
                return img;
            }

            throw new FileNotFoundException("Asset '" + key + "' not found in Properties.Resources or /Resources folder.");
        }

        public static Image FromRes(string resourceName)
        {
            var img = TryFromResx(resourceName);
            if (img == null)
                throw new MissingMemberException("Resource '" + resourceName + "' not found in Properties.Resources.");
            return img;
        }

        public static Image FromContent(string relativePathUnderResources)
        {
            string full = ResolveContentPath(relativePathUnderResources, true);
            return Image.FromFile(full);
        }

        public static void Clear()
        {
            foreach (var kv in _cache) kv.Value.Dispose();
            _cache.Clear();
        }

        // ===== Helpers =====
        private static Image TryFromResx(string name)
        {
            var t = typeof(Properties.Resources);
            var prop = t.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop == null) return null;
            return prop.GetValue(null, null) as Image;
        }

        private static string ResolveContentPath(string key, bool strict)
        {
            var root = Path.Combine(Application.StartupPath, "Resources");

            // Nếu key có đuôi -> kiểm tra trực tiếp
            if (HasExtension(key))
            {
                string rel = key.Replace('\\', '/');
                if (rel.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase))
                    rel = rel.Substring("Resources/".Length);

                var full = Path.Combine(root, rel);
                if (File.Exists(full)) return full;

                if (strict) throw new FileNotFoundException(full);
                return null;
            }

            // Không có đuôi -> thử từng ext
            for (int i = 0; i < _extensions.Length; i++)
            {
                var candidate = Path.Combine(root, key + _extensions[i]);
                if (File.Exists(candidate)) return candidate;
            }

            if (strict) throw new FileNotFoundException("Not found under /Resources: " + key);
            return null;
        }


        private static bool HasExtension(string s)
        {
            try { return !string.IsNullOrEmpty(Path.GetExtension(s)); }
            catch { return false; }
        }
    }
}
