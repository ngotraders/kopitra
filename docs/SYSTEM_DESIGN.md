# kopitra システム仕様書

## 1. システム概要

**kopitra**は、FXのコピートレード配信・購読を行うプラットフォームです。経験豊富なトレーダー（配信者）の取引をコピーし、複数の顧客（購読者）が同時に同じ戦略で運用できるシステムを提供します。

### 目的
- トレーダーのスキルを複数のユーザーに展開可能にする
- 配信者は自身の口座で取引し、購読者の口座に自動で同期される
- 資金規模に応じた柔軟な注文サイズ調整が可能

---

## 2. 主要機能要件

### 2.1 ユーザー管理
- **ユーザー登録・認証**
  - メールアドレスベースのユーザー登録
  - JWT ベースの認証
  - パスワード管理・リセット機能

- **ロールベースアクセス制御（RBAC）**
  - **Admin**: システム全体の管理、全ユーザーの設定変更・操作管理
  - **User**: 個別ユーザーアカウント、配信・購読の両機能を利用可能

- **管理者機能**
  - **代行操作**: Admin は特定ユーザーになりすましてログイン不要で全操作を実施可能
  - **直接設定変更**: Admin が管理画面から直接ユーザーの設定を変更（なりすまし不要）
    - ユーザーの基本情報（名前、メールなど）
    - 配信・購読権の付与・剥奪
    - ユーザーの口座管理
    - 購読契約の設定変更
  - **監査ログ**: 代行操作・直接設定変更の全アクションを記録
  - **ユーザー管理**: ユーザーの有効化・無効化、削除

- **UI分離戦略**
  - **汎用API**: 権限制御をAPIレベルで統一実装
  - **管理サイト**: 管理者用UI（`admin.kopitra.com` など）
  - **ユーザーサイト**: 個別ユーザー用UI（`app.kopitra.com` など）
  - 両サイトは同一の汎用APIを利用するが、UI側で表示内容・機能を制限
  - ユーザーサイトでは、ログイン中のユーザーのデータのみアクセス可能

### 2.2 口座管理
- **対応プラットフォーム**
  - MetaTrader 4（MT4）
  - MetaTrader 5（MT5）

- **口座情報管理**
  - 口座番号、サーバー情報の登録
  - 口座の有効性確認（接続テスト）
  - 複数口座の管理対応

- **EA（Expert Advisor）連携**
  - EA経由での口座情報の自動送信（残高、ポジション、取引履歴など）
  - EAからの注文実行コマンド受信
  - リアルタイムデータ同期

### 2.3 コピートレード機能
- **機能の概念**
  - **配信機能**: 特定ユーザーがシグナルを配信する能力
  - **購読機能**: 特定ユーザーが配信シグナルを購読・自動実行する能力
  - 同一ユーザーが両機能を持つことが可能

- **シグナル配信機能**
  - 配信権を持つユーザーが注文シグナルを生成・配信
  - シグナル情報：通貨ペア、売買方向、ポジションサイズ、エントリー価格、SL/TP設定
  - ポジションサイズの指定方法：
    - **実数値**: 配信者が指定した実際のロット数をそのまま配信
    - **パーセンテージ**: 配信者の口座残高に対するパーセンテージで指定
  - べき等性確保：同一シグナルの重複送信を防ぐため、signalIdで重複判定

- **購読機能**
  - 購読権を持つユーザーが配信者を検索・選択
  - 購読契約の開始・終了管理
  - 複数の配信者から同時購読可能

- **資金追従戦略**
  - **固定ロット**: 配信シグナルのロット数をそのまま実行
  - **比例戦略**: 購読者の口座残高と配信者の口座残高の比率に応じてロット数を調整
  - **パーセンテージ**: 購読者の口座残高に対する一定比率でロット数を設定
  - ポジションサイズが配信者パーセンテージの場合、購読者の資金追従戦略を適用して最終ロット数を計算

### 2.4 注文管理
- **注文フロー**
  1. 配信権を持つユーザーが注文シグナルを生成（EA自動/WEB手動/Admin代行）
  2. シグナルには一意のシグナルID（べき等性キー）を付与
  3. サーバーが購読契約を確認
  4. 資金追従戦略に基づいてロット数を計算
  5. 購読者の各口座に対して注文を配信
  6. 各購読者の口座のEA又はAdmin代行で注文を実行

- **成行注文**
  - 注文方法は成行注文のみを対象
  - SL（損切）・TP（利確）値も合わせて送信可能
  - 配信シグナル送信時に SL/TP をコピー

- **注文修正**
  - 配信者が SL/TP を変更 → 修正シグナル生成 → 購読者の対応ポジションの SL/TP も同期更新
  - 修正シグナルもシグナルIDで重複判定

- **決済・クローズ**
  - 配信者がポジションを決済 → クローズシグナル生成 → 購読者の対応ポジションも同期決済
  - クローズシグナルも べき等性により二重実行を防止

- **約定管理**
  - 各注文・決済について約定状態を厳密に管理
  - 注文約定ステータス: Pending（未約定）→ Executed（約定）→ Rejected（拒否）
  - 決済約定ステータス: Pending → Closed（決済済）→ Failed（決済失敗）
  - 約定確認：購読者口座のEAから実際の約定情報をサーバーに送信・検証

