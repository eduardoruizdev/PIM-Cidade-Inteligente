import { spawn } from 'node:child_process';
import path from 'node:path';
const extra = process.argv.slice(2);
const child = spawn(
  'dotnet',
  [
    'run',
    '--project',
    'backend/SantosConecta.API',
    '--no-restore',
    ...(extra.length ? ['--', ...extra] : []),
  ],
  {
    stdio: 'inherit',
    env: {
      ...process.env,
      DOTNET_CLI_HOME: path.resolve('.dotnet-home'),
      DOTNET_CLI_TELEMETRY_OPTOUT: '1',
    },
  },
);
process.on('SIGINT', () => child.kill('SIGINT'));
process.on('SIGTERM', () => child.kill('SIGTERM'));
child.on('exit', (code) => process.exit(code ?? 1));
