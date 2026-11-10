// The field and the portalled popover are both inside this interaction. A generic
// outside-click overlay also treats the arrow as outside, racing its toggle click.
export function watch(root, contentId, dotnet) {
    let disposed = false;
    const open = () => !disposed && root.classList.contains('browse-autocomplete-open');
    const content = () => document.getElementById(contentId);
    const arrow = () => root.querySelector('.mud-input-adornment-icon-button');
    const options = () => [...(content()?.querySelectorAll('[role="option"]:not(:disabled)') ?? [])];
    const inside = target => root.contains(target) || content()?.contains(target);
    const dismiss = () => { void dotnet.invokeMethodAsync('DismissBrowseAsync').catch(() => {}); };
    const outside = event => { if (open() && !inside(event.target)) dismiss(); };
    const consume = event => {
        event.preventDefault();
        event.stopImmediatePropagation();
    };
    const focusArrow = () => arrow()?.focus({ preventScroll: true });
    const keydown = event => {
        if (!open()) return;
        if (event.key === 'Escape') {
            // The same Escape must never reach a dialog's cancel handler.
            consume(event);
            dismiss();
            focusArrow();
            return;
        }

        const trigger = arrow();
        const items = options();
        const buttons = [...(content()?.querySelectorAll('button:not(:disabled)') ?? [])];
        const target = event.target;
        if (target === trigger && ((event.key === 'Tab' && !event.shiftKey) || event.key === 'ArrowDown' || event.key === 'ArrowUp')) {
            const selected = items.find(item => item.getAttribute('aria-selected') === 'true');
            const item = selected ?? (event.key === 'ArrowUp' ? items.at(-1) : items[0]) ?? buttons[0];
            if (item) {
                consume(event);
                item.focus({ preventScroll: true });
                reveal(item);
            }
        } else if (content()?.contains(target)) {
            const index = items.indexOf(target);
            if (index >= 0 && ['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) {
                consume(event);
                const next = event.key === 'Home' ? 0 : event.key === 'End' ? items.length - 1 :
                    Math.max(0, Math.min(items.length - 1, index + (event.key === 'ArrowDown' ? 1 : -1)));
                items[next]?.focus({ preventScroll: true });
                reveal(items[next]);
            } else if (event.key === 'Tab' && event.shiftKey && target === buttons[0]) {
                consume(event);
                focusArrow();
            } else if (event.key === 'Tab' && !event.shiftKey && target === buttons.at(-1)) {
                // Leave the portalled menu in the field's normal document order.
                const tabbable = [...document.querySelectorAll('a[href],button,input,select,textarea,[tabindex]')]
                    .filter(element => element.tabIndex >= 0 && !element.disabled && element.getClientRects().length && !content()?.contains(element));
                const next = tabbable[tabbable.indexOf(trigger) + 1];
                if (next) {
                    consume(event);
                    dismiss();
                    next.focus({ preventScroll: true });
                } else {
                    dismiss();
                    focusArrow();
                }
            }
        }
    };
    const reveal = item => {
        const list = document.getElementById(`${contentId}-list`);
        if (!list || !item) return;
        const bounds = list.getBoundingClientRect();
        const row = item.getBoundingClientRect();
        if (row.top < bounds.top) list.scrollTop += row.top - bounds.top;
        else if (row.bottom > bounds.bottom) list.scrollTop += row.bottom - bounds.bottom;
    };
    const updateArrow = () => {
        const trigger = arrow();
        if (!trigger) return;
        trigger.tabIndex = root.querySelector('input')?.readOnly ? -1 : 0;
        trigger.setAttribute('aria-expanded', String(open()));
        trigger.setAttribute('aria-haspopup', 'listbox');
        trigger.setAttribute('aria-controls', `${contentId}-list`);
        trigger.setAttribute('title', 'Browse options');
    };
    const observer = new MutationObserver(updateArrow);
    observer.observe(root, { attributes: true, attributeFilter: ['class'] });
    updateArrow();

    document.addEventListener('pointerdown', outside, true);
    document.addEventListener('focusin', outside, true);
    document.addEventListener('keydown', keydown, true);
    return {
        focusArrow,
        revealSelection() { reveal(content()?.querySelector('[aria-selected="true"]')); },
        dispose() {
            disposed = true;
            observer.disconnect();
            document.removeEventListener('pointerdown', outside, true);
            document.removeEventListener('focusin', outside, true);
            document.removeEventListener('keydown', keydown, true);
        },
    };
}
