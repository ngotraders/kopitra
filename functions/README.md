# Kopitra Functions - バックエンド実装

Kopitra FXコピートレードプラットフォームのバックエンド実装。Azure Functions（.NET 8.0）とイベントソーシングアーキテクチャを使用しています。

---

## 📋 プロジェクト概要

このプロジェクトは、Kopitraのすべてのバックエンド機能を提供するAzure Functionsアプリケーションです：
- シグナル配信と購読管理のREST APIエンドポイント
- MT4/MT5ターミナル向けEA（Expert Advisor）統合
- 不変監査ログのためのイベントソーシングインフラストラクチャ
- メトリクス計算と分析
- Admin管理機能

### 技術スタック
- **ランタイム**: Azure Functions v4（Worker SDK）
- **フレームワーク**: .NET 8.0
- **ORM**: Entity Framework Core 8.0
- **イベントソーシング**: EventFlow 1.2.0
- **データベース**: Azure SQL Database、SQLite（テスト用）
- **監視**: Application Insights
- **API ドキュメント**: OpenAPI/Swagger

---

## 🏗️ プロジェクト構造

```
functions/
├── src/
│   └── Kopitra.Api/                     # メインのAzure Functionsプロジェクト
│       ├── Program.cs                   # アプリケーション構成＆DI設定
│       ├── host.json                    # Azure Functionsランタイム設定
│       ├── local.settings.json          # ローカル開発設定
│       ├── Kopitra.Api.csproj          # プロジェクトファイル（.NET 8.0）
│       │
│       ├── Functions/                   # Azureファンクション実装
│       │   ├── Auth/                    # 認証エンドポイント
│       │   ├── Accounts/                # 口座管理エンドポイント
│       │   ├── Signals/                 # シグナル配信エンドポイント
│       │   ├── Subscriptions/           # 購読管理エンドポイント
│       │   ├── Orders/                  # 注文実行エンドポイント
│       │   ├── EA/                      # EAポーリングエンドポイント（GET /api/ea/signals）
│       │   ├── Metrics/                 # 分析エンドポイント
│       │   ├── Admin/                   # Admin管理エンドポイント
│       │   └── Health/                  # ヘルスチェックエンドポイント
│       │
│       ├── Domain/                      # ドメインモデル＆集約
│       │   ├── Entities/                # コアドメインエンティティ
│       │   ├── ValueObjects/            # 値オブジェクト
│       │   ├── Events/                  # ドメインイベント
│       │   ├── Exceptions/              # ドメイン例外
│       │   └── Services/                # ドメインサービス
│       │
│       ├── Infrastructure/              # データアクセス＆外部連携
│       │   ├── Data/                    # DBコンテキスト＆リポジトリ
│       │   ├── EventStore/              # イベントソーシング実装
│       │   ├── MessageQueue/            # SQLベースのメッセージキュー
│       │   └── Services/                # インフラストラクチャサービス
│       │
│       ├── Models/                      # DTOとリクエスト/レスポンスモデル
│       │   ├── Requests/                # APIリクエストDTO
│       │   ├── Responses/               # APIレスポンスDTO
│       │   └── Common/                  # 共有モデル
│       │
│       └── Common/                      # 共有ユーティリティ
│           ├── Constants.cs             # アプリケーション定数
│           ├── Extensions.cs            # 拡張メソッド
│           ├── Middleware/              # カスタムミドルウェア
│           ├── Auth/                    # 認証ヘルパー
│           └── Exceptions/              # 共通例外
│
├── tests/
│   └── Kopitra.Api.Tests/              # ユニット・統合テスト
│       ├── Unit/                        # ユニットテスト
│       ├── Integration/                 # 統合テスト
│       └── Fixtures/                    # テストデータとフィクスチャ
│
├── kopitra-functions.sln               # Visual Studioソリューションファイル
└── README.md                           # このファイル
```

---

## 🚀 はじめに

### 前提条件

#### 全OS共通
- .NET SDK 8.0以上
- Azure Functions Core Tools（v4）
- Azure CLI（デプロイメント用）
- Visual Studio 2022またはVS Code（C#拡張機能付き）

#### Mac/Linux
- Docker と Docker Compose
- SQLコマンドラインツール（`sqlcmd`）- テスト確認用

