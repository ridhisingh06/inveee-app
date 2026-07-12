import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/test-setup.ts',
    include: ['src/**/*.spec.ts', 'src/**/*.test.ts'],
    coverage: {
      provider: 'c8'
    },
    teardown: true,
    isolate: true,
    threads: false
  }
});

