# Azure Portal Configuration for Billing API

Based on your GitHub Actions workflow updates, here are the steps required in the Azure Portal to successfully deploy and run the **Billing API**.

## 1. Create the Container App
The `azure/container-apps-deploy-action@v2` action deploys to an existing Azure Container App. You must create the app first so the workflow can target it.

1. Go to the Azure Portal and navigate to your resource group: `pet-store-microservice`.
2. Click **Create** and search for **Container App**.
3. Use the following details:
   - **Container App Name**: `wpm-billing-containerapp` (Must exactly match the `containerAppName` in `main.yml`).
   - **Container Apps Environment**: Select the existing environment used by `wpm-containerapp` and `wpm-clinic-containerapp`.
4. In the **Container Details** tab:
   - Check the box to use an existing image (you can use a quickstart image for now, as the GitHub Action will overwrite it during the first successful run).
5. Click **Review + create** and then **Create**.

## 2. Configure Managed Identity for SQL & Service Bus
The Billing API now uses **Azure Managed Identity** (via `DefaultAzureCredential`) instead of passwords or Shared Access Keys. This requires enabling a System-Assigned Managed Identity on the Container App and granting it access to both Azure SQL and Service Bus.

### Enable Managed Identity:
1. Navigate to your new Container App (`wpm-billing-containerapp`).
2. Go to **Settings > Identity**.
3. Under the **System assigned** tab, set **Status** to **On** and click **Save**.
4. Note the generated **Object (principal) ID**.

### Grant Access to Azure Service Bus:
1. Go to your Service Bus namespace (`petstoredev`).
2. Navigate to **Access control (IAM) > Add role assignment**.
3. Select the role **Azure Service Bus Data Receiver** (and **Azure Service Bus Data Sender** if the API will send messages).
4. Assign access to **Managed identity**, select your `wpm-billing-containerapp`, and click **Review + assign**.

### Grant Access to Azure SQL:
1. Connect to your Azure SQL Database (`BillingDb`) using an Azure Active Directory admin account (via SSMS, Azure Data Studio, or the query editor in the portal).
2. Run the following SQL commands to create a user for the Managed Identity and grant it permissions:
   ```sql
   CREATE USER [wpm-billing-containerapp] FROM EXTERNAL PROVIDER;
   ALTER ROLE db_datareader ADD MEMBER [wpm-billing-containerapp];
   ALTER ROLE db_datawriter ADD MEMBER [wpm-billing-containerapp];
   ```

## 3. Verify ACR Permissions
Since your GitHub action uses ACR credentials (`acrUsername`, `acrPassword`), ensure the Container App has the ability to pull from your Azure Container Registry.
1. When setting up the container app, or under **Settings > Registry**, ensure the registry is linked using the Admin credentials or a Managed Identity with `AcrPull` permissions.

## Summary of `main.yml` Fixes Applied
I have also corrected a few issues in your `main.yml` modifications:
- **Image Names**: Changed `wpm-BillingApi-api` to `wpm-billing-api` (Docker image names must be entirely lowercase).
- **Container App Name**: Changed `wpm-BillingApi-containerapp` to `wpm-billing-containerapp` (Azure Container App names generally restrict uppercase letters and should match naming conventions).
- **Deploy Target**: Fixed `imageToDeploy` in the Billing deploy step, which was mistakenly still pointing to `${{ env.CLINIC_IMAGE_TAG }}` instead of `${{ env.BILLINGAPI_IMAGE_TAG }}`.
- **Environment Variables**: Removed `Wpm__ManagementBaseUrl` from the Billing API's deployment step, as this config is meant for the Clinic API, not the Billing API.
