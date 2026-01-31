import { resolve } from 'path';
import { unlinkSync, readdirSync } from 'fs';

/**
 * Playwright global setup: clean up old test database files.
 * Each test run creates a unique database (junobank-test-XXXXXX.db).
 * This cleans up databases older than 1 hour to prevent accumulation.
 */
export default function globalSetup() {
  const dataDir = resolve(__dirname, '../../src/JunoBank.Web/Data');
  
  try {
    const files = readdirSync(dataDir);
    const testDbPattern = /^junobank-test-[a-f0-9]+\.db$/;
    const oneHourAgo = Date.now() - (60 * 60 * 1000);
    
    let cleanedCount = 0;
    for (const file of files) {
      if (testDbPattern.test(file)) {
        const filePath = resolve(dataDir, file);
        try {
          const { mtimeMs } = require('fs').statSync(filePath);
          if (mtimeMs < oneHourAgo) {
            unlinkSync(filePath);
            cleanedCount++;
          }
        } catch {
          // File might be in use, skip it
        }
      }
    }
    
    if (cleanedCount > 0) {
      console.log(`[global-setup] Cleaned up ${cleanedCount} old test database(s)`);
    }
  } catch {
    // Data dir might not exist yet, that's fine
  }
}