#### Windows
- SQL Server 2019以上 **または** LocalDB（Visual Studioインストール時に含まれる）
- SQL Server Management Studio（SSMS）- 推奨

### インストール

```bash
# functionsディレクトリに移動
cd functions

# NuGetパッケージを復元
dotnet restore

# プロジェクトをビルド
dotnet build
```

---

## 🗄️ データベースセットアップ

### Mac/Linux: Docker を使用した SQL Server のセットアップ

#### 1. Docker ComposeでSQL Serverを起動

```bash
# プロジェクトのルートディレクトリに移動
cd /path/to/kopitra

# Docker ComposeでSQL Serverを起動
docker-compose up -d mssql

# コンテナが起動したか確認
docker-compose ps
```

#### 2. 接続確認

```bash
# SQL Serverに接続（パスワード: KopitraPassword123!）
sqlcmd -S localhost,1433 -U sa -P 'KopitraPassword123!' -Q "SELECT @@VERSION"

# または docker exec で確認
docker exec -it kopitra-mssql sqlcmd -U sa -P 'KopitraPassword123!' -Q "SELECT @@VERSION"
```

### Windows: LocalDB を使用したセットアップ

#### 1. LocalDB インスタンスの確認

```bash
# LocalDBのインスタンス一覧を表示
sqlcmd -L

# または
sqllocaldb info
```

#### 2. LocalDB インスタンスの作成（初回のみ）

```bash
# デフォルトの LocalDB インスタンスを作成
sqllocaldb create "kopitra"

# インスタンスを開始
sqllocaldb start "kopitra"

# 確認
sqllocaldb info "kopitra"
```

#### 3. local.settings.json を更新

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "SqlServerConnection": "Server=(localdb)\\kopitra;Database=kopitra_dev;Integrated Security=true;",
    "JwtSecret": "your-jwt-secret-for-local-testing",
    "APPINSIGHTS_INSTRUMENTATIONKEY": ""
  }
}
```

---

## 🔄 データベースマイグレーション

### 概要
このプロジェクトはEntity Framework Coreを使用してマイグレーションを管理します。

**マイグレーションファイルの場所:**
```
functions/src/Kopitra.Api/Infrastructure/Data/Migrations/
```

### マイグレーション実行方法

#### 前提条件
```bash
# Entity Framework Core ツールをインストール（初回のみ）
dotnet tool install --global dotnet-ef
```

#### 1. 既存マイグレーションの適用（通常の使用）

```bash
# functionsディレクトリで実行
cd functions/src/Kopitra.Api

# Mac/Linux （Docker SQL Server）
dotnet ef database update \
  --connection "Server=localhost,1433;Database=kopitra_dev;User Id=sa;Password=KopitraPassword123;"

# Windows （LocalDB）
dotnet ef database update \
  --connection "Server=(localdb)\\kopitra;Database=kopitra_dev;Integrated Security=true;"
```

#### 2. スキーマの初期化（初回のみ）

```bash
# データベースを作成
# Mac/Linux
dotnet ef database create \
  --connection "Server=localhost,1433;Database=kopitra_dev;User Id=sa;Password=KopitraPassword123;"

# Windows
dotnet ef database create \
  --connection "Server=(localdb)\\kopitra;Database=kopitra_dev;Integrated Security=true;"
```

#### 3. 新しいマイグレーションの作成

スキーマ変更後にマイグレーションを作成する場合：

```bash
# マイグレーションを生成
dotnet ef migrations add <MigrationName> -o Infrastructure/Data/Migrations

# 例：新しいテーブルを追加した場合
dotnet ef migrations add AddNewSignalMetricsTable -o Infrastructure/Data/Migrations

# マイグレーションを確認（推奨）
git diff Infrastructure/Data/Migrations/<MigrationName>.cs

# データベースに適用
# Mac/Linux
dotnet ef database update \
  --connection "Server=localhost,1433;Database=kopitra_dev;User Id=sa;Password=KopitraPassword123;"

# Windows
dotnet ef database update \
  --connection "Server=(localdb)\\kopitra;Database=kopitra_dev;Integrated Security=true;"
```

#### 4. マイグレーションのロールバック

前のマイグレーション状態に戻す：

```bash
# 1つ前のマイグレーション状態に戻す
# Mac/Linux
dotnet ef database update <PreviousMigrationName> \
  --connection "Server=localhost,1433;Database=kopitra_dev;User Id=sa;Password=KopitraPassword123;"