- **約定遅延許容値**
  - 配信者が設定可能：シグナル送信から約定までの許容時間（秒単位）
  - 購読者が設定可能：購読契約ごとに許容遅延時間を指定
  - いずれかの閾値を超過 → 注文キャンセル・アラート通知

### 2.5 配信・実行チャネル

#### 2.5.1 配信チャネル
- **EA自動配信**: MT4/MT5上で動作するEAが注文を自動検知してシグナル送信
- **WEB手動配信**: ブラウザベースのWEB画面から手動入力でシグナル配信
- **Admin代行配信**: 管理者が特定ユーザーになりすまして注文シグナルを配信

#### 2.5.2 実行チャネル
- **EA自動実行**: 購読ユーザーの MT4/MT5 上のEAがシグナルを受信して注文実行
- **Admin代行実行**: 管理者がシステムから直接、購読ユーザーの口座に注文を実行

### 2.6 分析機能
- **コピートレード（シグナル）ごとのメトリクス**
  - 配信数、購読数、約定数、成功率
  - 利益・損失、勝率、平均利益/損失
  - ROI（Return on Investment）
  - リスク指標（ドローダウン、シャープレシオ）
  - 配信から約定までの平均時間

- **配信者ごとのメトリクス**
  - 総シグナル配信数、約定率
  - 総利益・損失（配信実績）
  - 勝率、平均取引時間、平均利益/損失
  - 購読者数、購読アカウント数
  - 人気度（総購読額に基づく）
  - パフォーマンスグラフ（月単位、週単位）

- **購読者アカウント単位のメトリクス**
  - コピー実行数、成功率
  - 総利益・損失（実現利益）
  - 勝率、平均利益/損失
  - コピー対象の配信者別の成績
  - ドローダウン、最大損失
  - 取引期間別の成績（月単位、週単位、日単位）

- **ユーザー単位のメトリクス**
  - 配信者としての成績（複数口座の合計）
  - 購読者としての成績（複数アカウントの合計）
  - 配信者ランキング（勝率、利益）
  - 統合パフォーマンス

---

## 3. 非機能要件

### 3.1 パフォーマンス
- API応答時間: 1秒以下
- リアルタイム性: シグナル配信から各口座への注文実行までを5秒以内
- 同時ユーザー数: 初期段階で100ユーザー対応、将来的にスケール可能

### 3.2 信頼性・可用性
- システム稼働率: 99.5% 以上（除：定期メンテナンス）
- 注文データの永続化と監査ログ
- エラー時のリトライ・リカバリー機構
- べき等性設計：シグナルID、トランザクションIDによる重複実行防止
- 約定確認：注文送信→確認→再確認フロー（約定遅延許容値内で）

### 3.3 セキュリティ
- **通信**: TLS 1.2 以上による暗号化
- **認証**: JWT トークンベース
- **認可**: ロールベースアクセス制御（RBAC）
  - APIレベルで全エンドポイントに権限チェック実装
  - 各操作ごとに権限者（本人 or Admin）を厳密に判定
  - リソース所有者の確認（例：口座の所有者のみが変更可能）
- **Admin操作の制御**
  - Admin による直接設定変更は監査ログに詳細に記録
  - Admin の全アクションに操作理由（memo）を記録可能
  - Admin の操作ログは一般ユーザーには非表示
- **API Key管理**: 複数キーによる安全な口座連携
- **監査ログ**: 全取引・アクセス・管理操作の記録
  - actionType により、ユーザー操作 vs 管理者操作を区別
  - Admin代行と直接設定変更を分類記録

### 3.4 拡張性
- マイクロサービスアーキテクチャ
- API設計の統一化
- ログの集約化（Application Insights）

---

## 4. システムアーキテクチャ

