import {
  defineConfig,
  devices
} from '@playwright/test';

export default defineConfig({
  testDir: './e2e',

  fullyParallel: false,

  workers: 1,

  timeout: 90_000,

  expect: {
    timeout: 10_000
  },

  retries:
    process.env['CI']
      ? 1
      : 0,

  reporter:
    process.env['CI']
      ? [
          ['github'],
          [
            'html',
            {
              open: 'never'
            }
          ]
        ]
      : [
          ['list'],
          [
            'html',
            {
              open: 'never'
            }
          ]
        ],

  use: {
    baseURL:
      process.env['E2E_WEB_BASE_URL'] ??
      'http://127.0.0.1:4200',

    trace:
      'retain-on-failure',

    screenshot:
      'only-on-failure',

    video:
      'retain-on-failure'
  },

  webServer: {
    command:
      'npm start -- --host 127.0.0.1',

    url:
      'http://127.0.0.1:4200/catalog',

    reuseExistingServer:
      !process.env['CI'],

    timeout:
      120_000
  },

  globalSetup:
    './e2e/support/global-setup.ts',

  outputDir:
    'test-results',

  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome']
      }
    }
  ]
});