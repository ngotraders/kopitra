# Initial Admin User Setup

This project includes automatic database initialization with a default admin user on first startup.

## Default Credentials

The initial admin user is created with the following default credentials:

- **Email**: `admin@kopitra.local`
- **Password**: `Admin@123456`
- **Display Name**: `System Administrator`

## Customizing Initial Admin

You can customize the initial admin credentials by setting environment variables:

### Local Development (local.settings.json)

```json
{
  "Values": {
    "InitialAdmin__Email": "your-admin@example.com",
    "InitialAdmin__Password": "YourSecurePassword123!",
    "InitialAdmin__DisplayName": "Your Admin Name"
  }
}
```

### Azure (Application Settings)

Set the following application settings in Azure Portal or via CLI:

- `InitialAdmin__Email`
- `InitialAdmin__Password`
- `InitialAdmin__DisplayName`

## Security Considerations

⚠️ **IMPORTANT**: Change the default password immediately after first login, especially in production environments!

## How It Works

1. On application startup, `DatabaseInitializer` runs as a hosted service
2. It checks if any users exist in the database
3. If no users exist, it creates the initial admin user with credentials from configuration
4. The password is hashed before storage using PBKDF2
5. In development mode, the password is logged for convenience

## Troubleshooting

If the admin user is not created:

1. Check the application logs for any errors
2. Ensure the database connection string is correct
3. Verify migrations are applied: `dotnet ef database update`
4. Check that `InitialAdmin__*` configuration values are set correctly