# Windows
dotnet ef database update <PreviousMigrationName> \
  --connection "Server=(localdb)\\kopitra;Database=kopitra_dev;Integrated Security=true;"

# マイグレーションファイルを削除（ローカル開発のみ）
rm Infrastructure/Data/Migrations/<MigrationName>.cs
rm Infrastructure/Data/Migrations/<MigrationName>.Designer.cs
```

### マイグレーション管理のベストプラクティス

1. **マイグレーション前に確認**
   ```bash
   # どのような変更が加わるか確認
   dotnet ef migrations script <FromMigration> <ToMigration> > migration.sql
   ```

2. **Git にコミット**
   ```bash
   # マイグレーションを必ずコミット
   git add Infrastructure/Data/Migrations/
   git commit -m "Add migration: <MigrationName>"
   ```

3. **チーム間での同期**
   - マイグレーションファイルは常にバージョン管理に含める
   - Pull 後は自動的に `dotnet ef database update` を実行
   - マイグレーション順序の競合を避ける

4. **スクリプト化（推奨）**
   ```bash
   # migration.sh（Unix）
   #!/bin/bash
   cd functions/src/Kopitra.Api
   dotnet ef database update \
     --connection "Server=localhost,1433;Database=kopitra_dev;User Id=sa;Password=KopitraPassword123;"
   ```

### docker-compose.yml で SQL Server をリセットする

開発時にデータベースをリセットする場合：

```bash
# コンテナとボリュームを完全に削除
docker-compose down -v

# 新しいコンテナで起動
docker-compose up -d mssql

# マイグレーションを再実行
cd functions/src/Kopitra.Api
dotnet ef database update \
  --connection "Server=localhost,1433;Database=kopitra_dev;User Id=sa;Password=KopitraPassword123;"
```

---

### ローカル開発

#### Mac/Linux（Docker を使用）

```bash
# 1. Docker Compose で SQL Server を起動
cd /path/to/kopitra
docker-compose up -d mssql

# 2. マイグレーションを実行
cd functions/src/Kopitra.Api
dotnet ef database update \
  --connection "Server=localhost,1433;Database=kopitra_dev;User Id=sa;Password=KopitraPassword123;"

# 3. Azure Functions Core Tools を使用して実行
cd /path/to/kopitra/functions
func start

# または Azure Functions がバックグラウンドで実行されている場合：
# 別のターミナルウィンドウで
dotnet build
dotnet run
```

#### Windows（LocalDB を使用）

```powershell
# 1. LocalDB インスタンスを確認/作成
sqllocaldb create "kopitra"
sqllocaldb start "kopitra"

# 2. マイグレーションを実行
cd functions\src\Kopitra.Api
dotnet ef database update `
  --connection "Server=(localdb)\kopitra;Database=kopitra_dev;Integrated Security=true;"

# 3. Azure Functions で実行
cd path\to\kopitra2\functions
func start

# または Visual Studio から実行
# kopitra-functions.sln を開いて F5 キーを押す
```

#### VS Code でのデバッグ

```bash
# Mac/Linux または Windows
# VS Code でフォルダを開き、F5 キーを押す
# .vscode/launch.json が自動生成される

# デバッグ情報が表示され、http://localhost:7071 でアクセス可能
```

**ローカルAPI へのアクセス:**
- ベースURL: `http://localhost:7071`
- OpenAPI/Swagger: `http://localhost:7071/api/swagger`

### 設定ファイル

#### `local.settings.json`（ローカル開発）

**Mac/Linux（Docker SQL Server）:**
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "SqlServerConnection": "Server=localhost,1433;Database=kopitra_dev;User Id=sa;Password=KopitraPassword123;",
    "JwtSecret": "your-jwt-secret-for-local-testing",
    "APPINSIGHTS_INSTRUMENTATIONKEY": ""
  }
}
```

**Windows（LocalDB）:**
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "SqlServerConnection": "Server=(localdb)\\kopitra;Database=kopitra_dev;Integrated Security=true;",
    "JwtSecret": "your-jwt-secret-for-local-testing",
    "APPINSIGHTS_INSTRUMENTATIONKEY": ""
  }
}
```

#### `host.json`（ランタイム設定）
```json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "maxTelemetryItemsPerSecond": 20
      }
    }
  },
  "functionTimeout": "00:05:00"
}
```

