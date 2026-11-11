// Rebuild the concrete Trainer control inventory used by docs/TRAINER_AUDIT.md.
// No dependencies or private save data are needed.
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';

const main = 'Pkmds.Rcl/Components/MainTabPages/';
const dialogs = 'Pkmds.Rcl/Components/Dialogs/';
const files = [
    ['TrainerInfoTab', main, 'Format matrix and common-field table in TRAINER_AUDIT.md'],
    ['BadgesComponent', main, 'Badge table in TRAINER_AUDIT.md'],
    ['TrainerInfoSav2Section', `${main}Trainer/`, 'GSC; GS Ball is Crystal only'],
    ['TrainerInfoSav9SvSection', `${main}Trainer/`, 'SV; Blueberry controls require revision >= 2'],
    ['TrainerInfoSav9ZaSection', `${main}Trainer/`, 'ZA; DLC controls require revision != 0'],
    ['SayingsDialog', dialogs, 'Gen 6; nickname is XY only; read-only'],
    ['TrainerCardDialog', dialogs, 'Sword/Shield'],
    ['Fashion9Dialog', dialogs, 'SV or ZA according to section'],
    ['Raid9Dialog', dialogs, 'SV; Kitakami >= 1; Blueberry >= 2'],
    ['Donut9Dialog', dialogs, 'ZA DLC (revision != 0)'],
];
const tags = 'MudTextField|MudNumericField|MudSelect|MudCheckBox|BrowseAutocomplete|InputDate|MudIconButton|MudButton|GenderDisplayComponent|MudTabs|MudTabPanel|PropertyColumn|TemplateColumn|MudOverlay|MudProgressCircular';
const rows = [['Source', 'Control', 'Label or title', 'Read value', 'Write or action', 'Bounds', 'Format', 'Read-only or disabled', 'Scope']];
for (const [name, dir, scope] of files) {
    const path = `${dir}${name}.razor`;
    const text = readFileSync(path, 'utf8');
    // Component tags finish at /> or at the end of a line; ignore C# lambda arrows.
    const re = new RegExp(`<(${tags})\\b([\\s\\S]*?)(?:\\/>|(?<!=)>[ \\t]*\\r?\\n)`, 'g');
    for (const match of text.matchAll(re)) {
        const attrs = new Map();
        const attr = /([@\w:-]+)="([\s\S]*?)"(?=\s+[@\w:-]+\s*=|\s*\/?[>]|\s*$)/g;
        for (const m of match[2].matchAll(attr))
            attrs.set(m[1], m[2].replace(/\s+/g, ' '));
        let label = attrs.get('Label') ?? attrs.get('Title') ?? attrs.get('Text') ?? attrs.get('aria-label') ?? attrs.get('id') ?? '';
        if (!label && ['MudButton', 'MudTabPanel'].includes(match[1]))
            label = text.slice(match.index + match[0].length).split(`</${match[1]}>`)[0].trim().replace(/\s+/g, ' ');
        rows.push([
            `${path}:${text.slice(0, match.index).split('\n').length}`,
            match[1], label,
            attrs.get('@bind-Value:get') ?? attrs.get('@bind-Value') ?? attrs.get('Value') ?? attrs.get('Gender') ?? attrs.get('Property') ?? attrs.get('ActivePanelIndex') ?? '',
            attrs.get('@bind-Value:set') ?? attrs.get('ValueChanged') ?? attrs.get('OnClick') ?? attrs.get('OnChange') ?? attrs.get('ActivePanelIndexChanged') ?? (attrs.has('@bind-Value') ? 'two-way binding' : ''),
            ['Min', 'Max', 'MaxLength', 'step'].filter(a => attrs.has(a)).map(a => `${a}=${attrs.get(a)}`).join('; '),
            attrs.get('Format') ?? attrs.get('Type') ?? '',
            ['ReadOnly', 'Disabled'].filter(a => attrs.has(a)).map(a => `${a}=${attrs.get(a)}`).join('; '), scope,
        ]);
    }
}
mkdirSync('docs', { recursive: true });
writeFileSync('docs/trainer-controls.csv', rows.map(row => row.map(cell => `"${cell.replaceAll('"', '""')}"`).join(',')).join('\n') + '\n');
console.log(`Inventoried ${rows.length - 1} control declarations.`);
