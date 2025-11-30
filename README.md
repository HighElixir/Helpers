# HighElixir.Helpers

Unity開発を効率化するためのヘルパーライブラリ集です。ステートマシン、タイマー管理、オブジェクトプーリングなど、ゲーム開発に必要な汎用機能を提供します。

## 特徴

- **StateMachine**: 汎用的なステートマシンライブラリ（非同期対応、サブステートマシン対応）
- **Timers**: 柔軟なタイマー管理システム（カウントダウン、カウントアップ、パルス、アップダウン）
- **Pool**: オブジェクトプーリングシステム
- **Core**: 基本的なユーティリティクラス群
- **Unity**: Unity固有のヘルパー機能

## インストール

### Unity Package

以下のリンクから `.unitypackage` ファイルをダウンロードして、Unityプロジェクトにインポートしてください。

| パッケージ | ダウンロード |
|-----------|------------|
| **HighElixir.Helpers (全機能)** | [HighElixir.unitypackage](https://github.com/HighElixir/Helpers/raw/main/HighElixir.unitypackage) |
| **StateMachine のみ** | [highelixir.statemachine.unitypackage](https://github.com/HighElixir/Helpers/raw/main/Library開発用/Assets/HighElixir/Packages/highelixir.statemachine.unitypackage) |
| **Timers のみ** | [highelixir.timers.unitypackage](https://github.com/HighElixir/Helpers/raw/main/Library開発用/Assets/HighElixir/Packages/highelixir.timers.unitypackage) |

### インポート方法

1. 上記リンクから `.unitypackage` ファイルをダウンロード
2. Unityで `Assets > Import Package > Custom Package...` を選択
3. ダウンロードした `.unitypackage` ファイルを選択
4. インポートするファイルを選択して `Import` をクリック

## モジュール

### StateMachine

Unity向けの汎用ステートマシンライブラリです。

- ジェネリック設計で任意のコンテキスト・イベント・ステート型に対応
- 非同期処理をサポート（`StateAsync<TCont>`）
- ネストされたサブステートマシン構造をサポート
- イベント駆動による柔軟な状態遷移
- タグシステムによるステート分類

```csharp
using HighElixir.StateMachines;

// ステートマシンの作成
var machine = new StateMachine<MyContext, MyEvent, MyStateId>(context);

// ステートと遷移の登録
machine.RegisterState(MyStateId.Idle, new IdleState());
machine.RegisterState(MyStateId.Walk, new WalkState());
machine.RegisterTransition(MyStateId.Idle, MyEvent.StartWalk, MyStateId.Walk);

// 起動
await machine.Awake(MyStateId.Idle);
```

詳細は [StateMachine README](Library開発用/Assets/HighElixir/Scripts/StateMachine/README.md) を参照してください。

### Timers

複数のタイマーを一元管理し、リアクティブなイベント通知機能を提供します。

- **CountDownTimer**: 指定時間からのカウントダウン
- **CountUpTimer**: 経過時間の計測
- **PulseTimer**: 一定間隔でのイベント発火
- **UpAndDownTimer**: 上昇・下降方向の切り替え可能

```csharp
using HighElixir.Timers;

var timer = new Timer("MyTimerManager");

// 5秒のカウントダウンタイマー
timer.CountDownRegister(5f, out var ticket, name: "Countdown", andStart: true)
    .Subscribe(eventId =>
    {
        if (TimerEventRegistry.Equals(eventId, TimeEventType.Finished))
        {
            Debug.Log("カウントダウン完了！");
        }
    });
```

詳細は [Timers README](Library開発用/Assets/HighElixir/Scripts/Timer/README.md) を参照してください。

### Pool

効率的なオブジェクトプーリングシステムを提供します。

```csharp
using HighElixir.Pools;

// プールの作成と使用
var pool = new Pool<MyObject>(() => new MyObject(), 10);
var obj = pool.Get();
pool.Release(obj);
```

### Core

基本的なユーティリティクラス群を提供します。

- **Collections**: コレクション操作ヘルパー
- **Logging**: ロギングインターフェース
- **Observable**: リアクティブプログラミング基盤
- **EnumWrapper**: 列挙型のラッパー
- **RandomExtensions**: 乱数拡張メソッド
- **TextFilters**: テキストフィルタリング

### Unity

Unity固有のヘルパー機能を提供します。

- **SingletonBehaviour**: シングルトンパターンのMonoBehaviour実装
- **SceneManagement**: シーン管理ヘルパー
- **UI**: UIユーティリティ
- **Addressables**: Addressablesシステムのヘルパー
- **DOTween連携**: DOTweenとの統合機能

## 名前空間

```csharp
using HighElixir.StateMachines;      // ステートマシン
using HighElixir.Timers;             // タイマー
using HighElixir.Pools;              // オブジェクトプール
using HighElixir.Core;               // コアユーティリティ
using HighElixir.Unity;              // Unity固有機能
```

## 依存関係

- Unity 2021.3 以上を推奨
- UniRx（オプション - リアクティブ機能使用時）
- UniTask（オプション - 非同期機能使用時）

## ライセンス

© HighElixir
