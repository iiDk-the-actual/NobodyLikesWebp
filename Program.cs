using ImageMagick;
using System.Collections.Concurrent;

namespace NobodyLikesWebp
{
    public class Program : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private FileSystemWatcher fileWatcher;
        private readonly string downloadsPath;
        private readonly ConcurrentDictionary<string, DateTime> processedFiles = new();

        public Program()
        {
            downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";

            InitializeTrayIcon();
            InitializeFileWatcher();
        }

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Exit", null, OnExit);

            trayIcon = new NotifyIcon
            {
                Text = "NobodyLikesWebp",
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                ContextMenuStrip = trayMenu,
                Visible = true
            };
        }

        private void InitializeFileWatcher()
        {
            fileWatcher = new FileSystemWatcher
            {
                Path = downloadsPath,
                Filter = "*.webp",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            fileWatcher.Created += OnFileCreated;
            fileWatcher.Renamed += OnFileRenamed;
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            Thread.Sleep(500);
            TryConvertFile(e.FullPath);
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            if (e.Name.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
            {
                Thread.Sleep(500);
                TryConvertFile(e.FullPath);
            }
        }

        private void TryConvertFile(string filePath)
        {
            var now = DateTime.Now;

            if (processedFiles.TryGetValue(filePath, out var lastProcessed))
            {
                if ((now - lastProcessed).TotalSeconds < 10)
                    return;
                else
                    processedFiles[filePath] = now;
            }
            else
                processedFiles[filePath] = now;

            ConvertWebpToPng(filePath);
        }

        private void ConvertWebpToPng(string webpPath)
        {
            try
            {
                trayIcon?.ShowBalloonTip(3000, "Conversion Started", $"Converting {Path.GetFileName(webpPath)} to PNG", ToolTipIcon.Info);

                string pngPath = Path.ChangeExtension(webpPath, ".png");

                using (var image = new MagickImage(webpPath))
                    image.Write(pngPath);

                File.Delete(webpPath);

                trayIcon?.ShowBalloonTip(3000, "Conversion Complete", $"{Path.GetFileName(webpPath)} converted to PNG", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                trayIcon?.ShowBalloonTip(3000, "Conversion Error", $"Failed to convert {Path.GetFileName(webpPath)}: {ex.Message}", ToolTipIcon.Error);
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false;
            ShowInTaskbar = false;
            base.OnLoad(e);
        }

        [STAThread]
        public static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Program());
        }
    }
}