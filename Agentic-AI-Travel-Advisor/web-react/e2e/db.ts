import pg from 'pg';

/**
 * Read-only access to the API's PostgreSQL database, used to check what the UI and API actually stored.
 * Takes the same connection string as the API (Npgsql "Host=...;Database=..." or a postgres:// URL)
 * from E2E_DB_CONNECTION or ConnectionStrings__DefaultConnection. Never commit the value.
 */
export function dbConfigFromEnv(): pg.ClientConfig | null {
  const raw = process.env.E2E_DB_CONNECTION ?? process.env.ConnectionStrings__DefaultConnection;
  if (!raw) return null;
  if (/^postgres(ql)?:\/\//.test(raw)) return { connectionString: raw };

  const parts = new Map(
    raw
      .split(';')
      .filter((kv) => kv.includes('='))
      .map((kv) => {
        const i = kv.indexOf('=');
        return [kv.slice(0, i).trim().toLowerCase(), kv.slice(i + 1).trim()] as const;
      }),
  );
  return {
    host: parts.get('host') ?? parts.get('server'),
    port: Number(parts.get('port') ?? 5432),
    database: parts.get('database'),
    user: parts.get('username') ?? parts.get('user id') ?? parts.get('user'),
    password: parts.get('password'),
  };
}

export async function connectDb(config: pg.ClientConfig): Promise<pg.Client> {
  const client = new pg.Client(config);
  await client.connect();
  // Guards against a test accidentally writing: every check here only reads.
  await client.query('SET SESSION CHARACTERISTICS AS TRANSACTION READ ONLY');
  return client;
}
