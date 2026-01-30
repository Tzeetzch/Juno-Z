import { resolve } from 'path';
import { unlinkSync, existsSync } from 'fs';

/**
 * Playwright global setup: delete the SQLite database before each test run
 * so the app recreates it fresh with seed data (DbInitializer).
 */
export default function globalSetup() {
  const dbPath = resolve(__dirname, '../../src/JunoBank.Web/Data/junobank-test.db');

  if (existsSync(dbPath)) {
    unlinkSync(dbPath);
    console.log('[global-setup] Deleted database for fresh test run');
  } else {
    console.log('[global-setup] No existing database found, starting fresh');
  }
}
