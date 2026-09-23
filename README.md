# Syncronex: Enterprise-Grade Sync Middleware 🚀

Syncronex is a robust, asynchronous middleware solution built with **.NET 10** designed to reliably ingest webhooks (e.g., from Shopify) and distribute them to external systems (CRM, Accounting). It implements a modular monolith architecture, background processing, and advanced resilience patterns to ensure zero data loss during high-load or system outages.

## 🏗️ Architecture & Technologies

This project goes beyond simple CRUD operations, demonstrating enterprise-level engineering practices:

*   **Framework:** .NET 10 Minimal APIs
*   **Architecture:** Modular Monolith (Vertical Slice approach)
*   **Message Broker:** RabbitMQ (Asynchronous decoupling via Background Workers)
*   **Database:** PostgreSQL (Entity Framework Core 10)
*   **Resilience:** Polly (Exponential Backoff Retries & Circuit Breaker)
*   **Observability:** Serilog (Structured JSON Logging & Bootstrap Logger)
*   **Infrastructure:** Containerized with Docker Compose

## 💡 Core Features

1.  **Fail-Fast Ingestion:** High-throughput Minimal API endpoint that validates incoming payloads and immediately returns `HTTP 202 Accepted`, pushing the heavy lifting to a background queue.
2.  **Asynchronous Processing:** A persistent Background Worker (`AsyncEventingBasicConsumer`) reads messages from RabbitMQ, ensuring the main thread is never blocked.
3.  **Fault Tolerance (Polly):** Integration with external APIs is protected by Polly. If an external service goes down, the system automatically applies exponential backoff retries.
4.  **Dead-Letter Contingency:** If all retries fail (or a Circuit Breaker opens), the payload is safely persisted to a PostgreSQL `dead_letters` table for manual review, ensuring **zero data loss**.

## 🚀 How to Run Locally

### Prerequisites
*   [.NET 10 SDK](https://dotnet.microsoft.com/download)
*   [Docker Desktop](https://www.docker.com/products/docker-desktop)

### 1. Start Infrastructure
Run the following command to spin up the RabbitMQ broker and the PostgreSQL database:

    docker compose up -d

### 2. Apply Database Migrations
Navigate to the API folder and create the required tables:

    cd src/Syncronex.Api
    dotnet ef database update

### 3. Run the Application
Start the .NET engine:

    dotnet run

### 4. Test the Workflow
Send a POST request to `http://localhost:5271/api/webhooks/receive` using the provided sample JSON. Watch the console logs as the system queues the message, attempts to send it to the mock CRM, applies retry policies, and eventually saves it to the PostgreSQL Dead Letter table.

## 👨‍💻 Author
**Cristofer Aranguren** - Associate Degree in Computer Science
