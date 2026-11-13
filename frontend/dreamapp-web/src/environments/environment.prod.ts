// Production environment, swapped in by the "production" build configuration
// (see angular.json -> fileReplacements) and used by the GitHub Actions
// deploy workflow. apiUrl is the public HTTPS address of the deployed ASP.NET Core
// backend (Oracle Cloud VM behind Caddy, DuckDNS name; see docs/deploy.md).
export const environment = {
  production: true,
  apiUrl: 'https://dreamapp.duckdns.org/api'
};
