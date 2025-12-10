using DotNetEnv;
using System;
using System.IO;

namespace Station.Config
{
    /// <summary>
    /// Helper class to load and access environment variables from .env file
    /// </summary>
    public static class EnvironmentConfig
    {
        private static bool _isLoaded = false;

        /// <summary>
        /// Load environment variables from .env file
        /// </summary>
        public static void Load()
        {
            if (_isLoaded)
                return;

            try
            {
                // Try to find .env file in multiple locations
                var possiblePaths = new[]
                {
                    // Root of the solution
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", ".env"),
                    // In the bin directory (during development)
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"),
                    // Relative to working directory
                    Path.Combine(Directory.GetCurrentDirectory(), ".env")
                };

                foreach (var path in possiblePaths)
                {
                    var fullPath = Path.GetFullPath(path);
                    if (File.Exists(fullPath))
                    {
                        Env.Load(fullPath);
                        _isLoaded = true;
                        System.Diagnostics.Debug.WriteLine($"✅ Loaded .env file from: {fullPath}");
                        return;
                    }
                }

                System.Diagnostics.Debug.WriteLine("⚠️ .env file not found in any expected location");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading .env file: {ex.Message}");
            }
        }

        /// <summary>
        /// Get Mapbox access token
        /// </summary>
        public static string MapboxAccessToken
        {
            get
            {
                Load();
                return Env.GetString("MAPBOX_ACCESS_TOKEN", string.Empty);
            }
        }

        /// <summary>
        /// Get backend base URL
        /// </summary>
        public static string BackendBaseUrl
        {
            get
            {
                Load();
                return Env.GetString("BACKEND_BASE_URL", "http://localhost:5280");
            }
        }

        /// <summary>
        /// Get station ID
        /// </summary>
        public static string StationId
        {
            get
            {
                Load();
                return Env.GetString("STATION_ID", "ST01");
            }
        }
    }
}
