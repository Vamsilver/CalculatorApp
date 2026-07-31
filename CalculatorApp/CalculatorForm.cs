using System.Globalization;

namespace CalculatorApp;

public sealed class CalculatorForm : Form
{
    private readonly CalculatorEngine _engine = new();
    private readonly TextBox _display = new();
    private readonly ListBox _history = new();
    private readonly TableLayoutPanel _keypad = new();
    private readonly Button _themeButton = new();
    private bool _startNewNumber = true;
    private bool _darkTheme;

    public CalculatorForm()
    {
        Text = "Калькулятор";
        ClientSize = new Size(520, 570);
        MinimumSize = new Size(500, 550);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 11F);

        BuildInterface();
        ApplyTheme();
    }

    private void BuildInterface()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 3
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        Controls.Add(root);

        _display.Text = "0";
        _display.ReadOnly = true;
        _display.TextAlign = HorizontalAlignment.Right;
        _display.Font = new Font("Segoe UI", 25F, FontStyle.Bold);
        _display.Dock = DockStyle.Fill;
        _display.Margin = new Padding(4, 4, 4, 10);
        root.Controls.Add(_display, 0, 0);
        root.SetColumnSpan(_display, 2);

        _keypad.Dock = DockStyle.Fill;
        _keypad.ColumnCount = 4;
        _keypad.RowCount = 5;
        for (var i = 0; i < 4; i++) _keypad.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        for (var i = 0; i < 5; i++) _keypad.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
        root.Controls.Add(_keypad, 0, 1);

        AddButton("C", 0, 0, (_, _) => ClearAll());
        AddButton("±", 1, 0, (_, _) => ToggleSign());
        AddButton(",", 2, 0, (_, _) => EnterDecimalSeparator());
        AddButton("÷", 3, 0, OperationClick);
        AddButton("7", 0, 1, DigitClick); AddButton("8", 1, 1, DigitClick); AddButton("9", 2, 1, DigitClick); AddButton("×", 3, 1, OperationClick);
        AddButton("4", 0, 2, DigitClick); AddButton("5", 1, 2, DigitClick); AddButton("6", 2, 2, DigitClick); AddButton("−", 3, 2, OperationClick);
        AddButton("1", 0, 3, DigitClick); AddButton("2", 1, 3, DigitClick); AddButton("3", 2, 3, DigitClick); AddButton("+", 3, 3, OperationClick);
        AddButton("0", 0, 4, DigitClick, 2); AddButton("⌫", 2, 4, (_, _) => Backspace()); AddButton("=", 3, 4, EqualsClick);

        var historyPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(10, 0, 0, 0) };
        historyPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        historyPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        historyPanel.Controls.Add(new Label { Text = "История", Dock = DockStyle.Fill, Font = new Font(Font, FontStyle.Bold) }, 0, 0);
        _history.Dock = DockStyle.Fill;
        historyPanel.Controls.Add(_history, 0, 1);
        root.Controls.Add(historyPanel, 1, 1);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        actions.Controls.Add(ActionButton("Сохранить", SaveHistory));
        actions.Controls.Add(ActionButton("Загрузить", LoadHistory));
        _themeButton.Text = "Тёмная тема";
        _themeButton.AutoSize = true;
        _themeButton.Click += (_, _) => { _darkTheme = !_darkTheme; ApplyTheme(); };
        actions.Controls.Add(_themeButton);
        root.Controls.Add(actions, 0, 2);
        root.SetColumnSpan(actions, 2);
    }

    private Button ActionButton(string text, EventHandler handler)
    {
        var button = new Button { Text = text, AutoSize = true };
        button.Click += handler;
        return button;
    }

    private void AddButton(string text, int column, int row, EventHandler handler, int span = 1)
    {
        var button = new Button { Text = text, Dock = DockStyle.Fill, Margin = new Padding(4), Font = new Font("Segoe UI", 15F) };
        button.Click += handler;
        _keypad.Controls.Add(button, column, row);
        if (span > 1) _keypad.SetColumnSpan(button, span);
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
        _themeButton.Text = _darkTheme ? "Светлая тема" : "Тёмная тема";
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
