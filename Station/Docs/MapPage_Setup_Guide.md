# Hu?ng d?n c?u hình ArcGIS Runtime cho MapPage

## V?n d?: B?n d? loading mãi không xong

### Nguyên nhân chính:
1. **Thi?u API Key** - ArcGIS Runtime c?n API key d? ho?t d?ng d?y d?
2. **Không có k?t n?i internet** - Basemap c?n t?i t? server ArcGIS
3. **LoadingRing không du?c ?n** - L?i x? lý event

## Gi?i pháp dã áp d?ng:

### 1. C?u hình ArcGIS Runtime (App.xaml.cs)
```csharp
// Ðã thêm c?u hình trong App constructor
ArcGISRuntimeEnvironment.ApiKey = ""; // Ð? tr?ng cho basemap công khai
```

### 2. X? lý Loading State (MapPage.xaml.cs)
- ? Subscribe vào `map.Loaded` event
- ? Subscribe vào `map.LoadStatusChanged` event  
- ? Timeout sau 5 giây d? tránh loading vô h?n
- ? Hi?n th? l?i chi ti?t n?u load failed

### 3. C?i thi?n UI (MapPage.xaml)
- ? Loading overlay v?i background m?
- ? Hi?n th? text "Ðang t?i b?n d?..."
- ? G?i ý ki?m tra k?t n?i internet

## Cách l?y API Key (Tùy ch?n - d? s? d?ng d?y d? tính nang):

### Bu?c 1: Ðang ký tài kho?n ArcGIS Developer
1. Truy c?p: https://developers.arcgis.com/
2. Click "Sign Up" và t?o tài kho?n mi?n phí
3. Xác nh?n email

### Bu?c 2: T?o API Key
1. Ðang nh?p vào https://developers.arcgis.com/
2. Vào "Dashboard" ? "API Keys"
3. Click "Create API Key"
4. Ð?t tên (ví d?: "Station Map")
5. Ch?n các d?ch v? c?n thi?t:
   - ? Basemaps
   - ? Geocoding
   - ? Routing (n?u c?n)

### Bu?c 3: C?u hình API Key vào App
```csharp
// Trong App.xaml.cs, thay th? dòng này:
ArcGISRuntimeEnvironment.ApiKey = ""; 

// B?ng API key c?a b?n:
ArcGISRuntimeEnvironment.ApiKey = "YOUR_API_KEY_HERE";
```

## S? d?ng không c?n API Key:

**Tin t?t**: Các basemap công khai v?n ho?t d?ng mà không c?n API key!

- ? ArcGIS Streets
- ? ArcGIS Topographic  
- ? ArcGIS Imagery
- ? ArcGIS Oceans

Ch? c?n có **k?t n?i internet** là d?!

## Ki?m tra n?u v?n không load:

### 1. Ki?m tra Internet
- M? trình duy?t và th? truy c?p https://arcgis.com
- N?u không truy c?p du?c ? v?n d? v? m?ng/firewall

### 2. Ki?m tra Output Window trong Visual Studio
- View ? Output ? Show output from: Debug
- Tìm các dòng log:
  ```
  Map load status: Loading
  Map load status: Loaded
  Map loaded successfully
  ```

### 3. N?u th?y l?i "FailedToLoad"
- Ki?m tra firewall có ch?n ?ng d?ng không
- Th? d?i basemap trong ComboBox (Streets ? Topographic)
- Restart ?ng d?ng

## Các c?i ti?n dã th?c hi?n:

1. **Loading Timeout** - T? d?ng ?n loading sau 5 giây
2. **Error Dialog** - Hi?n th? l?i chi ti?t v?i hu?ng d?n
3. **Event Handling** - Subscribe dúng events d? bi?t khi nào load xong
4. **Better UX** - Loading overlay v?i text mô t? rõ ràng
5. **Debug Logging** - Log chi ti?t trong Output window

## Troubleshooting nhanh:

| V?n d? | Gi?i pháp |
|--------|-----------|
| Loading vô h?n | Ki?m tra internet, xem Output window |
| Màn hình den | API key sai ho?c không có internet |
| L?i "Unauthorized" | C?n thêm API key h?p l? |
| Map không zoom | Ð?i map load xong r?i m?i zoom |

## Luu ý quan tr?ng:

?? **API Key là MI?N PHÍ** cho m?c s? d?ng co b?n:
- 2,000,000 map tiles/tháng
- 20,000 geocoding requests/tháng  
- 20,000 routing requests/tháng

Ð? cho phát tri?n và testing!

## K?t lu?n:

B?n d? gi? s?:
- ? T? d?ng ?n loading khi load xong
- ? Hi?n th? l?i rõ ràng n?u có v?n d?
- ? Timeout sau 5 giây d? không load vô h?n
- ? Ho?t d?ng v?i basemap công khai (không c?n API key)

Ch? c?n có internet là map s? load thành công! ???