```
┌────────────────────────────────────────────────────────────────────────────┐
│                         Frontend Layer                                      │
├────────────────────────┬─────────────────────────────────────────────────┤
│  Admin Site            │           User Site                              │
│  (admin.kopitra.com)   │       (app.kopitra.com)                         │
│  - User Management     │  - Individual User Dashboard                     │
│  - Account Admin       │  - Signal Distribution                           │
│  - Subscription Admin  │  - Signal Subscription                           │
│  - Audit Logs          │  - Account Management                            │
└────────────────────────┴─────────────────────────────────────────────────┘
            │                           │
            └──────────────┬────────────┘
                           │ HTTPS
         ┌─────────────────▼──────────────────────────┐
         │      API Gateway / Auth                     │
         │     (Azure API Management)                  │
         │  - Unified API Endpoint                     │
         │  - OAuth2 / JWT Token Validation            │
         │  - Permission Control                       │
         │  - Request Routing                          │
         └──────────────┬───────────────────────────────┘
                        │
         ┌──────────────┴───────────────────────────┐
         │                                          │
    ┌────▼──────────────────┐            ┌──────────▼──────────┐
    │ Azure Functions       │            │ Azure Functions     │
    │ (Business Logic)      │            │ (EA Endpoint)       │
    │                       │            │                     │
    │ - User Management     │            │ - Signal Polling    │
    │ - Account Mgmt        │            │ - Message Dispatch  │
    │ - Subscription        │            │ - Order Execution   │
    │ - Reporting           │            │ - ACK Handling      │
    │ - Admin Operations    │            │ - Signal Delivery   │
    └────┬──────────────────┘            └──────────┬──────────┘
         │                                           │
         └─────────────────────┬─────────────────────┘
                               │
              ┌────────────────▼──────────────┐
              │  Azure SQL Database           │
              │                               │
              │  Event Store:                 │
              │  - DomainEvents (append-only) │
              │  - Snapshots                  │
              │  - Aggregates                 │
              │  - Subscriptions              │
              │  - Orders                     │
              │  - Signals                    │
              │  - Metrics (cached)           │
              │  - AuditLogs                  │
              └────────────┬───────────────────┘
                           │
         ┌─────────────────┴──────────────┐
         │                                │
    ┌────▼──────────┐         ┌──────────▼──────┐
    │ MT4/MT5       │         │ MT4/MT5         │
    │ Terminals     │         │ Terminals       │
    │ (EAs)         │         │ (EAs)           │
    │               │         │                 │
    │ - Poll /api   │         │ - Poll /api     │
    │   /ea/signals │         │   /ea/signals   │
    │ - Execute     │         │ - Execute       │
    │   Orders      │         │   Orders        │
    │ - Post ACKs   │         │ - Post ACKs     │
    │   to /api/ea  │         │   to /api/ea    │
    │   /executions │         │   /executions   │
    └───────────────┘         └─────────────────┘
```

### 4.1 アーキテクチャの特徴

#### EA対向エンドポイント
- **専用エンドポイント設計**: `/api/ea/*` プレフィックスで EA 用 API を統一
- **単一エンドポイント**: すべての EA 処理を Azure Functions の 1 つのエンドポイントで処理
- **ステートレス設計**: スケーリング容易

#### ポーリングベースの非同期メッセージング
```
EA Polling Flow:
1. EA が定期的に `GET /api/ea/signals?accountId=XXX&timestamp=` にアクセス
2. Azure Functions が、その account に対する配信済みシグナルを返却
3. EA が注文を実行
4. 注文完了後、EA が `POST /api/ea/executions` に約定情報を送信
5. Azure Functions が ACK を返却
6. EA がメッセージを削除（ポーリング対象外に）
```

- **メッセージ保持**: SQL Database のテーブルに保持（複雑な Queue は不要）
- **ポーリング間隔**: EA が設定可能（推奨: 1-5秒）
- **Timeout 対応**: 一定時間後、自動でメッセージをリセット

#### イベントソーシング設計
- **イベントストア**: DomainEvents テーブル（append-only）
  - Signal created, Signal modified, Signal closed
  - Order executed, Order failed
  - Execution confirmed
  - Subscription started, Subscription ended
- **スナップショット**: 定期的に集計して Snapshots テーブルに保存
- **メトリクス計算**: バッチ処理で毎日計算（夜間）

#### コスト最適化
- **単一 SQL Database**: イベントデータを 1 つの DB で集約
- **イベントベース処理**: Queue/Event Hub 不要（コスト削減）
- **非リアルタイム分析**: 夜間バッチで Metrics テーブルを更新
- **Azure Functions の効率**: インスタンス共有、短時間実行

---

## 5. データモデル

### 5.0 イベントソーシング基盤

#### DomainEvent（ドメインイベント - Append-only）
```
- eventId (PK)
- aggregateId (対象の Aggregate ID)
- aggregateType (User / Signal / Order / Subscription など)
- eventType (SignalCreated / SignalModified / OrderExecuted など)
- eventVersion (バージョン管理)
- eventData (JSON - イベントペイロード)
- metadata (JSON - ユーザーID、タイムスタンプなど)
- timestamp (イベント発生時刻)
- createdAt (レコード作成時刻)
```

#### SignalMessage（配信メッセージキュー - SQL ベース）
```
- messageId (PK - UUID)
- signalId (FK)
- subscriptionId (FK)
- subscriberAccountId (FK)
- messageStatus (Pending / Dispatched / Acknowledged / Failed)
- messageContent (JSON - シグナル詳細)
- sentAt (配信時刻)
- acknowledgedAt (ACK 受信時刻)
- expiresAt (有効期限)
- retryCount (リトライ回数)
- createdAt
```

#### ExecutionMessage（実行完了メッセージキュー - SQL ベース）
```
- messageId (PK - UUID)
- signalId (FK)
- subscriberAccountId (FK)
- executionData (JSON - 約定情報)
- executionStatus (Pending / Processed / Acknowledged)
- processedAt
- acknowledgedAt
- expiresAt (一定期間後は削除対象)
- createdAt
```

#### EventSnapshot（スナップショット - 計算結果キャッシュ）
```
- snapshotId (PK)
- aggregateId (FK)
- aggregateType
- aggregateState (JSON - 現在の状態)
- eventVersion (どのイベントまで処理したか)
- calculatedAt
- expiredAt (期限切れ?)
```

### 5.1 主要エンティティ

