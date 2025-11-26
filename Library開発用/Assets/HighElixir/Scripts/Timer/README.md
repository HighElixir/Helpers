# HighElixir.Timers

Unity向けの柔軟なタイマー管理ライブラリです。複数のタイマーを一元管理し、リアクティブなイベント通知機能を提供します。

## 特徴

- **複数タイマーの一元管理**: `Timer` クラスで複数のタイマーを効率的に管理
- **リアクティブイベント**: タイマーの状態変化を `IObservable<T>` で購読可能
- **4種類のタイマータイプ**: カウントダウン、カウントアップ、パルス、アップダウン
- **Tick対応**: フレーム単位での更新に対応したTickタイマーバリアント
- **スレッドセーフ**: ロック機構による安全な並行アクセス

## インストール

Unity Package Managerを使用してインストールしてください。

## タイマーの種類

### CountDownTimer (カウントダウンタイマー)

指定した時間から0までカウントダウンします。

```csharp
using HighElixir.Timers;

var timer = new Timer("MyTimerManager");

// 5秒のカウントダウンタイマーを登録
timer.CountDownRegister(5f, out var ticket, name: "Countdown", andStart: true)
    .Subscribe(eventId =>
    {
        if (TimerEventRegistry.Equals(eventId, TimeEventType.Finished))
        {
            Debug.Log("カウントダウン完了！");
        }
    });
```

### CountUpTimer (カウントアップタイマー)

0から無制限にカウントアップします。経過時間の計測などに使用します。

```csharp
// カウントアップタイマーを登録
timer.CountUpRegister(0f, out var ticket, name: "Elapsed", andStart: true);

// 後で経過時間を取得
if (timer.TryGetCurrentTime(ticket, out float current))
{
    Debug.Log($"経過時間: {current}秒");
}
```

### PulseTimer (パルスタイマー)

一定間隔でイベントを発火し続けます。定期的な処理に最適です。

```csharp
// 1秒ごとにイベントを発火するパルスタイマー
timer.PulseRegister(0f, 1f, out var ticket, name: "Pulse", andStart: true)
    .Subscribe(eventId =>
    {
        if (TimerEventRegistry.Equals(eventId, TimeEventType.Finished))
        {
            Debug.Log("パルス発火！");
        }
    });
```

### UpAndDownTimer (アップダウンタイマー)

上昇・下降方向を切り替え可能なタイマーです。UIアニメーションなどに便利です。

```csharp
// 3秒のアップダウンタイマー（上昇方向で開始）
timer.UpDownRegister(3f, out var ticket, name: "UpDown", reversing: true, andStart: true);

// 方向を切り替え
if (timer.TryGetTimer(ticket, out var t) && t is IUpAndDown upDown)
{
    upDown.ReverseDirection();
}
```

## 基本的な使い方

### 1. タイマーマネージャーの作成

```csharp
using HighElixir.Timers;

public class MyBehaviour : MonoBehaviour
{
    private Timer _timer;

    void Awake()
    {
        _timer = new Timer(gameObject.name);
    }
}
```

### 2. タイマーの登録と開始

```csharp
void Start()
{
    // カウントダウンタイマーを登録して開始
    _timer.CountDownRegister(10f, out _ticket, name: "GameTimer", andStart: true)
        .Subscribe(OnTimerEvent);
}

private void OnTimerEvent(int eventId)
{
    if (TimerEventRegistry.Equals(eventId, TimeEventType.Finished))
    {
        Debug.Log("タイマー完了");
    }
    else if (TimerEventRegistry.Equals(eventId, TimeEventType.Start))
    {
        Debug.Log("タイマー開始");
    }
}
```

### 3. 更新処理

```csharp
void Update()
{
    _timer.Update(Time.deltaTime);
}
```

### 4. タイマーの操作

```csharp
using HighElixir.Timers.Extensions;

// 開始
_timer.Start(_ticket);

// 停止
_timer.Stop(_ticket);

// リセット
_timer.Reset(_ticket);

// 再開（リセット＋開始）
_timer.Restart(_ticket);

// 初期化（イベント通知なし）
_timer.Initialize(_ticket);
```

