

[Sensor Nodes]  [Radar Nodes]  [Camera Nodes]
         ↓ TCP/IP (20–100 concurrent connections)
┌────────────────────────────────────────────────┐
│              STATION MACHINE                   │
│                                                │
│  ┌──────────────────────────────────────────┐  │
│  │             UI LAYER                     │  │
│  │         WinUI 3 Station App              │  │
│  │   (SignalR client + REST HTTP calls)     │  │
│  └──────────────┬─────────────────────────-─┘  │
│                 │ SignalR / REST                 │
│  ┌──────────────┴───────────────────────────┐  │
│  │            API LAYER                     │  │
│  │    ASP.NET Core 8  +  SignalR Hub        │  │
│  └──────────────┬───────────────────────────┘  │
│                 │ in-process Channel<T>          │
│  ┌──────────────┴───────────────────────────┐  │
│  │         SERVICES LAYER (Workers)         │  │
│  │                                          │  │
│  │  NodeConnectionManager                   │  │
│  │  IngestionWorker                         │  │
│  │  ProcessingWorker                        │  │
│  │  MediaWorker                             │  │
│  │  CommandDispatcher                       │  │
│  │  OutboxPublisher                         │  │
│  └──────────────┬───────────────────────────┘  │
│                 │ ADO.NET / EF Core              │
│  ┌──────────────┴───────────────────────────┐  │
│  │          DATABASE LAYER                  │  │
│  │  TimescaleDB  │  PostgreSQL  │  Files    │  │
│  └──────────────────────────────────────────┘  │
└────────────────────────────────────────────────┘
                    ↓ HTTP / RabbitMQ
              Central Server
