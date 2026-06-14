TLS certificates for Sites.Host.

Automatic renewal (recommended):
  Sites.Host runs CertMaintenance in the background (like Hephaestus Refiner loops).
  No cron. Set CertMaintenance:AcmeEmail in Sites.Host/appsettings.json and deploy.

Manual one-off (optional):
  dotnet run --project Sites.CertTool
  Use only when Sites.Host is stopped and you need an offline issue.

Output:
  sites.pfx          — Kestrel HTTPS certificate
  sites.cer          — public certificate
  acme-account.pem   — ACME account key (keep private)

Domains are discovered from Sites.Modules target hosts.