主な設定項目：
- `logging`: ログレベルとApplication Insights設定
- `functionTimeout`: ファンクションのタイムアウト時間（デフォルト5分）
- `extensions`: バインディング拡張機能の設定

**注意**: 本番環境では、機密情報（JwtSecret、SqlServerConnection）をバージョン管理にコミットしないでください。Azure Key Vaultを使用してください。

---

## 🔌 APIエンドポイント

### クイックリファレンス

| モジュール | エンドポイント | 目的 |
|--------|-----------|---------|
| **Auth** | POST /api/auth/* | 認証とJWTトークン管理 |
| **Accounts** | GET/POST/PUT /api/accounts/* | ユーザー口座管理 |
| **Signals** | POST /api/signals, GET/PUT /api/signals/* | シグナル配信（配信者） |
| **Subscriptions** | POST/GET/PUT /api/subscriptions/* | コピートレード購読 |
| **Orders** | POST /api/orders, GET /api/orders/* | 注文実行と追跡 |
| **EA** | GET /api/ea/signals, POST /api/ea/executions | EAポーリング＆ACK |
| **Metrics** | GET /api/metrics/* | 分析とパフォーマンスデータ |
| **Admin** | GET/PUT /api/admin/* | Admin管理機能 |

完全なAPI仕様については、[SYSTEM_DESIGN.md](../docs/SYSTEM_DESIGN.md) セクション6を参照してください。

---

## 🏛️ アーキテクチャとパターン

### イベントソーシングと EventFlow

このプロジェクトでは **EventFlow** (.NET用のイベントソーシングライブラリ) を使用して、すべてのドメイン変更を不変イベントとして記録します。

**EventFlow の主な特徴:**
- **イベント駆動型**: すべての状態変更がイベントとして記録される
- **集約ルート（Aggregate）**: 一貫性の境界となるエンティティ
- **コマンド/イベント分離**: CQRSパターンに準拠
- **メタデータサポート**: ユーザーID、タイムスタンプなどを自動記録
- **スナップショット**: パフォーマンス最適化用の状態キャッシュ

#### イベント定義

```csharp
// Signal集約のイベント定義
public class SignalCreatedEvent : AggregateEvent<Signal, SignalId>
{
    public string ProviderUserId { get; set; }
    public string Pair { get; set; }
    public decimal PositionSize { get; set; }
    public SignalDirection Direction { get; set; }
    
    public SignalCreatedEvent(
        string providerUserId, 
        string pair, 
        decimal positionSize,
        SignalDirection direction)
    {
        ProviderUserId = providerUserId;
        Pair = pair;
        PositionSize = positionSize;
        Direction = direction;
    }
}

// 修正イベント
public class SignalModifiedEvent : AggregateEvent<Signal, SignalId>
{
    public decimal? NewStopLoss { get; set; }
    public decimal? NewTakeProfit { get; set; }
    
    public SignalModifiedEvent(decimal? newSL, decimal? newTP)
    {
        NewStopLoss = newSL;
        NewTakeProfit = newTP;
    }
}

// クローズイベント
public class SignalClosedEvent : AggregateEvent<Signal, SignalId>
{
}
```

#### 集約の実装

```csharp
public class Signal : AggregateRoot<Signal, SignalId>
{
    public string ProviderUserId { get; private set; }
    public string Pair { get; private set; }
    public decimal PositionSize { get; private set; }
    public SignalDirection Direction { get; private set; }
    public decimal? StopLoss { get; private set; }
    public decimal? TakeProfit { get; private set; }
    public SignalStatus Status { get; private set; }
    
    // イベントハンドラ
    public void Apply(SignalCreatedEvent evt)
    {
        ProviderUserId = evt.ProviderUserId;
        Pair = evt.Pair;
        PositionSize = evt.PositionSize;
        Direction = evt.Direction;
        Status = SignalStatus.Active;
    }
    
    public void Apply(SignalModifiedEvent evt)
    {
        if (evt.NewStopLoss.HasValue)
            StopLoss = evt.NewStopLoss;
        if (evt.NewTakeProfit.HasValue)
            TakeProfit = evt.NewTakeProfit;
    }
    
    public void Apply(SignalClosedEvent evt)
    {
        Status = SignalStatus.Closed;
    }
    
    // ファクトリメソッド
    public static Signal Create(
        SignalId id,
        string providerUserId,
        string pair,
        decimal positionSize,
        SignalDirection direction)
    {
        var signal = new Signal { Id = id };
        signal.Emit(new SignalCreatedEvent(providerUserId, pair, positionSize, direction));
        return signal;
    }
    
    public void Modify(decimal? stopLoss, decimal? takeProfit)
    {
        Emit(new SignalModifiedEvent(stopLoss, takeProfit));
    }
    
    public void Close()
    {
        if (Status == SignalStatus.Closed)
            throw new InvalidOperationException("Signal already closed");
        Emit(new SignalClosedEvent());
    }
}
```

#### EventFlow の登録（Program.cs）

```csharp
// Program.cs で EventFlow を設定
var eventFlowOptions = EventFlowOptions.New()
    .AddEvents(typeof(SignalCreatedEvent).Assembly)
    .AddCommands(typeof(CreateSignalCommand).Assembly)
    .AddCommandHandlers(typeof(CreateSignalCommandHandler).Assembly)
    .UseEntityFrameworkEventStore(o => o
        .UseDbContextFactory(new KopitraDbContextFactory())
        .EnableCaching()
        .EnableSnapshotting())
    .Build();

builder.Services.AddSingleton(eventFlowOptions);
```

### メッセージキュー

従来の Azure Service Bus や Event Hub の代わりに、SQL Database テーブルを使用してメッセージを永続化：

```csharp
public class SignalMessage
{
    public string MessageId { get; set; }              // UUID
    public string SignalId { get; set; }              // FK
    public string SubscriptionId { get; set; }        // FK
    public string SubscriberAccountId { get; set; }   // FK
    public MessageStatus Status { get; set; }         // Pending, Dispatched, Acknowledged, Failed
    public string MessageContent { get; set; }        // JSON
    public DateTime SentAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**利点:**
- コスト削減（Queue/Event Hub 不要）
- シンプルな管理（SQL で直接クエリ可能）
- イベントストアと同じデータベース

### EAポーリングパターン

EAはリアルタイムpub/subメッセージを受け取りません。代わりにポーリング（短間隔のGET）でシグナルを取得：

```
ポーリング フロー:
1. EA が定期的に GET /api/ea/signals?accountId=XXX&lastMessageId=YYY
   ↓
2. Azure Functions が SignalMessage テーブルをクエリ
   → Status = Pending のメッセージを返す
   
3. EA がローカルで注文を実行
   ↓
4. 約定後、EA が POST /api/ea/executions
   → { orderId, executedPrice, executedLot, ... }
   
5. Azure Functions が ExecutionMessage テーブルに記録
   → Status = Acknowledged にマーク
   
6. EA がメッセージを削除（次のポーリングで取得しない）
```

**ポーリング間隔:** EA 設定可能（推奨: 1〜5秒）  
**メッセージ有効期限:** ExpiresAt に達すると自動削除対象

### CQRS パターン（暗黙的）

- **コマンド側**: Signal Aggregate が イベントを発行
  - `CreateSignalCommand` → `SignalCreatedEvent`
  - `ModifySignalCommand` → `SignalModifiedEvent`
  
- **クエリ側**: メトリクス・分析データの読み取り
  - キャッシュテーブル (Metrics) から直接読み取り
  - イベントストア再構築不要（高速クエリ）

---

## 🔐 認証と認可

### JWTトークン
すべてのAPIエンドポイントには、`Authorization`ヘッダーにJWTトークンが必要です：

```
Authorization: Bearer <jwt_token>
```

**トークン クレーム:**
- `sub`（サブジェクト）: userId
- `email`: ユーザーメール
- `role`: "Admin" または "User"
- `iat`: 発行時刻
- `exp`: 有効期限

### 権限チェック
すべてのエンドポイントで権限を検証します：

```csharp
[FunctionName("GetUserAccounts")]
public async Task<HttpResponseData> GetAccounts(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "accounts")] HttpRequestData req)
{
    // JWTからuserIdを抽出
    var userId = req.HttpContext.User.FindFirst("sub")?.Value;
    
    // 認証を検証
    if (string.IsNullOrEmpty(userId))
        return req.CreateResponse(HttpStatusCode.Unauthorized);
    
    // ユーザーの口座のみを取得
    var accounts = await _accountService.GetUserAccountsAsync(userId);
    
    return req.CreateResponse(HttpStatusCode.OK);
}
```

### Admin操作
Admin専用エンドポイントは`role`クレームをチェック：

```csharp
// Adminのみがアクセス可能
if (req.HttpContext.User.FindFirst("role")?.Value != "Admin")
    return req.CreateResponse(HttpStatusCode.Forbidden);
```

---

## 📊 メトリクスと監視

### Application Insights
すべてのファンクションはApplication Insightsにテレメトリを送信します：

```csharp
public class SignalService
{
    private readonly ILogger<SignalService> _logger;
    private readonly TelemetryClient _telemetry;
    
    public async Task<Signal> CreateSignalAsync(CreateSignalRequest req)
    {
        _logger.LogInformation("配信者 {ProviderId} のシグナルを作成中", req.ProviderId);
        
        var signal = // ... シグナル作成
        
        _telemetry.TrackEvent("SignalCreated", new Dictionary<string, string>
        {
            {"signalId", signal.SignalId},
            {"providerId", req.ProviderId}
        });
        
        return signal;
    }
}
```

### 監視する主要メトリクス
- **シグナル作成レート** - 1分あたりのシグナル数
- **約定レート** - 約定したシグナルの%
- **約定レイテンシ** - シグナル作成から約定までの時間
- **EAポーリング頻度** - EA1つあたりの1分間のポーリング数
- **メッセージキューの深さ** - SignalMessageテーブルの保留中メッセージ
- **エラーレート** - 失敗した操作
- **DB クエリパフォーマンス** - イベントストアの遅いクエリ

---

## 🧪 テスト

### ユニットテスト
`tests/Kopitra.Api.Tests/Unit/` に配置：

```bash
# すべてのユニットテストを実行
dotnet test

# 特定のテストクラスを実行
dotnet test --filter ClassName=SignalServiceTests

# カバレッジ付きで実行
dotnet test /p:CollectCoverage=true
```

### 統合テスト
`tests/Kopitra.Api.Tests/Integration/` に配置：
- 完全なAPIフロー（リクエスト→データベース）のテスト
- メモリ内またはSQLiteデータベースを使用
- イベントソーシング + スナップショットのテスト

### テストベストプラクティス
- 外部依存関係をモック化（データベースはしない）
- 成功パスとエラーシナリオの両方をテスト
- べき等性を検証（signalIdが重複を防止）
- すべてのエンドポイントの権限チェックをテスト
- イベントソーシング: イベント作成→スナップショット検証

---

## 🔄 デプロイメント

### 前提条件
- Azureサブスクリプション
- Azure CLIがインストールおよび認証済み
- リソースグループが作成済み

### Azureへのデプロイ

```bash
# ビルドを公開
dotnet publish -c Release

# Azure Functionsにデプロイ
func azure functionapp publish <FunctionAppName> --build remote

# またはAzure CLIを使用
az functionapp deployment source config-zip \
  -g <ResourceGroup> \
  -n <FunctionAppName> \
  --src ./bin/Release/net8.0/publish.zip
```

### 環境設定
Function Appの設定にAzure Key Vaultリファレンスを設定：

```bash
# Azure PortalまたはCLI経由で
az functionapp config appsettings set \
  -g <ResourceGroup> \
  -n <FunctionAppName> \
  --settings \
    SqlServerConnection="@Microsoft.KeyVault(SecretUri=https://myvault.vault.azure.net/secrets/SqlConnection/)" \
    JwtSecret="@Microsoft.KeyVault(SecretUri=https://myvault.vault.azure.net/secrets/JwtSecret/)"
```

### 継続的インテグレーション/デプロイメント
GitHub ActionsやAzure DevOpsを使用した自動デプロイメント：

```yaml
# .github/workflows/deploy.yml
name: Deploy to Azure Functions
on:
  push:
    branches: [main]
jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '8.0.x'
      - run: dotnet publish -c Release
      - uses: Azure/functions-action@v1
        with:
          app-name: ${{ secrets.FUNCTIONAPP_NAME }}
```

---

## 📝 開発ガイドライン

### コード組織
1. **Functions層**: 最小限のロジック、入力検証のみ
2. **Domain層**: ビジネスロジック、集約、ドメインイベント
3. **Infrastructure層**: データベース、リポジトリ、EventFlow統合
4. **Common層**: 共有ユーティリティ、定数

### 新しいAPIエンドポイント追加の流れ

1. **SYSTEM_DESIGN.md を更新** - セクション6（API設計）にエンドポイント仕様を追加

2. **ドメインイベントを定義** (`Domain/Events/`)
   ```csharp
   public class YourAggregateCreatedEvent : AggregateEvent<YourAggregate, YourAggregateId>
   {
       public string PropertyA { get; set; }
       public string PropertyB { get; set; }
   }
   ```

3. **集約を実装** (`Domain/Entities/`)
   - イベントハンドラ（`Apply` メソッド）
   - ビジネスロジックメソッド
   - ファクトリメソッド（`Create`）

4. **コマンドを定義** (`Domain/Commands/`)
   ```csharp
   public class CreateYourAggregateCommand : Command<YourAggregateId>
   {
       public string PropertyA { get; set; }
   }
   ```

5. **コマンドハンドラを実装** (`Infrastructure/Services/CommandHandlers/`)
   ```csharp
   public class CreateYourAggregateCommandHandler 
       : CommandHandler<YourAggregate, YourAggregateId, CreateYourAggregateCommand>
   {
       public Task ExecuteAsync(YourAggregate aggregate, CreateYourAggregateCommand command, ...)
       {
           var newAggregate = YourAggregate.Create(/* ... */);
           return Task.CompletedTask;
       }
   }
   ```

6. **APIエンドポイントを実装** (`Functions/<Module>/`)
   - JWT検証
   - 権限チェック
   - コマンド送信
   - レスポンス返却

7. **テストを追加** (`tests/`)
   - イベント生成テスト
   - コマンドハンドラテスト
   - APIエンドポイント統合テスト

### 実装スタイル

#### エラーハンドリング
```csharp
try
{
    var command = new CreateYourAggregateCommand { /* ... */ };
    await _commandBus.PublishAsync(command);
    return req.CreateResponse(HttpStatusCode.OK);
}
catch (ArgumentException ex)
{
    _logger.LogWarning(ex, "Invalid input: {Message}", ex.Message);
    return req.CreateResponse(HttpStatusCode.BadRequest);
}
catch (InvalidOperationException ex)
{
    _logger.LogWarning(ex, "Business rule violation: {Message}", ex.Message);
    return req.CreateResponse(HttpStatusCode.BadRequest);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    return req.CreateResponse(HttpStatusCode.InternalServerError);
}
```

#### ロギング
EventFlow のメタデータを活用した構造化ログ：
```csharp
_logger.LogInformation(
    "Signal created: AggregateId={AggregateId}, ProviderId={ProviderId}",
    signalId, request.ProviderId);
