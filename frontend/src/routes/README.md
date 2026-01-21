# Pages ルーター

このフォルダにはページレベルのコンポーネント（ルーターで指定されるコンポーネント）を配置します。

## ファイル構成

```
routes/
├── README.md (このファイル)
├── index.ts (ページをエクスポート)
├── Dashboard.tsx (ダッシュボード)
├── AdminUsers.tsx (ユーザー管理一覧)
├── AdminUserDetailPage.tsx (ユーザー詳細)
└── (他のページ)
```

## ⚠️ ルート作成のルール

**新しいページを作成するたびに、必ず以下を実施してください：**

1. **ページコンポーネントを作成**
   - `src/routes/` に `YourPage.tsx` を作成
   - コンポーネント名は `YourPage` とする

2. **`src/routes/index.ts` にエクスポート追加**

   ```typescript
   export * from "./YourPage";
   ```

3. **`src/App.tsx` にルート追加**
   ```typescript
   <Route path="/path" element={<ProtectedRoute><Layout><YourPage /></Layout></ProtectedRoute>} />
   ```

## ページ作成チェックリスト

- [ ] `src/routes/PageName.tsx` を作成
- [ ] `export const PageName: React.FC<Props> = ...` でコンポーネント定義
- [ ] `src/routes/index.ts` に追加: `export * from "./PageName";`
- [ ] `src/App.tsx` にルート追加: `<Route path="/path" element={<PageName />} />`
- [ ] TypeScript コンパイル確認: `npm run build`

## ページ vs コンポーネントの使い分け

| 種類          | 配置                         | 用途                                               |
| ------------- | ---------------------------- | -------------------------------------------------- |
| **Page**      | `src/routes/`                | ルーターで指定されるトップレベルコンポーネント     |
| **Component** | `src/features/*/components/` | ページ内のサブコンポーネント、機能別コンポーネント |

## 例

### ユーザー管理ページ

```
routes/
├── AdminUsers.tsx ← ページ（ルーター登録）
└── (index.ts に export)

features/admin/components/
├── AdminUserManagementPage.tsx ← サブコンポーネント
├── AdminUserDetailPage.tsx ← サブコンポーネント
├── AdminUserEditDialog.tsx ← ダイアログコンポーネント
└── AdminUserNewDialog.tsx ← ダイアログコンポーネント
```

### App.tsx でのルート定義

```tsx
<Route
  path="/admin/users"
  element={
    <ProtectedRoute>
      <Layout>
        <AdminUsers /> {/* routes/AdminUsers.tsx */}
      </Layout>
    </ProtectedRoute>
  }
/>
```
