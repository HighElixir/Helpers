# HighElixir.StateMachine ドキュメント

HighElixir.StateMachine は、Unity 向けの汎用ステートマシンライブラリです。  
任意のコンテキスト・イベント・ステート型を扱い、イベント駆動による状態遷移をサポートします。  
非同期処理やネストされたサブステートマシン構造にも対応しています。

---

## 目次

1. [概要](#概要)
2. [主要クラス](#主要クラス)
3. [基本的な使い方](#基本的な使い方)
4. [State クラス](#state-クラス)
5. [StateMachine クラス](#statemachine-クラス)
6. [ライフサイクル](#ライフサイクル)
7. [遷移の登録と実行](#遷移の登録と実行)
8. [StateInfo クラス](#stateinfo-クラス)
9. [非同期ステート (StateAsync)](#非同期ステート-stateasync)
10. [サブステートマシン](#サブステートマシン)
11. [イベントキュー](#イベントキュー)
12. [拡張メソッド](#拡張メソッド)
13. [ロギング](#ロギング)
14. [エラーハンドリング](#エラーハンドリング)
15. [オプション設定](#オプション設定)

---

## 概要

HighElixir.StateMachine は以下の特徴を持ちます：

- **ジェネリック設計**: `StateMachine<TCont, TEvt, TState>` により、コンテキスト・イベント・ステートの型を自由に定義可能
- **非同期対応**: `StateAsync<TCont>` を使用した非同期 Enter/Update/Exit 処理
- **サブステートマシン**: ネストされたステートマシン構造をサポート
- **イベントキュー**: 遅延イベント送信と柔軟なキュー処理モード
- **遷移制御**: 条件付き遷移許可やコマンドデキューのブロック機能
- **タグシステム**: ステートへのタグ付与と検索
- **購読パターン**: Reactive な遷移イベント購読

---

## 主要クラス

| クラス/インターフェース | 説明 |
|------------------------|------|
| `StateMachine<TCont, TEvt, TState>` | ステートマシン本体 |
| `State<TCont>` | 同期ステートの基底クラス |
| `StateAsync<TCont>` | 非同期ステートの基底クラス |
| `StateInfo` | ステート情報を保持する内部クラス |
| `TransitionResult` | 遷移結果を表す構造体 |
| `StateMachineOption<TCont, TEvt, TState>` | マシン構築時のオプション |
| `IEventQueue<TCont, TEvt, TState>` | イベントキューのインターフェース |
| `IStateMachineErrorHandler` | エラーハンドリングインターフェース |

---

## 基本的な使い方

### 1. ステートの定義

```csharp
// 同期ステート
public class IdleState : State<MyContext>
{
    public override void Enter()
    {
        // ステート開始時の処理
    }

    public override void Update(float deltaTime)
    {
        // 毎フレーム呼ばれる処理
    }

    public override void Exit()
    {
        // ステート終了時の処理
    }
}
```

### 2. ステートマシンの構築

```csharp
// コンテキストとステート/イベントの型を指定
var machine = new StateMachine<MyContext, MyEvent, MyStateId>(context);

// ステートの登録
machine.RegisterState(MyStateId.Idle, new IdleState());
machine.RegisterState(MyStateId.Walk, new WalkState());
machine.RegisterState(MyStateId.Run, new RunState());

// 遷移の登録
machine.RegisterTransition(MyStateId.Idle, MyEvent.StartWalk, MyStateId.Walk);
machine.RegisterTransition(MyStateId.Walk, MyEvent.StartRun, MyStateId.Run);
machine.RegisterTransition(MyStateId.Run, MyEvent.Stop, MyStateId.Idle);

// 初期化・起動
await machine.Awake(MyStateId.Idle);
```

### 3. 遷移の実行

```csharp
// 即時イベント送信
bool success = await machine.Send(MyEvent.StartWalk);

// 遅延イベント送信（次の Update で処理される）
machine.LazySend(MyEvent.StartRun);
```

### 4. 更新処理

```csharp
// Unity の Update() 等から呼び出す
await machine.Update(Time.deltaTime);
```

---

## State クラス

`State<TCont>` はすべてのステートの基底クラスです。

### プロパティ

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| `Parent` | `IStateMachine<TCont>` | 所属するステートマシン |
| `Tags` | `List<string>` | ステートに付与されたタグ一覧 |
| `Cont` | `TCont` | コンテキスト（保護されたプロパティ） |

### ライフサイクルメソッド

```csharp
public abstract class State<TCont>
{
    // ステートに入った時に呼ばれる
    public virtual void Enter() { }

    // ステートがアクティブな間、毎フレーム呼ばれる
    public virtual void Update(float deltaTime) { }

    // ステートを抜ける時に呼ばれる
    public virtual void Exit() { }
}
```

### 遷移制御メソッド

```csharp
// このステートへの遷移/からの遷移を許可するか
public virtual bool AllowTrans(EventState state) { return true; }

// コマンドキューからのデキューをブロックするか
public virtual bool BlockCommandDequeue() { return false; }
```

### タグ操作

```csharp
state.AddTag("grounded");
state.RemoveTag("jumping");
bool hasTag = state.HasTag("grounded");
```

---

## StateMachine クラス

`StateMachine<TCont, TEvt, TState>` はステートマシンの本体クラスです。

### コンストラクタ

```csharp
// 基本コンストラクタ
public StateMachine(
    TCont context,
    QueueMode mode = QueueMode.UntilFailures,
    IEventQueue<TCont, TEvt, TState> eventQueue = null,
    ILogger logger = null)

// オプションを指定するコンストラクタ
public StateMachine(TCont context, StateMachineOption<TCont, TEvt, TState> option)
```

### 主要プロパティ

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| `Context` | `TCont` | 実行コンテキスト |
| `Current` | `(TState id, StateInfo info)` | 現在のステート情報 |
| `Awaked` | `bool` | 初期化済みか |
| `IsRunning` | `bool` | 実行中か |
| `OnTransition` | `IObservable<TransitionResult>` | 遷移完了通知 |
| `OnCompletion` | `IObservable<StateInfo>` | ステート完了通知 |
| `EnableSelfTransition` | `bool` | 自己遷移の可否 |

---

## ライフサイクル

### 起動と停止

```csharp
// ステートマシンを初期化して起動
await machine.Awake(initialState);

// 一時停止（initialize=true で初期ステートにリセット）
machine.Pause(initialize: true);

// 再開
await machine.Resume();

// リセット（現在状態を初期ステートに戻す）
machine.Reset();

// リソース解放
machine.Dispose();
```

### 更新処理

```csharp
// 定期更新（イベントキュー処理とステート更新を実行）
await machine.Update(deltaTime);
```

---

## 遷移の登録と実行

### ステート登録

```csharp
// 基本的なステート登録
StateInfo info = machine.RegisterState(stateId, stateInstance);

// タグ付きで登録
StateInfo info = machine.RegisterState(stateId, stateInstance, "tag1", "tag2");
```

### 遷移登録

```csharp
// 基本的な遷移登録
machine.RegisterTransition(fromState, eventType, toState);

// コールバック付き遷移登録
IDisposable subscription = machine.RegisterTransition(
    fromState,
    eventType,
    toState,
    onTransition: result => Console.WriteLine($"遷移: {result}"),
    predicate: result => result.ToState != null  // 条件
);
```

### 任意遷移（Any Transition）

どのステートからでも遷移可能なグローバル遷移を登録します：

```csharp
// 任意遷移登録
machine.RegisterAnyTransition(eventType, toState);

// コールバック付き
machine.RegisterAnyTransition(eventType, toState, onTransition, predicate);
```

### イベント送信

```csharp
// 即時送信（結果を待つ）
bool success = await machine.Send(eventType);

// 遅延送信（キューに追加）
bool enqueued = machine.LazySend(eventType);

// 同一イベントが既にキューにある場合はスキップ
machine.LazySend(eventType, skipIfExist: true);
```

---

## StateInfo クラス

`StateInfo` はステートのメタ情報を保持する内部クラスです。

### 主要プロパティ

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| `ID` | `TState` | ステート識別子 |
| `State` | `State<TCont>` | ステートインスタンス |
| `Parent` | `StateMachine<...>` | 親ステートマシン |
| `Binded` | `bool` | ステートクラスが紐づけ済みか |

### 遷移許可イベント

```csharp
// 遷移許可条件の追加
info.AllowTrans += (eventState) => {
    // Entering/Exiting に対して許可するか判定
    return true;
};

// コマンドデキューブロック条件
info.BlockCommandDequeueFunc += () => {
    // true を返すとキュー処理をブロック
    return false;
};
```

---

## 非同期ステート (StateAsync)

`StateAsync<TCont>` は非同期処理に対応したステートの基底クラスです。

```csharp
public class LoadingState : StateAsync<MyContext>
{
    public override async Task EnterAsync(CancellationToken token)
    {
        await LoadResourcesAsync(token);
    }

    public override async Task UpdateAsync(CancellationToken token)
    {
        await ProcessAsync(token);
    }

    public override async Task ExitAsync(CancellationToken token)
    {
        await CleanupAsync(token);
    }
}
```

### キャンセルトークン

```csharp
// 現在のトークンを取得
CancellationToken token = machine.Take();

// 実行中の非同期処理をキャンセル
machine.Cancel();
```

---

## サブステートマシン

ネストされたステートマシン構造をサポートします。

### サブマシンのアタッチ

```csharp
// サブステートマシンを作成
var subMachine = new StateMachine<MyContext, SubEvent, SubState>(context);
subMachine.RegisterState(SubState.A, new StateA());
subMachine.RegisterState(SubState.B, new StateB());
subMachine.RegisterTransition(SubState.A, SubEvent.Next, SubState.B);

// オプション設定
var options = new StateMachine<...>.SubMachineOptions<SubState>
{
    ForwardEventsFirst = true,  // 子に先にイベントを転送
    OnExitResetState = true     // 親ステート終了時に子をリセット
};

// 親ステートにアタッチ
machine.AttachSubMachine(parentStateId, subMachine, SubState.A, options);
```

### SubMachineOptions

| オプション | 型 | デフォルト | 説明 |
|-----------|-----|-----------|------|
| `ForwardEventsFirst` | `bool` | `true` | 親の Send を子に先に試すか |
| `OnExitResetState` | `bool` | `true` | 親ステート Exit 時に子をリセットするか |
| `ExitMap` | `Dictionary<TSubState, TEvt>` | 空 | 子の特定ステート Enter 時に親にイベント送信 |

---

## イベントキュー

`LazySend` で送信されたイベントはキューに追加され、`Update` 時に処理されます。

### キューモード

```csharp
public enum QueueMode
{
    UntilSuccesses,  // 成功するまでキューを処理
    UntilFailures,   // 失敗するまでキューを処理（デフォルト）
    DoEverything     // すべて処理
}
```

### カスタムキュー

`IEventQueue<TCont, TEvt, TState>` を実装することで、独自のキュー処理を実現できます：

```csharp
public interface IEventQueue<TCont, TEvt, TState>
{
    QueueMode Mode { get; set; }
    bool Enqueue(TEvt item, bool skipIfExisting);
    Task Process();
    void Dispose();
}
```

---

## 拡張メソッド

### 遷移イベントの購読

```csharp
using HighElixir.StateMachines.Extension;

// 特定の遷移を購読
var subscription = machine.OnTransWhere(fromState, evt, toState)
    .Subscribe(result => Console.WriteLine(result));

// 任意の from から特定の to への遷移を購読
var subscription = machine.OnTransWhere(evt, toState)
    .Subscribe(result => Console.WriteLine(result));
```

### Enter/Exit イベントの購読

```csharp
// Enter イベント
machine.OnEnterEvent(stateId).Subscribe(_ => OnEnter());

// Exit イベント
machine.OnExitEvent(stateId).Subscribe(_ => OnExit());
```

### 複数遷移の一括登録

```csharp
// 同一ステートから複数遷移を登録
machine.RegisterTransitions(fromState,
    (Event1, ToState1),
    (Event2, ToState2),
    (Event3, ToState3));

// 任意遷移を複数登録
machine.RegisterAnyTransitions(
    (Event1, ToState1),
    (Event2, ToState2));
```

### 遅延送信

```csharp
// 指定時間後にイベント送信
await machine.SendEventWithDelayAsync(TimeSpan.FromSeconds(1), evt);

// ミリ秒指定
await machine.SendEventWithDelayAsync(500f, evt);
```

### 遷移ロック

```csharp
// 遷移後、指定時間は Exit をブロック
await machine.SendEventAndLockExitAsync(
    evt,
    TimeSpan.FromSeconds(0.5),
    onUnlock: () => Console.WriteLine("ロック解除"));
```

### タグ操作

```csharp
// 現在のステートが任意のタグを持つか
bool hasAny = machine.HasAny("tag1", "tag2");

// 現在のステートがすべてのタグを持つか
bool hasAll = machine.HasAll("tag1", "tag2");

// 子ステートマシンも含めてタグ検索
bool hasTagOnChild = machine.HasTagOnChild("tag");
```

### 遷移許可条件の追加

```csharp
// 条件付きで遷移を許可
IDisposable disposable = machine.AllowTrans(stateId, eventState =>
{
    return eventState == EventState.Exiting && someCondition;
});

// コマンドデキューのブロック
IDisposable disposable = machine.BlockCommandDequeue(stateId, () => isBlocked);
```

### 自動スコープ送信

```csharp
// using スコープで Enter と Exit を自動送信
using (machine.SendWith(enterEvent, exitEvent, isLazy: false))
{
    // 処理
}
// スコープを抜けると exitEvent が送信される
```

---

## ロギング

### ログレベル

```csharp
[Flags]
public enum LogLevel : uint
{
    None = 0,
    Info = 1 << 0,           // 一般情報
    Register = 1 << 1,       // ステート/遷移登録
    TransitionResult = 1 << 2, // 遷移結果
    Enter = 1 << 3,          // ステート進入
    StateUpdate = 1 << 4,    // ステート更新
    Exit = 1 << 5,           // ステート退出
    MachineLifeCycle = 1 << 6, // マシンライフサイクル
    LazyResult = 1 << 7,     // Lazy 評価結果
    Warning = 1 << 21,       // 警告
    OverrideWarning = 1 << 22, // 上書き警告
    Error = 1 << 41,         // エラー
    Fatal = 1 << 42,         // 致命的エラー
    ALL = INFO | WARN | ERROR
}
```

### ロガー設定

```csharp
machine.Logger = myLogger;  // ILogger 実装
machine.RequiredLoggerLevel = LogLevel.ALL;
```

### 遷移ロギング拡張

```csharp
using HighElixir.StateMachines.Extension;

// 遷移時に自動ログ出力
IDisposable subscription = machine.OnTransitionLogging();
```

---

## エラーハンドリング

### カスタムエラーハンドラ

```csharp
public class MyErrorHandler : IStateMachineErrorHandler
{
    public void Handle(Exception ex)
    {
        Debug.LogError($"StateMachine Error: {ex.Message}");
    }
}

machine.ErrorHandler = new MyErrorHandler();
```

---

## オプション設定

`StateMachineOption<TCont, TEvt, TState>` で構築時のオプションを指定できます。

```csharp
var option = new StateMachineOption<MyContext, MyEvent, MyState>
{
    // イベントキュー設定
    QueueMode = QueueMode.UntilFailures,
    Queue = customQueue,  // カスタムキュー（省略可）

    // ステート上書きルール
    EnableOverriding = false,  // true で上書き許可

    // 自己遷移の可否
    EnableSelfTransition = false,

    // ロギング
    Logger = myLogger,
    RequiredLoggerLevel = LogLevel.ALL
};

var machine = new StateMachine<MyContext, MyEvent, MyState>(context, option);
```

---

## 完了通知 (INotifyStateCompletion)

ステートの責務完了をマシンに通知するインターフェースです。

```csharp
public class CompletableState : State<MyContext>, INotifyStateCompletion
{
    private readonly Subject<byte> _completion = new();
    
    public IObservable<byte> Completion => _completion;

    public void NotifyCompletion()
    {
        _completion.OnNext(0);
    }
}

// マシン側で完了を購読
machine.OnCompletion.Subscribe(info =>
{
    Console.WriteLine($"State {info.ID} completed");
});
```

---

## 名前空間

```csharp
// メイン
using HighElixir.StateMachines;

// 拡張メソッド
using HighElixir.StateMachines.Extension;

// 非同期ステート
using HighElixir.StateMachines.Thead;

// 内部実装（通常は不要）
using HighElixir.StateMachines.Internal;
```

---

## ベストプラクティス

1. **ステートは単一責任で**: 各ステートは1つの状態を表現し、複雑なロジックは避ける
2. **タグを活用**: 複数のステートに共通する属性はタグで管理
3. **サブマシンで階層化**: 複雑な状態はサブステートマシンで分割
4. **遷移条件は AllowTrans で**: 動的な遷移制御は `AllowTrans` メソッドで実装
5. **エラーハンドラを設定**: 本番環境では必ず `ErrorHandler` を設定
6. **リソース解放**: 使用後は `Dispose()` を呼び出す

---

## ライセンス

© HighElixir
