const apiReadyUrl =
  process.env['E2E_API_READY_URL'] ??
  'http://127.0.0.1:5185/health/ready';

const workerReadyUrl =
  process.env['E2E_WORKER_READY_URL'] ??
  'http://127.0.0.1:8081/health/ready';

const maximumAttempts =
  60;

const retryDelayMs =
  1_000;

export default async function globalSetup():
  Promise<void> {
  await Promise.all([
    waitUntilReady(
      'API',
      apiReadyUrl
    ),

    waitUntilReady(
      'Worker',
      workerReadyUrl
    )
  ]);
}

async function waitUntilReady(
  name: string,
  url: string
): Promise<void> {
  let lastError:
    unknown = null;

  for (
    let attempt = 1;
    attempt <= maximumAttempts;
    attempt++
  ) {
    try {
      const response =
        await fetch(url);

      if (response.ok) {
        console.log(
          `${name} readiness confirmed: ${url}`
        );

        return;
      }

      lastError =
        new Error(
          `${name} returned HTTP ${response.status}.`
        );
    } catch (error) {
      lastError = error;
    }

    await delay(
      retryDelayMs
    );
  }

  throw new Error(
    `${name} did not become ready at ${url}.`,
    {
      cause:
        lastError
    }
  );
}

function delay(
  milliseconds: number
): Promise<void> {
  return new Promise(
    resolve =>
      setTimeout(
        resolve,
        milliseconds
      )
  );
}