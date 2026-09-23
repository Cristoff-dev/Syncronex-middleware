flowchart TD
    %% 1. Event Source
    SimulatedClient("Shopify Webhook Simulator<br>Order Placed Event") -->|"POST /api/webhooks/receive"| APIMiddleware

    %% 2. Ingestion Gateway (Web API)
    APIMiddleware[".NET 10 Minimal API<br>Feature: Webhooks Intake"]
    APIMiddleware -.->|"Fast Validation &<br>HTTP 202 Accepted"| SimulatedClient
    APIMiddleware -->|"Audit Log"| SerilogLogs("Serilog: Structured JSON Logs")
    APIMiddleware -->|"1. Publish Validated Payload"| RabbitMQ{"RabbitMQ Message Broker<br>Webhook Ingestion Queue"}

    %% 3. Asynchronous Worker
    RabbitMQ -->|"2. Dequeue Payload"| BackgroundWorker[".NET 10 Background Worker<br>Async Queue Consumer"]

    %% 4. Resilience Layer (Polly)
    BackgroundWorker -->|"3. Execute Request via Policy"| PollyCore{"Polly Resilience Engine<br>Retry & Circuit Breaker"}
    BackgroundWorker -->|"Worker Audit Log"| SerilogLogs

    %% Subgraph: Resilience Pipeline
    subgraph PollyResilience ["Polly Resilience Engine Pipeline"]
        RetryAlgorithm("Exponential Backoff Retry<br>Intervals: 2s, 4s, 8s...") -->|"Attempt Retry"| PollyCore
        CircuitBreakerMonitor("Circuit Breaker State Machine<br>Trips after N failures") -->|"Check Health State"| PollyCore
            PollyCore -.->|"Evaluate Failure Count"| CircuitBreakerMonitor
            PollyCore -.->|"Apply Delays"| RetryAlgorithm
        
        %% Success Path
        PollyCore -->|"Path A: External API Online"| ExternalAPIs["Simulated External Services<br>Shopify / HubSpot / Accounting CRM"]

        %% Fallback Path
        PollyCore -->|"Path B: Max Retries / Circuit Open"| PostgreSQLDB[("PostgreSQL Storage<br>Dead Letter / Fallback DB")]
    end

    %% Observability & Contingency
    ExternalAPIs -.->|"Log Dispatch Success"| SerilogLogs
    PostgreSQLDB -->|"Log Dead Letter Persistence"| SerilogLogs
    PostgreSQLDB -.->|"Set Record State: Pending_Review"| SerilogLogs

    %% Manual Operations / Operations Interface
    AdminOperator(("DevOps / Admin Operator")) -.->|"Manual Review of Dead Letters"| PostgreSQLDB
    AdminOperator -.->|"Re-queue Failed Webhook"| RabbitMQ