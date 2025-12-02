using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace KeyboardAutoSwitcher.Resources;

/// <summary>
/// Generates icons programmatically for the system tray
/// </summary>
public static class IconGenerator
{
    private const int IconSize = 32;

    /// <summary>
    /// Creates an icon with the specified text (keyboard layout indicator)
    /// </summary>
    public static Icon CreateLayoutIcon(string text, Color backgroundColor, Color textColor)
    {
        using var bitmap = new Bitmap(IconSize, IconSize);
        using var graphics = Graphics.FromImage(bitmap);
        
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        
        // Draw rounded rectangle background
        using var backgroundBrush = new SolidBrush(backgroundColor);
        using var path = CreateRoundedRectangle(1, 1, IconSize - 2, IconSize - 2, 6);
        graphics.FillPath(backgroundBrush, path);
        
        // Draw border
        using var borderPen = new Pen(Color.FromArgb(60, 0, 0, 0), 1);
        graphics.DrawPath(borderPen, path);
        
        // Draw text
        using var font = new Font("Segoe UI", 11, FontStyle.Bold);
        using var textBrush = new SolidBrush(textColor);
        
        var textSize = graphics.MeasureString(text, font);
        float x = (IconSize - textSize.Width) / 2;
        float y = (IconSize - textSize.Height) / 2;
        
        graphics.DrawString(text, font, textBrush, x, y);
        
        return Icon.FromHandle(bitmap.GetHicon());
    }

    /// <summary>
    /// Creates a Dvorak layout icon (green with "DV")
    /// </summary>
    public static Icon CreateDvorakIcon()
    {
        return CreateLayoutIcon("DV", Color.FromArgb(76, 175, 80), Color.White);
    }

    /// <summary>
    /// Creates an AZERTY layout icon (blue with "AZ")
    /// </summary>
    public static Icon CreateAzertyIcon()
    {
        return CreateLayoutIcon("AZ", Color.FromArgb(33, 150, 243), Color.White);
    }

    /// <summary>
    /// Creates a default/unknown layout icon (gray with "?")
    /// </summary>
    public static Icon CreateDefaultIcon()
    {
        return CreateLayoutIcon("KB", Color.FromArgb(158, 158, 158), Color.White);
    }

    private static GraphicsPath CreateRoundedRectangle(float x, float y, float width, float height, float radius)
    {
        var path = new GraphicsPath();
        
        path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
        path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
        path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        
        return path;
    }
}
