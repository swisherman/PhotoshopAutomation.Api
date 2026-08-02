# PhotoshopAutomation.Api

![.NET 10](https://img.shields.io/badge/.NET-10-purple)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Web%20API-blue)
![MongoDB](https://img.shields.io/badge/MongoDB-Database-47A248)
![Docker](https://img.shields.io/badge/Docker-Enabled-2496ED)
![REST API](https://img.shields.io/badge/API-REST-success)
![Status](https://img.shields.io/badge/Status-Active_Development-success)

PhotoshopAutomation.Api is the central orchestration service for the Mockup Workflow Platform. It coordinates workflow batches, manages processing state, exposes REST endpoints, and enables communication between Adobe Photoshop, MongoDB, the administration dashboard, and supporting services.

The API imports production manifests, persists workflow data in MongoDB, tracks batch progress, and provides the services required for automated mockup generation and workflow monitoring across the platform.

## Screenshots

### Swagger Overview

![Swagger Overview](docs/images/swagger-overview.png)

### Import Endpoint

![Import Endpoint](docs/images/import-endpoint.png)

### Batch Management Endpoint

![Batch Endpoint](docs/images/batch-endpoint.png)

## Features

### Workflow Orchestration

Coordinates the end-to-end processing of production batches across the Mockup Workflow Platform.

Responsibilities include:

- Managing workflow state
- Coordinating processing stages
- Tracking batch progress
- Exposing REST endpoints for connected services

---

### Batch Import

Imports production manifests into MongoDB.

During import the API:

- Stores product records
- Prevents duplicate imports using `SourceKey`
- Assigns workflow metadata
- Supports multiple product types
- Creates batch-ready workflow records

---

### Batch Management

Organizes imported products into processing batches.

Provides:

- Batch summaries
- Processing statistics
- Product counts
- Workflow progress
- Pending batch discovery

---

### Processing State Management

Tracks workflow progress throughout the processing pipeline.

Supports state transitions such as:

- Imported
- Ready
- Processing
- Mockup Complete
- Failed

This enables multiple services to safely participate in the same workflow.

---

### REST API

Exposes REST endpoints for platform components including:

- Photoshop UXP Plugin
- MockupWorkflow.Admin
- FolderCreator.Api
- BuildUploader

---

### MongoDB Integration

Uses MongoDB as the platform's central workflow database.

Stores:

- Product records
- Batch information
- Workflow metadata
- Processing status
- Mockup completion information

---

## Architecture

PhotoshopAutomation.Api is the central orchestration service of the Mockup Workflow Platform. It coordinates workflow state, exposes REST endpoints consumed by platform components, and persists production data in MongoDB.

![PhotoshopAutomation.Api Architecture](docs/images/architecture.png)

### Responsibilities

PhotoshopAutomation.Api is responsible for:

- Importing production manifests
- Persisting workflow records in MongoDB
- Organizing products into processing batches
- Managing workflow state transitions
- Exposing REST endpoints for connected services
- Tracking mockup generation progress
- Providing batch summaries and processing statistics

The Photoshop UXP Plugin retrieves workflow records from PhotoshopAutomation.Api, downloads source assets from PNGAPI, generates production-ready mockups in Adobe Photoshop, uploads completed assets, and reports processing results back to the API. MockupWorkflow.Admin monitors workflow execution while MongoDB serves as the platform's central workflow database.

---

## What This Project Demonstrates

This project demonstrates experience with:

- ASP.NET Core Web API development
- REST API design and implementation
- Workflow orchestration
- Batch processing systems
- MongoDB integration
- Distributed service communication
- Production workflow automation
- Docker-based deployment


---

## Technology Stack

| Technology | Purpose |
|------------|---------|
| **.NET 10** | Modern runtime for building high-performance applications. |
| **ASP.NET Core Web API** | Exposes REST endpoints for workflow orchestration and service communication. |
| **MongoDB** | Stores workflow records, batch information, and processing state. |
| **Docker** | Hosts the API alongside MongoDB and supporting services using Docker Compose. |
| **C#** | Primary implementation language for the API and business logic. |
| **REST APIs** | Enables communication between platform components, including the Photoshop UXP Plugin and MockupWorkflow.Admin. |
| **MockupWorkflow.Shared** | Shared domain models and contracts used across the platform. |

---
## REST Endpoints

PhotoshopAutomation.Api exposes REST endpoints used by the Photoshop UXP Plugin, MockupWorkflow.Admin, and supporting workflow services.

### Workflow Records

| Method | Endpoint | Description |
|---------|----------|-------------|
| GET | `/records` | Retrieve all workflow records. |
| POST | `/records/import` | Import production manifests into MongoDB. |
| GET | `/records/ready` | Retrieve workflow records ready for processing. |

---

### Batch Management

| Method | Endpoint | Description |
|---------|----------|-------------|
| GET | `/records/batches` | Retrieve workflow batch summaries. |
| GET | `/records/batches/{batchId}` | Retrieve all records for a batch. |
| GET | `/records/batches/{batchId}/ready` | Retrieve ready records for a batch. |
| GET | `/records/batches/pending` | Retrieve batches awaiting processing. |
---

### Workflow Processing

| Method | Endpoint | Description |
|---------|----------|-------------|
| POST | `/records/{id}/mockup-complete` | Mark a workflow record as successfully processed. |
| POST | `/records/{id}/mockup-failed` | Record a workflow processing failure. |
| PATCH | `/records/{id}/processed` | Mark a record as processed. |
| POST | `/records/batches/{batchId}/process-mockups` | Trigger mockup generation for a batch. |
| POST | `/records/batches/{batchId}/retry-failed` | Retry failed workflow items within a batch. |
---

### PSD Templates

| Method | Endpoint | Description |
|---------|----------|-------------|
| GET | `/psds` | Retrieve configured Photoshop workflow templates. |

---

### Logging

| Method | Endpoint | Description |
|---------|----------|-------------|
| POST | `/log` | Receive client log messages from the Photoshop UXP Plugin. |

---

### Workflow Lifecycle

A typical production batch progresses through the following stages:

1. Import a production manifest.
2. Create workflow records in MongoDB.
3. Retrieve pending batches.
4. Process records through the Photoshop UXP Plugin.
5. Report successful or failed processing.
6. Monitor workflow progress through MockupWorkflow.Admin.

## Related Projects

### MockupWorkflow.Platform

The umbrella repository that brings together the platform's services, documentation, and deployment resources.

---

### Photoshop UXP Plugin

Retrieves workflow records from PhotoshopAutomation.Api, performs Photoshop automation, and reports processing results back to the API.

---

### MockupWorkflow.Admin

Blazor Server administration dashboard used to monitor workflow progress, review batches, and manage processing operations through the API.

---

### MockupWorkflow.Shared

Shared domain models, DTOs, and MongoDB entities used across the platform to maintain consistency between services.

---

### PNGAPI

Stores and serves source artwork and generated mockups. The Photoshop UXP Plugin downloads source assets from PNGAPI and uploads completed mockups during workflow execution.

---

### FolderCreator.Api

Creates the directory structure required for new production batches before they enter the workflow.

---

## Roadmap

### Near-Term Enhancements

- Configurable workflow definitions stored in MongoDB
- Additional workflow processors for new product types
- Expanded validation and error reporting
- Enhanced batch filtering and search capabilities
- Improved workflow monitoring and diagnostics

---

### Future Enhancements

- Authentication and role-based authorization
- Workflow metrics and telemetry
- Cloud storage integration
- Distributed background processing
- Workflow scheduling and automation
- Additional platform integrations
---

**Status:** Active Development

PhotoshopAutomation.Api serves as the orchestration layer for the Mockup Workflow Platform. Core workflow orchestration, batch processing, and processing state management are complete. Current development is focused on expanding workflow capabilities, diagnostics, and platform integrations.

---

