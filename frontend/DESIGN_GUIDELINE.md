# Kopitra UI/UX ガイドライン

**最終更新**: 2026年1月22日

---

## 1️⃣ レイアウト基本方針

### ページ構成

```tsx
<Container maxWidth="lg" sx={{ mt: 4, mb: 6 }}>
  {/* ページ全体 */}
  <Box sx={{ mb: 4 }}>
    <Typography variant="h4">ページタイトル</Typography>
    <Typography variant="body2" color="text.secondary">
      説明文
    </Typography>
  </Box>

  {/* メインコンテンツ */}
  <Box sx={{ mt: 4 }}>
    <Card>
      <CardContent sx={{ p: 3 }}>{/* コンテンツ直置き（タイトル繰り返さない）*/}</CardContent>
    </Card>
  </Box>
</Container>
```

### スペーシング基準

| 用途                | 値      | 実寸    |
| ------------------- | ------- | ------- |
| ページ - コンテンツ | mb: 4   | 32px    |
| セクション間        | mb: 3-4 | 24-32px |
| カード内パディング  | p: 3    | 24px    |

---

## 2️⃣ 重複を避けるルール

### ❌ 避けるべきパターン

```tsx
// ページレベルでタイトル表示
<Typography variant="h4">ユーザー管理</Typography>

// その下の Card 内でも同じタイトル
<Card>
  <CardHeader title="ユーザー管理" />  ← ❌ 重複！
</Card>
```

### ✅ 正しいパターン

```tsx
// ページレベルのみ
<Typography variant="h4">ユーザー管理</Typography>

// Card 内はタイトルなし
<Card>
  <CardContent>
    {/* 直接コンテンツ */}
  </CardContent>
</Card>
```

### ルール

- **ページ全体のタイトル** → ページ最上部（Container内）のみ
- **Card 内のタイトル** → セクション分割時のみ（複数テーマがある場合）
- **説明文** → ページレベルのみ

---

## 3️⃣ 管理画面構造

### ページ・レイアウト階層

```
Page (AdminUsers.tsx)
├── Page Header（タイトル + 説明 + アクション）
└── AdminUserManagementPage（一覧/検索）

Detail View (AdminUserDetailPage.tsx)
├── Page Header（タイトル + 説明）
├── Section 1: 基本情報
├── Section 2: 権限設定
├── Section 3: アカウント
└── Section 4: 活動ログ
```

### リスト画面（マスター）レイアウト

```tsx
<Fade in={true} timeout={500}>
  <Container maxWidth="lg" sx={{ mt: 4, mb: 6 }}>
    {/* ページヘッダー */}
    <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", mb: 4 }}>
      <Box>
        <Typography variant="h4">ユーザー管理</Typography>
        <Typography variant="body2" color="text.secondary">
          システムユーザーを登録・管理できます
        </Typography>
      </Box>
      <Button variant="contained">+ 新規作成</Button>
    </Box>

    {/* メインコンテンツ */}
    <Card sx={{ mt: 4 }}>
      <CardContent sx={{ p: 3 }}>
        <TextField fullWidth placeholder="検索..." sx={{ mb: 2 }} />
        <Table>{/* ユーザー一覧 */}</Table>
      </CardContent>
    </Card>
  </Container>
</Fade>
```

### 詳細画面（デテール）レイアウト

```tsx
<Fade in={true} timeout={500}>
  <Container maxWidth="lg" sx={{ mt: 4, mb: 6 }}>
    {/* ページヘッダー */}
    <Box sx={{ mb: 4 }}>
      <Typography variant="h4">ユーザー詳細</Typography>
      <Typography variant="body2" color="text.secondary">
        {user.name} ({user.email})
      </Typography>
    </Box>

    {/* 複数セクション（Stack で整理） */}
    <Stack spacing={3}>
      <Card>
        <CardHeader title="基本情報" sx={{ pb: 1 }} />
        <Divider />
        <CardContent sx={{ p: 3 }}>{/* 基本情報内容 */}</CardContent>
        <CardActions>
          <Button>編集</Button>
          <Button color="error">削除</Button>
        </CardActions>
      </Card>

      <Card>
        <CardHeader title="権限設定" sx={{ pb: 1 }} />
        <Divider />
        <CardContent sx={{ p: 3 }}>{/* 権限内容 */}</CardContent>
      </Card>

      <Card>
        <CardHeader
          title="アカウント管理"
          action={<Button size="small">+ 追加</Button>}
          sx={{ pb: 1 }}
        />
        <Divider />
        <CardContent sx={{ p: 3 }}>{/* アカウント一覧 */}</CardContent>
      </Card>

      <Card>
        <CardHeader title="活動履歴" sx={{ pb: 1 }} />
        <Divider />
        <CardContent sx={{ p: 3 }}>{/* 活動履歴内容 */}</CardContent>
      </Card>
    </Stack>
  </Container>
</Fade>
```

### フォーム画面（新規作成・編集）レイアウト

```tsx
<Fade in={true} timeout={400}>
  <Container maxWidth="sm" sx={{ mt: 4, mb: 6 }}>
    <Box sx={{ mb: 4 }}>
      <Typography variant="h5" sx={{ fontWeight: 600, mb: 1 }}>
        ユーザー新規作成
      </Typography>
      <Typography variant="body2" color="text.secondary">
        新規ユーザーの情報を入力してください
      </Typography>
    </Box>

    <Card>
      <CardContent sx={{ p: 3 }}>
        <Stack spacing={2}>
          <TextField fullWidth label="表示名" />
          <TextField fullWidth label="メール" type="email" />
          {/* その他フォーム要素 */}
        </Stack>
      </CardContent>
      <Divider />
      <CardActions sx={{ p: 2, justifyContent: "flex-end" }}>
        <Button>キャンセル</Button>
        <Button variant="contained">作成</Button>
      </CardActions>
    </Card>
  </Container>
</Fade>
```

