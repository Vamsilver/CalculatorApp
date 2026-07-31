using System.Globalization;

namespace CalculatorApp;

public sealed class CalculatorForm : Form
{
    private readonly CalculatorEngine _engine = new();
    private readonly TextBox _display = new();
    private readonly ListBox _history = new();
    private readonly TableLayoutPanel _keypad = new();
    private readonly Button _themeButton = new();
    private readonly ToolTip _toolTip = new();
    private readonly TableLayoutPanel _root = new();
    private readonly FlowLayoutPanel _sidePanel = new();
    private readonly Button _menuButton = new();
    private readonly System.Windows.Forms.Timer _sidebarTimer = new() { Interval = 15 };
    private ColumnStyle _sidebarColumn = null!;
    private bool _sidebarOpen;
    private Button _equalsButton = null!;
    private bool _startNewNumber = true;
    private bool _darkTheme;

    public CalculatorForm()
    {
        Text = "Калькулятор";
        ClientSize = new Size(440, 720);
        MinimumSize = new Size(420, 680);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 11F);

        BuildInterface();
        _sidebarTimer.Tick += AnimateSidebar;
        ApplyTheme();
    }

    private void BuildInterface()
    {
        _root.Dock = DockStyle.Fill;
        _root.Padding = new Padding(8);
        _root.ColumnCount = 2;
        _root.RowCount = 3;
        _sidebarColumn = new ColumnStyle(SizeType.Absolute, 0);
        _root.ColumnStyles.Add(_sidebarColumn);
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
        Controls.Add(_root);

        _sidePanel.Dock = DockStyle.Fill;
        _sidePanel.FlowDirection = FlowDirection.TopDown;
        _sidePanel.WrapContents = false;
        _sidePanel.Margin = new Padding(0, 0, 6, 0);
        _sidePanel.Padding = new Padding(3, 8, 3, 3);
        var saveButton = IconButton("\uE74E", SaveHistory);
        var loadButton = IconButton("\uE896", LoadHistory);
        _toolTip.SetToolTip(saveButton, "Сохранить историю");
        _toolTip.SetToolTip(loadButton, "Загрузить историю");
        _sidePanel.Controls.Add(saveButton);
        _sidePanel.Controls.Add(loadButton);
        _themeButton.Text = "\uE708";
        _themeButton.Size = new Size(46, 46);
        _themeButton.Font = new Font("Segoe Fluent Icons", 18F);
        _themeButton.FlatStyle = FlatStyle.Flat;
        _themeButton.Margin = new Padding(0, 4, 0, 4);
        _themeButton.Click += (_, _) => { _darkTheme = !_darkTheme; ApplyTheme(); };
        _toolTip.SetToolTip(_themeButton, "Включить тёмную тему");
        _sidePanel.Controls.Add(_themeButton);
        _root.Controls.Add(_sidePanel, 0, 0);
        _root.SetRowSpan(_sidePanel, 3);

        _display.Text = "0";
        _display.ReadOnly = true;
        _display.TextAlign = HorizontalAlignment.Right;
        _display.Font = new Font("Segoe UI", 34F, FontStyle.Regular);
        _display.Dock = DockStyle.Fill;
        _display.BorderStyle = BorderStyle.None;
        _display.Margin = new Padding(4, 12, 4, 12);
        var displayPanel = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        _menuButton.Text = "\uE700";
        _menuButton.Font = new Font("Segoe Fluent Icons", 17F);
        _menuButton.FlatStyle = FlatStyle.Flat;
        _menuButton.FlatAppearance.BorderSize = 0;
        _menuButton.Dock = DockStyle.Left;
        _menuButton.Width = 46;
        _menuButton.Click += (_, _) => ToggleSidebar();
        _toolTip.SetToolTip(_menuButton, "Открыть боковую панель");
        displayPanel.Controls.Add(_display);
        displayPanel.Controls.Add(_menuButton);
        _root.Controls.Add(displayPanel, 1, 0);

        _keypad.Dock = DockStyle.Fill;
        _keypad.ColumnCount = 4;
        _keypad.RowCount = 6;
        for (var i = 0; i < 4; i++) _keypad.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        for (var i = 0; i < 6; i++) _keypad.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / 6));
        _keypad.Margin = Padding.Empty;
        _root.Controls.Add(_keypad, 1, 1);

        AddButton("%", 0, 0, (_, _) => ApplyUnary(value => value / 100));
        AddButton("CE", 1, 0, (_, _) => ClearEntry());
        AddButton("C", 2, 0, (_, _) => ClearAll());
        AddButton("⌫", 3, 0, (_, _) => Backspace());
        AddButton("1/x", 0, 1, (_, _) => ApplyUnary(value => value == 0 ? throw new DivideByZeroException("Деление на ноль невозможно.") : 1 / value));
        AddButton("x²", 1, 1, (_, _) => ApplyUnary(value => value * value));
        AddButton("²√x", 2, 1, (_, _) => ApplySquareRoot());
        AddButton("÷", 3, 1, OperationClick);
        AddButton("7", 0, 2, DigitClick); AddButton("8", 1, 2, DigitClick); AddButton("9", 2, 2, DigitClick); AddButton("×", 3, 2, OperationClick);
        AddButton("4", 0, 3, DigitClick); AddButton("5", 1, 3, DigitClick); AddButton("6", 2, 3, DigitClick); AddButton("−", 3, 3, OperationClick);
        AddButton("1", 0, 4, DigitClick); AddButton("2", 1, 4, DigitClick); AddButton("3", 2, 4, DigitClick); AddButton("+", 3, 4, OperationClick);
        AddButton("±", 0, 5, (_, _) => ToggleSign()); AddButton("0", 1, 5, DigitClick); AddButton(",", 2, 5, (_, _) => EnterDecimalSeparator());
        _equalsButton = AddButton("=", 3, 5, EqualsClick);

        var historyPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(0, 8, 0, 0) };
        historyPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        historyPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        historyPanel.Controls.Add(new Label { Text = "История", Dock = DockStyle.Fill, Font = new Font(Font, FontStyle.Bold) }, 0, 0);
        _history.Dock = DockStyle.Fill;
        historyPanel.Controls.Add(_history, 0, 1);
        _root.Controls.Add(historyPanel, 1, 2);
    }

    private void ToggleSidebar()
    {
        _sidebarOpen = !_sidebarOpen;
        _sidebarTimer.Start();
        _toolTip.SetToolTip(_menuButton, _sidebarOpen ? "Закрыть боковую панель" : "Открыть боковую панель");
    }

    private void AnimateSidebar(object? sender, EventArgs e)
    {
        const float expandedWidth = 58;
        const float step = 6;
        var target = _sidebarOpen ? expandedWidth : 0;
        var current = _sidebarColumn.Width;

        if (Math.Abs(current - target) <= step)
        {
            _sidebarColumn.Width = target;
            _sidebarTimer.Stop();
        }
        else
        {
            _sidebarColumn.Width = current + (_sidebarOpen ? step : -step);
        }

        _root.PerformLayout();
    }

    private static Button IconButton(string icon, EventHandler handler)
    {
        var button = new Button
        {
            Text = icon,
            Size = new Size(46, 46),
            Font = new Font("Segoe Fluent Icons", 18F),
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 4, 0, 4)
        };
        button.Click += handler;
        return button;
    }

    private Button AddButton(string text, int column, int row, EventHandler handler, int span = 1)
    {
        var button = new Button { Text = text, Dock = DockStyle.Fill, Margin = new Padding(4), Font = new Font("Segoe UI", 15F) };
        button.Click += handler;
        _keypad.Controls.Add(button, column, row);
        if (span > 1) _keypad.SetColumnSpan(button, span);
        return button;
    }

    private void ClearEntry()
    {
        _display.Text = "0";
        _startNewNumber = true;
    }

    private void ApplyUnary(Func<decimal, decimal> operation)
    {
        if (!TryReadDisplay(out var value)) return;
        try
        {
            _display.Text = CalculatorEngine.Format(operation(value));
            _startNewNumber = true;
        }
        catch (Exception ex) when (ex is DivideByZeroException or OverflowException)
        {
            ShowCalculationError(ex.Message);
        }
    }

    private void ApplySquareRoot()
    {
        if (!TryReadDisplay(out var value)) return;
        if (value < 0)
        {
            ShowCalculationError("Нельзя извлечь квадратный корень из отрицательного числа.");
            return;
        }

        _display.Text = CalculatorEngine.Format((decimal)Math.Sqrt((double)value));
        _startNewNumber = true;
    }

    private void DigitClick(object? sender, EventArgs e)
    {
        var digit = ((Button)sender!).Text;
        if (_startNewNumber || _display.Text == "0") _display.Text = digit;
        else if (_display.Text.Length < 28) _display.Text += digit;
        _startNewNumber = false;
    }

    private void EnterDecimalSeparator()
    {
        var separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        if (_startNewNumber) { _display.Text = "0" + separator; _startNewNumber = false; }
        else if (!_display.Text.Contains(separator)) _display.Text += separator;
    }

    private void ToggleSign()
    {
        if (!TryReadDisplay(out var value)) return;
        _display.Text = CalculatorEngine.Format(-value);
    }

    private void Backspace()
    {
        if (_startNewNumber) return;
        _display.Text = _display.Text.Length > 1 ? _display.Text[..^1] : "0";
    }

    private void OperationClick(object? sender, EventArgs e)
    {
        if (!TryReadDisplay(out var value)) return;
        var operation = ((Button)sender!).Text;
        try
        {
            _display.Text = CalculatorEngine.Format(_engine.SelectOperation(value, operation));
            _startNewNumber = true;
        }
        catch (Exception ex) when (ex is DivideByZeroException or OverflowException)
        {
            ShowCalculationError(ex.Message);
        }
    }

    private void EqualsClick(object? sender, EventArgs e)
    {
        if (!TryReadDisplay(out var value)) return;
        var left = _engine.Result;
        var operation = _engine.PendingOperation;
        try
        {
            var result = _engine.Equals(value);
            _display.Text = CalculatorEngine.Format(result);
            if (left is not null && operation is not null)
                _history.Items.Add($"{CalculatorEngine.Format(left.Value)} {operation} {CalculatorEngine.Format(value)} = {_display.Text}");
            _startNewNumber = true;
        }
        catch (Exception ex) when (ex is DivideByZeroException or OverflowException)
        {
            ShowCalculationError(ex.Message);
        }
    }

    private bool TryReadDisplay(out decimal value)
    {
        if (decimal.TryParse(_display.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)) return true;
        MessageBox.Show("На дисплее находится некорректное число.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        ClearAll();
        return false;
    }

    private void ShowCalculationError(string message)
    {
        MessageBox.Show(message, "Ошибка вычисления", MessageBoxButtons.OK, MessageBoxIcon.Error);
        ClearAll();
    }

    private void ClearAll()
    {
        _engine.Clear();
        _display.Text = "0";
        _startNewNumber = true;
    }

    private void SaveHistory(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog { Filter = "Текстовый файл (*.txt)|*.txt", FileName = "calculator-history.txt" };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        try { File.WriteAllLines(dialog.FileName, _history.Items.Cast<string>()); }
        catch (Exception ex) { MessageBox.Show($"Не удалось сохранить историю: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void LoadHistory(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = "Текстовый файл (*.txt)|*.txt" };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        try { _history.Items.Clear(); _history.Items.AddRange(File.ReadAllLines(dialog.FileName)); }
        catch (Exception ex) { MessageBox.Show($"Не удалось загрузить историю: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ApplyTheme()
    {
        var background = _darkTheme ? Color.FromArgb(35, 39, 47) : SystemColors.Control;
        var foreground = _darkTheme ? Color.WhiteSmoke : SystemColors.ControlText;
        ApplyColors(this, background, foreground);
        _display.BackColor = _darkTheme ? Color.FromArgb(22, 25, 30) : Color.White;
        _display.ForeColor = foreground;
        _history.BackColor = _display.BackColor;
        _history.ForeColor = foreground;
        _equalsButton.BackColor = _darkTheme ? Color.FromArgb(0, 95, 184) : Color.FromArgb(0, 120, 215);
        _equalsButton.ForeColor = Color.White;
        _equalsButton.FlatStyle = FlatStyle.Flat;
        _equalsButton.FlatAppearance.BorderSize = 0;
        _themeButton.Text = _darkTheme ? "\uE706" : "\uE708";
        _toolTip.SetToolTip(_themeButton, _darkTheme ? "Включить светлую тему" : "Включить тёмную тему");
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
}
