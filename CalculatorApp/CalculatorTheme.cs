using System.Globalization;

namespace CalculatorApp;

public sealed partial class CalculatorForm
{
    private void ApplyTheme()
    {
        _applyingTheme = true;
        _buttonAnimations.Clear();
        _interactionTimer.Stop();
        _displayFadeTimer.Stop();
        var background = _darkTheme ? Color.FromArgb(32, 32, 32) : Color.FromArgb(243, 243, 243);
        var foreground = _darkTheme ? Color.FromArgb(220, 220, 220) : SystemColors.ControlText;
        ApplyColors(this, background, foreground);
        _display.BackColor = background;
        _display.ForeColor = foreground;
        _expressionLabel.ForeColor = _darkTheme ? Color.FromArgb(190, 190, 190) : Color.FromArgb(90, 90, 90);
        var previousHistoryIcon = _historyButton.Image;
        _historyButton.Image = CreateFluentIcon("\uE81C", foreground);
        previousHistoryIcon?.Dispose();
        _history.BackColor = _darkTheme ? Color.FromArgb(40, 40, 40) : Color.White;
        _history.ForeColor = foreground;
        foreach (var button in _keypad.Controls.OfType<Button>())
        {
            var role = button.Tag as string;
            var buttonBackground = role switch
            {
                "number" => _darkTheme ? Color.FromArgb(59, 59, 59) : Color.FromArgb(251, 251, 251),
                "equals" => _darkTheme ? Color.FromArgb(0, 95, 184) : Color.FromArgb(0, 120, 215),
                _ => _darkTheme ? Color.FromArgb(50, 50, 50) : Color.FromArgb(247, 247, 247)
            };
            button.BackColor = buttonBackground;
            button.ForeColor = role == "equals" ? Color.White : foreground;
            button.FlatAppearance.BorderColor = _darkTheme ? Color.FromArgb(66, 66, 66) : Color.FromArgb(226, 226, 226);
            button.FlatAppearance.MouseOverBackColor = role == "equals"
                ? (_darkTheme ? Color.FromArgb(20, 110, 200) : Color.FromArgb(20, 130, 225))
                : (_darkTheme ? Color.FromArgb(72, 72, 72) : Color.FromArgb(232, 232, 232));
            RoundButton(button, 5);
        }
        foreach (var button in DescendantButtons(_scientificTools))
        {
            button.BackColor = background;
            button.ForeColor = foreground;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = _darkTheme
                ? Color.FromArgb(55, 55, 55)
                : Color.FromArgb(229, 229, 229);
        }
        _themeButton.Tag = _darkTheme ? "\uE706" : "\uE708";
        foreach (var button in _sidePanel.Controls.OfType<Button>())
        {
            button.FlatAppearance.MouseOverBackColor = _darkTheme
                ? Color.FromArgb(62, 67, 77)
                : Color.FromArgb(225, 225, 225);
            if (button.Tag is string glyph)
            {
                var previousImage = button.Image;
                button.Image = CreateFluentIcon(glyph, foreground);
                previousImage?.Dispose();
            }
        }
        foreach (var section in _sidePanel.Controls.OfType<Label>())
            section.ForeColor = _darkTheme ? Color.Silver : Color.DimGray;
        _equalsButton.FlatAppearance.BorderSize = 0;
        _themeButton.Text = _darkTheme ? "Светлая тема" : "Тёмная тема";
        _themeButton.FlatAppearance.MouseOverBackColor = _darkTheme
            ? Color.FromArgb(62, 67, 77)
            : Color.FromArgb(225, 225, 225);
        _toolTip.SetToolTip(_themeButton, _darkTheme ? "Включить светлую тему" : "Включить тёмную тему");
        ApplyRootDimming();
        _applyingTheme = false;
    }

    private static void ApplyColors(Control parent, Color background, Color foreground)
    {
        parent.BackColor = background;
        parent.ForeColor = foreground;
        foreach (Control child in parent.Controls)
        {
            if (child is Button) child.BackColor = background;
            ApplyColors(child, background, foreground);
        }
    }

    private static IEnumerable<Button> DescendantButtons(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is Button button) yield return button;
            foreach (var nested in DescendantButtons(child)) yield return nested;
        }
    }
}