#### User（ユーザー）
```
- userId (PK)
- email
- passwordHash
- fullName
- canProvide (配信権フラグ)
- canSubscribe (購読権フラグ)
- createdAt
- updatedAt
- isActive
```

#### Account（口座）
```
- accountId (PK)
- userId (FK)
- brokerType (MT4 / MT5)
- accountNumber
- serverName
- apiKey / apiSecret
- balance (キャッシュ)
- lastSyncedAt
- isConnected
- createdAt
```

#### ProviderProfile（配信プロフィール）
```
- providerProfileId (PK)
- userId (FK - 配信権を持つユーザー)
- description
- performanceMetrics (勝率、平均利益など)
- isVerified
- createdAt
```

#### Subscription（購読契約）
```
- subscriptionId (PK)
- providerUserId (FK)
- subscriberUserId (FK)
- subscriberAccountId (FK)
- fundingStrategy (FixedLot / Proportional / Percentage)
- strategyParams (ロット数またはパーセンテージ)
- executionDelayTolerance (約定遅延許容値: 秒)
- isActive
- startedAt
- endedAt
```

#### Signal（シグナル）
```
- signalId (PK - べき等性キー)
- providerUserId (FK)
- providerAccountId (FK)
- signalType (NewOrder / Modify / ClosePosition)
- pair (通貨ペア: EURUSD など)
- direction (Buy / Sell)
- positionSizeType (ActualLot / Percentage)
- positionSize (ロット数またはパーセンテージ値)
- entryPrice
- stopLoss
- takeProfit
- providerExecutionDelayTolerance (配信者設定の許容遅延: 秒)
- createdAt
- expiresAt
- transactionId (分散トランザクション追跡用)
```

#### ExecutionLog（注文実行ログ）
```
- executionLogId (PK)
- signalId (FK)
- subscriptionId (FK)
- subscriberUserId (FK)
- subscriberAccountId (FK)
- orderNumber (MT側のオーダー番号)
- executedLot (実際に約定したロット数)
- executedPrice
- orderStatus (Pending / Executed / Rejected)
- closeStatus (Pending / Closed / Failed)
- executedAt
- closedAt
- executionConfirmedAt (EA からの確認到着時刻)
- errorMessage
- transactionId (分散トランザクション追跡用)
```

#### AuditLog（監査ログ）
```
- auditLogId (PK)
- userId (FK - アクション対象のユーザー)
- adminUserId (FK - Admin が代行/設定変更した場合)
- action (Login / OrderCreated / SubscriptionStarted / AdminImpersonate / AdminDirectChange など)
- actionType (UserAction / AdminImpersonation / AdminDirectChange)
- resource (リソースタイプ: User / Account / Subscription / Signal など)
- resourceId (対象リソースID)
- details (変更内容の詳細)
- timestamp
```

#### SignalMetrics（シグナル単位のメトリクス）
```
- signalMetricsId (PK)
- signalId (FK)
- providerUserId (FK)
- pair (通貨ペア)
- direction (Buy / Sell)
- totalDistributed (配信された購読数)
- totalExecuted (実際に約定した数)
- executionRate (約定率%)
- totalProfit (総利益)
- totalLoss (総損失)
- winCount (勝ちトレード数)
- lossCount (負けトレード数)
- winRate (勝率%)
- avgProfit (平均利益)
- avgLoss (平均損失)
- roi (ROI%)
- maxDrawdown (最大ドローダウン%)
- avgExecutionTime (平均約定時間: 秒)
- calculatedAt (計算日時)
```

#### ProviderMetrics（配信者ごとのメトリクス）
```
- providerMetricsId (PK)
- providerUserId (FK)
- totalSignalsDistributed (総シグナル配信数)
- totalExecuted (総約定数)
- executionRate (約定率%)
- totalProfit (総利益)
- totalLoss (総損失)
- winCount (勝ちトレード数)
- lossCount (負けトレード数)
- winRate (勝率%)
- avgProfit (平均利益)
- avgLoss (平均損失)
- roi (ROI%)
- maxDrawdown (最大ドローダウン%)
- avgTradeTime (平均取引時間: 分)
- totalSubscribers (購読ユーザー数)
- totalSubscribedAccounts (購読アカウント総数)
- totalSubscribedAmount (購読総額)
- period (集計期間: Daily / Weekly / Monthly)
- startDate
- endDate
- calculatedAt
```

#### SubscriberAccountMetrics（購読アカウント単位のメトリクス）
```
- accountMetricsId (PK)
- subscriberUserId (FK)
- subscriberAccountId (FK)
- providerUserId (FK - コピー対象の配信者)
- totalCopyOrders (コピー実行数)
- totalExecuted (実際に約定した数)
- executionRate (約定率%)
- totalProfit (総利益)
- totalLoss (総損失)
- winCount (勝ちトレード数)
- lossCount (負けトレード数)
- winRate (勝率%)
- avgProfit (平均利益)
- avgLoss (平均損失)
- roi (ROI%)
- maxDrawdown (最大ドローダウン%)
- period (集計期間: Daily / Weekly / Monthly)
- startDate
- endDate
- calculatedAt
```

