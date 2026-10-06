# Web公開ビルド

公開用フォルダーはプロジェクト直下の **`docs/`** です。`doc/` ではありません。
`Build/WebGL/` は以前のローカル確認用出力（Git管理対象外）で、公開には使いません。

## 推奨手順

1. Unity 6000.3.24f1でこのプロジェクトを開きます。Unity HubでWebGL Build Supportが入っていることを確認します。
2. `Assets/Scenes/HouseBlockout.unity` を開き、編集内容を保存します。
3. `Virtual House > Build WebGL` を実行します。保存済みシーンから `docs/` を更新します。
4. Consoleの `Web build structure verified` と `WebGL build completed` を確認します。
5. 公開するときは `docs/` 内の変更・追加・削除を一緒にコミットしてpushします。GitHub Pagesの公開元は `main` / `/docs` に設定します。

ビルドはシーンを自動再生成しません。生成コードの変更を反映するときだけ、先に
`Virtual House > Regenerate Floor Plan Blockout` を実行してください。
これは生成シーンを作り直すため、シーンへの手編集がある場合は先に保存・退避してください。

## 出力構成

```text
docs/
  .nojekyll
  index.html
  Build/
    WebGL.loader.js
    WebGL.framework.js
    WebGL.data
    WebGL.wasm
  TemplateData/
    style.css
    （ロゴ・アイコンなど）
```

`index.html` だけ、または `Build/` だけをコピーしないでください。古いファイルとの混在を避け、フォルダー全体を扱います。
ファイル名・HTML参照・スマートフォン用全画面スタイルはビルド後に自動整形し、必要ファイルの存在を検査します。

## 通常のUnityビルド画面を使う場合

`File > Build Profiles` でWebを選択し、シーン一覧の `HouseBlockout` を有効にします。
出力先にプロジェクト直下の `docs` を選んでください（`docs/Build` を選ばないこと）。
こちらのビルドでも自動整形が実行されます。圧縮は無効にして、GitHub Pagesに特殊なレスポンスヘッダーを要求しない構成にしています。

ローカル確認は `Build And Run` を使います。`index.html` のダブルクリック（file://）では正常に読み込めません。
参考: [Unity公式・Webの開発と公開](https://docs.unity.com/en-us/engine/6000.3/manual/platform-specific/webgl/intro/gettingstarted)

## 今回の修正

通常ビルドが専用メニューの後処理を通らず、`docs.*` という名前と標準サイズのHTMLを出力していました。
名前が `docs.*` であること自体は読み込み不良ではありませんが、ビルド方法によって構成と画面サイズが変わっていました。
後処理をWebビルド共通のコールバックに移し、`WebGL.*` と全画面表示に統一しました。
PC用の標準中央寄せ（50%移動）も明示的に解除し、全画面化による表示位置のずれを防いでいます。
新しい出力の4ファイルが揃わない場合は、古いファイルと混ぜずエラーにします。
公開先の変更やpushはこの作業では行いません。
