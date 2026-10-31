import { defineConfig } from '@playwright/test';
export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  use: {
    baseURL: 'http://127.0.0.1:5173',
    channel: 'chrome',
    headless: true,
    viewport: { width: 1440, height: 1000 },
  },
  webServer: [
    {
      command: 'npm.cmd run dev -- --port 5173 --strictPort',
      url: 'http://127.0.0.1:5173',
      reuseExistingServer: true,
      timeout: 60000,
    },
    {
      command: 'npm.cmd run api',
      url: 'http://localhost:5080/swagger/index.html',
      reuseExistingServer: true,
      timeout: 60000,
    },
  ],
  reporter: 'list',
});
