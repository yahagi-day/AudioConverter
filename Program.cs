using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TrackForge
{
    class Program
    {
        private static Config? config;
        private static int totalFiles = 0;
        private static int convertedFiles = 0;
        private static int skippedFiles = 0;
        private static List<string> errorFiles = new List<string>();
        private static string logFile = "";

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("========================================");
            Console.WriteLine("TrackForge - DJ向け音声ファイル変換ツール");
            Console.WriteLine("========================================");

            // 設定ファイルの読み込み
            if (!LoadConfig())
            {
                Console.WriteLine("設定ファイルの読み込みに失敗しました。");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            // ffmpegの存在確認
            if (!CheckFFmpeg())
            {
                Console.WriteLine("ffmpegが見つかりません。ffmpegをインストールしてPATHに追加してください。");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            // ソースディレクトリの存在確認
            foreach (var sourceDirectory in config!.SourceDirectories)
            {
                if (!Directory.Exists(sourceDirectory))
                {
                    Console.WriteLine($"エラー: ソースディレクトリが見つかりません: {sourceDirectory}");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;
                }
            }

            // 出力ディレクトリの作成
            if (!Directory.Exists(config.OutputDirectory))
            {
                Directory.CreateDirectory(config.OutputDirectory);
                Console.WriteLine($"出力ディレクトリを作成しました: {config.OutputDirectory}");
            }

            // ログファイルの初期化
            logFile = Path.Combine(config.OutputDirectory, "conversion.log");
            await InitializeLog();

            Console.WriteLine();
            Console.WriteLine("設定確認:");
            Console.WriteLine("ソースディレクトリ:");
            foreach (var sourceDirectory in config.SourceDirectories)
            {
                Console.WriteLine($"  - {sourceDirectory}");
            }
            Console.WriteLine($"出力ディレクトリ: {config.OutputDirectory}");
            Console.WriteLine($"ビットレート: {config.Bitrate}");
            Console.WriteLine();

            // 音声ファイルの検索と変換
            Console.WriteLine("音声ファイルを検索中...");
            await ProcessAudioFiles();

            // 結果表示
            await ShowResults();

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static bool LoadConfig()
        {
            string configPath = "config.json";
            
            if (!File.Exists(configPath))
            {
                // デフォルト設定ファイルを作成
                var defaultConfig = new Config
                {
                    SourceDirectories = new[] { @"C:\Music\Source1", @"D:\Audio\Collection" },
                    OutputDirectory = @"C:\Users\YourName\Desktop\output",
                    Bitrate = "256k",
                    SupportedFormats = new[] { "*.flac", "*.wav", "*.m4a", "*.aac", "*.ogg", "*.wma", "*.ape", "*.mp3" }
                };

                string json = JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
                File.WriteAllText(configPath, json);
                Console.WriteLine($"デフォルト設定ファイルを作成しました: {configPath}");
                Console.WriteLine("設定を確認してから再実行してください。");
                return false;
            }

            try
            {
                string json = File.ReadAllText(configPath);
                config = JsonConvert.DeserializeObject<Config>(json);
                return config != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"設定ファイルの読み込みエラー: {ex.Message}");
                return false;
            }
        }

        private static bool CheckFFmpeg()
        {
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private static async Task InitializeLog()
        {
            var logContent = new List<string>
            {
                $"変換開始: {DateTime.Now}",
                "ソースディレクトリ:"
            };
            
            foreach (var sourceDirectory in config!.SourceDirectories)
            {
                logContent.Add($"  - {sourceDirectory}");
            }
            
            logContent.Add($"出力ディレクトリ: {config.OutputDirectory}");
            logContent.Add("");
            await File.WriteAllLinesAsync(logFile, logContent);
        }

        private static async Task ProcessAudioFiles()
        {
            foreach (var sourceDirectory in config!.SourceDirectories)
            {
                Console.WriteLine($"ディレクトリ: {sourceDirectory}");
                
                foreach (var format in config!.SupportedFormats)
                {
                    string formatName = format.Replace("*.", "").ToUpper();
                    Console.WriteLine($"  {formatName} ファイルを処理中...");

                    var files = Directory.GetFiles(sourceDirectory, format, SearchOption.AllDirectories);
                    
                    foreach (var file in files)
                    {
                        await ProcessFile(file, sourceDirectory);
                    }
                }
            }
        }

        private static async Task ProcessFile(string inputFile, string sourceDirectory)
        {
            totalFiles++;

            try
            {
                // 相対パスを計算
                string relativePath = Path.GetRelativePath(sourceDirectory, inputFile);
                
                // メタデータから楽曲名を取得
                string? trackTitle = await GetTrackTitle(inputFile);
                
                // 出力ファイル名を決定
                string outputFileName;
                if (!string.IsNullOrWhiteSpace(trackTitle))
                {
                    // メタデータの楽曲名を使用（不正な文字を置換）
                    outputFileName = SanitizeFileName(trackTitle) + ".mp3";
                    Console.WriteLine($"  メタデータから楽曲名を取得: {trackTitle}");
                }
                else
                {
                    // 元のファイル名を使用（拡張子をmp3に変更）
                    outputFileName = Path.ChangeExtension(Path.GetFileName(relativePath), ".mp3");
                }
                
                // 出力ファイルパス
                string outputDir = Path.Combine(config.OutputDirectory, Path.GetDirectoryName(relativePath) ?? "");
                string outputFile = Path.Combine(outputDir, outputFileName);
                
                // 出力ディレクトリを作成
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // 同名ファイルが既に存在する場合はスキップ
                if (File.Exists(outputFile))
                {
                    Console.WriteLine($"[SKIP] {relativePath} (既に存在)");
                    skippedFiles++;
                    return;
                }

                Console.WriteLine($"[変換中] {relativePath}");

                // ffmpegでの変換実行
                bool success = await ConvertWithFFmpeg(inputFile, outputFile);

                if (success)
                {
                    Console.WriteLine($"[完了] {relativePath}");
                    convertedFiles++;
                }
                else
                {
                    Console.WriteLine($"[エラー] {relativePath} の変換に失敗");
                    errorFiles.Add(inputFile);
                    await File.AppendAllTextAsync(logFile, $"変換エラー: {inputFile}{Environment.NewLine}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[エラー] {Path.GetFileName(inputFile)}: {ex.Message}");
                errorFiles.Add(inputFile);
                await File.AppendAllTextAsync(logFile, $"処理エラー: {inputFile} - {ex.Message}{Environment.NewLine}");
            }
        }

        private static async Task<string?> GetTrackTitle(string inputFile)
        {
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $"-v quiet -show_entries format_tags=title -of default=noprint_wrappers=1:nokey=1 \"{inputFile}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    return output.Trim();
                }
            }
            catch (Exception ex)
            {
                await File.AppendAllTextAsync(logFile, $"メタデータ取得エラー: {inputFile} - {ex.Message}{Environment.NewLine}");
            }
            return null;
        }

        private static string SanitizeFileName(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName.Trim();
        }

        private static async Task<bool> ConvertWithFFmpeg(string inputFile, string outputFile)
        {
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-hide_banner -loglevel error -i \"{inputFile}\" -map 0:a:0 -map 0:v? -c:a libmp3lame -b:a {config!.Bitrate} -c:v copy -map_metadata 0 -id3v2_version 3 -write_id3v1 1 -y \"{outputFile}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                process.Start();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                {
                    await File.AppendAllTextAsync(logFile, $"ffmpegエラー: {inputFile}{Environment.NewLine}{error}{Environment.NewLine}");
                }

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                await File.AppendAllTextAsync(logFile, $"ffmpeg実行エラー: {inputFile} - {ex.Message}{Environment.NewLine}");
                return false;
            }
        }

        private static async Task ShowResults()
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("変換完了!");
            Console.WriteLine("========================================");
            Console.WriteLine($"処理したファイル数: {totalFiles}");
            Console.WriteLine($"変換したファイル数: {convertedFiles}");
            Console.WriteLine($"スキップしたファイル数: {skippedFiles}");
            Console.WriteLine($"エラーファイル数: {errorFiles.Count}");
            Console.WriteLine();
            Console.WriteLine($"ログファイル: {logFile}");

            // 結果をログファイルにも記録
            var logContent = new List<string>
            {
                "",
                $"変換終了: {DateTime.Now}",
                $"処理したファイル数: {totalFiles}",
                $"変換したファイル数: {convertedFiles}",
                $"スキップしたファイル数: {skippedFiles}",
                $"エラーファイル数: {errorFiles.Count}"
            };

            if (errorFiles.Count > 0)
            {
                logContent.Add("");
                logContent.Add("エラーファイル一覧:");
                logContent.AddRange(errorFiles);
            }

            await File.AppendAllLinesAsync(logFile, logContent);

            if (totalFiles == 0)
            {
                Console.WriteLine();
                Console.WriteLine("注意: 音声ファイルが見つかりませんでした。");
                Console.WriteLine("ソースディレクトリのパスを確認してください。");
            }
        }
    }

    public class Config
    {
        public string[] SourceDirectories { get; set; } = Array.Empty<string>();
        public string OutputDirectory { get; set; } = "";
        public string Bitrate { get; set; } = "256k";
        public string[] SupportedFormats { get; set; } = Array.Empty<string>();
    }
}