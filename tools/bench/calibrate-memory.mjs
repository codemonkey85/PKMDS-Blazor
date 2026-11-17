import { chromium } from 'playwright';
import { startBrowserMemoryTrace } from './browser-memory.mjs';

const tracePath = process.argv[2];
if (!tracePath) throw new Error('Usage: node calibrate-memory.mjs <new-trace-path>');
const browser = await chromium.launch({ headless: true });
try {
    const context = await browser.newContext({ serviceWorkers: 'block' });
    const page = await context.newPage();
    await page.goto('data:text/html,<p>Chromium memory probe</p>');
    const stop = await startBrowserMemoryTrace(browser, tracePath, 100);
    const session = await browser.newBrowserCDPSession();
    await session.send('Tracing.requestMemoryDump', { levelOfDetail: 'background', deterministic: false });
    await page.evaluate(() => {
        globalThis.pkmdsMemoryProbe = new Uint8Array(32 * 1024 * 1024);
        globalThis.pkmdsMemoryProbe.fill(1);
    });
    const summary = await stop();
    const rendererBytes = summary.Samples.map(sample => sample.Processes
        .filter(process => process.Name === 'Renderer' && process.PrivateFootprintBytes !== null)
        .reduce((sum, process) => sum + process.PrivateFootprintBytes, 0));
    const observedDelta = Math.max(...rendererBytes) - Math.min(...rendererBytes);
    if (observedDelta < 24 * 1024 * 1024) throw new Error(`Known allocation was not detected: ${observedDelta} bytes. Raw trace: ${tracePath}`);
    console.log(JSON.stringify({ ProbeBytes: 32 * 1024 * 1024, ObservedRendererRangeBytes: observedDelta,
        Scope: 'Sensitivity check, not exact attribution; process activity and trace overhead remain included.', ...summary }, null, 2));
    await session.detach();
    await context.close();
} finally {
    await browser.close();
}
