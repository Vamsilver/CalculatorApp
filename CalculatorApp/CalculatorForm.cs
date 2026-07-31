using System.Globalization;

namespace CalculatorApp;

public sealed class CalculatorForm : Form
{
    private readonly CalculatorEngine _engine = new();
    private readonly TextBox _display = new();
    private readonly Label _modeLabel = new();
    private readonly Label _expressionLabel = new();
    private readonly ListBox _history = new();
    private readonly TableLayoutPanel _historyPanel = new();
    private readonly TableLayoutPanel _keypad = new();
    private readonly Button _themeButton = new();
    private readonly ToolTip _toolTip = new();
    private readonly TableLayoutPanel _root = new();
    private readonly FlowLayoutPanel _sidePanel = new();
    private readonly Button _menuButton = new();
    private readonly System.Windows.Forms.Timer _sidebarTimer = new() { Interval = 15 };
    private readonly System.Windows.Forms.Timer _historyTimer = new() { Interval = 15 };
    private RowStyle _historyRow = null!;
    private bool _sidebarOpen;
    private bool _historyOpen;
    private bool _fittingDisplay;
    private bool _scientificMode;
    private bool _degreesMode = true;
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
        _historyTimer.Tick += AnimateHistory;
        ApplyTheme();
    }

    private void BuildInterface()
    {
        _root.Dock = DockStyle.Fill;
        _root.Padding = new Padding(8);
        _root.ColumnCount = 1;
        _root.RowCount = 3;
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 125));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _historyRow = new RowStyle(SizeType.Absolute, 0);
        _root.RowStyles.Add(_historyRow);
        Controls.Add(_root);

        _sidePanel.Location = Point.Empty;
        _sidePanel.Size = new Size(0, ClientSize.Height);
        _sidePanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
        _sidePanel.FlowDirection = FlowDirection.TopDown;
        _sidePanel.WrapContents = false;
        _sidePanel.Margin = Padding.Empty;
        _sidePanel.Padding = new Padding(8, 10, 8, 8);
        _sidePanel.BorderStyle = BorderStyle.FixedSingle;
        var closeButton = SidebarButton("\uE700", "Калькулятор", (_, _) => ToggleSidebar());
        _toolTip.SetToolTip(closeButton, "Закрыть боковую панель");
        var modeSection = SidebarSection("РЕЖИМ");
        var standardButton = SidebarButton("\uE8EF", "Обычный", (_, _) => SwitchMode(false));
        var scientificButton = SidebarButton("\uE9D9", "Инженерный", (_, _) => SwitchMode(true));
        var fileSection = SidebarSection("ФАЙЛ И ИСТОРИЯ");
        var saveButton = SidebarButton("\uE74E", "Сохранить историю", SaveHistory);
        var loadButton = SidebarButton("\uE896", "Загрузить историю", LoadHistory);
        _toolTip.SetToolTip(saveButton, "Сохранить историю");
        _toolTip.SetToolTip(loadButton, "Загрузить историю");
        _sidePanel.Controls.Add(closeButton);
        _sidePanel.Controls.Add(modeSection);
        _sidePanel.Controls.Add(standardButton);
        _sidePanel.Controls.Add(scientificButton);
        _sidePanel.Controls.Add(fileSection);
        _sidePanel.Controls.Add(saveButton);
        _sidePanel.Controls.Add(loadButton);
        _sidePanel.Controls.Add(SidebarSection("ОФОРМЛЕНИЕ"));
        _themeButton.Text = "Тёмная тема";
        _themeButton.Tag = "\uE708";
        _themeButton.Image = CreateFluentIcon("\uE708", SystemColors.ControlText);
        _themeButton.ImageAlign = ContentAlignment.MiddleLeft;
        _themeButton.TextImageRelation = TextImageRelation.ImageBeforeText;
        _themeButton.Size = new Size(232, 46);
        _themeButton.Font = new Font("Segoe UI", 11F);
        _themeButton.TextAlign = ContentAlignment.MiddleLeft;
        _themeButton.Padding = new Padding(8, 0, 0, 0);
        _themeButton.FlatStyle = FlatStyle.Flat;
        _themeButton.FlatAppearance.BorderSize = 0;
        _themeButton.Margin = new Padding(0, 2, 0, 2);
        _themeButton.Cursor = Cursors.Hand;
        _themeButton.Click += (_, _) => { _darkTheme = !_darkTheme; ApplyTheme(); };
        _toolTip.SetToolTip(_themeButton, "Включить тёмную тему");
        _sidePanel.Controls.Add(_themeButton);
        Controls.Add(_sidePanel);

        _display.Text = "0";
        _display.ReadOnly = true;
        _display.TextAlign = HorizontalAlignment.Right;
        _display.Font = new Font("Segoe UI", 34F, FontStyle.Regular);
        _display.Dock = DockStyle.Fill;
        _display.BorderStyle = BorderStyle.None;
        _display.Margin = new Padding(4, 12, 4, 12);
        _display.TextChanged += (_, _) => FitDisplayText();
        _display.Resize += (_, _) => FitDisplayText();
        var displayPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            ColumnCount = 3,
            RowCount = 3
        };
        displayPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
        displayPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        displayPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
        displayPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        displayPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        displayPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _menuButton.Text = "\uE700";
        _menuButton.Font = new Font("Segoe Fluent Icons", 17F);
        _menuButton.FlatStyle = FlatStyle.Flat;
        _menuButton.FlatAppearance.BorderSize = 0;
        _menuButton.Dock = DockStyle.Fill;
        _menuButton.Click += (_, _) => ToggleSidebar();
        _toolTip.SetToolTip(_menuButton, "Открыть боковую панель");
        displayPanel.Controls.Add(_menuButton, 0, 0);

        _modeLabel.Text = "Обычный";
        _modeLabel.TextAlign = ContentAlignment.MiddleLeft;
        _modeLabel.Dock = DockStyle.Fill;
        _modeLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        displayPanel.Controls.Add(_modeLabel, 1, 0);

        _expressionLabel.Text = string.Empty;
        _expressionLabel.TextAlign = ContentAlignment.MiddleRight;
        _expressionLabel.Dock = DockStyle.Fill;
        _expressionLabel.Font = new Font("Segoe UI", 10F);
        _expressionLabel.ForeColor = Color.DimGray;
        _expressionLabel.AutoEllipsis = true;
        displayPanel.Controls.Add(_expressionLabel, 1, 1);
        displayPanel.SetColumnSpan(_expressionLabel, 2);

        var historyButton = new Button
        {
            Text = "\uE81C",
            Font = new Font("Segoe Fluent Icons", 16F),
            FlatStyle = FlatStyle.Flat,
            Dock = DockStyle.Fill
        };
        historyButton.FlatAppearance.BorderSize = 0;
        historyButton.Click += (_, _) => ToggleHistory();
        _toolTip.SetToolTip(historyButton, "Показать историю");
        displayPanel.Controls.Add(historyButton, 2, 0);

        displayPanel.Controls.Add(_display, 0, 2);
        displayPanel.SetColumnSpan(_display, 3);
        _root.Controls.Add(displayPanel, 0, 0);

        _keypad.Dock = DockStyle.Fill;
        _keypad.Margin = Padding.Empty;
        _root.Controls.Add(_keypad, 0, 1);
        BuildStandardKeypad();

        _historyPanel.Dock = DockStyle.Fill;
        _historyPanel.RowCount = 2;
        _historyPanel.Padding = new Padding(0, 8, 0, 0);
        _historyPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        _historyPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _historyPanel.Controls.Add(new Label
        {
            Text = "История · двойной щелчок — использовать результат",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            AutoEllipsis = true
        }, 0, 0);
        _history.Dock = DockStyle.Fill;
        _history.IntegralHeight = false;
        _history.HorizontalScrollbar = true;
        _history.DoubleClick += (_, _) => UseSelectedHistoryResult();
        _history.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            UseSelectedHistoryResult();
            e.Handled = true;
            e.SuppressKeyPress = true;
        };
        _historyPanel.Controls.Add(_history, 0, 1);
        _root.Controls.Add(_historyPanel, 0, 2);
        _sidePanel.BringToFront();
    }

    private void SwitchMode(bool scientific)
    {
        if (_scientificMode == scientific)
        {
            if (_sidebarOpen) ToggleSidebar();
            return;
        }

        _scientificMode = scientific;
        _modeLabel.Text = scientific ? "Инженерный" : "Обычный";
        ClearAll();
        if (scientific) BuildScientificKeypad();
        else BuildStandardKeypad();
        ApplyTheme();
        if (_sidebarOpen) ToggleSidebar();
    }

    private void ConfigureKeypad(int columns, int rows)
    {
        while (_keypad.Controls.Count > 0)
            _keypad.Controls[0].Dispose();
        _keypad.ColumnStyles.Clear();
        _keypad.RowStyles.Clear();
        _keypad.ColumnCount = columns;
        _keypad.RowCount = rows;
        for (var i = 0; i < columns; i++)
            _keypad.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columns));
        for (var i = 0; i < rows; i++)
            _keypad.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / rows));
    }

    private void BuildStandardKeypad()
    {
        ConfigureKeypad(4, 6);
        AddButton("%", 0, 0, (_, _) => ApplyUnary(value => $"{CalculatorEngine.Format(value)}%", value => value / 100));
        AddButton("CE", 1, 0, (_, _) => ClearEntry()); AddButton("C", 2, 0, (_, _) => ClearAll()); AddButton("⌫", 3, 0, (_, _) => Backspace());
        AddButton("1/x", 0, 1, (_, _) => ApplyUnary(value => $"1/{CalculatorEngine.Format(value)}", value => value == 0 ? throw new DivideByZeroException("Деление на ноль невозможно.") : 1 / value));
        AddButton("x²", 1, 1, (_, _) => ApplyUnary(value => $"{CalculatorEngine.Format(value)}²", value => value * value));
        AddButton("²√x", 2, 1, (_, _) => ApplySquareRoot()); AddButton("÷", 3, 1, OperationClick);
        AddButton("7", 0, 2, DigitClick); AddButton("8", 1, 2, DigitClick); AddButton("9", 2, 2, DigitClick); AddButton("×", 3, 2, OperationClick);
        AddButton("4", 0, 3, DigitClick); AddButton("5", 1, 3, DigitClick); AddButton("6", 2, 3, DigitClick); AddButton("−", 3, 3, OperationClick);
        AddButton("1", 0, 4, DigitClick); AddButton("2", 1, 4, DigitClick); AddButton("3", 2, 4, DigitClick); AddButton("+", 3, 4, OperationClick);
        AddButton("±", 0, 5, (_, _) => ToggleSign()); AddButton("0", 1, 5, DigitClick); AddButton(",", 2, 5, (_, _) => EnterDecimalSeparator());
        _equalsButton = AddButton("=", 3, 5, EqualsClick);
    }

    private void BuildScientificKeypad()
    {
        ConfigureKeypad(5, 7);
        Button? angleButton = null;
        angleButton = AddButton(_degreesMode ? "DEG" : "RAD", 0, 0, (_, _) =>
        {
            _degreesMode = !_degreesMode;
            angleButton!.Text = _degreesMode ? "DEG" : "RAD";
        });
        AddButton("π", 1, 0, (_, _) => SetConstant(Math.PI, "π")); AddButton("e", 2, 0, (_, _) => SetConstant(Math.E, "e"));
        AddButton("C", 3, 0, (_, _) => ClearAll()); AddButton("⌫", 4, 0, (_, _) => Backspace());
        AddButton("x²", 0, 1, (_, _) => ApplyUnary(value => $"{CalculatorEngine.Format(value)}²", value => value * value));
        AddButton("1/x", 1, 1, (_, _) => ApplyUnary(value => $"1/{CalculatorEngine.Format(value)}", value => value == 0 ? throw new DivideByZeroException("Деление на ноль невозможно.") : 1 / value));
        AddButton("|x|", 2, 1, (_, _) => ApplyUnary(value => $"|{CalculatorEngine.Format(value)}|", Math.Abs));
        AddButton("exp", 3, 1, (_, _) => ApplyUnary(value => $"exp({CalculatorEngine.Format(value)})", Math.Exp));
        AddButton("mod", 4, 1, (_, _) => ExecuteOperation("mod"));
        AddButton("²√x", 0, 2, (_, _) => ApplySquareRoot());
        AddButton("sin", 1, 2, (_, _) => ApplyTrig("sin", Math.Sin)); AddButton("cos", 2, 2, (_, _) => ApplyTrig("cos", Math.Cos));
        AddButton("tan", 3, 2, (_, _) => ApplyTrig("tan", Math.Tan)); AddButton("÷", 4, 2, OperationClick);
        AddButton("xʸ", 0, 3, (_, _) => ExecuteOperation("^")); AddButton("7", 1, 3, DigitClick); AddButton("8", 2, 3, DigitClick); AddButton("9", 3, 3, DigitClick); AddButton("×", 4, 3, OperationClick);
        AddButton("10ˣ", 0, 4, (_, _) => ApplyUnary(value => $"10^{CalculatorEngine.Format(value)}", value => Math.Pow(10, value)));
        AddButton("4", 1, 4, DigitClick); AddButton("5", 2, 4, DigitClick); AddButton("6", 3, 4, DigitClick); AddButton("−", 4, 4, OperationClick);
        AddButton("log", 0, 5, (_, _) => ApplyUnary(value => $"log({CalculatorEngine.Format(value)})", value => value > 0 ? Math.Log10(value) : throw new ArgumentOutOfRangeException(nameof(value), "Логарифм определён только для положительных чисел.")));
        AddButton("1", 1, 5, DigitClick); AddButton("2", 2, 5, DigitClick); AddButton("3", 3, 5, DigitClick); AddButton("+", 4, 5, OperationClick);
        AddButton("ln", 0, 6, (_, _) => ApplyUnary(value => $"ln({CalculatorEngine.Format(value)})", value => value > 0 ? Math.Log(value) : throw new ArgumentOutOfRangeException(nameof(value), "Логарифм определён только для положительных чисел.")));
        AddButton("±", 1, 6, (_, _) => ToggleSign()); AddButton("0", 2, 6, DigitClick); AddButton(",", 3, 6, (_, _) => EnterDecimalSeparator());
        _equalsButton = AddButton("=", 4, 6, EqualsClick);
    }

    private void SetConstant(double value, string name)
    {
        _display.Text = CalculatorEngine.Format(value);
        _expressionLabel.Text = name;
        _startNewNumber = true;
    }

    private void ApplyTrig(string name, Func<double, double> operation)
    {
        ApplyUnary(
            value => $"{name}({CalculatorEngine.Format(value)}{(_degreesMode ? "°" : string.Empty)})",
            value => operation(_degreesMode ? value * Math.PI / 180 : value));
    }

    private void ToggleHistory()
    {
        _historyOpen = !_historyOpen;
        _historyTimer.Start();
    }

    private void AnimateHistory(object? sender, EventArgs e)
    {
        const float expandedHeight = 175;
        const float step = 14;
        var target = _historyOpen ? expandedHeight : 0;
        var current = _historyRow.Height;

        if (Math.Abs(current - target) <= step)
        {
            _historyRow.Height = target;
            _historyTimer.Stop();
        }
        else
        {
            _historyRow.Height = current + (_historyOpen ? step : -step);
        }

        _root.PerformLayout();
    }

    private void UseSelectedHistoryResult()
    {
        if (_history.SelectedItem is not string entry) return;

        var equalsIndex = entry.LastIndexOf('=');
        if (equalsIndex < 0) return;

        var resultText = entry[(equalsIndex + 1)..].Trim();
        var styles = NumberStyles.Float | NumberStyles.AllowThousands;
        var parsed = double.TryParse(resultText, styles, CultureInfo.CurrentCulture, out var value)
            || double.TryParse(resultText, styles, CultureInfo.InvariantCulture, out value);

        if (!parsed || !double.IsFinite(value))
        {
            MessageBox.Show("Не удалось прочитать результат выбранной операции.", "История", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _engine.Clear();
        _display.Text = CalculatorEngine.Format(value);
        _expressionLabel.Text = "Результат из истории";
        _startNewNumber = true;

        if (_historyOpen)
        {
            _historyOpen = false;
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
        _sidebarTimer.Start();
        _toolTip.SetToolTip(_menuButton, _sidebarOpen ? "Закрыть боковую панель" : "Открыть боковую панель");
    }

    private void AnimateSidebar(object? sender, EventArgs e)
    {
        const float expandedWidth = 250;
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

    private void ApplyUnary(Func<double, string> expression, Func<double, double> operation)
    {
        if (!TryReadDisplay(out var value)) return;
        try
        {
            var result = operation(value);
            if (!double.IsFinite(result))
                throw new OverflowException("Результат выходит за допустимый диапазон чисел.");
            var operationText = expression(value);
            _display.Text = CalculatorEngine.Format(result);
            _expressionLabel.Text = $"{operationText} =";
            AddHistoryEntry($"{operationText} = {_display.Text}");
            _startNewNumber = true;
        }
        catch (Exception ex) when (ex is DivideByZeroException or OverflowException or ArgumentOutOfRangeException)
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

        _display.Text = CalculatorEngine.Format(Math.Sqrt(value));
        _expressionLabel.Text = $"√{CalculatorEngine.Format(value)} =";
        AddHistoryEntry($"√{CalculatorEngine.Format(value)} = {_display.Text}");
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
        => ExecuteOperation(((Button)sender!).Text);

    private void ExecuteOperation(string operation)
    {
        if (!TryReadDisplay(out var value)) return;
        var previousValue = _engine.Result;
        var previousOperation = _engine.PendingOperation;
        try
        {
            _display.Text = CalculatorEngine.Format(_engine.SelectOperation(value, operation));
            if (previousValue is not null && previousOperation is not null)
                AddHistoryEntry($"{CalculatorEngine.Format(previousValue.Value)} {previousOperation} {CalculatorEngine.Format(value)} = {_display.Text}");
            _expressionLabel.Text = $"{_display.Text} {operation}";
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
            {
                _expressionLabel.Text = $"{CalculatorEngine.Format(left.Value)} {operation} {CalculatorEngine.Format(value)} =";
                AddHistoryEntry($"{CalculatorEngine.Format(left.Value)} {operation} {CalculatorEngine.Format(value)} = {_display.Text}");
            }
            _startNewNumber = true;
        }
        catch (Exception ex) when (ex is DivideByZeroException or OverflowException)
        {
            ShowCalculationError(ex.Message);
        }
    }

    private bool TryReadDisplay(out double value)
    {
        if (double.TryParse(_display.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out value) && double.IsFinite(value)) return true;
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
        _expressionLabel.Text = string.Empty;
        _startNewNumber = true;
    }

    private void AddHistoryEntry(string entry)
    {
        _history.Items.Add(entry);
        _history.TopIndex = _history.Items.Count - 1;
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
        _equalsButton.BackColor = _darkTheme ? Color.FromArgb(0, 95, 184) : Color.FromArgb(0, 120, 215);
        _equalsButton.ForeColor = Color.White;
        _equalsButton.FlatStyle = FlatStyle.Flat;
        _equalsButton.FlatAppearance.BorderSize = 0;
        _themeButton.Text = _darkTheme ? "Светлая тема" : "Тёмная тема";
        _themeButton.FlatAppearance.MouseOverBackColor = _darkTheme
            ? Color.FromArgb(62, 67, 77)
            : Color.FromArgb(225, 225, 225);
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
