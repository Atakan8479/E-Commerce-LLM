import {
  readFileSync
} from 'node:fs';

import {
  resolve
} from 'node:path';

import {
  spawnSync
} from 'node:child_process';

const fixturePath =
  resolve(
    process.cwd(),
    'e2e',
    'fixtures',
    'reset-flashsale.sql'
  );

export function resetFlashSaleFixture():
  void {
  const sql =
    readFileSync(
      fixturePath,
      'utf8'
    );

  const result =
    spawnSync(
      'docker',
      [
        'exec',
        '-i',
        'flashsale-sqlserver',
        '/bin/bash',
        '-c',
        [
          'export SQLCMDPASSWORD="$MSSQL_SA_PASSWORD";',
          'exec /opt/mssql-tools18/bin/sqlcmd',
          '-S localhost',
          '-U sa',
          '-C',
          '-d FlashSaleOrchestrator'
        ].join(' ')
      ],
      {
        input: sql,
        encoding: 'utf8'
      }
    );

  if (
    result.error !== undefined
  ) {
    throw result.error;
  }

  if (result.status !== 0) {
    throw new Error(
      [
        'Failed to reset the E2E SQL fixture.',
        '',
        result.stdout,
        result.stderr
      ]
        .filter(
          value =>
            value.length > 0
        )
        .join('\n')
    );
  }

  console.log(
    result.stdout.trim()
  );
}