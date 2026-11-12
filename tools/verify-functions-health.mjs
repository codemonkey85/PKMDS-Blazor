// Exercise worker startup, DI, and HTTP binding without creating a report.
// SubmitBugReport rejects this content type before any GitHub issue is created.
const base = process.argv[2];
if (!base) throw new Error('Usage: node tools/verify-functions-health.mjs <function-app-url>');
const endpoint = new URL('api/SubmitBugReport', base.endsWith('/') ? base : base + '/');

for (let attempt = 1; attempt <= 6; attempt++) {
    try {
        const response = await fetch(endpoint, {
            method: 'POST',
            headers: {'Content-Type': 'application/octet-stream'},
            body: '',
            signal: AbortSignal.timeout(30_000),
        });
        const payload = await response.json().catch(() => null);
        if (response.status === 400 && payload?.error === 'Expected multipart/form-data.') {
            console.log('Functions worker is healthy: invalid content type returned the expected 400; no report created.');
            break;
        }
        throw new Error(`Expected report validation response; received HTTP ${response.status}.`);
    } catch (error) {
        if (attempt === 6) throw error;
        console.warn(`Worker probe ${attempt}/6 failed: ${error.message}; retrying after cold start.`);
        await new Promise(resolve => setTimeout(resolve, 5_000));
    }
}
