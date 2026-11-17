import { mkdir, writeFile } from 'node:fs/promises';
import { dirname } from 'node:path';

// Separate, opt-in profiling pass. Chromium memory dumps affect scheduling and
// allocations, so traced timings must not be mixed with normal benchmark runs.
export async function startBrowserMemoryTrace(browser, tracePath, intervalMs) {
    const session = await browser.newBrowserCDPSession();
    await session.send('Tracing.start', {
        transferMode: 'ReturnAsStream',
        traceConfig: {
            recordMode: 'recordUntilFull',
            traceBufferSizeInKb: 16384,
            includedCategories: ['disabled-by-default-memory-infra'],
            excludedCategories: ['*'],
            memoryDumpConfig: { triggers: [{ mode: 'background', periodic_interval_ms: intervalMs }] },
        },
    });
    return async function stop() {
        try {
            // Request a final non-deterministic dump: never force a GC.
            const finalDump = await session.send('Tracing.requestMemoryDump', { levelOfDetail: 'background', deterministic: false });
            const complete = new Promise((resolve, reject) => {
                const timer = setTimeout(() => reject(new Error('Memory trace completion timed out.')), 15_000);
                session.once('Tracing.tracingComplete', event => { clearTimeout(timer); resolve(event); });
            });
            await session.send('Tracing.end');
            const { stream, dataLossOccurred } = await complete;
            if (!stream) throw new Error('Chromium returned no memory trace stream.');
            const chunks = [];
            try {
                while (true) {
                    const part = await session.send('IO.read', { handle: stream });
                    chunks.push(part.base64Encoded ? Buffer.from(part.data, 'base64') : Buffer.from(part.data, 'utf8'));
                    if (part.eof) break;
                }
            } finally {
                await session.send('IO.close', { handle: stream });
            }
            const raw = Buffer.concat(chunks);
            await mkdir(dirname(tracePath), { recursive: true });
            await writeFile(tracePath, raw, { flag: 'wx' });
            if (dataLossOccurred || !finalDump.success) throw new Error(`Incomplete memory trace (data loss=${dataLossOccurred ?? false}, final dump=${finalDump.success}) retained at ${tracePath}.`);
            const summary = summarizeMemoryTrace(JSON.parse(raw.toString('utf8')));
            return {
                TracePath: tracePath, RequestedIntervalMs: intervalMs, LevelOfDetail: 'background',
                DataLossOccurred: dataLossOccurred ?? false, FinalDumpSucceeded: finalDump.success,
                ...summary,
                Scope: 'Sampled process private footprint reported by Chromium, including browser/renderer/GPU/utility processes and tracing overhead. On Windows this is private committed memory, not resident memory. Peaks between samples, shared/OS/GPU-driver memory and unreported processes are excluded. No forced GC. Allocator sizes are not summed into process totals.',
            };
        } finally {
            await session.detach();
        }
    };
}

export function summarizeMemoryTrace(trace) {
    if (!Array.isArray(trace.traceEvents)) throw new Error('Invalid memory trace.');
    const names = new Map();
    for (const event of trace.traceEvents) {
        if (event.name === 'process_name') names.set(event.pid, event.args?.name ?? 'unknown');
    }
    const samples = new Map();
    for (const event of trace.traceEvents) {
        const totals = event.args?.dumps?.process_totals;
        if (event.ph !== 'v' || !totals) continue;
        // GUID and timestamp identify a global dump; allocator events often
        // repeat that PID but must never be added to process totals again.
        const key = `${event.id}:${event.ts}`;
        if (!samples.has(key)) samples.set(key, { TimestampUs: event.ts, DumpId: event.id, Processes: [] });
        const sample = samples.get(key);
        if (sample.Processes.some(process => process.Id === event.pid)) throw new Error('Duplicate process total in one memory dump.');
        sample.Processes.push({
            Id: event.pid, Name: names.get(event.pid) ?? 'unknown',
            PrivateFootprintBytes: hexBytes(totals.private_footprint_bytes),
            ResidentBytes: hexBytes(totals.resident_set_bytes),
        });
    }
    const observed = [...samples.values()].sort((a, b) => a.TimestampUs - b.TimestampUs).map(sample => ({
        ...sample,
        ReportedPrivateFootprintBytes: sample.Processes.every(process => process.PrivateFootprintBytes !== null)
            ? sample.Processes.reduce((sum, process) => sum + process.PrivateFootprintBytes, 0) : null,
        ReportedResidentBytes: sample.Processes.every(process => process.ResidentBytes !== null)
            ? sample.Processes.reduce((sum, process) => sum + process.ResidentBytes, 0) : null,
    }));
    if (!observed.length) throw new Error('Memory trace contains no process-total samples.');
    const privateValues = observed.map(sample => sample.ReportedPrivateFootprintBytes).filter(value => value !== null);
    const residentValues = observed.map(sample => sample.ReportedResidentBytes).filter(value => value !== null);
    return {
        SampleCount: observed.length,
        PeakReportedPrivateFootprintBytes: privateValues.length ? Math.max(...privateValues) : null,
        PeakReportedResidentBytes: residentValues.length ? Math.max(...residentValues) : null,
        ResidentSampleCount: residentValues.length,
        Samples: observed,
    };
}

function hexBytes(value) {
    if (value === undefined) return null;
    if (typeof value !== 'string' || !/^[0-9a-f]+$/i.test(value)) throw new Error('Invalid hexadecimal memory counter.');
    const number = Number.parseInt(value, 16);
    if (!Number.isSafeInteger(number)) throw new Error('Memory counter exceeds numeric precision.');
    return number;
}