```

#### べき等性
Signal IDを使用した重複検出（EventFlow の集約IDで自動サポート）：
```csharp
// 同じ Signal ID で Create が呼ばれた場合、イベントストアが自動的に重複を検出
// Aggregate 再構築時に既存の状態が返される
```

### マイグレーション変更時の手順

スキーマ変更が必要な場合：
1. `Domain/Entities/` でプロパティを追加
2. 新しいイベントクラスを定義（または既存イベントに追加）
3. `Aggregate.Apply()` メソッドで処理を追加
4. `dotnet ef migrations add` でマイグレーション生成
5. テストで検証

---

## 🛠️ 便利なコマンド

### ビルド・テスト・実行

```bash
# ビルド
dotnet build

# テスト
dotnet test
dotnet test --filter Category=Integration

# ローカルで実行
func start

# クリーン
dotnet clean

# パッケージを復元
dotnet restore

# NuGetパッケージを追加
dotnet add package <PackageName>

# 公開
dotnet publish -c Release -o ./publish

# コードをフォーマット
dotnet format
```

### データベース・マイグレーション

```bash
# Entity Framework Core ツールをインストール（初回のみ）
dotnet tool install --global dotnet-ef

# 現在のマイグレーション状態を確認
dotnet ef migrations list

# 新しいマイグレーションを作成
dotnet ef migrations add <MigrationName> -o Infrastructure/Data/Migrations

