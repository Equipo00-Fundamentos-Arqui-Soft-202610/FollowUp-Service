# MediTrack - FollowUp Service Context

## Stack
- ASP.NET Core 8 Web API
- MySql.EntityFrameworkCore 8.0.8
- DDD Architecture

## Structure
FollowUpManagement/
├── Domain/Model/Aggregates/
├── Domain/Model/Commands/
├── Domain/Model/Queries/
├── Application/Internal/CommandServices/
├── Application/Internal/QueryServices/
├── Infrastructure/Persistence/EFC/Configuration/
└── Interfaces/REST/Controllers/
Interfaces/REST/Resources/
Interfaces/REST/Transform/

## Already implemented
- US04: GET /api/v1/medications?patientId={id}
- Entities: Medication, DoseSchedule
- AppDbContext with both DbSets

## Database: followup_db (MySQL local)