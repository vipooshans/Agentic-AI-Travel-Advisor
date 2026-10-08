# Agentic AI Travel Advisor & Booking System

The Agentic AI Travel Advisor & Booking System is designed to simplify travel planning by combining a Flutter mobile application, an ASP.NET web application, and a centralized PostgreSQL database.

This repository contains the complete solution for an AI-powered travel planning and booking platform with multiple client apps and a shared backend.

## Overview

The project includes:

- A backend API built with ASP.NET Core
- An AI-powered travel planning assistant with structured, schema-validated tool usage
- A React web application for all user roles
- An ASP.NET MVC portal for hotel owners, travel agents, and admins
- A Flutter mobile app for travelers
- PostgreSQL-based persistence and integration with a demo catalog and user roles

## Repository Structure

```text
.
├── Agentic-AI-Travel-Advisor/    # Main project solution
│   ├── agentic-ai/               # AI orchestration and planner logic
│   ├── database/                # Database scripts / schema references
│   ├── documentation/           # Architecture, API, database docs
│   ├── mobile-app/              # Flutter traveler app
│   ├── scripts/                 # Setup and automation scripts
│   ├── shared/                  # Shared core and infrastructure code
│   ├── testing/                 # Test plans, test cases, and execution results
│   ├── tests/                   # Automated test projects
│   ├── web-api/                 # ASP.NET Core backend API
│   ├── web-app/                 # ASP.NET Core MVC portal
│   ├── web-react/               # React + TypeScript web client
│   ├── .env.example             # Example environment variables
│   ├── docker-compose.yml       # Docker services for PostgreSQL and apps
│   ├── README.md                # Detailed project documentation
│   └── TravelAdvisor.slnx       # Solution file
├── docs/                        # Additional repository docs
├── .gitignore
└── README.md                   # Repository overview (this file)
```

## Key Features

- AI-guided trip planning and recommendation workflows
- Booking lifecycle management with pending/confirmed/completed states
- Role-based authentication and authorization
- Multi-platform clients for travelers and staff
- Docker-based local setup and deployment
- Test suites for backend, frontend, API contract, security, and performance

## Quick Start

Use the main project folder for development and deployment:

```bash
cd Agentic-AI-Travel-Advisor
cp .env.example .env
# Set POSTGRES_PASSWORD and JWT_KEY before starting the stack

docker compose up --build
```

Once the stack is running:

- API + Swagger: http://localhost:5000/swagger
- MVC portal: http://localhost:7000
- PostgreSQL host: localhost:5433

## Detailed Documentation

The main project contains full architecture and setup documentation:

- [Agentic-AI-Travel-Advisor/README.md](Agentic-AI-Travel-Advisor/README.md)
- [Agentic-AI-Travel-Advisor/documentation/ARCHITECTURE.md](Agentic-AI-Travel-Advisor/documentation/ARCHITECTURE.md)
- [Agentic-AI-Travel-Advisor/documentation/API.md](Agentic-AI-Travel-Advisor/documentation/API.md)
- [Agentic-AI-Travel-Advisor/documentation/DATABASE.md](Agentic-AI-Travel-Advisor/documentation/DATABASE.md)
- [Agentic-AI-Travel-Advisor/testing/README.md](Agentic-AI-Travel-Advisor/testing/README.md)

## Tech Stack

- ASP.NET Core
- PostgreSQL
- React + TypeScript
- Flutter
- Docker
- OpenAI-compatible AI integration

## License

This repository does not appear to include a license file in the root. Please check the project folder or repository settings for licensing terms before redistribution or commercial use.

## Notes

The main project README is located in the nested solution directory and contains the most complete setup instructions, configuration values, and testing guidance.
