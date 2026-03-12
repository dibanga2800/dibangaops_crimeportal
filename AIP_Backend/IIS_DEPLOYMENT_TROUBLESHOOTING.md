# IIS Deployment Troubleshooting Guide

## Issue: Port Binding Error (SocketException 10013)

### Problem
The application fails to start with error:
```
System.Net.Sockets.SocketException (10013): An attempt was made to access a socket in a way forbidden by its access permissions.
Failed to bind to address http://localhost:5000
```

### Root Causes

1. **Port 5000 is reserved by Windows** (common with Hyper-V)
2. **Application trying to bind to port when running under IIS** (IIS should handle binding)
3. **Missing ASP.NET Core Runtime** (causes missing DLL errors)

### Solutions Applied

#### ✅ Solution 1: Updated Program.cs
The application now detects when running under IIS and doesn't attempt to bind to specific ports. IIS handles port binding through the ASP.NET Core Module.

**Changes made:**
- Added IIS detection using environment variables
- Configured `UseIISIntegration()` for production
- Cleared default URL bindings when running under IIS

#### ✅ Solution 2: Diagnostic Script
Created `scripts/fix-port-binding.ps1` to diagnose port issues.

**To run:**
```powershell
# Run as Administrator for full functionality
cd "c:\Users\David Ibanga\COOP_AIP\AIP_Backend\scripts"
.\fix-port-binding.ps1
```

### Additional Troubleshooting Steps

#### Step 1: Verify ASP.NET Core Runtime Installation

**Check if .NET 8.0 Runtime is installed:**
```powershell
dotnet --list-runtimes
```

**Expected output should include:**
```
Microsoft.AspNetCore.App 8.0.x [/path/to/runtime]
Microsoft.NETCore.App 8.0.x [/path/to/runtime]
```

**If missing, install:**
1. Download from: https://dotnet.microsoft.com/download/dotnet/8.0
2. Install "ASP.NET Core Runtime 8.0.x - Windows Hosting Bundle"
3. This includes both the runtime and IIS module

#### Step 2: Check Port Reservations

**View reserved port ranges:**
```powershell
netsh interface ipv4 show excludedportrange protocol=tcp
```

**If port 5000 is in a reserved range:**
- Option A: Use a different port (not recommended for IIS)
- Option B: Exclude port 5000 from reservations (requires admin, complex)
- Option C: **Recommended** - Let IIS handle binding (already configured)

#### Step 3: Verify IIS Configuration

**Check ASP.NET Core Module:**
```powershell
Get-ItemProperty "HKLM:\SOFTWARE\Wow6432Node\Microsoft\IIS Extensions\ASP.NET Core Module V2" -ErrorAction SilentlyContinue
```

**Check Application Pool:**
1. Open IIS Manager
2. Navigate to Application Pools
3. Find your app pool
4. Verify:
   - .NET CLR Version: **No Managed Code** (for .NET Core/5+)
   - Managed Pipeline Mode: **Integrated**
   - Identity: **ApplicationPoolIdentity** (or appropriate account)

**Check Site Bindings:**
1. Open IIS Manager
2. Navigate to Sites → Your Site
3. Click "Bindings"
4. Verify bindings are configured (e.g., `*:80:` or `*:443:`)
5. The application should NOT bind to port 5000 directly

#### Step 4: Verify Application Deployment

**Check published files:**
- `AIPBackend.dll` should exist
- `web.config` should be in the root
- `appsettings.Production.json` should exist
- All required DLLs should be present

**For framework-dependent deployment:**
- Requires .NET runtime installed on server
- Missing DLLs (`Microsoft.AspNetCore.App.dll`, `Microsoft.AspNetCore.dll`) indicate runtime not installed

**For self-contained deployment:**
- No runtime needed on server
- All DLLs included in publish folder
- Larger deployment size

#### Step 5: Check Application Logs

**IIS stdout logs:**
- Location: `.\logs\stdout` (relative to application root)
- Check for startup errors

**Windows Event Log:**
```powershell
Get-EventLog -LogName Application -Source "IIS*" -Newest 20 | Format-List
```

**Application-specific logs:**
- Check if logging is configured in `appsettings.Production.json`
- Review any custom log files

### Deployment Checklist

- [ ] ASP.NET Core Runtime 8.0.x installed (Hosting Bundle)
- [ ] ASP.NET Core Module V2 installed
- [ ] IIS Application Pool configured correctly
- [ ] IIS Site bindings configured
- [ ] Application published to IIS directory
- [ ] `web.config` present and correct
- [ ] `appsettings.Production.json` configured
- [ ] Database connection string valid
- [ ] File permissions set correctly
- [ ] Application Pool identity has necessary permissions

