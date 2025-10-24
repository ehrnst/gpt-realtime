# Security

## Security Features

### 1. Token-Based Authentication

- **Short-lived tokens**: Tokens expire after 5 minutes
- **Server-side generation**: Tokens are generated only on the backend
- **Validation**: All WebSocket connections are validated with tokens

### 2. API Key Protection

- **Never exposed to frontend**: OpenAI API keys are only stored on the backend
- **Environment variables**: Keys are configured via environment variables, not hardcoded
- **Azure Key Vault integration**: Recommended for production deployments

### 3. CORS Configuration

- **Whitelist origins**: Only specified frontend origins are allowed
- **Credentials support**: Properly configured for secure cross-origin requests

### 4. WebSocket Security

- **Token validation**: WebSocket connections require valid tokens
- **Auto-cleanup**: Expired tokens are automatically removed
- **Connection limits**: Prevents token reuse attacks

## Security Best Practices

### Development

1. **Never commit `.env` files**
   - Use `.env.example` as template
   - Add `.env` to `.gitignore`

2. **Use HTTPS in production**
   - Configure SSL/TLS certificates
   - Use secure WebSocket (wss://)

3. **Keep dependencies updated**
   ```bash
   # Backend
   dotnet list package --outdated
   
   # Frontend
   npm audit
   npm update
   ```

### Production Deployment

1. **Use Azure Key Vault or similar**
   ```bash
   az keyvault secret set --vault-name <vault-name> --name openai-api-key --value <your-key>
   ```

2. **Enable Managed Identity**
   - Use Azure Managed Identity for Key Vault access
   - Avoid storing secrets in environment variables

3. **Configure WAF (Web Application Firewall)**
   - Use Azure Front Door or Application Gateway
   - Enable DDoS protection

4. **Implement Rate Limiting**
   - Add rate limiting to token endpoint
   - Limit WebSocket connections per IP

5. **Enable Logging and Monitoring**
   - Use Application Insights
   - Monitor for unusual patterns
   - Set up alerts for security events

### Input Validation

The application validates:
- Token format and expiration
- WebSocket message types
- Audio data encoding

### Known Limitations

1. **Browser-based audio capture**: Requires HTTPS in production
2. **Token storage**: Currently in-memory (use Redis for distributed systems)
3. **Rate limiting**: Not implemented (add in production)

## Reporting Security Issues

Please report security vulnerabilities to security@example.com or open a private security advisory on GitHub.

## Compliance

- **GDPR**: No user data is stored permanently
- **Audio processing**: All audio is processed in real-time and not stored
- **Logs**: Configure log retention policies per compliance requirements

## Security Checklist for Production

- [ ] HTTPS/TLS configured
- [ ] API keys stored in secure vault
- [ ] CORS origins restricted to production domains
- [ ] Rate limiting implemented
- [ ] WAF enabled
- [ ] Monitoring and alerting configured
- [ ] Regular dependency updates scheduled
- [ ] Security headers configured (CSP, HSTS, etc.)
- [ ] Token expiry tested
- [ ] WebSocket connections limited per user

## Updates

This security document should be reviewed quarterly and updated as needed.

Last Updated: 2024-10-24
