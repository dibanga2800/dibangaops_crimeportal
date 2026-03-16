# Publish and Copy to Remote Server Guide

This guide explains how to use the `publish-and-copy-to-server.ps1` script to publish your backend locally and copy it to a remote server, and how to run the **InsightFace** facial-recognition service on the same IIS server.

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

## InsightFace on the same IIS server

When **facial recognition** is enabled (`InsightFace:Enabled = true`), the .NET API calls a separate **InsightFace** Python service over HTTP. You can run that service on the **same Windows server** as IIS so the API uses `http://127.0.0.1:8000`.

### 1. Copy InsightFace service to the server

Copy the `InsightFaceService` folder to the server (e.g. next to the API or in a dedicated path):

- From your repo: `AIP_Backend\InsightFaceService\` (include `main.py`, `requirements.txt`; do **not** copy the `models` folder—it will be created on first run).

Example (from your dev machine, if you use the same publish script style):

```powershell
# Copy InsightFace service to server (adjust paths)
$insightFaceSource = "C:\Users\David Ibanga\COOP_AIP\AIP_Backend\InsightFaceService"
$remoteInsightFace = "\\SERVERNAME\C$\inetpub\InsightFaceService"
New-Item -ItemType Directory -Path $remoteInsightFace -Force
Copy-Item "$insightFaceSource\main.py", "$insightFaceSource\requirements.txt" -Destination $remoteInsightFace
```

### 2. Install Python on the server

- Install **Python 3.8+** on the Windows server (e.g. from [python.org](https://www.python.org/downloads/) or Windows Store).
- Ensure `python` and `pip` are on the PATH (or use the full path in the steps below).

### 3. Run InsightFace on the server

On the **remote server** (RDP or PowerShell Remoting), run once to create the venv and install dependencies; then start the service:

```powershell
cd C:\inetpub\InsightFaceService
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
uvicorn main:app --host 127.0.0.1 --port 8000
```

- Binding to `127.0.0.1` keeps the service local; only the .NET API (IIS) needs to reach it.
- Leave this window open, or run it as a **Windows Service** (see below) so it keeps running after logout/reboot.

### 4. Backend config (production)

On the server, ensure `appsettings.Production.json` (in the IIS-deployed API folder) has:

```json
"InsightFace": {
  "Enabled": true,
  "BaseUrl": "http://127.0.0.1:8000",
  "TimeoutSeconds": 30
}
```

Then publish/deploy the API as usual. The API will call InsightFace at `http://127.0.0.1:8000`.

### 5. Run InsightFace as a Windows Service (optional)

To have InsightFace start automatically and survive reboots, install it as a Windows Service using **NSSM** (Non-Sucking Service Manager) or a similar tool:

1. Download [NSSM](https://nssm.cc/download).
2. Install the service (run as Administrator on the server):

```powershell
nssm install InsightFaceService "C:\inetpub\InsightFaceService\.venv\Scripts\python.exe" "-m uvicorn main:app --host 127.0.0.1 --port 8000"
nssm set InsightFaceService AppDirectory "C:\inetpub\InsightFaceService"
nssm start InsightFaceService
```

Adjust paths if you placed InsightFace in a different folder. After that, the service starts with Windows and restarts on failure if configured in NSSM.

### Summary

| Step | Action |
|------|--------|
| 1 | Copy `InsightFaceService` (main.py, requirements.txt) to the server |
| 2 | Install Python 3.8+ on the server |
| 3 | On server: create venv, `pip install -r requirements.txt`, run `uvicorn main:app --host 127.0.0.1 --port 8000` (or install as Windows Service) |
| 4 | Set `InsightFace:BaseUrl` to `http://127.0.0.1:8000` in production config and publish the .NET API to IIS as usual |

---

## Post-Deployment Checklist

After deployment, verify:

- [ ] `appsettings.Production.json` exists on remote server
- [ ] Connection strings are correct
- [ ] JWT secret key is set
- [ ] SMTP credentials are configured
- [ ] **InsightFace:** If using facial recognition, InsightFace service is running on the server and `InsightFace:BaseUrl` is `http://127.0.0.1:8000`
- [ ] API responds to health check
- [ ] Application logs show no errors
- [ ] Alert rules are working correctly

## Next Steps

After successful deployment:

1. Test the API endpoint
2. Create a test incident to verify alert rules
3. Check application logs for any errors
4. Monitor for a few minutes to ensure stability
