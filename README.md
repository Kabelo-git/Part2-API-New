\# RaceDay API (Part 2)



RESTful API for RaceDay, an event management system for South African road running, walking and cycling events. It supports Organiser and Participant roles.



\## Tech Stack

\- C# / ASP.NET Core Web API (.NET 8.0)

\- Entity Framework Core (SQL Server)

\- xUnit integration tests

\- GitHub Actions (CI)



\## Prerequisites

\- .NET 8.0 SDK

\- SQL Server installed and running

\- Git



\## Setup Instructions

1\. Clone the repository:

&#x20;  ```

&#x20;  git clone https://github.com/Kabelo-git/Part2-API-New.git

&#x20;  cd Part2-API-New

&#x20;  ```

2\. Open `RaceDay.Api/appsettings.json` and set the connection string for your own database.

3\. Apply the database migrations:

&#x20;  ```

&#x20;  dotnet ef database update --project RaceDay.Api

&#x20;  ```

4\. Run the API:

&#x20;  ```

&#x20;  dotnet run --project RaceDay.Api

&#x20;  ```

5\. Open the endpoints shown in the console output to test the API.



\## Running the Tests

```

dotnet test

```



\## API Overview

\- \*\*Auth\*\*: register and log in

\- \*\*Events\*\*: create, view, update and delete events

\- \*\*Categories\*\*: manage race categories for an event

\- \*\*Enrolments\*\*: enrol participants in events

\- \*\*Profile\*\*: view and update the user profile

\- \*\*Results\*\*: capture and view race results



\## CI/CD

The GitHub Actions workflow in `.github/workflows/part2-build-test.yml` builds the project and runs the tests on every push.



!\[CI/CD screenshot](docs/ci-cd.png)



\## Demo Video

YouTube (unlisted): PASTE-YOUR-LINK-HERE

