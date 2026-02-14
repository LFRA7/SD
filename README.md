# Distributed Sensor Data Processing System

A distributed system for collecting, aggregating, and processing IoT sensor data using a microservices-based architecture.

## Architecture

The system consists of five main components working together:

```
[Wavy] --> [RabbitMQ] --> [Aggregator] --> [Server] <--> [Frontend]
                                              |
                                              v
                                         [SQL Server]
```

### Components

- **Wavy**: IoT sensor simulators that publish data to RabbitMQ topics
- **Aggregator**: Consumes messages from RabbitMQ, accumulates 5 records per topic, and sends them to the Server
- **Server**: Receives data via TCP, stores it in the database, and performs statistical calculations (average, min, max) via gRPC
- **Frontend**: ASP.NET Core MVC web interface for data visualization and analysis
- **Models**: Shared library with data models (Entity Framework)

## Technologies Used

- **.NET 8.0**
- **gRPC** - RPC communication for statistical calculations
- **RabbitMQ** - Message broker with Topic exchange
- **Entity Framework Core** - ORM for database access
- **SQL Server LocalDB** - Local database
- **TCP Sockets** - Communication between Aggregator and Server
- **ASP.NET Core MVC** - Web frontend

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server LocalDB](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb)
- [RabbitMQ Server](https://www.rabbitmq.com/download.html)

## Setup

### 1. RabbitMQ

Ensure RabbitMQ is installed and running:

```bash
# Windows (with Chocolatey)
choco install rabbitmq

# Or via Docker
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:management
```

Access the management interface: http://localhost:15672 (guest/guest)

### 2. Database

The system uses SQL Server LocalDB. The `SensorDataNew` database will be created automatically on first run of each component.

**Connection string used:**
```
Server=(localdb)\mssqllocaldb;Database=SensorDataNew;Trusted_Connection=True;MultipleActiveResultSets=true
```

### 3. Ports Used

- **RabbitMQ**: 5672 (AMQP), 15672 (Management UI)
- **Server TCP**: 6000
- **gRPC**: 7220
- **Frontend**: (configured in launchSettings.json)

## Running the System

### Startup Order

Execute the components in the following order:

#### 1. Server

```bash
cd Servidor
dotnet run
```

Available server commands:
- `media <topic>` - Calculate average of values for a topic
- `min <topic>` - Find minimum value for a topic
- `max <topic>` - Find maximum value for a topic
- `exit` - Shutdown the server

#### 2. Aggregator

```bash
cd Agregador
dotnet run
```

On startup, you'll be prompted to enter topics to subscribe to (comma-separated):
```
Digite o(s) tópico(s) a subscrever (separados por vírgula, ex: temperatura,humidade):
```

Example: `temperature,humidity,pressure`

#### 3. Wavy (Data Producer)

```bash
cd Wavy
dotnet run
```

#### 4. Frontend (Optional)

```bash
cd Frontend
dotnet run
```

Access the web application through your browser at the indicated address.

## Data Flow

1. **Publishing**: Wavy sensors publish data in the format `WavyId:Value` to specific topics on RabbitMQ
2. **Aggregation**: The Aggregator consumes messages, stores them in its local database, and when it accumulates 5 records for a topic, sends them to the Server
3. **Processing**: The Server receives data via TCP, stores it in the database, and provides commands for statistical analysis
4. **Analysis**: The Server uses gRPC to request calculations (average, min, max) processed remotely
5. **Visualization**: The Frontend allows querying and visualizing the processed data

## Communication Protocol

### Aggregator → Server (TCP)

```
Client: DATA:<topic>
Server: 100 OK
Client: <WavyId>:<Value>
Client: <WavyId>:<Value>
...
Client: END
Server: 100 OK
```

### Server Commands

```
media <topic>   # Example: media temperature
min <topic>     # Example: min humidity
max <topic>     # Example: max pressure
```

## Data Models

### SensorData (Aggregator)
- `WavyId`: Sensor identifier
- `Topic`: Topic/sensor type
- `Value`: Measured value
- `Timestamp`: Measurement date and time
- `Processed`: Indicates if already sent to server

### SensorDataProcessed (Server)
- `WavyId`: Sensor identifier
- `Topic`: Topic/sensor type
- `Value`: Measured value
- `Timestamp`: Reception date and time

## Development

### Project Structure

```
SD/
├── Agregador/          # RabbitMQ consumer and TCP client
│   ├── Program.cs
│   ├── Agregador.csproj
│   └── Protos/
├── Servidor/           # TCP server and gRPC client
│   ├── Program.cs
│   ├── Servidor.csproj
│   └── Protos/
├── Wavy/              # Data producer for RabbitMQ
│   └── Wavy.csproj
├── Frontend/          # ASP.NET Core MVC web interface
│   ├── Controllers/
│   ├── Views/
│   └── Frontend.csproj
├── Models/            # Shared EF Core models
│   └── Models.csproj
└── README.md
```

### Build

```bash
# Build all projects
dotnet build

# Build specific project
cd <project>
dotnet build
```

### Clean

```bash
# Clean build artifacts
dotnet clean
```

## Troubleshooting

### RabbitMQ is not accessible
Check if the RabbitMQ service is running:
```bash
# Windows
rabbitmq-service.bat status
```

### Database connection error
Ensure SQL Server LocalDB is installed:
```bash
sqllocaldb info
sqllocaldb start mssqllocaldb
```

### Port 6000 already in use
Change the port in [Servidor/Program.cs:31](Servidor/Program.cs#L31) and [Agregador/Program.cs:124](Agregador/Program.cs#L124).

## License

Academic project - Distributed Systems

## Authors

Developed as part of the Distributed Systems practical assignment.
