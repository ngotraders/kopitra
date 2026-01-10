# Kopitra2 EA実装ガイド

## 概要

**Kopitra2** は、FXコピートレード配信・購読プラットフォーム向けのExpert Advisor（EA）です。配信者の取引をリアルタイムで購読者に配信し、自動的に複数口座で同期・実行するシステムを実現します。

本EAは以下の特徴を持つ**ポーリングベースの非同期メッセージング アーキテクチャ**に対応：
- **単一EA対応エンドポイント**: `/api/ea/*` で統一
- **SQLベースのメッセージキュー**: 複雑なQueue/Event Hubは不要
- **イベントソーシング設計**: すべての取引をDomainEventsとして記録
- **コスト最適化**: ポーリング間隔で無駄を最小化

## ファイル構成

**kopitra2/ea/ フォルダ:**
- `KopitraLib.mqh` - ライブラリ（MT4/MT5共通、バージョン0.2.0）
- `KopitraAgent.mq4` - MT4用EA
- `KopitraAgent.mq5` - MT5用EA

## API仕様

### エンドポイント一覧

| エンドポイント | メソッド | 説明 | リクエスト | レスポンス |
|---|---|---|---|---|
| `/api/ea/sessions` | POST | セッション作成・認証 | authMethod, authKey, accountId, deviceId | sessionId, token, status |
| `/api/ea/sessions/current` | DELETE | セッション削除 | - | - |
| `/api/ea/signals` | GET | シグナル取得（ポーリング） | accountId, timestamp | signals[] |
| `/api/ea/executions` | POST | 実行結果送信 | orderTicket, symbol, volume, ... | messageId, acknowledged |
| `/api/ea/executions/ack` | POST | メッセージACK送信 | messageId, status | - |
| `/api/ea/heartbeat` | POST | ハートビート送信 | accountId, status, metrics | - |
| `/api/ea/snapshot` | POST | スナップショット送信 | accountId, snapshot | - |

### セッション作成リクエスト

```json
{
  "authMethod": "account_session_key",
  "authenticationKey": "your-auth-key",
  "accountId": "account-uuid",
  "deviceId": "mt4-terminal-xxxxx",
  "platform": {
    "name": "MetaTrader 4",
    "build": 1090
  },
  "capabilities": {
    "events": ["InitAck", "SignalCommand", "ExecutionRequest", "ShutdownNotice"],
    "allowsOrderSubmission": false
  }
}
```

### セッション作成レスポンス

```json
{
  "sessionId": "session-uuid",
  "sessionToken": "jwt-token",
  "status": "authenticated"
}
```

