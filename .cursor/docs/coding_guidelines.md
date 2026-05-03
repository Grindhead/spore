# Coding Guidelines — Project Spore

Summary for AI and human developers. **Full workflow:** user rules (TDD, Memory Bank); **architecture:** `memory-bank/systemPatterns.md`.

## TDD

- Write failing tests first (Red); implement minimal code to pass (Green); refactor with tests green.
- Do not delete tests; fix or refactor them.
- EditMode for pure C# logic; PlayMode for integration (spawning, scenes, save/load).

## Memory Bank

- Read `memory-bank/` at task start; do not assume context outside it.
- When changing architecture or patterns, update `systemPatterns.md`, `activeContext.md`, `progress.md`.
- Rules and docs under `.cursor/` should reference memory-bank and not contradict it.

## Code Changes

- One file at a time where possible; focused edits.
- Refactor over rewrite; keep files under 200–300 lines; modular, testable code.
- No magic numbers; named constants; robust error handling and assertions.

## Environment

- Respect dev/test/prod contexts; no one-off scripts in main code paths.
- Keep code deployment-safe; use Cursor rules (`.cursor/rules/`) for Unity, Blender, performance, and testing.
