
## Azure Blob Storage
Image uploads (event banners and profile pictures) are handled by the API (`ImagesController`) and stored in Azure Blob Storage, in the `raceday-images` container. The MVC app never talks to Azure: it sends files to the API and loads images through the API.
Configure the API with `dotnet user-secrets set "AzureStorage:ConnectionString" "<connection string>" --project RaceDay.Api`.

## CI/CD
GitHub Actions builds the solution, the MVC project, and runs the tests on every push.