---

## 4️⃣ コンポーネント使用方針

### アニメーション

| 要素         | タイプ        | 時間  |
| ------------ | ------------- | ----- |
| ページ遷移   | Fade          | 500ms |
| モーダル表示 | Slide (up)    | 300ms |
| ホバー効果   | 背景+シャドウ | 200ms |

### 実装例

```tsx
// ページ全体のFade
<Fade in={true} timeout={500}>
  <Container maxWidth="lg" sx={{ mt: 4, mb: 6 }}>
    {/* ページコンテンツ */}
  </Container>
</Fade>

// ホバー時のカード拡大
<Card sx={{
  transition: "boxShadow 0.2s ease-in-out",
  "&:hover": {
    boxShadow: "0 4px 12px rgba(0,0,0,0.15)"
  }
}}>
```

### 色・フォント

- **テーマ設定** → `src/theme/` または Material-UI デフォルトを参照
- **カスタムカラー** → テーマの `palette` セクションで定義済み

---

## 5️⃣ ナビゲーション構造

### ページ遷移フロー

**リスト → 詳細 → 編集**:

```
一覧 → [詳細表示] → 詳細画面 → [編集] → ダイアログ/新規ページ
  ↓              ↓
詳細画面 ← [← 戻る] 一覧
```

**新規作成フロー**:

```
一覧 → [新規作成] → フォームページ → [送信] → 詳細画面で確認
```

### 状態管理パターン

```tsx
// App.tsx で管理
const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
const [showUserDetail, setShowUserDetail] = useState(false);

// リストと詳細を切り替え
{
  showUserDetail && selectedUserId ? (
    <AdminUserDetailPage userId={selectedUserId} onBack={() => setShowUserDetail(false)} />
  ) : (
    <AdminUsers
      onViewDetail={(userId) => {
        setSelectedUserId(userId);
        setShowUserDetail(true);
      }}
    />
  );
}
```

### コールバックパターン

```tsx
// 親コンポーネント
<ChildComponent onViewDetail={(id) => handleViewDetail(id)} onBack={() => handleBack()} />;

// 子コンポーネント
interface Props {
  onViewDetail: (id: string) => void;
  onBack: () => void;
}
```

### ルーティング構造

```
/admin
├── /users              → ユーザー一覧
├── /users/new          → ユーザー新規作成
├── /users/:userId      → ユーザー詳細
├── /accounts           → アカウント管理
├── /accounts/:id       → アカウント詳細
├── /signals            → シグナル管理
├── /settings           → システム設定
└── /audit-logs         → 監査ログ
```

---

## 6️⃣ 実装チェックリスト

新しい管理画面を作成する際のチェック:

- [ ] ページトップに Fade トランジション (500ms) 適用
- [ ] Container maxWidth="lg" を使用
- [ ] ページ上部にタイトル (h4) + 説明文 (body2) 配置
- [ ] 上下マージン mt: 4, mb: 6 を設定
- [ ] アクションボタン（新規作成等）はページ上部に右配置
- [ ] Card で main コンテンツをラップ、p: 3 パディング設定
- [ ] **Card 内でページタイトル繰り返さない** ⭐
- [ ] 複数セクション時は Stack spacing={3} で整理
- [ ] 各セクション Card に CardHeader でタイトル配置 OK
- [ ] CardActions でボタン配置
- [ ] リスト表示は Table または Grid を使用
- [ ] 詳細ビューはコールバック経由で遷移
- [ ] 色・テキスト色はテーマから取得
- [ ] レスポンシブ: Grid で xs={12} md={6} 等を指定
- [ ] キーボード操作サポート (Tab キーでフォーカス可能)
- [ ] アクセシビリティ確認 (aria-label、label要素等)

---

## 7️⃣ よくある質問

**Q: Card 内でタイトルを表示するべき？**  
A: 複数セクションがある場合のみ CardHeader で OK。ページタイトルと同じ内容は NG。

**Q: ページレベルの説明文は常に必要？**  
A: はい。ページ目的を説明する body2 テキストは配置。Card 内には繰り返さない。

**Q: ホバー時の背景色は？**  
A: テーマで定義。Material-UI デフォルトまたは `src/theme/` を参照。

**Q: フォント大きさの指定は？**  
A: Material-UI の `variant` を使用 (h1-h6, body1-2, caption等)。

**Q: レスポンシブ対応は？**  
A: Container maxWidth="lg" で自動対応。Grid xs={12} md={6} 等で調整。

**Q: アニメーション時間の変更は？**  
A: テーマの `transitions` で一元管理。

**Q: 新規作成フローはどうするべき？**  
A: 別ページ `(new)` または モーダルダイアログで実装。ページ内フォームは避ける。

**Q: 詳細画面で編集する場合は？**  
A: ダイアログモーダル or 別ページで実装。詳細画面上でのインライン編集は避ける。

**Q: マスター・デテール配置は？**  
A: Grid container で xs={12} md={6} 指定。スマートフォンは縦積み、デスクトップは左右配置。

**Q: ステータス表示の色は？**  
A: color="success"（成功）、color="error"（失敗）、color="warning"（注意）、color="default"（通常）を使用。

---

## 📌 ファイル参照

| 内容                 | 参照先                                       |
| -------------------- | -------------------------------------------- |
| テーマ設定           | `src/theme/` または Material-UI 設定ファイル |
| コンポーネント実装   | `src/components/`                            |
| ページコンポーネント | `src/routes/`                                |
| API 通信             | `src/api/`                                   |

---

**方針**: セマンティックで一貫性のあるUIを実装。レイアウトと指標ルールを守ることで、スケーラビリティと保守性を確保。
