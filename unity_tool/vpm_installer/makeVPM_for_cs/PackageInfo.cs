using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace VRCProjectSetup
{
    public class PackageInfo
    {
        public string name { get; set; }
        public string package_id { get; set; }
        public bool enabled { get; set; }
    }

    public class PackageConfig
    {
        public PackageInfo[] packages { get; set; }
    }

    class Program
    {
        /// <summary>
        /// 指定されたコマンドを実行し、終了コードを返す
        /// </summary>
        /// <param name="command">実行するコマンド（例: vpm）</param>
        /// <param name="arguments">コマンド引数</param>
        /// <param name="workingDirectory">作業ディレクトリ</param>
        /// <returns>終了コード</returns>
        static int RunCommand(string command, string arguments, string workingDirectory = null)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = true, // 出力のキャプチャが不要な場合は true
            };

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                psi.WorkingDirectory = workingDirectory;
            }

            Console.WriteLine($"実行コマンド: {command} {arguments}");
            using (Process proc = Process.Start(psi))
            {
                proc.WaitForExit();
                return proc.ExitCode;
            }
        }

        static void Main(string[] args)
        {
            // スクリプトがあるディレクトリ（実行ファイルのフォルダ）を親フォルダとする
            string projectPath = AppContext.BaseDirectory;
            Console.WriteLine("----------------------Project path: " + projectPath);

            // ユーザーにプロジェクト名を入力させる
            Console.Write("プロジェクト名を入力してください: ");
            string projectName = Console.ReadLine().Trim();
            Console.WriteLine("----------------------Project name: " + projectName);

            // 親フォルダ内にプロジェクト名と同じ名前のフォルダを作成
            string projectDir = Path.Combine(projectPath, projectName);

            // プロジェクトフォルダが存在しなければ、新規作成
            if (!Directory.Exists(projectDir))
            {
                Console.WriteLine($"Creating VRC Avatar project: {projectName} in {projectPath}");
                int ret = RunCommand("vpm", $"new \"{projectName}\" Avatar -p \"{projectPath}\"");
                if (ret != 0)
                {
                    Console.WriteLine("Error: プロジェクト作成に失敗しました。");
                    return;
                }
            }
            else
            {
                Console.WriteLine($"Project {projectName} already exists.");
            }

            Console.WriteLine("------------------Project directory: " + projectDir);
            Directory.SetCurrentDirectory(projectDir);

            // Packages ディレクトリをチェック（存在しなければ作成）
            string packagesDir = Path.Combine(projectDir, "Packages");
            if (!Directory.Exists(packagesDir))
            {
                Directory.CreateDirectory(packagesDir);
            }

            // JSONファイルからパッケージ情報を読み込み
            string packagesJsonPath = Path.Combine(projectPath, "packages.json");
            if (!File.Exists(packagesJsonPath))
            {
                Console.WriteLine($"Error: packages.json が見つかりません。場所: {packagesJsonPath}");
                return;
            }

            string jsonContent = File.ReadAllText(packagesJsonPath);
            PackageConfig config = JsonSerializer.Deserialize<PackageConfig>(jsonContent);
            if (config == null || config.packages == null || config.packages.Length == 0)
            {
                Console.WriteLine("パッケージ情報がありません。");
                return;
            }

            // JSONに記載された各パッケージについて、enabled が true ならインストール
            foreach (var pkg in config.packages)
            {
                if (!pkg.enabled)
                {
                    Console.WriteLine($"{pkg.name} (ID: {pkg.package_id}) は無効です。スキップします。");
                    continue;
                }

                string pkgDir = Path.Combine("Packages", pkg.package_id);
                if (!Directory.Exists(Path.Combine(projectDir, pkgDir)))
                {
                    Console.WriteLine($"{pkg.name} (ID: {pkg.package_id}) が見つかりません。インストールします...");
                    int ret = RunCommand("vpm", $"add package {pkg.package_id} -p \"{projectDir}\"");
                    if (ret != 0)
                    {
                        Console.WriteLine($"Error: {pkg.name} のインストールに失敗しました。");
                    }
                }
                else
                {
                    Console.WriteLine($"{pkg.name} (ID: {pkg.package_id}) は既にインストールされています。");
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Avatar project '{projectName}' created and updated successfully!");
            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }
    }
}
