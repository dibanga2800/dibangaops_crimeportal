# InsightFace Hosting Plan

Step-by-step guide to host the InsightFace facial recognition service on your Windows server alongside your .NET API.

---

## Prerequisites

- Remote Windows server with IIS (where your backend already runs)
- RDP or PowerShell Remoting access to the server
- Python 3.8+ installer (or download during setup)

---

## Step 1: Copy InsightFace service files to the server

From your dev machine, copy the service files to the server. **Do not copy the `models` folder**—it will be downloaded automatically on first run.

**Files to copy:**
- `AIP_Backend/InsightFaceService/main.py`
- `AIP_Backend/InsightFaceService/requirements.txt`

**Example (PowerShell from your machine):**

```powershell
$source = "C:\Users\David Ibanga\COOP_AIP\AIP_Backend\InsightFaceService"
$dest = "\\ADV1SERVER\C$\inetpub\InsightFaceService"
New-Item -ItemType Directory -Path $dest -Force
Copy-Item "$source\main.py", "$source\requirements.txt" -Destination $dest
```

Or manually: copy the folder to `C:\inetpub\InsightFaceService` on the server (excluding `models`).

---

## Step 2: Install Python on the server

1. RDP into the server.
2. Download **Python 3.10** or 3.11 from [python.org/downloads](https://www.python.org/downloads/windows/).
3. Run the installer.
4. **Enable** “Add Python to PATH”.
5. Choose “Install for all users” if possible.
6. Complete the installation.
7. Open a **new** PowerShell or CMD and confirm: `python --version`

---

## Step 3: Create virtual environment and install dependencies

On the server, in PowerShell:

```powershell
cd C:\inetpub\InsightFaceService
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
```

If you get an execution policy error when activating:

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

First run will download InsightFace models (~500MB) into `models/`. Allow several minutes.

---

## Step 4: Test the service manually

Still in the activated venv:

```powershell
uvicorn main:app --host 127.0.0.1 --port 8000
```

- You should see “Uvicorn running…”
- Test: `Invoke-WebRequest -Uri http://127.0.0.1:8000/health -Method GET`
- Response should be `{"status":"ok"}`.

Stop with Ctrl+C. Next steps make it run permanently.

---

## Step 5: Verify backend config

On the server, open `C:\inetpub\AIPBackend\appsettings.Production.json` and confirm:

```json
"InsightFace": {
  "Enabled": true,
  "BaseUrl": "http://127.0.0.1:8000",
  "TimeoutSeconds": 30
}
```

Restart IIS if you change this:

```powershell
iisreset
```

---

## Step 6: Run InsightFace as a Windows Service (recommended)

To start automatically and survive reboots:

### 6a. Install NSSM

1. Download NSSM from [nssm.cc/download](https://nssm.cc/download).
2. Extract `nssm.exe` (from the `win64` folder) to `C:\inetpub\InsightFaceService\` or another folder on PATH.

### 6b. Create the service

In an **elevated** PowerShell (Run as Administrator):

```powershell
cd C:\inetpub\InsightFaceService
nssm install InsightFaceService "C:\inetpub\InsightFaceService\.venv\Scripts\python.exe" "-m uvicorn main:app --host 127.0.0.1 --port 8000"
```

If Python is installed elsewhere, use its full path. Then:

```powershell
nssm set InsightFaceService AppDirectory "C:\inetpub\InsightFaceService"
nssm start InsightFaceService
```

### 6c. Confirm it’s running

```powershell
Invoke-WebRequest -Uri http://127.0.0.1:8000/health -Method GET
Get-Service InsightFaceService
```

---

## Step 7: Firewall

InsightFace listens on `127.0.0.1` only, so no firewall changes are needed. Only the .NET API on the same machine can reach it.

---

## Verification checklist

- [ ] InsightFace health check: `http://127.0.0.1:8000/health` returns OK
- [ ] Backend `InsightFace:Enabled` is `true` and `BaseUrl` is `http://127.0.0.1:8000`
- [ ] IIS restarted after any config change
- [ ] Face search in the app works (incident form → Search by image)

---

## Troubleshooting

| Issue | Action |
|-------|--------|
| `python` not found | Reinstall Python with “Add to PATH” enabled; open a new terminal |
| `pip install` fails | Try `pip install --upgrade pip` and retry |
| Models download slow | First run can take 5–10 minutes; keep the window open |
| Port 8000 in use | Change port in uvicorn and in `BaseUrl` |
| Service won’t start | Run `uvicorn` manually to see error messages; check `AppDirectory` and Python path in NSSM |
| Face search fails | Confirm InsightFace service is running and health check returns OK |

---

## Quick reference

| Item | Value |
|------|-------|
| Service URL | `http://127.0.0.1:8000` |
| Health check | `GET http://127.0.0.1:8000/health` |
| Install path | `C:\inetpub\InsightFaceService` |
| Service name | `InsightFaceService` |
