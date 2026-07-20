#!/usr/bin/env python3
"""Regenerate derived data after Codex edits a data generator."""

import json
import re
import subprocess
import sys
from pathlib import Path
from typing import Any, Iterable


def strings(value: Any) -> Iterable[str]:
    if isinstance(value, str):
        yield value
    elif isinstance(value, dict):
        for child in value.values():
            yield from strings(child)
    elif isinstance(value, list):
        for child in value:
            yield from strings(child)


def changed_paths(payload: dict[str, Any]) -> set[str]:
    paths: set[str] = set()
    for container_name in ("tool_input", "tool_response"):
        container = payload.get(container_name) or {}
        if isinstance(container, dict):
            for key in ("file_path", "filePath", "path"):
                value = container.get(key)
                if isinstance(value, str):
                    paths.add(value.replace("\\", "/"))

        for value in strings(container):
            for match in re.finditer(
                r"^\*\*\* (?:Add|Update|Delete) File: (.+)$", value, re.MULTILINE
            ):
                paths.add(match.group(1).strip().replace("\\", "/"))
            for match in re.finditer(r"^diff --git a/(.+?) b/(.+)$", value, re.MULTILINE):
                paths.add(match.group(2).strip().replace("\\", "/"))
    return paths


def run(command: list[str], repo: Path) -> None:
    result = subprocess.run(command, cwd=repo, check=False)
    if result.returncode:
        raise SystemExit(result.returncode)


def main() -> None:
    payload = json.load(sys.stdin)
    paths = changed_paths(payload)
    repo = Path(__file__).resolve().parents[2]

    if any(Path(path).name == "generate-descriptions.cs" for path in paths):
        pokeapi = repo.parent / "pokeapi"
        showdown = repo.parent / "pokemon-showdown"
        missing = [str(path) for path in (pokeapi, showdown) if not path.is_dir()]
        if missing:
            print(
                "Skipped description regeneration; missing source checkout(s): "
                + ", ".join(missing),
                flush=True,
            )
            return

        print("Regenerating ability, move, and item data...", flush=True)
        run(
            [
                "dotnet",
                "run",
                "tools/generate-descriptions.cs",
                "--",
                "--pokeapi",
                str(pokeapi),
                "--showdown",
                str(showdown),
            ],
            repo,
        )
    elif any(Path(path).name == "generate-tm-data.cs" for path in paths):
        print("Regenerating TM data...", flush=True)
        run(["dotnet", "run", "tools/generate-tm-data.cs"], repo)


if __name__ == "__main__":
    main()
