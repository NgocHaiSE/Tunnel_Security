# Cấu hình môi trường (Environment Configuration)

## 📋 Tổng quan

Project này sử dụng file `.env` để quản lý các biến môi trường và thông tin nhạy cảm như API keys, URLs, và cấu hình khác.

## 🚀 Cách thiết lập

### 1. Tạo file .env

Copy file `.env.example` thành `.env`:

```bash
cp .env.example .env
```

### 2. Cấu hình các biến môi trường

Mở file `.env` và cập nhật các giá trị:

```bash
# Mapbox Configuration
MAPBOX_ACCESS_TOKEN=your_actual_mapbox_token_here

# Backend Configuration
BACKEND_BASE_URL=http://localhost:5280
STATION_ID=ST01
```

### 3. Lấy Mapbox Access Token

1. Truy cập [Mapbox Account](https://account.mapbox.com/)
2. Đăng nhập hoặc tạo tài khoản mới
3. Vào phần **Access Tokens**
4. Copy token mặc định hoặc tạo token mới
5. Dán vào file `.env` của bạn

## 📁 Cấu trúc

- `.env` - File chứa biến môi trường thực tế (KHÔNG commit lên Git)
- `.env.example` - Template file với placeholder values (commit lên Git)
- `Station/Config/EnvironmentConfig.cs` - Helper class để đọc biến môi trường

## 🔒 Bảo mật

⚠️ **Quan trọng**: File `.env` đã được thêm vào `.gitignore` để tránh commit thông tin nhạy cảm lên repository.

**KHÔNG BAO GIỜ**:
- Commit file `.env` lên Git
- Share API keys hoặc tokens công khai
- Hardcode sensitive data trong source code

## 💻 Sử dụng trong code

Để sử dụng các biến môi trường trong code C#:

```csharp
using Station.Config;

// Lấy Mapbox token
string mapboxToken = EnvironmentConfig.MapboxAccessToken;

// Lấy Backend URL
string backendUrl = EnvironmentConfig.BackendBaseUrl;

// Lấy Station ID
string stationId = EnvironmentConfig.StationId;
```

## 🛠️ Các biến môi trường có sẵn

| Biến | Mô tả | Giá trị mặc định |
|------|-------|------------------|
| `MAPBOX_ACCESS_TOKEN` | Mapbox API access token | (bắt buộc) |
| `BACKEND_BASE_URL` | URL của backend server | `http://localhost:5280` |
| `STATION_ID` | ID của station | `ST01` |

## 🐛 Xử lý lỗi

Nếu gặp lỗi "Mapbox token not found" hoặc tương tự:

1. Kiểm tra file `.env` có tồn tại trong thư mục root của solution
2. Kiểm tra file `.env` có chứa đúng biến `MAPBOX_ACCESS_TOKEN`
3. Rebuild project để copy file `.env` vào output directory
4. Kiểm tra logs để xem đường dẫn file `.env` được load

## 📦 Dependencies

Project sử dụng package [DotNetEnv](https://github.com/tonerdo/dotnet-env) để đọc file `.env`.

```xml
<PackageReference Include="DotNetEnv" Version="3.1.1" />
```

## 🔄 Migration từ hardcoded values

Trước đây, các giá trị được hardcoded trong `MonitoringDashboardPage.xaml.cs`:

```csharp
// ❌ Cũ - Hardcoded
private const string MapboxToken = "pk.eyJ1...";
```

Bây giờ sử dụng `.env`:

```csharp
// ✅ Mới - Từ .env file
private string MapboxToken => EnvironmentConfig.MapboxAccessToken;
```

## 📚 Tài liệu tham khảo

- [Mapbox Documentation](https://docs.mapbox.com/)
- [DotNetEnv GitHub](https://github.com/tonerdo/dotnet-env)
- [Best Practices for Environment Variables](https://12factor.net/config)