#### UserMetrics（ユーザー単位の統合メトリクス）
```
- userMetricsId (PK)
- userId (FK)
- providerMetrics (配信者としての集計 - ProviderMetrics参照)
- subscriberMetrics (購読者としての集計 - SubscriberAccountMetrics合計)
- totalAccounts (保有アカウント数)
- totalProfit (総利益)
- totalLoss (総損失)
- roi (ROI%)
- period (集計期間: Daily / Weekly / Monthly)
- startDate
- endDate
- calculatedAt
```

---

## 6. API設計（概要）


### 6.1 認証・ユーザー管理
- `POST /api/auth/register` - ユーザー登録
- `POST /api/auth/login` - ログイン（ユーザー or Admin）
- `POST /api/auth/refresh` - トークン更新
- `POST /api/auth/logout` - ログアウト
- `GET /api/users/me` - 現在のユーザー情報
- `PUT /api/users/{userId}` - ユーザー情報更新（権限チェック: 自分自身またはAdmin）
- `GET /api/users` - ユーザー一覧（権限チェック: Admin のみ）
- `GET /api/users/{userId}` - ユーザー詳細（権限チェック: 自分自身またはAdmin）
- `POST /api/users` - ユーザー作成（権限チェック: Admin のみ）
- `PUT /api/users/{userId}/permissions` - ユーザー権限変更（権限チェック: Admin のみ）
- `PUT /api/users/{userId}/status` - ユーザー有効化/無効化（権限チェック: Admin のみ）
- `DELETE /api/users/{userId}` - ユーザー削除（権限チェック: Admin のみ）

### 6.2 口座管理
- `POST /api/accounts` - 口座登録（権限チェック: 対象ユーザー or Admin）
- `GET /api/accounts` - 口座一覧取得（権限チェック: 自分のアカウント or Admin）
- `GET /api/accounts/{accountId}` - 口座詳細取得（権限チェック: 所有者 or Admin）
- `PUT /api/accounts/{accountId}` - 口座情報更新（権限チェック: 所有者 or Admin）
- `DELETE /api/accounts/{accountId}` - 口座削除（権限チェック: 所有者 or Admin）
- `POST /api/accounts/{accountId}/verify` - 口座接続テスト（権限チェック: 所有者 or Admin）

### 6.3 配信機能
- `GET /api/providers` - 配信権を持つユーザー一覧検索（権限チェック: 全員）
- `GET /api/providers/{userId}` - 配信者詳細情報（権限チェック: 全員）
- `PUT /api/providers/profile` - 配信プロフィール編集（権限チェック: 対象ユーザー or Admin）
- `GET /api/providers/performance` - パフォーマンス統計（権限チェック: 全員）
- `POST /api/signals` - シグナル作成（権限チェック: 配信権を持つユーザー or Admin代行）
- `GET /api/signals` - シグナル履歴（権限チェック: 配信者 or 購読者 or Admin）
- `PUT /api/signals/{signalId}` - シグナル修正（権限チェック: 配信者 or Admin）
- `POST /api/signals/{signalId}/close` - シグナルクローズ（権限チェック: 配信者 or Admin）

### 6.4 購読機能
- `POST /api/subscriptions` - 購読開始（権限チェック: 購読ユーザー or Admin）
- `GET /api/subscriptions` - 購読契約一覧（権限チェック: 配信者・購読者 or Admin）
- `GET /api/subscriptions/{subscriptionId}` - 購読詳細（権限チェック: 関連ユーザー or Admin）
- `PUT /api/subscriptions/{subscriptionId}` - 購読設定変更（権限チェック: 購読者 or Admin）
- `DELETE /api/subscriptions/{subscriptionId}` - 購読解除（権限チェック: 購読者 or Admin）

### 6.5 注文・約定管理
- `POST /api/orders` - 注文実行（権限チェック: Admin代行 or EA）
- `GET /api/orders` - 注文履歴（権限チェック: 関連ユーザー or Admin）
- `PUT /api/orders/{orderId}` - 注文修正（権限チェック: Admin or システム）
- `POST /api/orders/{orderId}/close` - 注文決済（権限チェック: Admin or EA）
- `POST /api/executions/confirm` - 約定確認受信（権限チェック: EA or Admin）

### 6.6 Admin 管理機能
- `GET /api/admin/users` - ユーザー一覧（権限チェック: Admin のみ）
- `GET /api/admin/users/{userId}` - ユーザー詳細（権限チェック: Admin のみ）
- `PUT /api/admin/users/{userId}` - ユーザー設定直接変更（権限チェック: Admin のみ）
- `POST /api/admin/users/{userId}/permissions` - ユーザー権限設定（権限チェック: Admin のみ）
- `PUT /api/admin/accounts/{accountId}` - 口座設定管理（権限チェック: Admin のみ）
- `PUT /api/admin/subscriptions/{subscriptionId}` - 購読契約管理（権限チェック: Admin のみ）
- `GET /api/admin/audit-logs` - 監査ログ一覧（権限チェック: Admin のみ）
- `GET /api/admin/audit-logs/{userId}` - ユーザー別監査ログ（権限チェック: Admin のみ）

