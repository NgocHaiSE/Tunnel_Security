using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Station.Models;

namespace Station.Services
{
    /// <summary>
    /// Service quản lý cấu hình trạm - lưu trong AppData
    /// </summary>
    public class StationConfigService
    {
        private readonly string _configFilePath;
        private StationConfig? _currentConfig;

        public StationConfigService()
        {
            // Lưu config trong AppData\Local\IntrustionDetectionStation
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var stationFolder = Path.Combine(appDataPath, "IntrustionDetectionStation");
            
            // Tạo thư mục nếu chưa có
            Directory.CreateDirectory(stationFolder);
            
            _configFilePath = Path.Combine(stationFolder, "station-config.json");
        }

        /// <summary>
        /// Lấy đường dẫn file config (để debug hoặc hiển thị cho user)
        /// </summary>
        public string ConfigFilePath => _configFilePath;

        /// <summary>
        /// Lấy cấu hình hiện tại
        /// </summary>
        public async Task<StationConfig?> GetConfigAsync()
        {
            // Return cached config nếu có
            if (_currentConfig != null)
                return _currentConfig;

            // Kiểm tra file có tồn tại không
            if (!File.Exists(_configFilePath))
                return null;

            try
            {
                var json = await File.ReadAllTextAsync(_configFilePath);
                _currentConfig = JsonSerializer.Deserialize<StationConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return _currentConfig;
            }
            catch (Exception ex)
            {
                // Log error nếu cần
                System.Diagnostics.Debug.WriteLine($"Error loading config: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lưu cấu hình mới
        /// </summary>
        public async Task<bool> SaveConfigAsync(StationConfig config)
        {
            try
            {
                config.LastModified = DateTimeOffset.UtcNow;
                
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var json = JsonSerializer.Serialize(config, options);
                await File.WriteAllTextAsync(_configFilePath, json);
                
                // Update cache
                _currentConfig = config;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving config: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra trạm đã được cấu hình chưa
        /// </summary>
        public async Task<bool> IsConfiguredAsync()
        {
            var config = await GetConfigAsync();
            return config != null && 
                   !string.IsNullOrEmpty(config.StationCode) && 
                   !string.IsNullOrEmpty(config.CenterUrl);
        }

        /// <summary>
        /// Reset cấu hình (khi cài đặt lại)
        /// </summary>
        public bool ResetConfig()
        {
            try
            {
                if (File.Exists(_configFilePath))
                    File.Delete(_configFilePath);
                
                _currentConfig = null;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error resetting config: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validate cấu hình
        /// </summary>
        public (bool IsValid, string[] Errors) ValidateConfig(StationConfig config)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(config.StationCode))
                errors.Add("Mã trạm không được để trống");

            if (string.IsNullOrWhiteSpace(config.StationName))
                errors.Add("Tên trạm không được để trống");

            if (string.IsNullOrWhiteSpace(config.Area))
                errors.Add("Khu vực không được để trống");

            if (string.IsNullOrWhiteSpace(config.Route))
                errors.Add("Tuyến không được để trống");

            if (string.IsNullOrWhiteSpace(config.CenterUrl))
                errors.Add("URL Center không được để trống");
            else if (!Uri.TryCreate(config.CenterUrl, UriKind.Absolute, out _))
                errors.Add("URL Center không hợp lệ");

            if (config.HeartbeatIntervalSeconds < 5)
                errors.Add("Heartbeat interval phải >= 5 giây");

            if (config.AlertSyncIntervalSeconds < 1)
                errors.Add("Alert sync interval phải >= 1 giây");

            return (errors.Count == 0, errors.ToArray());
        }

        /// <summary>
        /// Export config ra file (để backup)
        /// </summary>
        public async Task<bool> ExportConfigAsync(string destinationPath)
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    // Dùng FileStream để copy async
                    using var sourceStream = new FileStream(_configFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                    using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
                    await sourceStream.CopyToAsync(destinationStream);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Import config từ file
        /// </summary>
        public async Task<bool> ImportConfigAsync(string sourcePath)
        {
            try
            {
                if (!File.Exists(sourcePath))
                    return false;

                var json = await File.ReadAllTextAsync(sourcePath);
                var config = JsonSerializer.Deserialize<StationConfig>(json);
                
                if (config == null)
                    return false;

                return await SaveConfigAsync(config);
            }
            catch
            {
                return false;
            }
        }
    }

    // Extension method cho File.CopyAsync (nếu chưa có)
    internal static class FileExtensions
    {
        public static async Task CopyAsync(string sourceFile, string destinationFile, bool overwrite)
        {
            using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            using var destinationStream = new FileStream(destinationFile, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            await sourceStream.CopyToAsync(destinationStream);
        }
    }
}