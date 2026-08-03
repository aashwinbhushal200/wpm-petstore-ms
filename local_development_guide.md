# Local Development Guide (with Azure Managed Identity)

Now that the solution uses Azure Managed Identity (via `DefaultAzureCredential` and `Authentication=Active Directory Default`), you no longer use passwords to connect to the Azure SQL Database or Azure Service Bus.

When running the project locally, the `Azure.Identity` library automatically tries to find your personal Azure AD credentials from your local development environment. It will check for credentials in the following order:
1. Environment Variables (if set)
2. Developer tools like **Visual Studio** or **VS Code** (the account you are logged in with)
3. The **Azure CLI** (`az login`)
4. The **Azure Developer CLI** (`azd auth login`)
5. Azure PowerShell

## Prerequisites for Local Development

To run this solution locally, you must authenticate your local environment and ensure your personal Azure account has the right permissions in the Azure Portal.

### Step 1: Authenticate Locally
You must be signed into your Azure account locally using the **same account** that has permissions to the Azure resources.
The easiest way is to use the Azure CLI:
1. Install the [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) if you haven't already.
2. Open a terminal (PowerShell/Command Prompt) and run:
   ```bash
   az login
   ```
3. A browser window will open. Log in with your Azure credentials.
*(Alternatively, you can sign in to Visual Studio under **Tools > Options > Azure Service Authentication**, or sign into the Azure Account extension in VS Code).*

### Step 2: Grant Your Azure Account Access to SQL
Since the connection string (`Authentication=Active Directory Default`) uses your personal credential, you need to add your personal Azure account as a user to your Azure SQL Databases (`ManagementDb`, `ClinicDb`, `BillingDb`).

Connect to your database via SSMS or the Azure Portal query editor (using an admin account) and run:
```sql
-- Replace 'your.email@domain.com' with your actual Azure login email
CREATE USER [your.email@domain.com] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [your.email@domain.com];
ALTER ROLE db_datawriter ADD MEMBER [your.email@domain.com];
ALTER ROLE db_ddladmin ADD MEMBER [your.email@domain.com]; -- If you need to run EF Migrations locally
```
*(Repeat this for each database your APIs connect to).*

### Step 3: Grant Your Azure Account Access to Service Bus
For the Clinic API and Billing API to communicate locally with Azure Service Bus (`petstoredev`):
1. Go to the Service Bus namespace in the Azure Portal.
2. Navigate to **Access control (IAM) > Add role assignment**.
3. Select **Azure Service Bus Data Owner** (so you can send and receive).
4. Assign access to **User, group, or service principal** and select your personal Azure account email.
5. Click **Review + assign**.

## (Optional) Running with Local Databases Instead
If you don't want to connect to Azure while developing locally, you can override the connection strings in `appsettings.Development.json` for each API to point to a local SQL Server or LocalDB instance. 

Example `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "ClinicDb": "Server=(localdb)\\MSSQLLocalDB;Database=ClinicDb_Local;Trusted_Connection=True;"
  }
}
```
*Note: Service Bus cannot be easily emulated locally without 3rd party tools, so it's recommended to continue using a shared dev Service Bus in Azure even if using a local database.*