### 6.7 分析・メトリクス機能
- `GET /api/metrics/signals/{signalId}` - シグナル単位のメトリクス取得（権限チェック: 配信者 or Admin）
- `GET /api/metrics/providers/{userId}` - 配信者のメトリクス取得（権限チェック: 全員）
  - クエリパラメータ: `period=daily|weekly|monthly`, `startDate`, `endDate`
- `GET /api/metrics/provider/accounts/{accountId}` - 配信者の口座別メトリクス（権限チェック: 所有者 or Admin）
- `GET /api/metrics/subscribers/{userId}` - 購読者のメトリクス取得（権限チェック: 本人 or Admin）
  - クエリパラメータ: `period=daily|weekly|monthly`, `startDate`, `endDate`
- `GET /api/metrics/subscribers/accounts/{accountId}` - 購読アカウント単位のメトリクス（権限チェック: 所有者 or Admin）
  - クエリパラメータ: `period=daily|weekly|monthly`, `startDate`, `endDate`, `providerId`（フィルター）
- `GET /api/metrics/users/{userId}` - ユーザー統合メトリクス（権限チェック: 本人 or Admin）
  - クエリパラメータ: `period=daily|weekly|monthly`, `startDate`, `endDate`
- `GET /api/metrics/admin/provider-rankings` - 配信者ランキング（権限チェック: 全員）
  - クエリパラメータ: `sortBy=winRate|profit|roi`, `limit=10`
- `GET /api/metrics/admin/performance-report` - パフォーマンスレポート（権限チェック: Admin のみ）
  - クエリパラメータ: `period=daily|weekly|monthly`, `startDate`, `endDate`

### 6.8 Webhook
- `POST /api/webhooks/mt-signal` - MT4/MT5 EA からのシグナル受信
- `POST /api/webhooks/account-sync` - 口座情報更新受信
- `POST /api/webhooks/execution-confirm` - 約定確認受信（EA確認用）

---

## 7. 技術スタック

### バックエンド
- **Runtime**: Azure Functions（.NET / Node.js）
- **API Gateway**: Azure API Management
- **Database**: Azure SQL Database / Cosmos DB
- **Messaging**: Azure Service Bus / Event Hub
- **Auth**: Azure AD / JWT
- **Logging**: Application Insights
- **IaC**: Bicep

### フロントエンド
- **Framework**: React + TypeScript
- **UI Library**: Material-UI / Tailwind CSS
- **State Management**: Redux / Zustand
- **HTTP Client**: Axios
- **Deployment**: Azure Static Web Apps

### 外部連携
- **MetaTrader API**: MT4/MT5の公式API
- **MT4/MT5 Custom Indicator**: DLL/MQL4/MQL5

---

## 8. ユースケース

### UC1: 配信権ユーザーの取引をEA経由で自動配信
1. 配信権を持つユーザーがMT4/MT5で新規注文を発注
2. EA が注文検知 → API経由でサーバーにシグナル送信（ポジションサイズ: 実数値またはパーセンテージ）
3. サーバーが購読契約を確認
4. 各購読者の資金追従戦略に基づいてロット計算
5. 購読者口座へシグナル配信
6. 各購読者のEAがシグナルを受け取り注文を実行
7. 購読者のEAが約定情報をサーバーに送信
8. サーバーが約定を確認（約定遅延許容値内か確認）

### UC2: WEB画面からの手動シグナル配信
1. 配信権を持つユーザーがWEB画面にログイン
2. 「シグナル配信」フォームで注文内容を入力（ポジションサイズ指定方法、SL/TP値）
3. 確認ボタンをクリック
4. サーバーが購読契約を確認 → ロット計算 → 注文配信
5. 購読者のEAが注文を実行・約定確認を返す

### UC3: Admin代行での注文配信
1. Admin が特定ユーザーになりすまし、WEB画面またはAPI経由でシグナルを配信
2. サーバーが購読契約を確認 → ロット計算 → 注文配信
3. 代行操作は監査ログに「Admin による代行: ユーザーX」として記録

### UC4: Admin代行での注文実行
1. Admin が購読ユーザーになりすまし、システムから直接口座に注文を実行
2. 約定確認フローを経て、注文状態を追跡
3. 全操作を監査ログに記録

### UC5: 約定遅延の管理
1. 配信ユーザーがシグナル送信時に「許容遅延: 5秒」と設定
2. 購読契約に「許容遅延: 3秒」と設定されている場合、より厳しい 3秒を適用
3. シグナル送信から 3秒以内に約定確認がない → 注文キャンセル・アラート

### UC6: 注文修正（SL/TP変更）
1. 配信ユーザーが既存ポジションのSL/TPを変更 → 修正シグナル送信
2. 修正シグナルは同じシグナルIDで識別（べき等性）
3. 購読者のポジションのSL/TPも同期更新

### UC7: ポジション決済
1. 配信ユーザーがポジションを決済 → クローズシグナル送信
2. 購読者のEAが対応するポジションを自動決済
3. 決済確認を返す

