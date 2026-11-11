import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';

const source = readFileSync(new URL('../../Pkmds.Rcl/wwwroot/js/browseAutocomplete.js', import.meta.url), 'utf8');

function fixture() {
    const document = new EventTarget();
    const observers = [];
    const calls = [];
    let opened = false;
    const element = (attributes = {}) => ({
        attributes, tabIndex: 0, disabled: false,
        setAttribute(name, value) { this.attributes[name] = value; },
        getAttribute(name) { return this.attributes[name]; },
        focus() { document.activeElement = this; },
        getClientRects() { return [this.getBoundingClientRect()]; },
        getBoundingClientRect() { return {top: 0, bottom: 48}; },
    });
    const input = element();
    const arrow = element();
    arrow.tabIndex = -1;
    const nextField = element();
    const list = {scrollTop: 0, getBoundingClientRect: () => ({top: 100, bottom: 327})};
    const options = Array.from({length: 10}, (_, index) => ({
        ...element({'aria-selected': String(index === 5)}),
        getBoundingClientRect: () => ({top: 100 + index * 48 - list.scrollTop, bottom: 148 + index * 48 - list.scrollTop}),
    }));
    const previous = element();
    const next = element();
    const content = {
        contains: target => [...options, previous, next].includes(target),
        querySelector: () => options.find(option => option.attributes['aria-selected'] === 'true'),
        querySelectorAll: selector => (selector.startsWith('[role=') ? options : [...options, previous, next])
            .filter(option => !option.disabled),
    };
    const root = {
        classList: {contains: () => opened},
        contains: target => target === input || target === arrow,
        querySelector: selector => selector === 'input' ? input : arrow,
    };
    document.getElementById = id => id.endsWith('-list') ? list : content;
    document.querySelectorAll = () => [input, arrow, nextField, ...options, previous, next];
    const watch = vm.runInNewContext(source.replace('export function watch', 'function watch') + '\nwatch', {
        document,
        MutationObserver: class {
            constructor(callback) { this.callback = callback; observers.push(this); }
            observe() {}
            disconnect() { this.disconnected = true; }
        },
    });
    const events = watch(root, 'options', {invokeMethodAsync: name => { calls.push(name); return Promise.resolve(); }});
    const dispatch = (type, target, properties = {}) => {
        const event = new Event(type, {cancelable: true, bubbles: true});
        Object.defineProperty(event, 'target', {value: target});
        Object.assign(event, properties);
        document.dispatchEvent(event);
        return event;
    };
    return {
        document, root, input, arrow, nextField, options, previous, next, list, calls, events, dispatch,
        open() { opened = true; observers.filter(observer => !observer.disconnected).forEach(observer => observer.callback()); },
        key(target, key, shiftKey = false) { return dispatch('keydown', target, {key, shiftKey}); },
    };
}

test('Escape closes only the browse list and cannot cancel the enclosing editor', () => {
    const f = fixture();
    let enclosingDialogCancelled = false;
    f.document.addEventListener('keydown', () => { enclosingDialogCancelled = true; });
    f.open();
    const event = f.key(f.arrow, 'Escape');
    assert.equal(event.defaultPrevented, true);
    assert.equal(enclosingDialogCancelled, false);
    assert.deepEqual(f.calls, ['DismissBrowseAsync']);
    assert.equal(f.document.activeElement, f.arrow);
});

test('Escape outside browsing remains available to enclosing editors', () => {
    const f = fixture();
    let enclosingDialogCancelled = false;
    f.document.addEventListener('keydown', () => { enclosingDialogCancelled = true; });
    const event = f.key(f.input, 'Escape');
    assert.equal(event.defaultPrevented, false);
    assert.equal(enclosingDialogCancelled, true);
    assert.equal(f.calls.length, 0);
});

test('browse trigger is tabbable before opening, and keys enter and navigate enabled options', () => {
    const f = fixture();
    assert.equal(f.arrow.tabIndex, 0);
    assert.equal(f.arrow.getAttribute('aria-expanded'), 'false');
    f.options[6].disabled = true;
    f.open();
    assert.equal(f.arrow.getAttribute('aria-expanded'), 'true');
    f.key(f.arrow, 'Tab');
    assert.equal(f.document.activeElement, f.options[5]);
    f.key(f.options[5], 'ArrowDown');
    assert.equal(f.document.activeElement, f.options[7]);
    f.key(f.options[7], 'Home');
    assert.equal(f.document.activeElement, f.options[0]);
    f.key(f.options[0], 'Tab', true);
    assert.equal(f.document.activeElement, f.arrow);
});

test('Tab can leave the portalled paging controls in normal field order', () => {
    const f = fixture();
    f.open();
    const event = f.key(f.next, 'Tab');
    assert.equal(event.defaultPrevented, true);
    assert.equal(f.document.activeElement, f.nextField);
    assert.deepEqual(f.calls, ['DismissBrowseAsync']);
});

test('selected row is revealed within a short list without moving focus', () => {
    const f = fixture();
    f.open();
    f.arrow.focus();
    f.events.revealSelection();
    assert.equal(f.list.scrollTop, 61);
    assert.equal(f.options[5].getBoundingClientRect().bottom, f.list.getBoundingClientRect().bottom);
    assert.equal(f.document.activeElement, f.arrow);
});

test('outside dismissal ignores the field and options, and disposal removes listeners', () => {
    const f = fixture();
    f.open();
    f.dispatch('pointerdown', f.arrow);
    f.dispatch('focusin', f.options[0]);
    assert.equal(f.calls.length, 0);
    f.dispatch('pointerdown', f.nextField);
    assert.deepEqual(f.calls, ['DismissBrowseAsync']);
    f.events.dispose();
    f.key(f.arrow, 'Escape');
    f.dispatch('focusin', f.nextField);
    assert.equal(f.calls.length, 1);
});
