# HighElixir.StateMachine Naming Migration Plan

## Scope
- Directory typo: `Scripts/Extention` -> `Scripts/Extension`
- Directory typo: `Scripts/Thead` -> `Scripts/Threading`
- Namespace typo: `HighElixir.StateMachines.Thead` -> `HighElixir.StateMachines.Threading`
- Type typo: `LogLevelExtention` -> `LogLevelExtension`
- Legacy folder: `Scripts/Thead/SequenceBlocks(削除予定)` cleanup

## Current Status
- `HighElixir.StateMachines.Threading.StateAsync<TCont>` already exists as a compatibility bridge.
- Core runtime still imports `HighElixir.StateMachines.Thead`.
- `README.md` documents both namespaces (`Threading` recommended, `Thead` compatibility).

## Migration Strategy (Non-breaking)
1. Add compatibility aliases first.
- Keep old namespaces/types, mark as `[Obsolete]` with exact replacement message.
- Ensure public API signatures remain source-compatible for one release cycle.

2. Switch internal references.
- Replace internal `using HighElixir.StateMachines.Thead;` with `...Threading;`.
- Move new code generation/documentation to corrected names only.

3. Normalize names and paths.
- Rename `LogLevelExtention` to `LogLevelExtension` and provide shim type if needed.
- Rename folders in Unity with `.meta` continuity handling.

4. Remove deprecated surface in major release.
- Drop old namespace/type shims after migration window.
- Delete `SequenceBlocks(削除予定)` once no references remain.

## Concrete Task List
- [ ] Add `[Obsolete]` to old namespace entry points (`Thead` APIs).
- [ ] Update all internal `using` directives to `Threading`.
- [ ] Introduce `LogLevelExtension` and legacy alias strategy.
- [ ] Rename `Extention` directory and validate asmdef references.
- [ ] Rename `Thead` directory and validate asmdef references.
- [ ] Remove `SequenceBlocks(削除予定)` when unused.
- [ ] Update README/examples to corrected spellings only.
- [ ] Add EditMode tests for old/new namespace coexistence.

## Acceptance Criteria
- Existing projects compile without code changes on migration release.
- New sample code uses only `Threading` / `Extension` spellings.
- Build/test passes before and after folder rename.
- Obsolete warnings clearly indicate replacement API.
