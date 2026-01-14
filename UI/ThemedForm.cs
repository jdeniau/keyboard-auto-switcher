namespace KeyboardAutoSwitcher.UI
{
    /// <summary>
    /// Base form class that automatically handles theme changes
    /// </summary>
    public class ThemedForm : Form
    {
        protected ThemeColors Theme { get; private set; }

        public ThemedForm()
        {
            Theme = ThemeHelper.GetThemeColors();

            // Start monitoring and subscribe to theme changes
            ThemeHelper.StartMonitoring();
            ThemeHelper.ThemeChanged += OnThemeChangedInternal;
            Disposed += (s, e) => ThemeHelper.ThemeChanged -= OnThemeChangedInternal;
        }

        private void OnThemeChangedInternal(object? sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnThemeChangedInternal(sender, e)));
                return;
            }

            Theme = ThemeHelper.GetThemeColors();
            ApplyThemeToForm();
            ApplyThemeToControls(Controls);
            OnThemeChanged();
        }

        /// <summary>
        /// Override this method to apply theme to specific controls
        /// </summary>
        protected virtual void OnThemeChanged()
        {
        }

        /// <summary>
        /// Applies base theme colors to the form
        /// </summary>
        protected void ApplyThemeToForm()
        {
            BackColor = Theme.Background;
            ForeColor = Theme.TextPrimary;
        }

        /// <summary>
        /// Recursively applies theme to all controls
        /// </summary>
        protected void ApplyThemeToControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                ApplyThemeToControl(control);

                if (control.HasChildren)
                {
                    ApplyThemeToControls(control.Controls);
                }
            }
        }

        /// <summary>
        /// Applies theme to a single control based on its type
        /// </summary>
        protected virtual void ApplyThemeToControl(Control control)
        {
            switch (control)
            {
                case GroupBox groupBox:
                    groupBox.ForeColor = Theme.TextPrimary;
                    break;
                case Label label:
                    label.ForeColor = Theme.TextPrimary;
                    break;
                case TextBox textBox:
                    textBox.BackColor = Theme.BackgroundToolbar;
                    textBox.ForeColor = Theme.TextPrimary;
                    break;
                case ComboBox comboBox:
                    comboBox.BackColor = Theme.BackgroundToolbar;
                    comboBox.ForeColor = Theme.TextPrimary;
                    break;
                case ListView listView:
                    ApplyThemeToListView(listView);
                    break;
                case Button button:
                    ApplyThemeToButton(button);
                    break;
                case FlowLayoutPanel flowPanel:
                    flowPanel.BackColor = Theme.Background;
                    break;
                case TableLayoutPanel tablePanel:
                    tablePanel.BackColor = Theme.Background;
                    break;
                case Panel panel:
                    panel.BackColor = Theme.Background;
                    break;
                default:
                    // Other control types don't need special theming
                    break;
            }
        }

        /// <summary>
        /// Applies theme to a ListView control with owner-drawn headers
        /// </summary>
        protected void ApplyThemeToListView(ListView listView)
        {
            listView.BackColor = Theme.BackgroundToolbar;
            listView.ForeColor = Theme.TextPrimary;
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

        /// <summary>
        /// Applies theme to a button control
        /// </summary>
        protected void ApplyThemeToButton(Button button)
        {
            // Check if it's a primary button (stored in Tag)
            bool isPrimary = button.Tag is bool b && b;

            if (isPrimary)
            {
                button.BackColor = Theme.ButtonPrimaryBackground;
                button.ForeColor = Theme.ButtonPrimaryText;
            }
            else
            {
                if (button.Enabled)
                {
                    button.BackColor = Theme.BackgroundToolbar;
                    button.ForeColor = Theme.TextPrimary;
                }
                else
                {
                    button.BackColor = Theme.ButtonDisabledBackground;
                    button.ForeColor = Theme.ButtonDisabledText;
                }
            }
            button.FlatAppearance.BorderColor = Theme.Border;
        }

        /// <summary>
        /// Creates a primary action button
        /// </summary>
        protected Button CreatePrimaryButton(string text, EventHandler? onClick = null)
        {
            Button button = CreateButtonBase(text, onClick);
            button.Tag = true; // Mark as primary
            button.BackColor = Theme.ButtonPrimaryBackground;
            button.ForeColor = Theme.ButtonPrimaryText;
            return button;
        }

        /// <summary>
        /// Creates a secondary action button
        /// </summary>
        protected Button CreateSecondaryButton(string text, EventHandler? onClick = null)
        {
            Button button = CreateButtonBase(text, onClick);
            button.Tag = false; // Mark as secondary
            button.BackColor = Theme.BackgroundToolbar;
            button.ForeColor = Theme.TextPrimary;

            // Handle disabled state colors
            button.EnabledChanged += (s, e) =>
            {
                if (s is Button btn && btn.Tag is bool isPrimary && !isPrimary)
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

        private Button CreateButtonBase(string text, EventHandler? onClick)
        {
            Button button = new()
            {
                Text = text,
                Size = new Size(ButtonFactory.StandardWidth, ButtonFactory.StandardHeight),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, ButtonFactory.Spacing, ButtonFactory.Spacing),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderColor = Theme.Border;
            button.FlatAppearance.BorderSize = 1;

            if (onClick != null)
            {
                button.Click += onClick;
            }

            return button;
        }
    }
}