### UC8: 購読ユーザーが配信者を購読開始
1. 購読権を持つユーザーがWEB画面で配信者一覧を検索
2. 配信者の詳細ページでパフォーマンスを確認
3. 「購読開始」をクリック
4. 購読口座と資金追従戦略（固定ロット / 比例 / パーセンテージ）を選択
5. 約定遅延許容値を設定
6. 購読開始直後は過去シグナルの同期は行わない（新規シグナルのみ）

### UC9: 購読ユーザーが購読設定を変更
1. 購読ユーザーが「購読管理」ページから契約を選択
2. 「設定変更」をクリック
3. 資金追従戦略や約定遅延許容値を更新
4. 既存ポジションは変わらず、以降のシグナルから新戦略を適用

### UC10: 重複シグナル送信への対応
1. 同じ配信ユーザーが誤って同じシグナルを2度送信
2. サーバーがシグナルIDで重複判定 → 2番目の送信は無視
3. データベースの整合性を保証（べき等性）

### UC11: Admin が管理画面でユーザー設定を直接変更
1. Admin が管理サイト（admin.kopitra.com）にログイン
2. 「ユーザー管理」から特定ユーザーを検索
3. ユーザーの基本情報（名前、メール）を直接編集
4. 「保存」をクリック → API `/api/admin/users/{userId}` に PUT リクエスト
5. サーバーが Admin 権限を確認 → ユーザー情報を更新
6. 監査ログに「Admin による直接設定変更: 名前、メール」と記録
7. このユーザーは変更を認識できるが、自分自身は操作していない

### UC12: Admin がユーザーの権限を管理
1. Admin が管理画面でユーザーを選択
2. 「権限設定」セクションで「配信権」「購読権」のチェックボックスを切り替え
3. 「保存」をクリック → API `/api/admin/users/{userId}/permissions` に POST リクエスト
4. サーバーが Admin 権限を確認 → ユーザーの権限を更新
5. 監査ログに「Admin がユーザーXの配信権を付与」と記録

### UC13: Admin が口座設定を直接管理
1. Admin が管理画面で「口座管理」を選択
2. 特定ユーザーの口座を検索
3. 口座情報（API Key、接続テスト実施）を直接管理
4. 必要に応じて口座を無効化/削除
5. API: `/api/admin/accounts/{accountId}` に PUT リクエスト
6. 監査ログに「Admin による口座設定変更」と記録

### UC14: Admin が購読契約を直接管理
1. Admin が管理画面で「購読管理」を選択
2. 特定の購読契約を検索
3. 資金追従戦略や約定遅延許容値を直接変更
4. 必要に応じて購読契約を無効化/削除
5. API: `/api/admin/subscriptions/{subscriptionId}` に PUT リクエスト
6. 監査ログに「Admin による購読契約設定変更」と記録

### UC15: Admin が監査ログを確認
1. Admin が管理画面の「監査ログ」セクションをアクセス
2. 全ユーザーのアクションログを確認（filter: Admin操作、対象ユーザー、日時範囲）
3. Admin による直接設定変更と代行操作を区別して表示
4. 特定ユーザーの監査ログを詳細表示 → `/api/admin/audit-logs/{userId}`
5. Admin の操作はここで完全に追跡可能

### UC16: ユーザーサイトでのアクセス制御
1. 個別ユーザーがユーザーサイト（app.kopitra.com）にログイン
2. 自分のデータのみアクセス可能（他ユーザーのアカウント・シグナル・購読契約は非表示）
3. APIリクエスト: `GET /api/accounts` → サーバーが権限チェック
   - リクエスト元ユーザーID = ログイン中のユーザーID の場合のみデータ返却
4. 他ユーザーのデータに直接アクセスしようとしても 403 Forbidden
5. API は汎用的で、UIレベルで制限を実装

### UC17: API の権限制御
1. ユーザーがログインすると JWT トークンに userId とロール(Admin/User)を含む
2. API Gateway が各エンドポイントで権限チェック
3. 例：`PUT /api/users/{userId}` に対して
   - JWT の userId == 対象の {userId} → 許可
   - JWT のロール == Admin → 許可
   - その他 → 403 Forbidden
4. Admin システムから直接呼び出す場合も同じ API を使用
5. Admin トークンで呼び出された API は監査ログに別途記録

### UC18: 配信者がシグナルごとのメトリクスを確認
1. 配信者が個別ユーザーサイト（app.kopitra.com）にログイン
2. 「配信履歴」セクションで特定シグナルをクリック
3. 「詳細・メトリクス」タブを開く
4. API: `GET /api/metrics/signals/{signalId}` でメトリクス取得
5. 配信数、約定数、利益/損失、勝率、ROI などを表示
6. グラフで時系列推移を視覚化

### UC19: 配信者がパフォーマンス統計を確認
1. 配信者が個別ユーザーサイトの「パフォーマンス」ページをアクセス
2. 期間セレクタ（日単位、週単位、月単位）で期間を選択
3. API: `GET /api/metrics/providers/{userId}?period=monthly&startDate=...&endDate=...` でメトリクス取得
4. 総利益、勝率、ドローダウン、購読者数などを表示
5. パフォーマンスグラフを表示（月別、週別の推移）