# マイグレーションスクリプトを生成（確認用）
dotnet ef migrations script <FromMigration> <ToMigration> > migration.sql

# データベースを更新（Mac/Linux - Docker）
dotnet ef database update \
  --connection "Server=localhost,1433;Database=kopitra_dev;User Id=sa;Password=KopitraPassword123;"

# データベースを更新（Windows - LocalDB）
dotnet ef database update \
  --connection "Server=(localdb)\\kopitra;Database=kopitra_dev;Integrated Security=true;"

# 前のマイグレーション状態に戻す
dotnet ef database update <PreviousMigrationName> \
  --connection "Server=<your-connection-string>"

# マイグレーションを削除（ローカル開発のみ）
dotnet ef migrations remove
```

### Docker（Mac/Linux）

```bash
# SQL Server を起動
docker-compose up -d mssql

# SQL Server に接続確認
docker exec -it kopitra-mssql sqlcmd -U sa -P 'KopitraPassword123!' -Q "SELECT @@VERSION"

# SQL Server のログを確認
docker-compose logs -f mssql

# SQL Server コンテナを停止
docker-compose down

# SQL Server をリセット（データベース削除）
docker-compose down -v
```

### LocalDB（Windows）

```powershell
# LocalDB インスタンスを作成
sqllocaldb create "kopitra"

