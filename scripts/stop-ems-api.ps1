# Stops any running EMS.API host so MSBuild can copy outputs into EMS.API\bin\...
# Run from repo root:  powershell -NoProfile -File scripts\stop-ems-api.ps1

$procs = Get-Process -Name "EMS.API" -ErrorAction SilentlyContinue
if ($procs) {
    $procs | Stop-Process -Force
    Write-Host "Stopped EMS.API (PID(s): $($procs.Id -join ', '))."
} else {
    Write-Host "No EMS.API process was running."
}
