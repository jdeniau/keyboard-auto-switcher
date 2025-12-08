using KeyboardAutoSwitcher.Models;
using KeyboardAutoSwitcher.Services;
using System.Management;

namespace KeyboardAutoSwitcher.UI
{
    /// <summary>
    /// Factory for creating uniformly styled buttons
    /// </summary>
    internal static class ButtonFactory
    {
        public const int StandardHeight = 32;
        public const int StandardWidth = 110;
        public const int SmallWidth = 90;
        public const int Spacing = 8;

        /// <summary>
        /// Creates a primary action button (highlighted, e.g., Save, OK, Select)
        /// </summary>
        public static Button CreatePrimary(string text, ThemeColors theme, EventHandler? onClick = null)
        {
            Button button = CreateBase(text, theme, onClick);
            button.Tag = true; // Mark as primary for theme updates
            button.BackColor = theme.ButtonPrimaryBackground;
            button.ForeColor = theme.ButtonPrimaryText;
            return button;
        }

        /// <summary>
        /// Creates a secondary action button (normal, e.g., Cancel, Add, Edit)
        /// </summary>
        public static Button CreateSecondary(string text, ThemeColors theme, EventHandler? onClick = null)
        {
            Button button = CreateBase(text, theme, onClick);
            button.Tag = false; // Mark as secondary for theme updates
            button.BackColor = theme.BackgroundToolbar;
            button.ForeColor = theme.TextPrimary;

            // Handle disabled state colors
            button.EnabledChanged += (s, e) =>
            {
                if (s is Button btn)
                {
                    ThemeColors currentTheme = ThemeHelper.GetThemeColors();
                    if (btn.Enabled)
                    {
                        btn.BackColor = currentTheme.BackgroundToolbar;
                        btn.ForeColor = currentTheme.TextPrimary;
                    }
                    else
                    {
                        btn.BackColor = currentTheme.ButtonDisabledBackground;
                        btn.ForeColor = currentTheme.ButtonDisabledText;
                    }
                }
            };

            return button;
        }

        private static Button CreateBase(string text, ThemeColors theme, EventHandler? onClick)
        {
            Button button = new()
            {
                Text = text,
                Size = new Size(StandardWidth, StandardHeight),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, Spacing, Spacing),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderColor = theme.Border;
            button.FlatAppearance.BorderSize = 1;

            if (onClick != null)
            {
                button.Click += onClick;
            }

            return button;
        }
    }

    /// <summary>
    /// Helper for styling ListView controls with theme support
    /// </summary>
    internal static class ListViewHelper
    {
        /// <summary>
        /// Applies theme colors and owner-draw for column headers
        /// </summary>
        public static void ApplyTheme(ListView listView, ThemeColors theme)
        {
            listView.BackColor = theme.BackgroundToolbar;
            listView.ForeColor = theme.TextPrimary;
            listView.BorderStyle = BorderStyle.FixedSingle;

            // Only set up owner draw once
            if (!listView.OwnerDraw)
            {
                listView.OwnerDraw = true;

                listView.DrawColumnHeader += (s, e) =>
                {
                    ThemeColors currentTheme = ThemeHelper.GetThemeColors();
                    using SolidBrush backBrush = new(currentTheme.BackgroundSecondary);
                    using Pen borderPen = new(currentTheme.Border);

                    e.Graphics.FillRectangle(backBrush, e.Bounds);
                    e.Graphics.DrawRectangle(borderPen, e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);

                    TextRenderer.DrawText(
                        e.Graphics,
                        e.Header?.Text ?? "",
                        e.Font,
                        new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height),
                        currentTheme.TextPrimary,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                };

                listView.DrawItem += (s, e) => e.DrawDefault = true;
                listView.DrawSubItem += (s, e) => e.DrawDefault = true;
            }
        }
    }

    /// <summary>
    /// Form for configuring keyboard layout switching settings
    /// </summary>
    public class ConfigurationForm : ThemedForm
    {
        private readonly IConfigurationService _configService;
        private readonly AppConfiguration _editingConfig;

        // Controls
        private readonly ComboBox _defaultLayoutCombo;
        private readonly ListView _deviceListView;
        private Button _addDeviceButton = null!;
        private Button _editDeviceButton = null!;
        private Button _removeDeviceButton = null!;
        private Button _detectDeviceButton = null!;
        private Button _saveButton = null!;
        private Button _cancelButton = null!;

