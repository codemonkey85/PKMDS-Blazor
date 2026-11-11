// A small scanner for the quoted attributes and explicit @() expressions used by
// the Trainer inventory. This is not a general Razor/C# parser.
function skipString(text, start, verbatim = false) {
    const quote = text[start];
    for (let i = start + 1; i < text.length; i++) {
        if (!verbatim && text[i] === '\\') {
            i++;
        } else if (text[i] === quote) {
            if (verbatim && text[i + 1] === quote) i++;
            else return i + 1;
        }
    }
    throw new Error(`Unterminated string at offset ${start}`);
}

function skipExpression(text, start) {
    let depth = 1;
    for (let i = start + 2; i < text.length; i++) {
        if (text[i] === '"' || text[i] === "'") {
            i = skipString(text, i, text[i - 1] === '@') - 1;
        } else if (text[i] === '(') {
            depth++;
        } else if (text[i] === ')' && --depth === 0) {
            return i + 1;
        }
    }
    throw new Error(`Unterminated Razor expression at offset ${start}`);
}

export function* parseOpeningTags(text, tags) {
    const tag = new RegExp(`<(${tags})\\b`, 'g');
    const attribute = /([@\w:-]+)\s*(=\s*)?/y;
    for (let match; (match = tag.exec(text));) {
        const attrs = new Map();
        let i = tag.lastIndex;
        while (i < text.length) {
            if (/\s/.test(text[i])) { i++; continue; }
            if (text[i] === '>' || text.startsWith('/>', i)) break;
            attribute.lastIndex = i;
            const attr = attribute.exec(text);
            if (!attr) throw new Error(`Invalid attribute at offset ${i}`);
            i = attribute.lastIndex;
            let value = 'true'; // Bare boolean component attribute.
            if (attr[2]) {
                const quote = text[i];
                if (quote !== '"' && quote !== "'")
                    throw new Error(`Expected quoted attribute at offset ${i}`);
                const start = ++i;
                while (i < text.length && text[i] !== quote) {
                    if (text.startsWith('@(', i)) i = skipExpression(text, i);
                    else i++;
                }
                if (i === text.length) throw new Error(`Unterminated attribute at offset ${start}`);
                value = text.slice(start, i++).replace(/\s+/g, ' ');
            }
            attrs.set(attr[1], value);
        }
        if (i === text.length) throw new Error(`Unterminated tag at offset ${match.index}`);
        const end = i + (text[i] === '/' ? 2 : 1);
        yield { name: match[1], index: match.index, end, attrs };
        tag.lastIndex = end;
    }
}
