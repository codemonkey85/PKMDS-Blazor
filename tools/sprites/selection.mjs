import path from 'node:path';

export function gameSets(mapping) {
    const sets = [...mapping.matchAll(/new\("(generation-[^" ]+)", "(png|gif|webp)"\)/g)]
        .map(([, directory, extension]) => ({directory, extension}));
    if (sets.length < 20) throw new Error('Could not extract all game sprite sets');
    return sets;
}

const variants = new Set(['', 'female', 'shiny', 'shiny/female', 'transparent', 'transparent/female']);
export function isConsumedSprite(file, sets) {
    if (/^sprites\/pokemon\/other\/home\/(?:shiny\/)?(?:female\/)?[^/]+\.png$/.test(file)) return true;
    return sets.some(set => {
        const prefix = 'sprites/pokemon/versions/' + set.directory + '/';
        if (!file.startsWith(prefix) || !file.endsWith('.' + set.extension)) return false;
        const relative = file.slice(prefix.length);
        return variants.has(path.posix.dirname(relative) === '.' ? '' : path.posix.dirname(relative));
    });
}