# LocalDB インスタンスを開始
sqllocaldb start "kopitra"

# LocalDB インスタンスを停止
sqllocaldb stop "kopitra"

# LocalDB インスタンスを削除
sqllocaldb delete "kopitra"

# LocalDB インスタンス一覧を表示
sqllocaldb info

# SQL Server に接続（SSMS）
# Server name: (localdb)\kopitra
# Authentication: Windows Authentication
```

---

## 📚 関連ドキュメント

- **システム設計**: [SYSTEM_DESIGN.md](../docs/SYSTEM_DESIGN.md) - 完全な仕様とAPI設計
- **ルート AGENTS.md**: [AGENTS.md](../AGENTS.md) - 全体開発ガイドライン
- **バックエンド AGENTS.md**: [AGENTS.md](./AGENTS.md)（作成予定） - バックエンド固有ガイドライン

---

## 🔗 重要な注意事項

### ⚠️ 変更前に

1. **仕様更新**: APIやデータモデル、アーキテクチャを変更する場合は、[SYSTEM_DESIGN.md](../docs/SYSTEM_DESIGN.md)を最初に更新してください
2. **データベースマイグレーション**: スキーマ変更に対してマイグレーションスクリプトを作成してください
3. **後方互換性**: APIの変更がクライアント（特にEA）を破壊しないことを確認してください
4. **イベントバージョニング**: 新しいイベントフィールドを追加する場合、バージョニングを適切に処理してください

### 📋 実装チェックリスト

- [ ] 新しいAPIまたは変更されたコントラクトを追加する場合、SYSTEM_DESIGN.mdを更新
- [ ] 認証/認可チェックを実装
- [ ] 包括的なエラーハンドリングを追加
- [ ] ユニット・統合テストを作成
- [ ] デバッグ用のロギングを追加
- [ ] 必要に応じてデータベーススキーマを更新
- [ ] コミットメッセージで破壊的な変更を文書化
- [ ] 複数のユーザーロール（User、Admin）でテスト
- [ ] 該当する場合、べき等性を検証

---

## 💬 サポートとトラブルシューティング

### よくある問題

**問題**: `func start` が「ワーカープロセスを起動できません」で失敗
```bash
# 解決策: ローカルキャッシュをクリアしてリビルド
dotnet clean
dotnet build
func start
```

**問題**: データベース接続エラー
```bash
# local.settings.jsonの接続文字列を確認
# SQL Serverが実行中であることを確認
# 接続テスト: sqlcmd -S . -d kopitra_dev
```

**問題**: JWTトークン検証が失敗
```bash
# 認証サービスとAPI間のJwtSecretが一致していることを確認
# トークンの有効期限を確認
# Authorizationヘッダーの形式を確認: "Bearer <token>"
```

---

## 📞 リソース

- **Azure Functions ドキュメント**: https://learn.microsoft.com/ja-jp/azure/azure-functions/
- **EventFlow ドキュメント**: https://github.com/eventflow/EventFlow
- **Entity Framework Core**: https://learn.microsoft.com/ja-jp/ef/core/

---

**最終更新**: 2026年1月11日

**重要**: 仕様が変更されたときは、[SYSTEM_DESIGN.md](../docs/SYSTEM_DESIGN.md)を常に最新の状態に保つようにしてください。
