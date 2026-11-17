import test from 'node:test';
import assert from 'node:assert/strict';
import { summarizeMemoryTrace } from '../bench/browser-memory.mjs';

const dump = (pid, ts, id, totals) => ({ ph: 'v', pid, ts, id, args: { dumps: { process_totals: totals } } });

test('aggregates simultaneous process totals, ignoring duplicate allocator events', () => {
    const result = summarizeMemoryTrace({ traceEvents: [
        { name: 'process_name', pid: 1, args: { name: 'Browser' } },
        { name: 'process_name', pid: 2, args: { name: 'Renderer' } },
        dump(1, 10, 'a', { private_footprint_bytes: '100', resident_set_bytes: '80' }),
        dump(2, 10, 'a', { private_footprint_bytes: '200', resident_set_bytes: '100' }),
        { ph: 'v', pid: 2, ts: 10, id: 'a', args: { dumps: { allocators: { v8: { size: 'ffff' } } } } },
        dump(1, 20, 'b', { private_footprint_bytes: '300' }),
        dump(2, 20, 'b', { private_footprint_bytes: '400' }),
    ] });
    assert.equal(result.SampleCount, 2);
    assert.equal(result.PeakReportedPrivateFootprintBytes, 0x700);
    assert.equal(result.PeakReportedResidentBytes, 0x180);
    assert.equal(result.ResidentSampleCount, 1);
    assert.equal(result.Samples[0].Processes[1].Name, 'Renderer');
    assert.equal(result.Samples[1].ReportedResidentBytes, null);
});

test('unavailable counters remain unavailable, rather than looking like zero memory', () => {
    const result = summarizeMemoryTrace({ traceEvents: [dump(1, 10, 'a', {})] });
    assert.equal(result.PeakReportedPrivateFootprintBytes, null);
    assert.equal(result.PeakReportedResidentBytes, null);
    assert.equal(result.ResidentSampleCount, 0);
});

test('rejects duplicated totals, invalid hex and counters beyond numeric precision', () => {
    const first = dump(1, 10, 'a', { private_footprint_bytes: '100' });
    assert.throws(() => summarizeMemoryTrace({ traceEvents: [first, first] }), /Duplicate process total/);
    for (const value of ['bad-counter', 'ffffffffffffffff']) {
        assert.throws(() => summarizeMemoryTrace({ traceEvents: [dump(1, 10, 'a', { private_footprint_bytes: value })] }));
    }
    assert.throws(() => summarizeMemoryTrace({ traceEvents: [] }), /no process-total samples/);
});