### Testing the Fix

1. **Rebuild and republish the application:**
   ```powershell
   cd "c:\Users\David Ibanga\COOP_AIP\AIP_Backend"
   dotnet publish -c Release -o "C:\inetpub\wwwroot\AIPBackend"
   ```

2. **Restart IIS:**
   ```powershell
   iisreset
   ```

3. **Check if application starts:**
   - Open IIS Manager
   - Navigate to your site
   - Check if it's running (should show as "Started")
   - Try accessing the site URL

4. **Verify no port binding errors:**
   - Check Event Viewer → Windows Logs → Application
   - Look for .NET Runtime errors
   - Should not see port binding errors

### Common Issues and Solutions

#### Issue: "Missing DLLs" Error
**Solution:** Install ASP.NET Core Runtime 8.0.x Hosting Bundle

#### Issue: "500.30 In-Process Start Failure"
**Solution:** 
- Check `web.config` `hostingModel` (should be "inprocess")
- Verify runtime is installed
- Check application logs

#### Issue: "500.0 ANCM In-Process Handler Load Failure"
**Solution:**
- Verify `processPath` in `web.config` points to correct `dotnet.exe`
- Check if `dotnet` is in system PATH
- Verify runtime version matches application target framework

#### Issue: Port still binding to 5000
**Solution:**
- Verify `Program.cs` changes are deployed
- Check if `ASPNETCORE_ENVIRONMENT` is set to "Production"
- Verify IIS environment variables are set correctly

## Brute-Force Protection: IIS / Reverse Proxy Rate Limiting

Even with application-level protections in place, you should also configure IIS (or any fronting reverse proxy) to limit brute-force login traffic at the edge.

### Recommended endpoints to protect

- `POST /api/Auth/login`
- `POST /api/Auth/2fa/complete`
- `POST /api/Auth/refresh`

### Option 1: IIS Dynamic IP Restrictions

1. Install the **Dynamic IP Restrictions** module in IIS (if not already installed).
2. In IIS Manager, select your **site** → **Dynamic IP Restrictions**.
3. Configure:
   - **Deny IP address based on the number of requests over a period of time**:
     - Maximum requests: `20`
     - Time period (in seconds): `60`
   - **Deny IP address based on concurrent requests**:
     - Maximum concurrent requests: `10`
4. Set **Deny Action Type** to `Abort` or `Unauthorized` (401) depending on your monitoring needs.

This blocks obvious bots from hammering the auth endpoints before they reach ASP.NET Core.

### Option 2: web.config urlRewrite rules (coarse throttling)

You can also add URL Rewrite rules to specifically target auth endpoints:

```xml
<configuration>
  <system.webServer>
    <rewrite>
      <rules>
        <!-- Example: simple block rule for abusive IPs hitting /api/Auth/login -->
        <rule name="BlockAbusiveLoginIPs" stopProcessing="true">
          <match url="^api/Auth/login$" ignoreCase="true" />
          <conditions logicalGrouping="MatchAll">
            <!-- Replace 1.2.3.4 with actual bad IPs or IP ranges -->
            <add input="{REMOTE_ADDR}" pattern="^1\.2\.3\.4$" />
          </conditions>
          <action type="CustomResponse" statusCode="429" statusReason="Too Many Requests" statusDescription="Too Many Requests" />
        </rule>
      </rules>
    </rewrite>
  </system.webServer>
</configuration>
```

For production you would typically manage these rules via a WAF/CDN (Cloudflare, Azure Front Door, etc.), but this pattern shows how to wire IIS-level protection if needed.

### Next Steps

1. **Run the diagnostic script** to identify specific issues
2. **Verify runtime installation** using `dotnet --list-runtimes`
3. **Republish the application** with the updated `Program.cs`
4. **Test the deployment** following the testing steps above

### Additional Resources

- [ASP.NET Core IIS Deployment](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/)
- [Troubleshoot ASP.NET Core on IIS](https://docs.microsoft.com/en-us/aspnet/core/test/troubleshoot-azure-iis)
- [Windows Port Exclusions](https://docs.microsoft.com/en-us/troubleshoot/windows-client/networking/tcpip-port-exclusion-range)

---

**Last Updated:** Based on error diagnosis from 2025-12-05
**Status:** Program.cs updated to prevent port binding under IIS
