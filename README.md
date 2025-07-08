# AudioConverter (C#版)

音声ファイル一括変換プログラムのC#版です。バッチファイルでは処理できない特殊文字を含むファイル名にも対応しています。

## 特徴

- **特殊文字対応**: `!`, `%`, `^`, `&` などを含むファイル名も正しく処理
- **Unicode完全サポート**: 日本語ファイル名の完全対応
- **長いパス対応**: 260文字制限を回避
- **複数ディレクトリ対応**: 複数のソースディレクトリから一括変換
- **メタデータ活用**: 楽曲名がある場合はファイル名に反映
- **詳細なエラーハンドリング**: 分かりやすいエラー情報
- **設定ファイル**: JSON形式で柔軟な設定管理

## 必要な環境

- .NET 9.0 以上
- ffmpeg (PATHに追加済み)
- ffprobe (通常ffmpegに含まれる)

## 使用方法

1. **ビルド**:
   ```bash
   dotnet build
   ```

2. **実行**:
   ```bash
   dotnet run
   ```

3. **初回実行時**: 
   - `config.json` が自動作成されます
   - 設定を確認・修正してから再実行してください

## 設定ファイル (config.json)

```json
{
  "SourceDirectories": [
    "C:\\Music\\Source1",
    "D:\\Audio\\Collection"
  ],
  "OutputDirectory": "C:\\Users\\YourName\\Desktop\\output",
  "Bitrate": "256k",
  "SupportedFormats": [
    "*.flac",
    "*.wav", 
    "*.m4a",
    "*.aac",
    "*.ogg",
    "*.wma",
    "*.ape",
    "*.mp3"
  ]
}
```

## 対応形式

- **入力**: FLAC, WAV, M4A, AAC, OGG, WMA, APE, MP3
- **出力**: MP3 (256kbps)

## ログファイル

変換結果は `conversion.log` に記録されます：
- 変換開始・終了時刻
- 処理統計
- エラーファイル一覧
- 詳細なエラー情報

## トラブルシューティング

- **ffmpegが見つからない**: PATHにffmpegを追加してください
- **ソースディレクトリが見つからない**: `config.json`のパスを確認してください
- **権限エラー**: 管理者権限で実行するか、書き込み可能なディレクトリを指定してください