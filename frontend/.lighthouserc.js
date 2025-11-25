module.exports = {
  ci: {
    collect: {
      startServerCommand: 'npm run preview',
      startServerReadyPattern: 'Local:',
      startServerReadyTimeout: 10000,
      url: ['http://localhost:4173'],
      numberOfRuns: 3,
    },
    assert: {
      assertions: {
        'categories:performance': ['error', { minScore: 0.8 }],
        'categories:accessibility': ['error', { minScore: 0.75 }],
        'categories:best-practices': ['error', { minScore: 0.75 }],
        'categories:seo': ['error', { minScore: 0.75 }],
      },
    },
    upload: {
      target: 'temporary-public-storage',
    },
  },
};