        private readonly List<InstalledKeyboardLayout> _installedLayouts = [];

        public ConfigurationForm(IConfigurationService configService)
        {
            _configService = configService;
            _editingConfig = configService.Configuration.Clone();

            // Load installed layouts
            _installedLayouts = KeyboardLayout.GetInstalledLayouts();

            // Initialize controls
            _defaultLayoutCombo = new ComboBox();
            _deviceListView = new ListView();

            InitializeComponent();
            LoadConfiguration();
        }

        private void InitializeComponent()
        {
            Text = "Configuration - Keyboard Auto Switcher";
            Size = new Size(700, 500);
            MinimumSize = new Size(600, 400);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Background;
            ForeColor = Theme.TextPrimary;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            // Set icon
            try
            {
                Icon = Resources.IconGenerator.CreateDefaultIcon();
            }
            catch { }

            // Create layout
            TableLayoutPanel mainLayout = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                RowCount = 3,
                ColumnCount = 1
            };
            _ = mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _ = mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _ = mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Default layout section
            GroupBox defaultLayoutGroup = CreateDefaultLayoutSection();
            mainLayout.Controls.Add(defaultLayoutGroup, 0, 0);

            // Device mappings section
            GroupBox deviceMappingsGroup = CreateDeviceMappingsSection();
            mainLayout.Controls.Add(deviceMappingsGroup, 0, 1);

            // Buttons panel
            Panel buttonsPanel = CreateButtonsPanel();
            mainLayout.Controls.Add(buttonsPanel, 0, 2);

            Controls.Add(mainLayout);
        }

        private GroupBox CreateDefaultLayoutSection()
        {
            GroupBox group = new()
            {
                Text = "Disposition par défaut",
                Dock = DockStyle.Fill,
                ForeColor = Theme.TextPrimary,
                Padding = new Padding(10),
                Height = 80
            };

            TableLayoutPanel layout = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            _ = layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _ = layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            Label label = new()
            {
                Text = "Quand aucun périphérique configuré n'est connecté :",
                AutoSize = true,
                ForeColor = Theme.TextPrimary,
                Anchor = AnchorStyles.Left,
                Padding = new Padding(0, 5, 10, 0)
            };

            _defaultLayoutCombo.Dock = DockStyle.Fill;
            _defaultLayoutCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _defaultLayoutCombo.BackColor = Theme.BackgroundToolbar;
            _defaultLayoutCombo.ForeColor = Theme.TextPrimary;
            _defaultLayoutCombo.FlatStyle = FlatStyle.Flat;

            // Populate with installed layouts
            foreach (InstalledKeyboardLayout installedLayout in _installedLayouts)
            {
                _ = _defaultLayoutCombo.Items.Add(installedLayout);
            }

            layout.Controls.Add(label, 0, 0);
            layout.Controls.Add(_defaultLayoutCombo, 1, 0);
            group.Controls.Add(layout);

            return group;
        }

        private GroupBox CreateDeviceMappingsSection()
        {
            GroupBox group = new()
            {
                Text = "Périphériques USB configurés",
                Dock = DockStyle.Fill,
                ForeColor = Theme.TextPrimary,
                Padding = new Padding(10)
            };

            TableLayoutPanel layout = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            _ = layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _ = layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            // Device list view
            _deviceListView.Dock = DockStyle.Fill;
            _deviceListView.View = View.Details;
            _deviceListView.FullRowSelect = true;
            ListViewHelper.ApplyTheme(_deviceListView, Theme);
            _ = _deviceListView.Columns.Add("Nom", 150);
            _ = _deviceListView.Columns.Add("VID", 60);
            _ = _deviceListView.Columns.Add("PID", 60);
            _ = _deviceListView.Columns.Add("Disposition", 250);
            _deviceListView.SelectedIndexChanged += OnDeviceSelectionChanged;
            _deviceListView.DoubleClick += OnEditDeviceClick;

            // Buttons panel
            FlowLayoutPanel buttonsPanel = new()
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                Padding = new Padding(10, 0, 0, 0)
            };

