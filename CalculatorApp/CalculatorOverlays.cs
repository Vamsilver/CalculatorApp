using System.Globalization;

namespace CalculatorApp;

public sealed partial class CalculatorForm
{
    private void ToggleHistory()
    {
        _historyOpen = !_historyOpen;
        UpdateDimmingTarget();
        if (_historyOpen)
        {
            _historyPanel.Visible = true;
            _historyPanel.BringToFront();
        }
        _historyTimer.Start();
    }

    private void AnimateHistory(object? sender, EventArgs e)
    {
        const float expandedHeight = ExpandedHistoryHeight;
        const float step = 14;
        var target = _historyOpen ? expandedHeight : 0;
        var current = _historyPanel.Height;

        if (Math.Abs(current - target) <= step)
        {
            _historyPanel.Height = (int)target;
            _historyTimer.Stop();
            if (!_historyOpen) _historyPanel.Visible = false;
        }
        else
        {
            _historyPanel.Height = (int)(current + (_historyOpen ? step : -step));
        }

        LayoutHistoryPanel();
    }

    private void LayoutHistoryPanel()
    {
        const int margin = 3;
        _historyPanel.SetBounds(
            margin,
            ClientSize.Height - _historyPanel.Height - margin,
            Math.Max(1, ClientSize.Width - margin * 2),
            _historyPanel.Height);
    }

    private void UseSelectedHistoryResult()
    {
        if (_history.SelectedItem is not string entry) return;

        var equalsIndex = entry.LastIndexOf('=');
        if (equalsIndex < 0) return;

        var resultText = entry[(equalsIndex + 1)..].Trim();
        var expressionText = entry[..equalsIndex].Trim();
        var styles = NumberStyles.Float | NumberStyles.AllowThousands;
        var parsed = double.TryParse(resultText, styles, CultureInfo.CurrentCulture, out var value)
            || double.TryParse(resultText, styles, CultureInfo.InvariantCulture, out value);

        if (!parsed || !double.IsFinite(value))
        {
            MessageBox.Show("Не удалось прочитать результат выбранной операции.", "История", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _engine.Clear();
        _display.Text = FormatValue(value);
        _expressionLabel.Text = string.IsNullOrWhiteSpace(expressionText)
            ? "Результат из истории"
            : $"{expressionText} =";
        _calculationExpression = string.Empty;
        _expressionHasCurrentValue = true;
        _startNewNumber = true;

        if (_historyOpen)
        {
            _historyOpen = false;
            UpdateDimmingTarget();
            _historyTimer.Start();
        }
    }

    private void FitDisplayText()
    {
        if (_fittingDisplay || _display.ClientSize.Width <= 0) return;

        _fittingDisplay = true;
        try
        {
            const float maximumSize = 34F;
            const float minimumSize = 16F;
            var availableWidth = Math.Max(1, _display.ClientSize.Width - 12);
            var selectedSize = minimumSize;

            for (var size = maximumSize; size >= minimumSize; size -= 1F)
            {
                using var candidateFont = new Font("Segoe UI", size, FontStyle.Regular);
                var measuredWidth = TextRenderer.MeasureText(
                    _display.Text,
                    candidateFont,
                    new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;

                if (measuredWidth <= availableWidth)
                {
                    selectedSize = size;
                    break;
                }
            }

            if (Math.Abs(_display.Font.Size - selectedSize) > 0.1F)
            {
                var previousFont = _display.Font;
                _display.Font = new Font("Segoe UI", selectedSize, FontStyle.Regular);
                previousFont.Dispose();
            }
        }
        finally
        {
            _fittingDisplay = false;
        }
    }

    private void ToggleSidebar()
    {
        _sidebarOpen = !_sidebarOpen;
        UpdateDimmingTarget();
        _sidebarTimer.Start();
        _toolTip.SetToolTip(_menuButton, _sidebarOpen ? "Закрыть боковую панель" : "Открыть боковую панель");
    }

    private void DismissOpenPanels()
    {
        var changed = false;
        if (_sidebarOpen)
        {
            _sidebarOpen = false;
            _sidebarTimer.Start();
            _toolTip.SetToolTip(_menuButton, "Открыть боковую панель");
            changed = true;
        }
        if (_historyOpen)
        {
            _historyOpen = false;
            _historyTimer.Start();
            changed = true;
        }
        if (changed) UpdateDimmingTarget();
    }

    private void AnimateSidebar(object? sender, EventArgs e)
    {
        const float expandedWidth = ExpandedSidebarWidth;
        const float step = 18;
        var target = _sidebarOpen ? expandedWidth : 0;
        var current = _sidePanel.Width;

        if (Math.Abs(current - target) <= step)
        {
            _sidePanel.Width = (int)target;
            _sidebarTimer.Stop();
        }
        else
        {
            _sidePanel.Width = (int)(current + (_sidebarOpen ? step : -step));
        }

        _sidePanel.BringToFront();
    }

    private static Label SidebarSection(string text) => new()
    {
        Text = text,
        Size = new Size(232, 32),
        Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
        ForeColor = Color.DimGray,
        TextAlign = ContentAlignment.BottomLeft,
        Padding = new Padding(10, 0, 0, 4),
        Margin = new Padding(0, 6, 0, 0)
    };

    private static Button SidebarButton(string glyph, string text, EventHandler handler)
    {
        var button = new Button
        {
            Text = text,
            Tag = glyph,
            Image = CreateFluentIcon(glyph, SystemColors.ControlText),
            ImageAlign = ContentAlignment.MiddleLeft,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            Size = new Size(232, 46),
            Font = new Font("Segoe UI", 11F),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 2, 0, 2),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(225, 225, 225);
        button.Click += handler;
        return button;
    }

    private static Bitmap CreateFluentIcon(string glyph, Color color)
    {
        var bitmap = new Bitmap(28, 28);
        using var graphics = Graphics.FromImage(bitmap);
        using var font = new Font("Segoe Fluent Icons", 15F);
        using var brush = new SolidBrush(color);
        graphics.Clear(Color.Transparent);
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        graphics.DrawString(glyph, font, brush, new PointF(2, 2));
        return bitmap;
    }
}
