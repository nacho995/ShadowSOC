# ShadowSOC

**A real-time Security Operations Center dashboard that visualizes live cyber attacks on a dark interactive world map, powered by real threat intelligence from AbuseIPDB.**

![.NET](https://img.shields.io/badge/.NET_10-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Angular](https://img.shields.io/badge/Angular_21-DD0031?style=flat-square&logo=angular&logoColor=white)
![Apache Kafka](https://img.shields.io/badge/Apache_Kafka-231F20?style=flat-square&logo=apachekafka&logoColor=white)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-FF6600?style=flat-square&logo=rabbitmq&logoColor=white)
![SignalR](https://img.shields.io/badge/SignalR-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Leaflet](https://img.shields.io/badge/Leaflet-199900?style=flat-square&logo=leaflet&logoColor=white)
![Tailwind CSS](https://img.shields.io/badge/Tailwind_CSS-06B6D4?style=flat-square&logo=tailwindcss&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=flat-square&logo=docker&logoColor=white)

---

## Screenshots

> _Screenshots / GIF coming soon. The dashboard features a dark hacker aesthetic with a full-screen attack map, terminal feed, and live statistics._

---

## Features

- **Real-time attack map** -- Dark-themed world map (CartoDB Dark Matter tiles) with animated ripple markers showing attacks as they happen
- **Live terminal feed** -- Scrollable monospace log of every detected alert with timestamp, severity, attack type, and origin
- **Statistics dashboard** -- Top attacking countries, attack type breakdown with bar charts, and severity distribution (Critical / High / Medium / Low)
- **Real threat intelligence** -- Sources IP addresses from AbuseIPDB's blacklist (confidence >= 90%), not synthetic data
- **Detection engine** -- Filters raw events by severity (High and Critical pass through) and sensitive ports (port 22/SSH always triggers an alert)
- **IP geolocation** -- Resolves attacker coordinates and country via ip-api.com with in-memory caching to respect rate limits
- **Graceful degradation** -- Falls back to a curated list of known malicious IPs when AbuseIPDB is rate-limited
- **WebSocket push** -- Alerts are pushed to the browser in real time via SignalR; no polling
- **MITRE ATT&CK IDs** -- Each simulated event includes a MITRE technique identifier

---

## Architecture

```
                          ShadowSOC Architecture
  ============================================================

  +-----------------+         +---------------------+
  |   AbuseIPDB     |         |   ip-api.com        |
  |  (blacklist)    |         |   (geolocation)     |
  +--------+--------+         +---------+-----------+
           |                            |
           v                            v
  +-----------------+    Kafka    +---------------------+
  |   Simulator     | ---------> |     Consumer        |
  |                 |  "security |                     |
  | Fetches real    |  -events"  | Detection engine:   |
  | malicious IPs,  |  topic     | - Severity >= High  |
  | generates       |            | - Port 22 (SSH)     |
  | SecurityEvents  |            | Geolocates IPs      |
  +-----------------+            +----------+----------+
                                            |
                                            | RabbitMQ
                                            | "alerts" queue
                                            v
                                 +---------------------+
                                 |       API           |
                                 |                     |
                                 | ASP.NET + SignalR   |
                                 | Consumes RabbitMQ,  |
                                 | broadcasts to all   |
                                 | connected clients   |
                                 +----------+----------+
                                            |
                                            | WebSocket
                                            | (SignalR)
                                            v
                                 +---------------------+
                                 |   Angular Dashboard |
                                 |                     |
                                 | - Leaflet attack map|
                                 | - Terminal feed     |
                                 | - Live statistics   |
                                 +---------------------+
```

---

## Tech Stack

| Layer           | Technology                        | Purpose                                      |
| --------------- | --------------------------------- | -------------------------------------------- |
| Frontend        | Angular 21, Leaflet, Tailwind CSS | Interactive dashboard and dark-themed map     |
| Real-time Push  | SignalR (WebSocket)               | Push alerts to the browser without polling    |
| API             | ASP.NET Web API (.NET 10)         | Host SignalR hub, bridge RabbitMQ to clients  |
| Message Queue   | RabbitMQ                          | Decouple detection from presentation          |
| Event Streaming | Apache Kafka                      | High-throughput raw event pipeline            |
| Threat Intel    | AbuseIPDB API                     | Real-world malicious IP addresses             |
| Geolocation     | ip-api.com                        | Resolve IP to latitude, longitude, and country|
| Infrastructure  | Docker Compose                    | RabbitMQ, Kafka, and Zookeeper containers     |

---

## Project Structure

```
ShadowSOC/
├── docker-compose.yml              # RabbitMQ + Kafka + Zookeeper
├── ShadowSOC.sln                   # .NET solution file
│
├── ShadowSOC.Simulator/            # Event generator
│   ├── Worker.cs                   # Fetches AbuseIPDB IPs, produces Kafka events
│   └── appsettings.json            # AbuseIPDB API key goes here
│
├── ShadowSOC.Consumer/             # Detection service
│   ├── Worker.cs                   # Kafka consumer, detection rules, geolocation
│   └── appsettings.json
│
├── ShadowSOC.Api/                  # Web API
│   ├── Program.cs                  # App configuration (SignalR, CORS, etc.)
│   ├── Hubs/AlertHub.cs            # SignalR hub endpoint
│   └── Services/
│       ├── AlertBroadcastService.cs # RabbitMQ listener -> SignalR broadcaster
│       └── RabbitMQService.cs       # RabbitMQ connection helper
│
├── ShadowSOC.Shared/               # Shared library
│   └── SecurityEvent.cs            # SecurityEvent model, Severity & Protocol enums
│
└── ShadowSOC.Web/                  # Angular 21 frontend
    └── src/app/
        ├── app.ts / app.html       # Root component: layout, stats, terminal feed
        ├── signalr.ts              # SignalR service (WebSocket connection)
        └── attack-map/
            └── attack-map.ts       # Leaflet map with ripple animations
```

---

## How It Works

### 1. Threat Intelligence (Simulator)

The Simulator queries the [AbuseIPDB blacklist API](https://www.abuseipdb.com/) every 5 minutes, fetching the top 50 IPs with a confidence score of 90% or higher. These are real IPs reported by the security community. If the API is rate-limited, the service falls back to a hardcoded list of known malicious IPs.

Every 2--5 seconds, it generates a `SecurityEvent` using a random IP from the cache, a random attack type (Brute Force, SQL Injection, DDoS, etc.), and a weighted severity distribution (~40% Critical, ~35% High, ~15% Medium, ~10% Low). Events are published to the Kafka `security-events` topic.

### 2. Detection Engine (Consumer)

The Consumer reads from Kafka and applies detection rules:

- **Severity filter**: Only events with severity `High` or `Critical` generate an alert
- **Port filter**: Any event targeting port 22 (SSH) generates an alert regardless of severity

Qualifying events are geolocated via ip-api.com (with in-memory caching to avoid the 45 req/min rate limit) and then published to the RabbitMQ `alerts` queue.

### 3. Real-time API

The ASP.NET API runs a background service (`AlertBroadcastService`) that consumes the RabbitMQ `alerts` queue and broadcasts each message to all connected clients via the SignalR hub at `/hubs/alerts`.

### 4. Dashboard (Angular)

The Angular app connects to the SignalR hub on startup. When an alert arrives:

- A **marker** is placed on the Leaflet map at the attacker's coordinates
- An animated **ripple effect** (three expanding rings) draws attention to the attack location
- The **terminal feed** prepends the alert with timestamp, severity, attack type, IP, and country
- **Statistics** update in real time: top countries, attack type distribution, and severity breakdown with proportional bar charts

---

## Detection Rules

The Consumer applies these rules to decide which raw events become alerts:

| Rule             | Condition                          | Rationale                                                    |
| ---------------- | ---------------------------------- | ------------------------------------------------------------ |
| Severity filter  | `Severity >= High`                 | Low/Medium events are noise; High and Critical need attention |
| SSH monitor      | `DestinationPort == 22`            | SSH brute force is one of the most common real-world attacks  |

Events that do not match any rule are silently dropped. This keeps the dashboard focused on actionable threats.

---

## Quick Start

### Prerequisites

- [Docker](https://docs.docker.com/get-docker/) and Docker Compose
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (v22+ recommended)
- [Angular CLI](https://angular.dev/) (`npm install -g @angular/cli`)
- (Optional) Free [AbuseIPDB API key](https://www.abuseipdb.com/account/api) for real threat data

### 1. Clone the repository

```bash
git clone https://github.com/YOUR_USERNAME/ShadowSOC.git
cd ShadowSOC
```

### 2. Start the infrastructure

```bash
docker-compose up -d
```

This starts RabbitMQ (ports 5672 / 15672), Kafka (port 9092), and Zookeeper.

### 3. Configure the AbuseIPDB API key

Edit `ShadowSOC.Simulator/appsettings.json` and add your key:

```json
{
  "AbuseIPDB": {
    "ApiKey": "YOUR_API_KEY_HERE"
  }
}
```

> If you skip this step, the Simulator will use fallback IPs automatically.

### 4. Start the backend services

Open three separate terminals:

```bash
# Terminal 1 - Simulator (generates events)
dotnet run --project ShadowSOC.Simulator

# Terminal 2 - Consumer (detection + geolocation)
dotnet run --project ShadowSOC.Consumer

# Terminal 3 - API (SignalR hub)
dotnet run --project ShadowSOC.Api
```

### 5. Start the frontend

```bash
cd ShadowSOC.Web
npm install
ng serve
```

### 6. Open the dashboard

Navigate to **http://localhost:4200** in your browser. Alerts should start appearing on the map within a few seconds.

You can also access the RabbitMQ management UI at **http://localhost:15672** (guest / guest).

---

## Event Model

Each `SecurityEvent` flowing through the pipeline contains:

```
Id               : GUID
OriginIp         : Attacker IP (from AbuseIPDB)
DestinationIp    : Target IP (simulated internal network)
TypeOfAttack     : Brute Force | Port Scan | SQL Injection | DDoS | XSS | RCE | SSH Exploit | Directory Traversal
Severity         : Low | Medium | High | Critical
Protocol         : TCP | UDP | ICMP
SourcePort       : Random ephemeral port (1024-65535)
DestinationPort  : 80 | 443 | 22 | 21 | 25 | 3389 | 8080 | 3306
MitreID          : MITRE ATT&CK technique ID
WhenStarted      : UTC timestamp
WhenEnded        : UTC timestamp
Latitude         : Populated by Consumer after geolocation
Longitude        : Populated by Consumer after geolocation
Country          : Populated by Consumer after geolocation
```

---

## License

This project is built for educational and portfolio purposes.
