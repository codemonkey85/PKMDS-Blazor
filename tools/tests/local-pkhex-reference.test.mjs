import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import path from 'node:path';
import test from 'node:test';

// Evaluation alone needs no external checkout. The space also exercises argument quoting.
const sourcePath = path.resolve('local PKHeX probe/PKHeX.Core.csproj');

function references(project, local) {
    const result = spawnSync('dotnet', [
        'msbuild', project, '-nologo', '-getItem:PackageReference,ProjectReference',
        `-p:UseLocalPKHeX=${local}`, `-p:PKHeXSourcePath=${sourcePath}`,
    ], { encoding: 'utf8' });
    assert.equal(result.status, 0, result.error?.message ?? result.stdout + result.stderr);
    const items = JSON.parse(result.stdout).Items;
    return {
        packages: items.PackageReference.map(item => item.Identity).sort(),
        projects: items.ProjectReference.map(item => path.resolve(item.FullPath)).sort(),
    };
}

test('local PKHeX replaces the direct package only when enabled', () => {
    const project = 'Pkmds.Core/Pkmds.Core.csproj';
    const normal = references(project, false);
    assert.ok(normal.packages.includes('PKHeX.Core'));
    assert.ok(!normal.projects.includes(sourcePath));

    const local = references(project, true);
    assert.deepEqual(local.packages, normal.packages.filter(name => name !== 'PKHeX.Core'));
    assert.deepEqual(local.projects, [...normal.projects, sourcePath].sort());
});

for (const project of [
    'Pkmds.Rcl/Pkmds.Rcl.csproj',
    'Pkmds.Web/Pkmds.Web.csproj',
    'Pkmds.Tests/Pkmds.Tests.csproj',
    'Pkmds.Functions/Pkmds.Functions.csproj',
    'tools/legalization-outcomes/LegalizationOutcomes.csproj',
]) {
    test(`local PKHeX keeps unrelated references intact in ${project}`, () => {
        const normal = references(project, false);
        assert.ok(!normal.packages.includes('PKHeX.Core'));
        assert.deepEqual(references(project, true), normal);
    });
}