            _addDeviceButton = ButtonFactory.CreateSecondary("➕ Ajouter", Theme, OnAddDeviceClick);

            _editDeviceButton = ButtonFactory.CreateSecondary("✏️ Modifier", Theme, OnEditDeviceClick);
            _editDeviceButton.Enabled = false;

            _removeDeviceButton = ButtonFactory.CreateSecondary("🗑️ Supprimer", Theme, OnRemoveDeviceClick);
            _removeDeviceButton.Enabled = false;
            _removeDeviceButton.Margin = new Padding(0, 0, ButtonFactory.Spacing, ButtonFactory.Spacing * 2);

            _detectDeviceButton = ButtonFactory.CreatePrimary("🔍 Détecter", Theme, OnDetectDeviceClick);

            buttonsPanel.Controls.Add(_addDeviceButton);
            buttonsPanel.Controls.Add(_editDeviceButton);
            buttonsPanel.Controls.Add(_removeDeviceButton);
            buttonsPanel.Controls.Add(_detectDeviceButton);

            layout.Controls.Add(_deviceListView, 0, 0);
            layout.Controls.Add(buttonsPanel, 1, 0);
            group.Controls.Add(layout);

            return group;
        }

        private Panel CreateButtonsPanel()
        {
            Panel panel = new()
            {
                Dock = DockStyle.Fill,
                Height = 50,
                Padding = new Padding(0, 10, 0, 0)
            };

            FlowLayoutPanel flowPanel = new()
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            _saveButton = ButtonFactory.CreatePrimary("💾 Enregistrer", Theme, OnSaveClick);
            _saveButton.Margin = new Padding(ButtonFactory.Spacing, 0, 0, 0);

            _cancelButton = ButtonFactory.CreateSecondary("Annuler", Theme, OnCancelClick);
            _cancelButton.Margin = new Padding(0);

            // Add in reverse order because FlowDirection is RightToLeft
            flowPanel.Controls.Add(_cancelButton);
            flowPanel.Controls.Add(_saveButton);
            panel.Controls.Add(flowPanel);

            return panel;
        }

        private void LoadConfiguration()
        {
            // Load default layout
            foreach (InstalledKeyboardLayout layout in _defaultLayoutCombo.Items)
            {
                if (layout.LayoutId == _editingConfig.DefaultLayoutId)
                {
                    _defaultLayoutCombo.SelectedItem = layout;
                    break;
                }
            }

            // If not found, try to find by language ID
            if (_defaultLayoutCombo.SelectedItem == null && _defaultLayoutCombo.Items.Count > 0)
            {
                int targetLangId = _editingConfig.DefaultLayoutId & 0xFFFF;
                foreach (InstalledKeyboardLayout layout in _defaultLayoutCombo.Items)
                {
                    if ((layout.LayoutId & 0xFFFF) == targetLangId)
                    {
                        _defaultLayoutCombo.SelectedItem = layout;
                        break;
                    }
                }
            }

            // If still not found, select first
            if (_defaultLayoutCombo.SelectedItem == null && _defaultLayoutCombo.Items.Count > 0)
            {
                _defaultLayoutCombo.SelectedIndex = 0;
            }

            // Load device mappings
            RefreshDeviceList();
        }

        private void RefreshDeviceList()
        {
            _deviceListView.Items.Clear();

            foreach (UsbDeviceMapping mapping in _editingConfig.DeviceMappings)
            {
                ListViewItem item = new(mapping.DeviceName);
                _ = item.SubItems.Add(mapping.VendorId);
                _ = item.SubItems.Add(mapping.ProductId);
                _ = item.SubItems.Add(mapping.LayoutDisplayName);
                item.Tag = mapping;
                _ = _deviceListView.Items.Add(item);
            }

            UpdateButtonStates();
        }

        private void OnDeviceSelectionChanged(object? sender, EventArgs e)
        {
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            bool hasSelection = _deviceListView.SelectedItems.Count > 0;
            _editDeviceButton.Enabled = hasSelection;
            _removeDeviceButton.Enabled = hasSelection;
        }

        private void OnAddDeviceClick(object? sender, EventArgs e)
        {
            UsbDeviceMapping newMapping = new()
            {
                DeviceName = "Nouveau périphérique"
            };

            using DeviceMappingDialog dialog = new(_installedLayouts, newMapping, false);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _editingConfig.DeviceMappings.Add(dialog.Mapping);
                RefreshDeviceList();
            }
        }

        private void OnEditDeviceClick(object? sender, EventArgs e)
        {
            if (_deviceListView.SelectedItems.Count == 0)
            {
                return;
            }

            if (_deviceListView.SelectedItems[0].Tag is not UsbDeviceMapping mapping)
            {
                return;
            }

            using DeviceMappingDialog dialog = new(_installedLayouts, mapping.Clone(), true);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                int index = _editingConfig.DeviceMappings.IndexOf(mapping);
                if (index >= 0)
                {
                    _editingConfig.DeviceMappings[index] = dialog.Mapping;
                    RefreshDeviceList();
                }
            }
        }

        private void OnRemoveDeviceClick(object? sender, EventArgs e)
        {
            if (_deviceListView.SelectedItems.Count == 0)
            {
                return;
            }

            if (_deviceListView.SelectedItems[0].Tag is not UsbDeviceMapping mapping)
            {
                return;
            }

            DialogResult result = MessageBox.Show(
                $"Voulez-vous vraiment supprimer le périphérique \"{mapping.DeviceName}\" ?",
                "Confirmer la suppression",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _ = _editingConfig.DeviceMappings.Remove(mapping);
                RefreshDeviceList();
            }
        }

        private void OnDetectDeviceClick(object? sender, EventArgs e)
        {
            using UsbDeviceDetectionDialog dialog = new(_installedLayouts);
            if (dialog.ShowDialog(this) == DialogResult.OK && dialog.SelectedMapping != null)
            {
                _editingConfig.DeviceMappings.Add(dialog.SelectedMapping);
                RefreshDeviceList();
            }
        }

        private void OnSaveClick(object? sender, EventArgs e)
        {
            // Update default layout from selection
            if (_defaultLayoutCombo.SelectedItem is InstalledKeyboardLayout selectedLayout)
            {
                _editingConfig.DefaultLayoutId = selectedLayout.LayoutId;
                _editingConfig.DefaultLayoutDisplayName = selectedLayout.DisplayName;
            }

            try
            {
                _configService.Save(_editingConfig);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(
                    $"Erreur lors de l'enregistrement : {ex.Message}",
                    "Erreur",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void OnCancelClick(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    /// <summary>
    /// Dialog for editing a device mapping
    /// </summary>
    public class DeviceMappingDialog : ThemedForm
    {
        private readonly List<InstalledKeyboardLayout> _installedLayouts;

        private readonly TextBox _deviceNameTextBox;
        private readonly TextBox _vidTextBox;
        private readonly TextBox _pidTextBox;
        private readonly ComboBox _layoutCombo;
        private Button _okButton = null!;
        private Button _cancelButton = null!;

        public UsbDeviceMapping Mapping { get; private set; }

        public DeviceMappingDialog(List<InstalledKeyboardLayout> installedLayouts, UsbDeviceMapping mapping, bool isEdit)
        {
            _installedLayouts = installedLayouts;
            Mapping = mapping;

            _deviceNameTextBox = new TextBox();
            _vidTextBox = new TextBox();
            _pidTextBox = new TextBox();
            _layoutCombo = new ComboBox();

            InitializeComponent(isEdit);
            LoadMapping();
        }

        private void InitializeComponent(bool isEdit)
        {
            Text = isEdit ? "Modifier le périphérique" : "Ajouter un périphérique";
            Size = new Size(450, 280);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            ForeColor = Theme.TextPrimary;

            TableLayoutPanel layout = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                RowCount = 5,
                ColumnCount = 2
            };
            _ = layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _ = layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Device name
            layout.Controls.Add(CreateLabel("Nom :"), 0, 0);
            _deviceNameTextBox.Dock = DockStyle.Fill;
            _deviceNameTextBox.BackColor = Theme.BackgroundToolbar;
            _deviceNameTextBox.ForeColor = Theme.TextPrimary;
            layout.Controls.Add(_deviceNameTextBox, 1, 0);

            // VID
            layout.Controls.Add(CreateLabel("Vendor ID (VID) :"), 0, 1);
            _vidTextBox.Dock = DockStyle.Fill;
            _vidTextBox.BackColor = Theme.BackgroundToolbar;
            _vidTextBox.ForeColor = Theme.TextPrimary;
            _vidTextBox.MaxLength = 4;
            _vidTextBox.CharacterCasing = CharacterCasing.Upper;
            layout.Controls.Add(_vidTextBox, 1, 1);

            // PID
            layout.Controls.Add(CreateLabel("Product ID (PID) :"), 0, 2);
            _pidTextBox.Dock = DockStyle.Fill;
            _pidTextBox.BackColor = Theme.BackgroundToolbar;
            _pidTextBox.ForeColor = Theme.TextPrimary;
            _pidTextBox.MaxLength = 4;
            _pidTextBox.CharacterCasing = CharacterCasing.Upper;
            layout.Controls.Add(_pidTextBox, 1, 2);

            // Layout
            layout.Controls.Add(CreateLabel("Disposition clavier :"), 0, 3);
            _layoutCombo.Dock = DockStyle.Fill;
            _layoutCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _layoutCombo.FlatStyle = FlatStyle.Flat;
            _layoutCombo.BackColor = Theme.BackgroundToolbar;
            _layoutCombo.ForeColor = Theme.TextPrimary;
            foreach (InstalledKeyboardLayout installedLayout in _installedLayouts)
            {
                _ = _layoutCombo.Items.Add(installedLayout);
            }
            layout.Controls.Add(_layoutCombo, 1, 3);

            // Buttons
            FlowLayoutPanel buttonsPanel = new()
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 10, 0, 0)
            };

            _okButton = ButtonFactory.CreatePrimary("OK", Theme, OnOkClick);
            _okButton.Margin = new Padding(ButtonFactory.Spacing, 0, 0, 0);

            _cancelButton = ButtonFactory.CreateSecondary("Annuler", Theme, (s, e) => { DialogResult = DialogResult.Cancel; Close(); });
            _cancelButton.Margin = new Padding(0);

            // Add in reverse order because FlowDirection is RightToLeft
            buttonsPanel.Controls.Add(_cancelButton);
            buttonsPanel.Controls.Add(_okButton);

            layout.SetColumnSpan(buttonsPanel, 2);
            layout.Controls.Add(buttonsPanel, 0, 4);

            Controls.Add(layout);
        }

        private Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = Theme.TextPrimary,
                Anchor = AnchorStyles.Left,
                Padding = new Padding(0, 5, 10, 0)
            };
        }

        private void LoadMapping()
        {
            _deviceNameTextBox.Text = Mapping.DeviceName;
            _vidTextBox.Text = Mapping.VendorId;
            _pidTextBox.Text = Mapping.ProductId;

            // Select layout
            foreach (InstalledKeyboardLayout layout in _layoutCombo.Items)
            {
                if (layout.LayoutId == Mapping.LayoutId)
                {
                    _layoutCombo.SelectedItem = layout;
                    break;
                }
            }

            // Try language ID match if exact not found
            if (_layoutCombo.SelectedItem == null && Mapping.LayoutId != 0)
            {
                int targetLangId = Mapping.LayoutId & 0xFFFF;
                foreach (InstalledKeyboardLayout layout in _layoutCombo.Items)
                {
                    if ((layout.LayoutId & 0xFFFF) == targetLangId)
                    {
                        _layoutCombo.SelectedItem = layout;
                        break;
                    }
                }
            }

            // Default to first if nothing selected
            if (_layoutCombo.SelectedItem == null && _layoutCombo.Items.Count > 0)
            {
                _layoutCombo.SelectedIndex = 0;
            }
        }

        private void OnOkClick(object? sender, EventArgs e)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(_deviceNameTextBox.Text))
            {
                _ = MessageBox.Show("Veuillez entrer un nom pour le périphérique.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_vidTextBox.Text) || _vidTextBox.Text.Length != 4)
            {
                _ = MessageBox.Show("Le Vendor ID (VID) doit être un code hexadécimal de 4 caractères.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_pidTextBox.Text) || _pidTextBox.Text.Length != 4)
            {
                _ = MessageBox.Show("Le Product ID (PID) doit être un code hexadécimal de 4 caractères.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_layoutCombo.SelectedItem == null)
            {
                _ = MessageBox.Show("Veuillez sélectionner une disposition clavier.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Update mapping
            InstalledKeyboardLayout selectedLayout = (InstalledKeyboardLayout)_layoutCombo.SelectedItem;
            Mapping.DeviceName = _deviceNameTextBox.Text.Trim();
            Mapping.VendorId = _vidTextBox.Text.Trim().ToUpperInvariant();
            Mapping.ProductId = _pidTextBox.Text.Trim().ToUpperInvariant();
            Mapping.LayoutId = selectedLayout.LayoutId;
            Mapping.LayoutDisplayName = selectedLayout.DisplayName;

            DialogResult = DialogResult.OK;
            Close();
        }
    }

    /// <summary>
    /// Dialog for detecting connected USB devices
    /// </summary>
    public class UsbDeviceDetectionDialog : ThemedForm
    {
        private readonly List<InstalledKeyboardLayout> _installedLayouts;

        private readonly ListView _deviceListView;
        private Button _refreshButton = null!;
        private Button _selectButton = null!;
        private Button _cancelButton = null!;
        private readonly ComboBox _layoutCombo;

        public UsbDeviceMapping? SelectedMapping { get; private set; }

        public UsbDeviceDetectionDialog(List<InstalledKeyboardLayout> installedLayouts)
        {
            _installedLayouts = installedLayouts;

            _deviceListView = new ListView();
            _layoutCombo = new ComboBox();

            InitializeComponent();
            RefreshDeviceList();
        }

        private void InitializeComponent()
        {
            Text = "Détecter un périphérique USB";
            Size = new Size(600, 450);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            ForeColor = Theme.TextPrimary;

            TableLayoutPanel mainLayout = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                RowCount = 4,
                ColumnCount = 1
            };
            _ = mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _ = mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _ = mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _ = mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Instructions
            Label instructions = new()
            {
                Text = "Sélectionnez un périphérique USB dans la liste ci-dessous :",
                AutoSize = true,
                ForeColor = Theme.TextPrimary,
                Padding = new Padding(0, 0, 0, 10)
            };
            mainLayout.Controls.Add(instructions, 0, 0);

            // Device list
            _deviceListView.Dock = DockStyle.Fill;
            _deviceListView.View = View.Details;
            _deviceListView.FullRowSelect = true;
            ListViewHelper.ApplyTheme(_deviceListView, Theme);
            _ = _deviceListView.Columns.Add("Description", 250);
            _ = _deviceListView.Columns.Add("VID", 60);
            _ = _deviceListView.Columns.Add("PID", 60);
            _ = _deviceListView.Columns.Add("Device ID", 180);
            _deviceListView.SelectedIndexChanged += OnDeviceSelectionChanged;
            mainLayout.Controls.Add(_deviceListView, 0, 1);

            // Layout selection
            FlowLayoutPanel layoutPanel = new()
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 10, 0, 10)
            };

            Label layoutLabel = new()
            {
                Text = "Disposition clavier à utiliser :",
                AutoSize = true,
                ForeColor = Theme.TextPrimary,
                Anchor = AnchorStyles.Left,
                Padding = new Padding(0, 5, 10, 0)
            };

            _layoutCombo.Width = 300;
            _layoutCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _layoutCombo.FlatStyle = FlatStyle.Flat;
            _layoutCombo.BackColor = Theme.BackgroundToolbar;
            _layoutCombo.ForeColor = Theme.TextPrimary;
            foreach (InstalledKeyboardLayout layout in _installedLayouts)
            {
                _ = _layoutCombo.Items.Add(layout);
            }
            if (_layoutCombo.Items.Count > 0)
            {
                _layoutCombo.SelectedIndex = 0;
            }

            layoutPanel.Controls.Add(layoutLabel);
            layoutPanel.Controls.Add(_layoutCombo);
            mainLayout.Controls.Add(layoutPanel, 0, 2);

            // Buttons
            FlowLayoutPanel buttonsPanel = new()
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            _refreshButton = ButtonFactory.CreateSecondary("🔄 Actualiser", Theme, (s, e) => RefreshDeviceList());

            _selectButton = ButtonFactory.CreatePrimary("✓ Sélectionner", Theme, OnSelectClick);
            _selectButton.Margin = new Padding(ButtonFactory.Spacing, 0, 0, 0);
            _selectButton.Enabled = false;

            _cancelButton = ButtonFactory.CreateSecondary("Annuler", Theme, (s, e) => { DialogResult = DialogResult.Cancel; Close(); });
            _cancelButton.Margin = new Padding(0);

            // Add in reverse order because FlowDirection is RightToLeft
            buttonsPanel.Controls.Add(_cancelButton);
            buttonsPanel.Controls.Add(_selectButton);
            buttonsPanel.Controls.Add(_refreshButton);
            mainLayout.Controls.Add(buttonsPanel, 0, 3);

            Controls.Add(mainLayout);
        }

        private void OnDeviceSelectionChanged(object? sender, EventArgs e)
        {
            _selectButton.Enabled = _deviceListView.SelectedItems.Count > 0;
        }

        private void RefreshDeviceList()
        {
            _deviceListView.Items.Clear();
            Cursor = Cursors.WaitCursor;

            try
            {
                using ManagementObjectSearcher searcher = new(@"Select * From Win32_USBHub");
                using ManagementObjectCollection collection = searcher.Get();

                foreach (ManagementObject device in collection.Cast<ManagementObject>())
                {
                    try
                    {
                        string? pnpDeviceId = device.GetPropertyValue("PNPDeviceID") as string;
                        string? description = device.GetPropertyValue("Description") as string
                                            ?? device.GetPropertyValue("Name") as string
                                            ?? "Unknown Device";

                        if (string.IsNullOrEmpty(pnpDeviceId))
                        {
                            continue;
                        }

                        // Parse VID and PID from PNPDeviceID (format: USB\VID_XXXX&PID_YYYY\...)
                        string vid = "";
                        string pid = "";

                        int vidIndex = pnpDeviceId.IndexOf("VID_", StringComparison.OrdinalIgnoreCase);
                        if (vidIndex >= 0 && vidIndex + 8 <= pnpDeviceId.Length)
                        {
                            vid = pnpDeviceId.Substring(vidIndex + 4, 4);
                        }

                        int pidIndex = pnpDeviceId.IndexOf("PID_", StringComparison.OrdinalIgnoreCase);
                        if (pidIndex >= 0 && pidIndex + 8 <= pnpDeviceId.Length)
                        {
                            pid = pnpDeviceId.Substring(pidIndex + 4, 4);
                        }

                        if (string.IsNullOrEmpty(vid) || string.IsNullOrEmpty(pid))
                        {
                            continue;
                        }

                        ListViewItem item = new(description);
                        _ = item.SubItems.Add(vid);
                        _ = item.SubItems.Add(pid);
                        _ = item.SubItems.Add(pnpDeviceId);
                        item.Tag = new DetectedUsbDevice { Vid = vid, Pid = pid, Description = description };
                        _ = _deviceListView.Items.Add(item);
                    }
                    finally
                    {
                        device?.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(
                    $"Erreur lors de la détection des périphériques : {ex.Message}",
                    "Erreur",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void OnSelectClick(object? sender, EventArgs e)
        {
            if (_deviceListView.SelectedItems.Count == 0)
            {
                return;
            }

            if (_layoutCombo.SelectedItem == null)
            {
                _ = MessageBox.Show("Veuillez sélectionner une disposition clavier.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ListViewItem selectedItem = _deviceListView.SelectedItems[0];
            if (selectedItem.Tag is not DetectedUsbDevice deviceInfo)
            {
                return;
            }
            InstalledKeyboardLayout selectedLayout = (InstalledKeyboardLayout)_layoutCombo.SelectedItem;

            SelectedMapping = new UsbDeviceMapping
            {
                DeviceName = deviceInfo.Description,
                VendorId = deviceInfo.Vid,
                ProductId = deviceInfo.Pid,
                LayoutId = selectedLayout.LayoutId,
                LayoutDisplayName = selectedLayout.DisplayName
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// Represents a detected USB device
        /// </summary>
        private sealed class DetectedUsbDevice
        {
            public required string Vid { get; init; }
            public required string Pid { get; init; }
            public required string Description { get; init; }
        }
    }
}