### 5. リソースの解放

```csharp
void OnDestroy()
{
    _timer?.Dispose();
}
```

## 遅延操作

タイマーの操作を次のUpdateで実行する遅延操作が可能です。

```csharp
// 遅延開始
_timer.Start(_ticket, isLazy: true);

// 遅延停止
_timer.Stop(_ticket, isLazy: true);

// 遅延リセット
_timer.Reset(_ticket, isLazy: true);
```

## Tickタイマー

フレーム単位で更新されるTickタイマーを使用することもできます。

```csharp
// Tick版カウントダウン（10フレーム）
timer.CountDownRegister(10f, out var ticket, name: "TickTimer", isTick: true, andStart: true);

// Tick版パルス（5フレームごとにイベント）
timer.PulseRegister(0f, 5f, out var ticket, name: "TickPulse", isTick: true, andStart: true);
```

## イベント一覧

| イベント | 説明 |
|---------|------|
| `TimeEventType.Start` | タイマー開始時 |
| `TimeEventType.Stop` | タイマー停止時 |
| `TimeEventType.Reset` | タイマーリセット時 |
| `TimeEventType.Initialize` | タイマー初期化時 |
| `TimeEventType.Finished` | タイマー完了時 |

## リアクティブプロパティ

タイマーの現在値をリアクティブに監視できます。

```csharp
// TimeData（Current, Delta）を購読
timer.GetReactiveProperty(_ticket)
    .Subscribe(data =>
    {
        Debug.Log($"現在値: {data.Current}");
    });

// 現在値のみを購読
timer.GetCurrentReactive(_ticket)
    .Subscribe(current =>
    {
        Debug.Log($"現在値: {current}");
    });
```

## 正規化された経過時間

`INormalizeable`を実装したタイマー（CountDownTimer, PulseTimer, UpAndDownTimer）では、0〜1に正規化された経過時間を取得できます。

```csharp
if (timer.TryGetNormalizedElapsed(_ticket, out float normalized))
{
    // 0.0 〜 1.0 の値
    progressBar.fillAmount = normalized;
}
```

## エラーハンドリング

```csharp
_timer.OnError += HandleError;

private void HandleError(Exception ex)
{
    Debug.LogError($"タイマーエラー: {ex.Message}");
}
```

## API リファレンス

### Timer クラス

| メソッド | 説明 |
|---------|------|
| `CountDownRegister(duration, out ticket, ...)` | カウントダウンタイマーを登録 |
| `CountUpRegister(initTime, out ticket, ...)` | カウントアップタイマーを登録 |
| `PulseRegister(initTime, interval, out ticket, ...)` | パルスタイマーを登録 |
| `UpDownRegister(duration, out ticket, ...)` | アップダウンタイマーを登録 |
| `UnRegister(ticket)` | タイマーを登録解除 |
| `Update(deltaTime)` | タイマーを更新 |
| `TryGetCurrentTime(ticket, out current)` | 現在時間を取得 |
| `IsRunning(ticket)` | 実行中かどうか |
| `IsFinished(ticket)` | 完了したかどうか |
| `Contains(ticket)` | タイマーが存在するか |
| `ChangeInitialTime(ticket, newInitial)` | 初期時間を変更 |
| `GetSnapshot()` | スナップショットを取得 |
| `Dispose()` | リソースを解放 |

### ITimer インターフェース

| プロパティ/メソッド | 説明 |
|------------------|------|
| `InitialTime` | リセット時に戻る時間 |
| `Current` | 現在の時間 |
| `IsRunning` | 実行中かどうか |
| `IsFinished` | 完了したかどうか |
| `TimeReactive` | 時間変化を通知するObservable |
| `ReactiveTimerEvent` | イベントを通知するObservable |
| `Start()` | タイマーを開始 |
| `Stop()` | タイマーを停止 |
| `Reset()` | タイマーをリセット |
| `Initialize()` | タイマーを初期化（イベントなし） |
| `Update(dt)` | タイマーを更新 |

## License

このライブラリはHighElixirの一部です。
