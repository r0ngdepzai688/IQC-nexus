import { randomBytes } from "node:crypto";
import path from "node:path";

import { defineConfig, devices } from "@playwright/test";

const outputDirectory = path.resolve("test-results");
const runtimeDirectory = path.resolve(".playwright-runtime");
const databasePath = path.join(runtimeDirectory, "iqc-nexus-e2e.db");
const dataHubPath = path.join(runtimeDirectory, "data-hub");
const seedPassword = process.env.IQC_E2E_SEED_PASSWORD ?? randomBytes(32).toString("base64url");
const jwtSecret = process.env.IQC_E2E_JWT_SECRET ?? randomBytes(48).toString("base64");

// The worker inherits this process-only value; it is never written to disk or logs.
process.env.IQC_E2E_SEED_PASSWORD = seedPassword;
process.env.IQC_E2E_JWT_SECRET = jwtSecret;

export default defineConfig({
  testDir: "./e2e",
  outputDir: outputDirectory,
  fullyParallel: false,
  workers: 1,
  retries: process.env.CI ? 1 : 0,
  timeout: 30_000,
  expect: {
    timeout: 5_000,
  },
  reporter: process.env.CI
    ? [["line"], ["html", { open: "never", outputFolder: "playwright-report" }]]
    : [["line"]],
  use: {
    baseURL: "http://localhost:3000",
    trace: "on-first-retry",
    screenshot: "off",
    video: "off",
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
  webServer: [
    {
      command: "node -e \"const fs=require('node:fs');fs.rmSync('.playwright-runtime',{recursive:true,force:true});fs.mkdirSync('.playwright-runtime',{recursive:true})\" && dotnet run --project ../backend/src/IqcQms.Api/IqcQms.Api.csproj --no-restore --no-launch-profile --urls http://localhost:5000",
      url: "http://localhost:5000/api/health",
      timeout: 120_000,
      reuseExistingServer: false,
      stdout: "pipe",
      stderr: "pipe",
      env: {
        ASPNETCORE_ENVIRONMENT: "Testing",
        DatabaseConfig__ConnectionString: `Data Source=${databasePath};Pooling=False`,
        DataHub__BasePath: dataHubPath,
        IQC_SYNTHETIC_USER_SEED_PASSWORD: seedPassword,
        JwtSettings__Secret: jwtSecret,
        "Logging__LogLevel__Microsoft.EntityFrameworkCore": "Warning",
      },
    },
    {
      command: "npm run dev -- --hostname localhost",
      url: "http://localhost:3000/login",
      timeout: 120_000,
      reuseExistingServer: false,
      stdout: "pipe",
      stderr: "pipe",
      env: {
        NEXT_PUBLIC_API_URL: "http://localhost:5000/api",
      },
    },
  ],
});
