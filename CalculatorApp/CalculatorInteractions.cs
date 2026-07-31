using System.Globalization;

namespace CalculatorApp;

public sealed partial class CalculatorForm
{
    private Button AddButton(string text, int column, int row, EventHandler handler, int span = 1)
    {
        var isNumberKey = text.All(char.IsDigit) || text is "," or "±";
        var fontSize = _scientificMode ? (text.Length > 4 ? 9F : 11F) : 12F;
        var button = new Button
        {
            Text = text,
            Tag = text == "=" ? "equals" : isNumberKey ? "number" : "function",
            Dock = DockStyle.Fill,
            Margin = new Padding(2),
            Font = new Font("Segoe UI Variable Text", fontSize),
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(0, 0, 0, 2),
            UseCompatibleTextRendering = false,
            FlatStyle = FlatStyle.Flat,
            TabStop = false,
            CausesValidation = false,
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.Paint += (_, e) => DrawButtonBorder(button, e.Graphics);
        button.Resize += (_, _) =>
        {
            RoundButton(button, 5);
            FitKeypadButtonText(button);
        };
        button.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left || _sidebarOpen || _historyOpen) return;
            _buttonAnimations.Remove(button);
            var pressed = Blend(GetButtonBaseColor(button), _darkTheme ? Color.White : Color.Black, 0.14);
            button.BackColor = pressed;
            button.FlatAppearance.MouseDownBackColor = pressed;
            button.FlatAppearance.MouseOverBackColor = pressed;
        };
        button.MouseUp += (_, _) =>
        {
            StartButtonAnimation(button, GetButtonHoverColor(button));
            ActiveControl = null;
        };
        button.MouseLeave += (_, _) => StartButtonAnimation(button, GetButtonBaseColor(button));
        button.Click += (sender, args) =>
        {
            if (_sidebarOpen || _historyOpen)
            {
                DismissOpenPanels();
                return;
            }
            handler(sender, args);
        };
        _keypad.Controls.Add(button, column, row);
        if (span > 1) _keypad.SetColumnSpan(button, span);
        return button;
    }

    private void StartButtonAnimation(Button button, Color target, int delay = 0)
    {
        if (button.IsDisposed) return;
        _buttonAnimations[button] = new ButtonAnimation(button.BackColor, target, -delay, 9);
        _interactionTimer.Start();
    }

    private void AnimateInteractions(object? sender, EventArgs e)
    {
        foreach (var pair in _buttonAnimations.ToArray())
        {
            var button = pair.Key;
            var animation = pair.Value;
            if (button.IsDisposed)
            {
                _buttonAnimations.Remove(button);
                continue;
            }

            animation.Frame++;
            if (animation.Frame < 0) continue;
            var progress = Math.Clamp(animation.Frame / (double)animation.Duration, 0, 1);
            var eased = 1 - Math.Pow(1 - progress, 3);
            var color = Blend(animation.Start, animation.End, eased);
            button.BackColor = color;
            button.FlatAppearance.MouseOverBackColor = color;

            if (progress < 1) continue;
            button.BackColor = animation.End;
            button.FlatAppearance.MouseOverBackColor = GetButtonHoverColor(button);
            _buttonAnimations.Remove(button);
        }

        if (_buttonAnimations.Count == 0) _interactionTimer.Stop();
    }

    private void AnimateKeypadEntrance()
    {
        var delay = 0;
        foreach (var button in _keypad.Controls.OfType<Button>().OrderBy(control => _keypad.GetRow(control)).ThenBy(control => _keypad.GetColumn(control)))
        {
            var target = GetButtonBaseColor(button);
            button.BackColor = Blend(target, BackColor, 0.55);
            StartButtonAnimation(button, target, delay++);
        }
    }

    private void StartDisplayAnimation()
    {
        if (_applyingTheme) return;
        _displayFadeFrame = 0;
        _displayFadeTimer.Start();
    }

    private void AnimateDisplay(object? sender, EventArgs e)
    {
        const int duration = 9;
        _displayFadeFrame++;
        var progress = Math.Clamp(_displayFadeFrame / (double)duration, 0, 1);
        var eased = 1 - Math.Pow(1 - progress, 3);
        var foreground = _darkTheme ? Color.FromArgb(220, 220, 220) : SystemColors.ControlText;
        _display.ForeColor = Blend(_display.BackColor, foreground, 0.42 + eased * 0.58);
        if (progress < 1) return;
        _display.ForeColor = foreground;
        _displayFadeTimer.Stop();
    }

    private void UpdateDimmingTarget()
    {
        var overlayOpen = _sidebarOpen || _historyOpen;
        _targetDimAmount = overlayOpen ? 0.24 : 0;
        _dimmingTimer.Start();
    }

    private void AnimateDimming(object? sender, EventArgs e)
    {
        const double step = 0.035;
        var difference = _targetDimAmount - _dimAmount;
        if (Math.Abs(difference) <= step)
        {
            _dimAmount = _targetDimAmount;
            _dimmingTimer.Stop();
        }
        else
        {
            _dimAmount += Math.Sign(difference) * step;
        }

        ApplyRootDimming();
        if (_dimAmount <= 0 && _pendingKeypadEntrance)
        {
            _pendingKeypadEntrance = false;
            AnimateKeypadEntrance();
        }
    }

    private void ApplyRootDimming()
    {
        var baseBackground = _darkTheme ? Color.FromArgb(32, 32, 32) : Color.FromArgb(243, 243, 243);
        var dimmedBackground = Blend(baseBackground, Color.Black, _dimAmount);
        SetBackgroundRecursive(_root, dimmedBackground);

        foreach (var button in _keypad.Controls.OfType<Button>())
        {
            var color = Blend(GetButtonBaseColor(button), Color.Black, _dimAmount);
            button.BackColor = color;
            button.FlatAppearance.MouseOverBackColor = _dimAmount <= 0
                ? GetButtonHoverColor(button)
                : color;
        }
        foreach (var button in DescendantButtons(_scientificTools))
        {
            button.BackColor = dimmedBackground;
            button.FlatAppearance.MouseOverBackColor = _dimAmount <= 0
                ? (_darkTheme ? Color.FromArgb(55, 55, 55) : Color.FromArgb(229, 229, 229))
                : dimmedBackground;
        }
    }

    private static void SetBackgroundRecursive(Control parent, Color color)
    {
        parent.BackColor = color;
        foreach (Control child in parent.Controls)
            SetBackgroundRecursive(child, color);
    }

    private Color GetButtonBaseColor(Button button) => (button.Tag as string) switch
    {
        "number" => _darkTheme ? Color.FromArgb(59, 59, 59) : Color.FromArgb(251, 251, 251),
        "equals" => _darkTheme ? Color.FromArgb(0, 95, 184) : Color.FromArgb(0, 120, 215),
        _ => _darkTheme ? Color.FromArgb(50, 50, 50) : Color.FromArgb(247, 247, 247)
    };

    private Color GetButtonHoverColor(Button button) => button.Tag as string == "equals"
        ? (_darkTheme ? Color.FromArgb(20, 110, 200) : Color.FromArgb(20, 130, 225))
        : (_darkTheme ? Color.FromArgb(72, 72, 72) : Color.FromArgb(232, 232, 232));

    private static Color Blend(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            (int)Math.Round(from.A + (to.A - from.A) * amount),
            (int)Math.Round(from.R + (to.R - from.R) * amount),
            (int)Math.Round(from.G + (to.G - from.G) * amount),
            (int)Math.Round(from.B + (to.B - from.B) * amount));
    }

    private static void FitKeypadButtonText(Button button)
    {
        if (button.ClientSize.Width <= 0 || button.ClientSize.Height <= 0) return;
        var maximumSize = button.Text.Length > 4 ? 9F : 12F;
        var selectedSize = 8F;

        for (var size = maximumSize; size >= 8F; size -= 0.5F)
        {
            using var candidate = new Font("Segoe UI", size);
            var measured = TextRenderer.MeasureText(
                button.Text,
                candidate,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            if (measured.Width <= button.ClientSize.Width - 10 && measured.Height <= button.ClientSize.Height - 12)
            {
                selectedSize = size;
                break;
            }
        }

        if (Math.Abs(button.Font.Size - selectedSize) <= 0.1F) return;
        var previousFont = button.Font;
        button.Font = new Font("Segoe UI", selectedSize);
        previousFont.Dispose();
    }

    private void FitHistoryTitle()
    {
        if (_historyTitleLabel.ClientSize.Width <= 0) return;
        string[] variants =
        {
            "История · двойной щелчок — использовать результат",
            "История · двойной щелчок — выбрать",
            "История вычислений",
            "История"
        };

        _historyTitleLabel.Text = variants[^1];
        foreach (var variant in variants)
        {
            var width = TextRenderer.MeasureText(
                variant,
                _historyTitleLabel.Font,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
            if (width > _historyTitleLabel.ClientSize.Width - 4) continue;
            _historyTitleLabel.Text = variant;
            break;
        }
    }

    private static void RoundButton(Control control, int radius)
    {
        if (control.Width <= 0 || control.Height <= 0) return;
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        var diameter = radius * 2;
        var bounds = new Rectangle(0, 0, control.Width, control.Height);
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        var previousRegion = control.Region;
        control.Region = new Region(path);
        previousRegion?.Dispose();
    }

    private void DrawButtonBorder(Button button, Graphics graphics)
    {
        if (button.Tag as string == "equals" || button.Width < 3 || button.Height < 3) return;

        var previousSmoothing = graphics.SmoothingMode;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        const float inset = 0.75F;
        const float radius = 5F;
        var bounds = new RectangleF(inset, inset, button.Width - inset * 2 - 1, button.Height - inset * 2 - 1);
        var diameter = radius * 2;
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        using var pen = new Pen(
            _darkTheme ? Color.FromArgb(82, 82, 82) : Color.FromArgb(198, 198, 198),
            1.15F);
        graphics.DrawPath(pen, path);
        graphics.SmoothingMode = previousSmoothing;
    }
}