## EA通信フロー

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. セッション確立                                              │
│    POST /api/ea/sessions                                       │
│    → authMethod, authenticationKey, accountId, deviceId, ...  │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ 2. 定期的なシグナルポーリング（推奨: 1-5秒ごと）              │
│    GET /api/ea/signals?accountId=XXX&timestamp=                │
│    ← [ { messageId, signalId, pair, direction, volume, ... } ] │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3. EA側で注文実行                                              │
│    MT4/MT5 OrderSend() / Trade API                             │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ 4. 実行結果送信                                                │
│    POST /api/ea/executions                                     │
│    { orderTicket, symbol, volume, price, profit, ... }        │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ 5. メッセージACK送信                                           │
│    POST /api/ea/executions/ack                                 │
│    { messageId, status: "processed" }                          │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ 6. 定期的なハートビート送信（推奨: 15秒ごと）                 │
│    POST /api/ea/heartbeat                                      │
│    { accountId, status, timestamp, metrics: {...} }           │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ 7. 定期的なスナップショット送信（推奨: 60秒ごと）              │
│    POST /api/ea/snapshot                                       │
│    { accountId, timestamp, snapshot: {balance, positions} }   │
└─────────────────────────────────────────────────────────────────┘
```

## データモデル

### シグナルメッセージ（SignalMessage）

EAが`GET /api/ea/signals`で受け取るデータ構造：

```json
{
  "messageId": "msg-uuid-xxxxxx",
  "signalId": "sig-uuid-xxxxxx",
  "signalType": "NewOrder",
  "pair": "EURUSD",
  "direction": "Buy",
  "positionSizeType": "ActualLot",
  "positionSize": 1.5,
  "entryPrice": 1.0850,
  "stopLoss": 1.0800,
  "takeProfit": 1.0900,
  "expiresAt": "2026-01-11T12:34:56Z"
}
```

**signalType の種類:**
- `NewOrder` - 新規エントリーシグナル
- `Modify` - ポジション修正（SL/TP変更）
- `ClosePosition` - ポジション決済

**positionSizeType の種類:**
- `ActualLot` - 実数値（ロット数）で指定
- `Percentage` - 配信者口座残高のパーセンテージで指定

### 実行メッセージ（ExecutionMessage）

EAが`POST /api/ea/executions`で送信するデータ構造：

```json
{
  "orderTicket": 12345,
  "symbol": "EURUSD",
  "volume": 1.5,
  "openPrice": 1.0852,
  "stopLoss": 1.0800,
  "takeProfit": 1.0900,
  "profit": 150.50,
  "type": 0,
  "openTime": "2026-01-11T12:34:56Z",
  "executionStatus": "Executed"
}
```

**executionStatus の種類:**
- `Executed` - 約定済み
- `Pending` - 約定待機中
- `Rejected` - 注文拒否

### ハートビート送信内容

```json
{
  "accountId": "account-uuid-xxxxxx",
  "status": "connected",
  "timestamp": "2026-01-11T12:34:56Z",
  "metrics": {
    "balance": 10000.0,
    "equity": 10500.0,
    "marginFree": 8000.0
  }
}
```

### スナップショット送信内容

```json
{
  "accountId": "account-uuid-xxxxxx",
  "timestamp": "2026-01-11T12:34:56Z",
  "snapshot": {
    "balance": 10000.0,
    "equity": 10500.0,
    "marginFree": 8000.0,
    "positions": [
      {
        "ticket": 12345,
        "symbol": "EURUSD",
        "volume": 1.5,
        "type": 0,
        "price": 1.0850,
        "stopLoss": 1.0800,
        "takeProfit": 1.0900,
        "profit": 150.50
      }
    ]
  }
}
```

## ライブラリ仕様（KopitraLib.mqh）

### バージョン
- **0.2.0** （kopitra2対応）

### 主要構造体

**KopitraConfig** - EA設定パラメータを保持：
```cpp
struct KopitraConfig
{
  string apiBaseUrl;               // APIベースURL
  string accountId;                // 口座ID
  string authMethod;               // 認証方式
  string authKey;                  // 認証キー
  string deviceId;                 // デバイスID（自動生成可）
  bool enableOrderSubmission;      // 注文送信可否
  int heartbeatIntervalSeconds;    // ハートビート間隔
  int pollIntervalSeconds;         // ポーリング間隔
  int snapshotIntervalSeconds;     // スナップショット間隔
  int sessionRetrySeconds;         // セッション再試行間隔
  int httpTimeoutMs;               // HTTP タイムアウト
};
```

**KopitraSession** - セッション状態を管理：
```cpp
struct KopitraSession
{
  KopitraSessionState state;       // Idle / Pending / Authenticated
  string sessionId;                // セッションID
  string authToken;                // 認証トークン
  long outboxSequence;             // メッセージシーケンス番号
  datetime lastHeartbeat;          // 最後のハートビート送信時刻
  datetime lastPoll;               // 最後のポーリング実行時刻
  datetime lastAttempt;            // 最後のセッション確立試行時刻
  int retryAfterHint;              // サーバーからの遅延指示
};
```

### 主要API関数

**セッション管理:**
- `KopitraStartup(ctx, config)` - EAを初期化
- `KopitraEnsureSession(ctx)` - セッションを確保（未接続時は自動作成）
- `KopitraCreateSession(ctx)` - セッション作成
- `KopitraDeleteSession(ctx)` - セッション削除

**シグナル・実行:**
- `KopitraFetchSignals(ctx, response)` - `/api/ea/signals` からシグナル取得
- `KopitraProcessSignals(ctx, payload)` - 受信シグナルを処理
- `KopitraSubmitExecution(ctx, executionData)` - `/api/ea/executions` に実行結果送信
- `KopitraSubmitExecutionAck(ctx, messageId)` - `/api/ea/executions/ack` にACK送信

**通知送信:**
- `KopitraSendHeartbeat(ctx)` - `/api/ea/heartbeat` にハートビート送信
- `KopitraSendSnapshot(ctx)` - `/api/ea/snapshot` にスナップショット送信

**イベント処理:**
- `KopitraOnTimer(ctx)` - タイマーイベント（1秒ごと）
- `KopitraOnTick(ctx)` - ティックイベント
- `KopitraOnTrade(ctx)` - 取引イベント

**ユーティリティ:**
- `KopitraJsonEscape(value)` - JSON エスケープ
- `KopitraFormatIso8601(datetime)` - ISO 8601形式に変換
- `KopitraExtractJsonField(json, field)` - JSONフィールド抽出
- `KopitraHttpRequest(ctx, method, path, body, response, timeout)` - HTTP通信

## EA設定ガイド

### 共通パラメータ（MT4/MT5）

```
InpApiBaseUrl          = "https://api.kopitra.example.com"    // APIベースURL
InpAccountId           = "account-uuid-xxxxx"                  // 口座ID
InpAuthMethod          = "account_session_key"                 // 認証方式
InpAuthKey             = "your-auth-key"                       // 認証キー
InpDeviceId            = ""                                    // デバイスID（自動生成）
InpHeartbeatSeconds    = 15                                    // ハートビート間隔（秒）
InpPollSeconds         = 5                                     // ポーリング間隔（秒）
InpSnapshotSeconds     = 60                                    // スナップショット間隔（秒）
InpSessionRetrySeconds = 10                                    // セッション再試行間隔（秒）
InpHttpTimeoutMs       = 5000                                  // HTTP タイムアウト（ms）
InpEnableOrderSubmission = false                               // 注文送信可否
```

### パラメータ説明

- **InpApiBaseUrl**: Kopitra2 APIサーバーのベースURL。HTTPSで暗号化通信。
- **InpAccountId**: 口座を一意に識別するUUID。サーバーから割り当てられる。
- **InpAuthMethod**: 認証方式。`account_session_key` が標準。
- **InpAuthKey**: 認証キー。安全に保管し、本番環境では環境変数から取得推奨。
- **InpDeviceId**: デバイスを識別するID。未設定の場合は自動生成（`mt4-terminal-xxxxx` 形式）。
- **InpHeartbeatSeconds**: ハートビート送信間隔。サーバー接続確認用。
- **InpPollSeconds**: シグナル取得の定期ポーリング間隔。推奨: 1-5秒。
- **InpSnapshotSeconds**: スナップショット送信間隔。口座状態の定期同期。
- **InpSessionRetrySeconds**: セッション確立失敗時の再試行待機時間。
- **InpHttpTimeoutMs**: HTTPリクエストのタイムアウト時間。最小1000ms推奨。
- **InpEnableOrderSubmission**: 注文送信機能の有効化（現在は false 推奨）。

## 動作フロー

### 初期化フロー
1. EA読み込み → `OnInit()` 呼び出し
2. `KopitraStartup()` で初期化
3. `KopitraEnsureSession()` で セッション作成
4. タイマーイベント設定

### 通常実行フロー
```
タイマーイベント（1秒ごと）
  ├─ セッション確保
  ├─ ハートビート送信（15秒ごと）
  ├─ シグナルポーリング（5秒ごと）
  │  └─ シグナル処理 → メッセージACK送信
  └─ スナップショット送信（60秒ごと）

