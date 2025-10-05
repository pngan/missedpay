# MissedPay

A multi-tenant financial management application built with .NET Aspire, PostgreSQL, and React.

## Setup

### Environment Variables

This project uses multiple methods to store sensitive API credentials:

#### Option 1: Environment Variables (.env file)

1. Copy `.env.example` to `.env`:
   ```bash
   cp .env.example .env
   ```

2. Edit `.env` and add your actual Akahu API credentials:
   ```
   AKAHU_USER_TOKEN=your_actual_user_token
   AKAHU_APP_TOKEN=your_actual_app_token
   ```

3. **Important**: The `.env` file is already in `.gitignore` and will not be committed to version control.

#### Option 2: appsettings.Development.json (Recommended for local development)

1. Copy `missedpay.ApiService/appsettings.Development.json.example` to `appsettings.Development.json`:
   ```bash
   cp missedpay.ApiService/appsettings.Development.json.example missedpay.ApiService/appsettings.Development.json
   ```

2. Edit `appsettings.Development.json` and add your actual tokens in the `Akahu` section:
   ```json
   {
     "Akahu": {
       "UserToken": "your_actual_user_token",
       "AppToken": "your_actual_app_token"
     }
   }
   ```

3. **Important**: `appsettings.Development.json` is in `.gitignore` and will not be committed.

**Note**: Environment variables take precedence over appsettings.json values.

### Getting Akahu API Tokens

1. Sign up at [Akahu Developers](https://developers.akahu.nz/)
2. Create an application to get your App Token
3. Complete the OAuth flow to get a User Token

## Project Structure

- `missedpay.AppHost` - .NET Aspire orchestration
- `missedpay.ApiService` - REST API with multi-tenancy
- `missedpay.Web` - Blazor web application
- `missedpay.Frontend` - React frontend application
- `missedpay.ServiceDefaults` - Shared service configuration

## Multi-Tenancy

The application implements production-ready multi-tenancy using:
- UUIDv7 tenant IDs
- Header-based tenant resolution (`X-Tenant-Id` header)
- Global query filters in Entity Framework Core
- JWT-based tenant resolution support

See `MULTI_TENANCY.md` for detailed documentation.

## Running the Application

1. Go the AppHost directory
   ```bash
   cd missedpay.AppHost
   ```
2. Set up your `.env` file (see above)
3. Run with .NET Aspire:
   ```bash
   dotnet run
   ```

## Testing API Endpoints

Use the `missedpay.ApiService.http` file with the VS Code REST Client extension. The file automatically loads tokens from your `.env` file. 

The query to load Accounts and Transactions from Akahu is:
```
POST {{ApiService_HostAddress}}/api/Akahu/refresh-all
```
