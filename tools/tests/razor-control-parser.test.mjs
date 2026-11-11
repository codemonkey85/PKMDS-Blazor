import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import { parseOpeningTags } from '../razor-control-parser.mjs';

test('inline button labels stay paired with their actions', () => {
    const text = '<MudButton OnClick="@Cancel">Cancel</MudButton>\n<MudButton OnClick="@Confirm">Confirm</MudButton>';
    const buttons = [...parseOpeningTags(text, 'MudButton')];
    assert.deepEqual(buttons.map(b => [b.attrs.get('OnClick'), text.slice(b.end).split('</MudButton>')[0]]),
        [['@Cancel', 'Cancel'], ['@Confirm', 'Confirm']]);
});

test('bare attributes preserve adjacent quoted values and Razor expressions', () => {
    const text = '<MudCheckBox Disabled="@context.Item.IsEmpty" Dense />\n' +
        '<MudSelect Label="@($"Berry {slot + 1}")" ValueChanged="@(v => { SetBerry(d, slot, v); StateHasChanged(); })" Dense Variant="@Variant.Outlined">';
    const [checkbox, select] = [...parseOpeningTags(text, 'MudCheckBox|MudSelect')];
    assert.equal(checkbox.attrs.get('Disabled'), '@context.Item.IsEmpty');
    assert.equal(checkbox.attrs.get('Dense'), 'true');
    assert.equal(select.attrs.get('Label'), '@($"Berry {slot + 1}")');
    assert.equal(select.attrs.get('ValueChanged'), '@(v => { SetBerry(d, slot, v); StateHasChanged(); })');
    assert.equal(select.attrs.get('Dense'), 'true');
    assert.equal(select.attrs.get('Variant'), '@Variant.Outlined');
});

test('real dialog controls retain inline labels and empty-slot restrictions', () => {
    const readDialog = name => readFileSync(new URL(`../../Pkmds.Rcl/Components/Dialogs/${name}.razor`, import.meta.url), 'utf8');
    const text = readDialog('TrainerCardDialog');
    const buttons = [...parseOpeningTags(text, 'MudButton')];
    for (const action of ['Cancel', 'Confirm']) {
        const button = buttons.find(b => b.attrs.get('OnClick') === `@${action}`);
        assert.ok(button);
        assert.equal(text.slice(button.end).split('</MudButton>')[0].trim(), action);
    }
    const checkboxes = [...parseOpeningTags(readDialog('Fashion9Dialog'), 'MudCheckBox')];
    assert.ok(checkboxes.length > 0);
    assert.ok(checkboxes.every(b => b.attrs.get('Disabled') === '@context.Item.IsEmpty'));
    const selects = [...parseOpeningTags(readDialog('Donut9Dialog'), 'MudSelect')];
    assert.equal(selects.length, 2);
    assert.ok(selects.every(s => s.attrs.get('Variant') === '@Variant.Outlined' && s.attrs.get('Dense') === 'true'));
    assert.ok(selects.every(s => !s.attrs.get('ValueChanged').includes('Dense')));
});
