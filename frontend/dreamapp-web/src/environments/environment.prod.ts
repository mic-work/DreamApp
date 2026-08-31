// Production environment, swapped in by the "production" build configuration
// (see angular.json -> fileReplacements) and used by the GitHub Actions
// deploy workflow. Replace REPLACE_WITH_YOUR_BACKEND_URL with the real,
// HTTPS URL of your deployed ASP.NET Core backend before deploying — this
// repository intentionally never hardcodes a real backend address.
export const environment = {
  production: true,
  apiUrl: 'https://REPLACE_WITH_YOUR_BACKEND_URL/api'
};