### UC20: 購読者がアカウント単位の成績を確認
1. 購読者が個別ユーザーサイトにログイン
2. 「購読アカウント」セクションで特定アカウントを選択
3. 「成績」タブをクリック
4. API: `GET /api/metrics/subscribers/accounts/{accountId}?period=weekly&startDate=...&endDate=...&providerId=...` でメトリクス取得
5. コピー実行数、約定数、利益/損失、勝率、ROI を表示
6. 配信者ごとのサブフィルター機能で特定配信者の成績を確認

### UC21: 購読者がユーザー単位の統合成績を確認
1. 購読者が「ダッシュボード」にアクセス
2. 「統合パフォーマンス」セクションを表示
3. API: `GET /api/metrics/users/{userId}?period=monthly&startDate=...&endDate=...` でメトリクス取得
4. 複数アカウントの合計成績（総利益、勝率、ROI）を表示
5. 配信者としての成績と購読者としての成績を分けて表示

### UC22: 一般ユーザーが配信者ランキングを確認
1. 一般ユーザーが「配信者検索」ページをアクセス
2. ランキング順（勝率、利益、ROI）でソート可能
3. API: `GET /api/metrics/admin/provider-rankings?sortBy=roi&limit=10` でランキング取得
4. 配信者の勝率、平均利益、購読者数などを表示
5. ランキング上位の配信者の情報を閲覧して購読を検討

### UC23: Admin が全体パフォーマンスレポートを確認
1. Admin が管理サイトの「レポーティング」セクションをアクセス
2. 期間（日単位、週単位、月単位）と集計範囲を選択
3. API: `GET /api/metrics/admin/performance-report?period=monthly&startDate=...&endDate=...` でレポート取得
4. 全体のシグナル配信数、約定数、総利益、平均勝率などを表示
5. トップ配信者、トップ購読アカウントなどのランキング表示
6. レポートをエクスポート（CSV/PDF）可能

---

## 10. 開発フェーズ

### Phase 1: MVP（最小機能セット）
- ユーザー登録・認証（User, Admin ロール）
- 口座登録・管理（単一口座）
- WEB手動配信のみ
- 固定ロット戦略のみ
- 成行注文のみ対応
- シンプルなシグナル・注文管理
- べき等性（シグナルIDベース）
- 基本的な監査ログ
- API レベルでの汎用的権限制御
- Admin による直接設定変更機能（ユーザー、権限）
- 管理サイトの基本機能

### Phase 2: 拡張機能
- EA自動配信対応（MT4/MT5 EA統合）
- ポジションサイズのパーセンテージ指定対応
- 複数資金追従戦略対応（比例戦略、パーセンテージ）
- SL/TP コピー機能
- 注文修正機能
- 約定遅延許容値設定
- 約定確認フロー（EA確認）
- **分析機能の本格実装**
  - シグナル単位のメトリクス計算・保存
  - 配信者ごとのメトリクス（日単位、週単位、月単位）
  - 購読アカウント単位のメトリクス
  - ユーザー統合メトリクス
  - 配信者ランキング機能
  - パフォーマンスレポート機能
- リアルタイム通知（WebSocket）
- Admin 代行機能の完全実装
- 管理サイトの全機能実装（口座管理、購読管理、監査ログ）

### Phase 3: 高度な機能
- リスク管理機能（ドローダウン制限など）
- 複数口座同時管理
- 高度な統計分析（シャープレシオ、ソルティノレシオなど）
- レポーティング・エクスポート機能（CSV/PDF）
- モバイルアプリ
- MT5対応の強化
- リアルタイム分析ダッシュボード

---

## 10. 制約事項・注意点

1. **規制対応**: 各国の金融規制に対応する必要がある可能性
2. **MT4/MT5互換性**: バージョン依存性の管理が必要
3. **スリッページ対応**: 約定時の価格差分を考慮した設計
4. **資金管理**: 顧客資産の管理と分離
5. **トラブルシューティング**: EA接続問題時の対応フロー
6. **べき等性**: シグナルIDおよびトランザクションIDによる重複実行防止の厳密な実装
7. **約定確認**: EA からの約定報告メカニズムの信頼性確保
8. **Admin代行・直接設定**: 代行操作による不正防止（監査ログ、権限管理）
9. **権限制御**: APIレベルでの統一的な権限チェック実装
10. **UI分離**: 管理サイトと個別ユーザーサイトの機能制限を正しく実装
11. **メトリクス計算**: 正確で信頼性の高いメトリクス計算（手数料、スプレッド考慮）
12. **データ集計**: 大量の取引データの効率的な集計・キャッシング
13. **リアルタイム vs バッチ処理**: メトリクス更新のタイミング設計
14. **グラフ可視化**: 大量データの効率的なグラフ描画

---

## 11. 次のステップ

1. **詳細API仕様書作成** → `API_SPECIFICATION.md`
2. **データベーススキーマ設計** → `DATABASE_SCHEMA.md`
3. **フロントエンド画面設計** → `FRONTEND_DESIGN.md`
4. **分析機能の詳細設計** → `ANALYTICS_SPECIFICATION.md`
5. **EA開発ガイド** → `EA_DEVELOPMENT_GUIDE.md`
6. **インフラ設定** → Bicep テンプレート作成
