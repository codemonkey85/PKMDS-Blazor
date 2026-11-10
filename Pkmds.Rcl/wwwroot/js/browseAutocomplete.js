// The field and the portalled popover are both inside this interaction. A generic
// outside-click overlay also treats the arrow as outside, racing its toggle click.
export function watch(root, contentId, dotnet) {
    let disposed = false;
    const open = () => !disposed && root.classList.contains('browse-autocomplete-open');
    const inside = target => root.contains(target) || document.getElementById(contentId)?.contains(target);
    const dismiss = () => { void dotnet.invokeMethodAsync('DismissBrowseAsync').catch(() => {}); };
    const outside = event => { if (open() && !inside(event.target)) dismiss(); };
    const escape = event => {
        if (open() && event.key === 'Escape') {
            dismiss();
            focusArrow();
        }
    };
    const focusArrow = () => root.querySelector('.mud-input-adornment-icon-button')?.focus({ preventScroll: true });
    const updateArrow = () => {
        const arrow = root.querySelector('.mud-input-adornment-icon-button');
        if (!arrow) return;
        arrow.setAttribute('aria-expanded', String(open()));
        arrow.setAttribute('aria-haspopup', 'listbox');
        arrow.setAttribute('aria-controls', `${contentId}-list`);
        arrow.setAttribute('title', 'Browse options');
    };
    const observer = new MutationObserver(updateArrow);
    observer.observe(root, { attributes: true, attributeFilter: ['class'] });
    updateArrow();

    document.addEventListener('pointerdown', outside, true);
    document.addEventListener('focusin', outside, true);
    document.addEventListener('keydown', escape, true);
    return {
        focusArrow,
        dispose() {
            disposed = true;
            observer.disconnect();
            document.removeEventListener('pointerdown', outside, true);
            document.removeEventListener('focusin', outside, true);
            document.removeEventListener('keydown', escape, true);
        },
    };
}
