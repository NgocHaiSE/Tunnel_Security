- Xây dựng kiến trúc tổng thể cho dự án hệ giống giám sát
- Hệ thống sẽ có 1 máy chủ trung tâm và các máy trạm 
- Máy trạm có chức năng nhận dữ liệu từ các cảm biến, hiển thị trên máy trạm đó và ghi vào csdl
- Máy chủ trung tâm có chức năng theo dõi và tổng hợp thông tin từ các máy trạm, có thể kết nối vào máy đó để điều khiển từ xa
- Máy trạm có các luồng dữ liệu được truyền từ các sensors, camera, radar
- Sử dụng các phương pháp truyền qua dây, wifi
- Hãy thiết kế có các tầng Database, services (chuyên xử lý các logic điều khiển, truyền nhận dữ liệu và xử lý, buffer dữ liệu thời gian thực), tầng giao diện
- Hãy thiết kế tầng database sao cho vừa lưu được dữ liệu realtime truyền về lẫn các dữ liệu người dùng thông thường, tốt nhất hãy sử dụng nhiều database
- Sử dụng cả hàng đợi để xử lý các điều khiển và yêu cầu

[Sensors · Cameras · Radar]
        ↓ TCP / Serial / RTSP / WiFi
┌──────────────────────────────┐
│        STATION MACHINE       │wr
│                              │
│   WinUI 3 Station App        │
│         ↕ SignalR/REST       │
│   Station Backend            │
│   (ASP.NET Core 8)           │
│   ┌──────────────────────┐   │
│   │ Ingestion Service    │   │  ← reads sensors/cameras/radar
│   │ Processing Service   │   │  ← thresholds, alerts
│   │ Outbox Publisher     │   │──────────→ Central RabbitMQ
│   └──────────────────────┘   │               (when online)
│   ┌──────────┐ ┌──────────┐  │
│   │TimescaleDB│ │PostgreSQL│  │  ← local sensor data + config/users
│   └──────────┘ └──────────┘  │
└──────────────────────────────┘

                ↓ network

┌──────────────────────────────────────┐
│           CENTRAL SERVER             │
│                                      │
│  RabbitMQ → Central Backend          │
│             (ASP.NET Core 8)         │
│  ┌─────────────────────────────┐     │
│  │ Station Aggregator Service  │     │
│  │ Alert Aggregator Service    │     │
│  └─────────────────────────────┘     │
│  ┌──────────┐  ┌────────────────┐    │
│  │TimescaleDB│  │  PostgreSQL    │    │
│  └──────────┘  └────────────────┘    │
│                    ↕ SignalR/REST     │
│         WinUI 3 Center App           │
│         (RDP for remote station view)│
└──────────────────────────────────────┘
