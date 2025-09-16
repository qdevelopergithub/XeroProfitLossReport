# Xero OAuth 2.0 Integration

This project implements the complete Xero OAuth 2.0 authorization flow as described in the Xero API documentation.

## Features

✅ **Complete OAuth 2.0 Flow Implementation**
- Authorization URL generation with proper scopes
- State parameter for CSRF protection
- Code exchange for access tokens
- Token response handling with all required fields

✅ **Security Features**
- State parameter validation to prevent CSRF attacks
- Secure token storage recommendations
- Proper error handling and logging

✅ **Production Ready**
- Dependency injection configuration
- Comprehensive logging
- Swagger API documentation
- Test interface

## Quick Start

### 1. Configure Your Xero App

1. Go to [Xero Developer Portal](https://developer.xero.com/myapps)
2. Create a new app or use an existing one
3. Note down your **Client ID** and **Client Secret**
4. Set the **Redirect URI** to: `https://localhost:7290/oauth/callback`

### 2. Update Configuration

Edit `appsettings.json` and replace the placeholder values:

```json
{
  "Xero": {
    "ClientId": "YOUR_ACTUAL_CLIENT_ID",
    "ClientSecret": "YOUR_ACTUAL_CLIENT_SECRET",
    "RedirectUri": "https://localhost:7290/oauth/callback",
    "AuthorizationUrl": "https://login.xero.com/identity/connect/authorize",
    "TokenUrl": "https://identity.xero.com/connect/token"
  }
}
```

### 3. Run the Application

```bash
dotnet run
```

The application will start on:
- HTTPS: `https://localhost:7290`
- HTTP: `http://localhost:5096`

### 4. Test the OAuth Flow

1. Navigate to `https://localhost:7290` to see the test interface
2. Click "Authorize with Xero" to start the OAuth flow
3. Complete the authorization in Xero
4. You'll be redirected back with the tokens

## API Endpoints

### OAuth Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/oauth/authorize` | GET | Initiates OAuth flow, redirects to Xero |
| `/oauth/callback` | GET | Handles Xero callback and exchanges code for tokens |
| `/oauth/status` | GET | Returns OAuth configuration status |

### Documentation

- **Swagger UI**: `https://localhost:7290/swagger`
- **Test Interface**: `https://localhost:7290`

## OAuth Flow Implementation

### Step 1: Authorization Request

The application generates the authorization URL with the following parameters:

```
https://login.xero.com/identity/connect/authorize?
  response_type=code&
  client_id=YOUR_CLIENT_ID&
  redirect_uri=https://localhost:7290/oauth/callback&
  scope=openid profile email accounting.transactions&
  state=RANDOM_STATE_STRING
```

### Step 2: Code Exchange

After user authorization, Xero redirects back with a code. The application then exchanges this code for tokens:

```http
POST https://identity.xero.com/connect/token
Authorization: Basic base64(client_id:client_secret)
Content-Type: application/x-www-form-urlencoded

grant_type=authorization_code&
code=AUTHORIZATION_CODE&
redirect_uri=https://localhost:7290/oauth/callback
```

### Step 3: Token Response

The response includes:

```json
{
  "access_token": "JWT_ACCESS_TOKEN",
  "id_token": "JWT_ID_TOKEN",
  "refresh_token": "REFRESH_TOKEN",
  "expires_in": 1800,
  "token_type": "Bearer",
  "scope": ["openid", "profile", "email", "accounting.transactions"]
}
```

## Token Information

### Access Token (JWT)
- **Expires**: 30 minutes
- **Contains**: User information, authentication event ID, scopes
- **Usage**: Authenticate API requests to Xero

### ID Token (JWT)
- **Expires**: 5 minutes
- **Contains**: User identity information
- **Usage**: Identify the authenticated user

### Refresh Token
- **Expires**: 60 days
- **Usage**: Obtain new access tokens when they expire

## Security Considerations

1. **State Parameter**: Used to prevent CSRF attacks
2. **HTTPS Required**: Xero requires HTTPS for redirect URIs
3. **Token Storage**: Store tokens securely (database, encrypted)
4. **Token Refresh**: Implement refresh token logic for long-running apps

## Production Deployment

For production deployment:

1. **Update Redirect URI**: Change to your production domain
2. **Environment Variables**: Use secure configuration management
3. **Token Storage**: Implement secure token storage (database)
4. **Session Management**: Use proper session/cookie management
5. **HTTPS**: Ensure HTTPS is properly configured

## Troubleshooting

### Common Issues

1. **Invalid Redirect URI**: Ensure the URI in your Xero app matches exactly
2. **Client ID/Secret Mismatch**: Verify your credentials in appsettings.json
3. **HTTPS Required**: Xero requires HTTPS for production redirect URIs
4. **State Mismatch**: Ensure state parameter is properly handled

### Debug Information

Check the application logs for detailed error information. The service includes comprehensive logging for troubleshooting.

## Next Steps

After successful OAuth implementation:

1. **Store Tokens Securely**: Implement database storage for tokens
2. **Add Token Refresh**: Implement refresh token logic
3. **Make Xero API Calls**: Use the access token to call Xero APIs
4. **Add User Management**: Implement user session management
5. **Error Handling**: Add comprehensive error handling and user feedback

## Support

For issues related to:
- **Xero API**: Check [Xero Developer Documentation](https://developer.xero.com/documentation)
- **This Implementation**: Check the application logs and Swagger documentation
