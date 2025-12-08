using System.Text.RegularExpressions;

namespace KeyboardAutoSwitcher.UI
{
    /// <summary>
    /// Form that displays logs with syntax highlighting
    /// </summary>
    public partial class LogViewerForm : ThemedForm
    {
        private readonly RichTextBox _logTextBox;
        private readonly System.Windows.Forms.Timer _refreshTimer;
        private readonly string _logDirectory;
        private string? _currentLogFile;
        private long _lastFilePosition;
        private FileSystemWatcher? _fileWatcher;
        private readonly ToolStrip? _toolStrip;

        public LogViewerForm()
        {
            _logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "KeyboardAutoSwitcher",
                "logs");

            InitializeComponent();

            // Create log text box
            _logTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Theme.Background,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Cascadia Code", 10F, FontStyle.Regular, GraphicsUnit.Point)
                       ?? new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point),
                BorderStyle = BorderStyle.None,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both
            };

            // Create toolbar
            _toolStrip = CreateToolStrip();

            // Add controls
            Controls.Add(_logTextBox);
            Controls.Add(_toolStrip);

            // Setup refresh timer for live updates
            _refreshTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000 // Check every second
            };
            _refreshTimer.Tick += OnRefreshTimerTick;

            // Load initial logs
            LoadLatestLogFile();

            // Setup file watcher
            SetupFileWatcher();

            // Start auto-refresh
            _refreshTimer.Start();
        }

        protected override void OnThemeChanged()
        {
            base.OnThemeChanged();
            ApplyLogViewerTheme();
        }

        private void ApplyLogViewerTheme()
        {
            // Update text box
            _logTextBox.BackColor = Theme.Background;
            _logTextBox.ForeColor = Theme.TextPrimary;

            // Update toolbar
            if (_toolStrip != null)
            {
                _toolStrip.BackColor = Theme.BackgroundToolbar;
                _toolStrip.Renderer = new ThemedToolStripRenderer(Theme);

                foreach (ToolStripItem item in _toolStrip.Items)
                {
                    if (item is ToolStripLabel label)
                    {
                        label.ForeColor = item.Tag?.ToString() == "filelabel"
                            ? Theme.TextSecondary
                            : Theme.TextPrimary;
                    }
                    else if (item is ToolStripButton button)
                    {
                        button.ForeColor = button.Tag?.ToString() == "autoscroll"
                            ? button.Checked ? Theme.HighlightConnected : Theme.TextSecondary
                            : Theme.TextPrimary;
                    }
                }
            }

            // Reload logs with new colors
            _lastFilePosition = 0;
            _logTextBox.Clear();
            RefreshCurrentLog();
        }

        private void InitializeComponent()
        {
            Text = "Keyboard Auto Switcher - Logs";
            Size = new Size(900, 600);
            MinimumSize = new Size(600, 400);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Background;

            // Set icon
            try
            {
                Icon = Resources.IconGenerator.CreateDefaultIcon();
            }
            catch { }
        }

        private ToolStrip CreateToolStrip()
        {
            ToolStrip toolStrip = new()
            {
                BackColor = Theme.BackgroundToolbar,
                ForeColor = Theme.TextPrimary,
                GripStyle = ToolStripGripStyle.Hidden,
                Renderer = new ThemedToolStripRenderer(Theme)
            };

            // Refresh button
            ToolStripButton refreshButton = new("🔄 Actualiser")
            {
                ForeColor = Theme.TextPrimary
            };
            refreshButton.Click += (s, e) => LoadLatestLogFile();

            // Clear button
            ToolStripButton clearButton = new("🗑️ Effacer l'affichage")
            {
                ForeColor = Theme.TextPrimary
            };
            clearButton.Click += (s, e) => _logTextBox.Clear();

            // Open folder button
            ToolStripButton openFolderButton = new("📁 Ouvrir le dossier")
            {
                ForeColor = Theme.TextPrimary
            };
            openFolderButton.Click += OnOpenFolder;

            // Auto-scroll checkbox
            ToolStripLabel autoScrollLabel = new("Auto-scroll:")
            {
                ForeColor = Theme.TextPrimary
            };
            ToolStripButton autoScrollCheckbox = new("✓")
            {
                Checked = true,
                CheckOnClick = true,
                ForeColor = Theme.HighlightConnected,
                Tag = "autoscroll"
            };
            autoScrollCheckbox.Click += (s, e) =>
            {
                autoScrollCheckbox.ForeColor = autoScrollCheckbox.Checked
                    ? Theme.HighlightConnected
                    : Theme.TextSecondary;
                autoScrollCheckbox.Text = autoScrollCheckbox.Checked ? "✓" : "✗";
            };

            // Current file label
            ToolStripLabel fileLabel = new()
            {
                ForeColor = Theme.TextSecondary,
                Alignment = ToolStripItemAlignment.Right,
                Tag = "filelabel"
            };

            _ = toolStrip.Items.Add(refreshButton);
            _ = toolStrip.Items.Add(clearButton);
            _ = toolStrip.Items.Add(new ToolStripSeparator());
            _ = toolStrip.Items.Add(openFolderButton);
            _ = toolStrip.Items.Add(new ToolStripSeparator());
            _ = toolStrip.Items.Add(autoScrollLabel);
            _ = toolStrip.Items.Add(autoScrollCheckbox);
            _ = toolStrip.Items.Add(fileLabel);

            return toolStrip;
        }
        private void SetupFileWatcher()
        {
            if (!Directory.Exists(_logDirectory))
            {
                _ = Directory.CreateDirectory(_logDirectory);
            }

            _fileWatcher = new FileSystemWatcher(_logDirectory, "*.txt")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _fileWatcher.Changed += (s, e) =>
            {
                if (e.FullPath == _currentLogFile)
                {
                    _ = BeginInvoke(RefreshCurrentLog);
                }
            };

            _fileWatcher.Created += (s, e) =>
            {
                _ = BeginInvoke(LoadLatestLogFile);
            };
        }

        private void LoadLatestLogFile()
        {
            try
            {
                if (!Directory.Exists(_logDirectory))
                {
                    AppendColoredText("En attente des premiers logs...\n", Theme.LogInfo);
                    return;
                }

                string[] logFiles = [.. Directory.GetFiles(_logDirectory, "log*.txt").OrderByDescending(File.GetLastWriteTime)];

                if (logFiles.Length == 0)
                {
                    AppendColoredText("Aucun fichier de log trouvé.\n", Theme.LogWarning);
                    return;
                }

                _currentLogFile = logFiles[0];
                _lastFilePosition = 0;
                _logTextBox.Clear();

                // Update file label
                UpdateFileLabel(Path.GetFileName(_currentLogFile));

                RefreshCurrentLog();
            }
            catch (Exception ex)
            {
                AppendColoredText($"Erreur lors du chargement des logs: {ex.Message}\n", Theme.LogError);
            }
        }

        private void RefreshCurrentLog()
        {
            if (_currentLogFile == null || !File.Exists(_currentLogFile))
            {
                return;
            }

            try
            {
                using FileStream fs = new(_currentLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                if (fs.Length < _lastFilePosition)
                {
                    // File was truncated or rotated, reload from beginning
                    _lastFilePosition = 0;
                    _logTextBox.Clear();
                }

                _ = fs.Seek(_lastFilePosition, SeekOrigin.Begin);

                using StreamReader reader = new(fs);
                string newContent = reader.ReadToEnd();

                if (!string.IsNullOrEmpty(newContent))
                {
                    AppendLogLines(newContent);
                    _lastFilePosition = fs.Position;

                    // Auto-scroll if enabled
                    if (IsAutoScrollEnabled())
                    {
                        _logTextBox.SelectionStart = _logTextBox.TextLength;
                        _logTextBox.ScrollToCaret();
                    }
                }
            }
            catch (IOException)
            {
                // File is being written, will retry next tick
            }
            catch (Exception ex)
            {
                AppendColoredText($"Erreur de lecture: {ex.Message}\n", Theme.LogError);
            }
        }

        private void AppendLogLines(string content)
        {
            string[] lines = content.Split('\n');

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    _logTextBox.AppendText("\n");
                    continue;
                }

                ParseAndAppendLine(line);
            }
        }

        private void ParseAndAppendLine(string line)
        {
            // Pattern: 2024-12-02 01:28:35 [INF] Message
            Match match = Regex.Match(line, @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \[(\w{3})\] (.*)$");

            if (match.Success)
            {
                string timestamp = match.Groups[1].Value;
                string level = match.Groups[2].Value;
                string message = match.Groups[3].Value;

                // Append timestamp
                AppendColoredText(timestamp + " ", Theme.LogTimestamp);

                // Append level with appropriate color
                (Color levelColor, string? levelText) = level.ToUpperInvariant() switch
                {
                    "DBG" => (Theme.LogDebug, "[DBG]"),
                    "INF" => (Theme.LogInfo, "[INF]"),
                    "WRN" => (Theme.LogWarning, "[WRN]"),
                    "ERR" => (Theme.LogError, "[ERR]"),
                    "FTL" => (Theme.LogFatal, "[FTL]"),
                    _ => (Theme.TextPrimary, $"[{level}]")
                };

                AppendColoredText(levelText + " ", levelColor);

                // Append message with highlighting
                AppendMessageWithHighlighting(message);
                _logTextBox.AppendText("\n");
            }
            else
            {
                // Non-matching line (possibly continuation or stack trace)
                AppendColoredText(line + "\n", Theme.TextPrimary);
            }
        }

        private void AppendMessageWithHighlighting(string message)
        {
            // Highlight specific patterns in the message
            (string, Color)[] patterns =
            [
                (@"(Dvorak|AZERTY|French|TypeMatrix)", Theme.HighlightKeyboard),
                (@"(0x[0-9A-Fa-f]+)", Theme.HighlightHex),
                (@"(connected|connecté|détecté)", Theme.HighlightConnected),
                (@"(disconnected|non détecté)", Theme.HighlightDisconnected),
                (@"(Switching|Activating|changée)", Theme.HighlightAction),
            ];

            int lastIndex = 0;
            List<(int Start, int Length, Color Color)> matches = [];

            foreach ((string? pattern, Color color) in patterns)
            {
                foreach (Match match in Regex.Matches(message, pattern, RegexOptions.IgnoreCase))
                {
                    matches.Add((match.Index, match.Length, color));
                }
            }

            // Sort by position
            matches = [.. matches.OrderBy(m => m.Start)];

            foreach ((int Start, int Length, Color Color) match in matches)
            {
                if (match.Start > lastIndex)
                {
                    // Append text before match
                    AppendColoredText(message[lastIndex..match.Start], Theme.TextPrimary);
                }

                if (match.Start >= lastIndex)
                {
                    // Append matched text with color
                    AppendColoredText(message.Substring(match.Start, match.Length), match.Color);
                    lastIndex = match.Start + match.Length;
                }
            }

            // Append remaining text
            if (lastIndex < message.Length)
            {
                AppendColoredText(message[lastIndex..], Theme.TextPrimary);
            }
        }

        private void AppendColoredText(string text, Color color)
        {
            _logTextBox.SelectionStart = _logTextBox.TextLength;
            _logTextBox.SelectionLength = 0;
            _logTextBox.SelectionColor = color;
            _logTextBox.AppendText(text);
            _logTextBox.SelectionColor = Theme.TextPrimary;
        }

        private bool IsAutoScrollEnabled()
        {
            if (_toolStrip == null)
            {
                return true;
            }

            foreach (ToolStripItem item in _toolStrip.Items)
            {
                if (item.Tag?.ToString() == "autoscroll" && item is ToolStripButton btn)
                {
                    return btn.Checked;
                }
            }
            return true;
        }

        private void UpdateFileLabel(string fileName)
        {
            if (_toolStrip == null)
            {
                return;
            }

            foreach (ToolStripItem item in _toolStrip.Items)
            {
                if (item.Tag?.ToString() == "filelabel" && item is ToolStripLabel label)
                {
                    label.Text = $"Fichier: {fileName}";
                    break;
                }
            }
        }

        private void OnOpenFolder(object? sender, EventArgs e)
        {
            try
            {
                if (Directory.Exists(_logDirectory))
                {
                    _ = System.Diagnostics.Process.Start("explorer.exe", _logDirectory);
                }
                else
                {
                    _ = MessageBox.Show(
                        $"Le dossier de logs n'existe pas encore:\n{_logDirectory}",
                        "Keyboard Auto Switcher",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnRefreshTimerTick(object? sender, EventArgs e)
        {
            RefreshCurrentLog();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Hide instead of close to allow reopening
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            _refreshTimer.Stop();
            _refreshTimer.Dispose();
            _fileWatcher?.Dispose();
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refreshTimer?.Dispose();
                _fileWatcher?.Dispose();
                _logTextBox?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Custom renderer for themed toolbar
    /// </summary>
    public class ThemedToolStripRenderer(ThemeColors theme) : ToolStripProfessionalRenderer(new ThemedColorTable(theme))
    {
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // Don't render border
        }
    }

    /// <summary>
    /// Color table for themed toolbar
    /// </summary>
    public class ThemedColorTable(ThemeColors theme) : ProfessionalColorTable
    {
        private readonly ThemeColors Theme = theme;

        public override Color ToolStripGradientBegin => Theme.BackgroundToolbar;
        public override Color ToolStripGradientMiddle => Theme.BackgroundToolbar;
        public override Color ToolStripGradientEnd => Theme.BackgroundToolbar;
        public override Color MenuItemSelected => Theme.ButtonHover;
        public override Color MenuItemSelectedGradientBegin => Theme.ButtonHover;
        public override Color MenuItemSelectedGradientEnd => Theme.ButtonHover;
        public override Color MenuItemBorder => Theme.Border;
        public override Color ButtonSelectedHighlight => Theme.ButtonHover;
        public override Color ButtonSelectedGradientBegin => Theme.ButtonHover;
        public override Color ButtonSelectedGradientEnd => Theme.ButtonHover;
        public override Color ButtonPressedGradientBegin => Theme.ButtonPressed;
        public override Color ButtonPressedGradientEnd => Theme.ButtonPressed;
        public override Color SeparatorDark => Theme.Separator;
        public override Color SeparatorLight => Theme.Separator;
    }
}