ティック/取引イベント
  └─ 実行結果送信
```

### エラーハンドリング

**HTTP通信エラー:**
- 自動リトライ（sessionRetrySeconds待機後）
- 最大3回まで再試行
- ログ出力で通知

**セッション確立失敗:**
- 定期的に再試行
- connectionTimeoutまで待機後、自動リセット

**メッセージ送信失敗:**
- ログ出力
- 次回ポーリングで再送

**タイムアウト:**
- httpTimeoutMs で制御
- 遅延応答はスキップして次のサイクルに進行

## セッション状態管理

```
IDLE
  ↓ (セッション作成試行)
PENDING (認証待機中)
  ↓ (認証成功)
AUTHENTICATED (接続完了)
  ↓ (セッション削除)
IDLE
```

## ロギング

すべてのログは以下フォーマットで出力：
```
[Kopitra][LEVEL] メッセージ
```

レベル:
- `INFO` - 正常な操作情報
- `WARN` - 注意が必要な状態
- `ERROR` - エラー発生

例:
```
[Kopitra][INFO] Context initialized for account 12345678
[Kopitra][WARN] Failed to configure the 1-second timer; relying on ticks for background work.
[Kopitra][ERROR] Authentication key is required to establish a session.
```

## MT4 特別対応

MT4は`OnTradeTransaction`コールバックが存在しないため、以下の方法で実行情報を追跡：

**実装: `KopitraDetectExecutionChanges()`**
- ティック/タイマーごとに注文数の変化を検出
- 新規に開かれた注文の詳細を自動的に収集
- `/api/ea/executions` に送信

これにより、MT4でもMT5と同等の実行追跡が実現されます。

## MT5 標準対応

MT5は`OnTradeTransaction`コールバックで直接実行情報を捕捉：

**実装: `OnTradeTransaction()` 内で自動処理**
- transactionType, deal, order, symbol, volume
- price, profit, reason, requestType, retcode
- comment などを自動送信

## 重要な設計原則

### べき等性（Idempotency）
- **SignalID**: 重複したシグナルの重複実行を防止
- **MessageID**: メッセージの一度だけ処理を保証
- **Idempotency-Key**: HTTP リクエストの重複を検知

### メッセージ順序保証
- `outboxSequence` により、メッセージの順序を管理
- 遅延したメッセージも正しく処理

### トランザクション追跡
- `transactionId` により、分散トランザクションの追跡が可能
- 監査ログでの完全な履歴記録

### コスト最適化
- ポーリング間隔で通信量を制御
- SQLベースのメッセージキューで複雑な基盤不要
- 非リアルタイム分析で計算コスト削減

## トラブルシューティング

### セッション作成失敗
- **原因**: 認証キーが無効、APIサーバーが応答しない
- **対応**: `InpAuthKey` を確認、ネットワーク接続を確認

### ポーリング応答なし
- **原因**: タイムアウト、ネットワーク遅延
- **対応**: `InpHttpTimeoutMs` を増加、`InpPollSeconds` を調整

### 注文が実行されない
- **原因**: 口座権限不足、シグナル有効期限切れ
- **対応**: 口座設定を確認、`expiresAt` の時刻を確認

### メモリ不足
- **原因**: ポジション数が多い、ログバッファオーバーフロー
- **対応**: スナップショット間隔を短縮、不要な通貨ペアを削除

## ベストプラクティス

1. **本番環境への導入前**
   - デモ口座で十分なテストを実施
   - ネットワーク遅延を想定したタイムアウト値を設定
   - ログをモニタリングして異常を検知

2. **セキュリティ**
   - 認証キーを環境変数から読み込み
   - HTTPS通信を必須に
   - 定期的に認証キーをローテーション

3. **パフォーマンス**
   - ポーリング間隔は 5秒～30秒の範囲で調整
   - ハートビート間隔は 15秒～60秒の範囲で調整
   - スナップショット間隔は 60秒～300秒の範囲で調整

4. **監視と運用**
   - EAログを外部システムに転送（例: Application Insights）
   - セッション状態を定期的に確認
   - エラー発生時のアラート設定
