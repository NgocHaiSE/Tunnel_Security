using DotNetEnv;
using System;
using System.IO;

namespace Center.Config
{
    public static class EnvironmentConfig
    {
        private static bool _loaded;

        public static void Load()
        {
            if (_loaded) return;
            var paths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", ".env"),
                Path.Combine(Directory.GetCurrentDirectory(), ".env")
            };
            foreach (var p in paths)
            {
                var full = Path.GetFullPath(p);
                if (File.Exists(full)) { Env.Load(full); _loaded = true; return; }
            }
        }

        public static string BackendBaseUrl
        {
            get { Load(); return Env.GetString("CENTER_BACKEND_URL", "http://localhost:5281"); }
        }
    }
}
