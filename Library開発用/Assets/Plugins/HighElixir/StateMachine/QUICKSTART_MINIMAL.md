# HighElixir.StateMachine Minimal Guide

このガイドは「まず動かす」ための最小セットです。

## 1. 使うAPIは4つだけ
- `RegisterState`
- `RegisterTransition`
- `Awake`
- `Send`

## 2. 最小コード
```csharp
using HighElixir.StateMachines;

enum S { Idle, Walk }
enum E { Go }
sealed class Ctx { }
sealed class IdleState : State<Ctx> { }
sealed class WalkState : State<Ctx> { }

var m = new StateMachine<Ctx, E, S>(new Ctx());
m.RegisterState(S.Idle, new IdleState());
m.RegisterState(S.Walk, new WalkState());
m.RegisterTransition(S.Idle, E.Go, S.Walk);

await m.Awake(S.Idle);
await m.Send(E.Go);
```

## 3. Update運用
- 毎フレーム `await m.Update(deltaTime)` を呼ぶ。
- `LazySend` を使う場合は `Update` が必須。

## 4. 非同期Stateの推奨名前空間
- 推奨: `HighElixir.StateMachines.Threading`
- 互換: `HighElixir.StateMachines.Thead`

## 5. 最初の運用ルール（推奨）
- 1ステート1責務
- 初期導入時は `QueueMode.UntilFailures` のまま
- 破棄時は `Dispose()`
