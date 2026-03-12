# Publish and Copy to Remote Server Guide

This guide explains how to use the `publish-and-copy-to-server.ps1` script to publish your backend locally and copy it to a remote server.

## Quick Start

### Basic Usage (Network Share)

If you have a network share or admin share access:

```powershell
cd "C:\Users\David Ibanga\COOP_AIP\AIP_Backend\deployment"
.\publish-and-copy-to-server.ps1 `
  -RemoteServerPath "\\SERVERNAME\C$\inetpub\AIPBackend"
```

### With Credentials

If you need to authenticate:

```powershell
.\publish-and-copy-to-server.ps1 `
  -RemoteServerPath "\\SERVERNAME\C$\inetpub\AIPBackend" `
  -RemoteServerName "SERVERNAME" `
  -RemoteServerUser "DOMAIN\username" `
  -RemoteServerPassword "password"
```

### With IIS Restart

To also restart IIS on the remote server:

```powershell
.\publish-and-copy-to-server.ps1 `
  -RemoteServerPath "\\SERVERNAME\C$\inetpub\AIPBackend" `
  -RemoteServerName "SERVERNAME" `
  -RemoteServerUser "DOMAIN\username" `
  -RemoteServerPassword "password" `
  -RestartIIS
```

## Parameters

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| `ProjectPath` | Path to `.csproj` file | No | `..\AIPBackend.csproj` |
| `LocalPublishPath` | Local folder to publish to | No | `.\publish` |
| `RemoteServerPath` | **Remote server path** (UNC path) | **Yes** | None |
| `RemoteServerName` | Server name for IIS restart | No | None |
| `RemoteServerUser` | Username for authentication | No | None |
| `RemoteServerPassword` | Password for authentication | No | None |
| `SiteName` | IIS site name (for restart) | No | `AIPBackend` |
| `RestartIIS` | Restart IIS after deployment | No | `$false` |

## Examples

### Example 1: Using Admin Share (C$)

```powershell
.\publish-and-copy-to-server.ps1 `
  -RemoteServerPath "\\ADV1SERVER\C$\inetpub\AIPBackend"
```

### Example 2: Using Network Share

```powershell
.\publish-and-copy-to-server.ps1 `
  -RemoteServerPath "\\ADV1SERVER\AIPBackendShare"
```

### Example 3: With Domain Credentials

```powershell
.\publish-and-copy-to-server.ps1 `
  -RemoteServerPath "\\ADV1SERVER\C$\inetpub\AIPBackend" `
  -RemoteServerName "ADV1SERVER" `
  -RemoteServerUser "DOMAIN\yourusername" `
  -RemoteServerPassword "yourpassword" `
  -RestartIIS
```

### Example 4: Custom Project Path

```powershell
.\publish-and-copy-to-server.ps1 `
  -ProjectPath "C:\Users\David Ibanga\COOP_AIP\AIP_Backend\AIPBackend.csproj" `
  -RemoteServerPath "\\ADV1SERVER\C$\inetpub\AIPBackend"
```

## What the Script Does

1. ✅ **Validates** project file exists
2. ✅ **Cleans** local publish directory
3. ✅ **Publishes** application in Release mode
4. ✅ **Verifies** remote server access
5. ✅ **Creates backup** of existing deployment
6. ✅ **Copies** files using Robocopy (reliable and fast)
7. ✅ **Optionally restarts IIS** on remote server
8. ✅ **Cleans up** temporary network drives

## Troubleshooting

### "Cannot access remote server path"

**Solution:** 
- Ensure the server path is correct (use `\\SERVERNAME\C$\path` format)
- Check if you have network access to the server
- Verify admin share (C$) is accessible
- Try providing credentials with `-RemoteServerUser` and `-RemoteServerPassword`

### "Cannot write to remote path"

**Solution:**
- Check file permissions on the remote server
- Ensure the account has write access to `C:\inetpub\AIPBackend`
- Try running as Administrator

### "Robocopy failed"

**Solution:**
- Check network connectivity
- Verify the remote path exists
- Ensure sufficient disk space on remote server
- Check Windows Firewall settings

### IIS Restart Not Working

**Solution:**
- Ensure `-RemoteServerName` is provided
- Verify PowerShell Remoting is enabled on the server
- Check WinRM service is running
- Ensure you have admin rights on the remote server

## Security Notes

⚠️ **Important:**
- Never hardcode passwords in scripts
- Consider using `Read-Host -AsSecureString` for password input
- Use Windows Credential Manager for storing credentials
- Use service accounts with minimal required permissions

### Secure Password Input (Alternative)

Instead of passing password as parameter, use:

```powershell
$securePassword = Read-Host -Prompt "Enter password" -AsSecureString
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
$password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

.\publish-and-copy-to-server.ps1 `
  -RemoteServerPath "\\SERVERNAME\C$\inetpub\AIPBackend" `
  -RemoteServerUser "DOMAIN\username" `
  -RemoteServerPassword $password
```

## Post-Deployment Checklist

After deployment, verify:

- [ ] `appsettings.Production.json` exists on remote server
- [ ] Connection strings are correct
- [ ] JWT secret key is set
- [ ] SMTP credentials are configured
- [ ] API responds to health check
- [ ] Application logs show no errors
- [ ] Alert rules are working correctly

## Next Steps

After successful deployment:

1. Test the API endpoint
2. Create a test incident to verify alert rules
3. Check application logs for any errors
4. Monitor for a few minutes to ensure stability
