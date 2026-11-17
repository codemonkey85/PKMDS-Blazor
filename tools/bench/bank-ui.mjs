// Real Bank component observations. Runs only inside run-bench's fresh marked
// loopback context; the app's existing guard runs before seeding synthetic rows.
export async function profileBankUi(page, baseUrl, timeout) {
    const started = performance.now();
    await page.goto(`${baseUrl}/bench?suite=bank-ui`, { waitUntil: 'load', timeout });
    await page.waitForSelector('#bench-root[data-bench-state="done"], #bench-root[data-bench-state="error"]', { timeout });
    if (await page.getAttribute('#bench-root', 'data-bench-state') === 'error') {
        throw new Error(await page.textContent('#bench-error'));
    }
    const report = JSON.parse(await page.textContent('#bench-result'));
    if (report.Suite !== 'bank-ui' || report.Fixtures?.['bank-rows'] !== '8000') throw new Error('Unexpected Bank UI fixture.');
    await waitForCards(20);
    const readyMs = performance.now() - started;
    const observations = { NavigationToCardsMs: readyMs, CardsAt20: await page.locator('.bank-card').count() };
    const results = [];
    const cdp = await page.context().newCDPSession(page);
    await cdp.send('Performance.enable');
    // These are point-in-time CDP snapshots, not total browser or peak memory.
    observations.Before = await snapshot();
    const original = await page.evaluate(async () => {
        const bank = await import('/js/bank.js');
        const bridge = await import('/js/benchmark.js');
        const bytes = await bank.exportAll();
        return { Hash: await bridge.fingerprint(bytes), Count: JSON.parse(new TextDecoder().decode(bytes)).length };
    });
    if (original.Count !== 8000) throw new Error('Bank UI seed lost records.');

    const firstNickname = await page.locator('.bank-card-nickname').first().innerText();
    await timed('bank-ui-select', async () => {
        await page.locator('.bank-card input[type="checkbox"]').first().check();
        await page.getByRole('button', { name: 'Export .pk* (1)', exact: true }).waitFor();
    });
    await timed('bank-ui-next-page', async () => {
        await page.getByRole('button', { name: 'Next page', exact: true }).click();
        await page.waitForFunction(previous => document.querySelector('.bank-card-nickname')?.textContent.trim() !== previous, firstNickname);
        await waitForCards(20);
    });
    await page.getByRole('button', { name: 'Previous page', exact: true }).click();
    await page.waitForFunction(previous => document.querySelector('.bank-card-nickname')?.textContent.trim() === previous, firstNickname);
    observations.SelectionSurvivedPaging = await page.locator('.bank-card input[type="checkbox"]').first().isChecked();
    if (!observations.SelectionSurvivedPaging) throw new Error('Bank selection lost across pages.');

    await timed('bank-ui-filter', async () => {
        await page.getByLabel('Search species / nickname').fill('BENCH0999');
        await page.getByLabel('Search species / nickname').press('Tab');
        await waitForCards(8);
    });
    if ((await page.locator('.bank-card-nickname').allInnerTexts()).some(name => name.trim() !== 'BENCH0999')) {
        throw new Error('Bank search returned the wrong Pokémon.');
    }
    await page.getByLabel('Search species / nickname').fill('');
    await page.getByLabel('Search species / nickname').press('Tab');
    await waitForCards(20);
    await page.getByLabel('Per page', { exact: true }).click();
    await page.getByRole('option', { name: '100', exact: true }).click();
    await waitForCards(100);
    observations.CardsAt100 = await page.locator('.bank-card').count();
    observations.Scroll = await page.locator('.bank-card-grid').evaluate(async element => {
        const intervals = [];
        let previous = performance.now();
        const originalTop = element.scrollTop;
        const max = element.scrollHeight - element.clientHeight;
        for (let i = 0; i < 60; i++) {
            element.scrollTop = max * (i / 59);
            await new Promise(requestAnimationFrame);
            const now = performance.now();
            intervals.push(now - previous);
            previous = now;
        }
        const reachedBottom = Math.abs(element.scrollTop - max) < 2;
        element.scrollTop = originalTop;
        return { Scrollable: max > 0, ReachedBottom: reachedBottom, FrameIntervalsMs: intervals };
    });
    if (!observations.Scroll.Scrollable || !observations.Scroll.ReachedBottom) throw new Error('Bank grid did not scroll.');
    observations.After = await snapshot();
    const finalHash = await page.evaluate(async () => {
        const bank = await import('/js/bank.js');
        const bridge = await import('/js/benchmark.js');
        return bridge.fingerprint(await bank.exportAll());
    });
    observations.BankUnchanged = original.Hash === finalHash;
    if (!observations.BankUnchanged) throw new Error('Read-only Bank interactions changed stored data.');
    report.Results = results;
    report.UiObservations = observations;
    report.NavToBenchmarkDoneMs = performance.now() - started;
    report.UserAgent = await page.evaluate(() => navigator.userAgent);
    await cdp.detach();
    return report;

    async function waitForCards(count) {
        await page.waitForFunction(expected => document.querySelectorAll('.bank-card').length === expected, count, { timeout });
    }
    async function timed(name, action) {
        const before = performance.now();
        await action();
        const elapsed = performance.now() - before;
        results.push({ Name: name, Iterations: 1, OpsPerIteration: 1, TotalMs: elapsed, MeanMs: elapsed, MinMs: elapsed, MaxMs: elapsed, StdDevMs: 0 });
    }
    async function snapshot() {
        const dom = await cdp.send('Memory.getDOMCounters');
        const metrics = await cdp.send('Performance.getMetrics');
        return {
            ...dom,
            AttachedBankElements: await page.locator('.bank-tab-root *').count(),
            Metrics: Object.fromEntries(metrics.metrics.filter(item => ['JSHeapUsedSize', 'JSHeapTotalSize', 'LayoutCount', 'RecalcStyleCount'].includes(item.name)).map(item => [item.name, item.value])),
        };
    }
}
