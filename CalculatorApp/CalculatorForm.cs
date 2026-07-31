using System.Globalization;

namespace CalculatorApp;

public sealed class CalculatorForm : Form
{
    private readonly CalculatorEngine _engine = new();
    private readonly TextBox _display = new();
    private readonly Label _modeLabel = new();
    private readonly Label _expressionLabel = new();
    private readonly Label _historyTitleLabel = new();
    private readonly ListBox _history = new();
    private readonly TableLayoutPanel _historyPanel = new();
    private readonly TableLayoutPanel _scientificTools = new();
    private readonly FlowLayoutPanel _formatBar = new();
    private readonly TableLayoutPanel _memoryBar = new();
    private readonly FlowLayoutPanel _functionBar = new();
    private readonly TableLayoutPanel _keypad = new();
    private readonly Button _themeButton = new();
    private readonly ToolTip _toolTip = new();
    private readonly TableLayoutPanel _root = new();
    private readonly FlowLayoutPanel _sidePanel = new();
    private readonly Button _menuButton = new();
    private readonly Button _historyButton = new();
    private readonly System.Windows.Forms.Timer _sidebarTimer = new() { Interval = 15 };
    private readonly System.Windows.Forms.Timer _historyTimer = new() { Interval = 15 };
    private readonly System.Windows.Forms.Timer _interactionTimer = new() { Interval = 16 };
    private readonly System.Windows.Forms.Timer _displayFadeTimer = new() { Interval = 16 };
    private readonly System.Windows.Forms.Timer _dimmingTimer = new() { Interval = 16 };
    private readonly Dictionary<Button, ButtonAnimation> _buttonAnimations = new();
    private RowStyle _scientificToolsRow = null!;
    private bool _sidebarOpen;
    private bool _historyOpen;
    private bool _fittingDisplay;
    private bool _scientificMode;
    private bool _degreesMode = true;
    private bool _forceScientific;
    private bool _secondFunctions;
    private double _memory;
    private bool _hasMemory;
    private readonly Stack<(double? Accumulator, string? Operation)> _parentheses = new();
    private string _calculationExpression = string.Empty;
    private bool _expressionHasCurrentValue;
    private int _displayFadeFrame;
    private bool _applyingTheme;
    private double _dimAmount;
    private double _targetDimAmount;
    private bool _pendingKeypadEntrance;

    private sealed class ButtonAnimation(Color start, Color end, int frame, int duration)
    {
        public Color Start { get; } = start;
        public Color End { get; } = end;
        public int Frame { get; set; } = frame;
        public int Duration { get; } = duration;
    }
    private Button _equalsButton = null!;
    private bool _startNewNumber = true;
    private bool _darkTheme;

    public CalculatorForm()
    {
        Text = "Калькулятор";
        ClientSize = new Size(320, 520);
        MinimumSize = new Size(320, 520);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI Variable Text", 10F);

        BuildInterface();
        _sidebarTimer.Tick += AnimateSidebar;
        _historyTimer.Tick += AnimateHistory;
        _interactionTimer.Tick += AnimateInteractions;
        _displayFadeTimer.Tick += AnimateDisplay;
        _dimmingTimer.Tick += AnimateDimming;
        Resize += (_, _) => LayoutHistoryPanel();
        ApplyTheme();
        AnimateKeypadEntrance();
    }

    private void BuildInterface()
    {
        _root.Dock = DockStyle.Fill;
        _root.Padding = new Padding(3);
        _root.ColumnCount = 1;
        _root.RowCount = 3;
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 125));
        _scientificToolsRow = new RowStyle(SizeType.Absolute, 34);
        _root.RowStyles.Add(_scientificToolsRow);
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
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
        _display.Margin = new Padding(4, 1, 4, 7);
        _display.TextChanged += (_, _) =>
        {
            FitDisplayText();
            StartDisplayAnimation();
        };
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
        _modeLabel.Font = new Font("Segoe UI Variable Display", 14F, FontStyle.Bold);
        displayPanel.Controls.Add(_modeLabel, 1, 0);

        _expressionLabel.Text = string.Empty;
        _expressionLabel.TextAlign = ContentAlignment.BottomRight;
        _expressionLabel.Dock = DockStyle.Fill;
        _expressionLabel.Font = new Font("Segoe UI", 10F);
        _expressionLabel.ForeColor = Color.DimGray;
        _expressionLabel.AutoEllipsis = true;
        displayPanel.Controls.Add(_expressionLabel, 1, 1);
        displayPanel.SetColumnSpan(_expressionLabel, 2);

        _historyButton.Text = string.Empty;
        _historyButton.Image = CreateFluentIcon("\uE81C", SystemColors.ControlText);
        _historyButton.ImageAlign = ContentAlignment.MiddleCenter;
        _historyButton.FlatStyle = FlatStyle.Flat;
        _historyButton.Dock = DockStyle.Fill;
        _historyButton.Margin = Padding.Empty;
        _historyButton.Padding = Padding.Empty;
        _historyButton.FlatAppearance.BorderSize = 0;
        _historyButton.Click += (_, _) => ToggleHistory();
        _toolTip.SetToolTip(_historyButton, "Показать историю");
        displayPanel.Controls.Add(_historyButton, 2, 0);

        displayPanel.Controls.Add(_display, 0, 2);
        displayPanel.SetColumnSpan(_display, 3);
        _root.Controls.Add(displayPanel, 0, 0);

        BuildScientificTools();
        _root.Controls.Add(_scientificTools, 0, 1);

        _keypad.Dock = DockStyle.Fill;
        _keypad.Margin = Padding.Empty;
        _root.Controls.Add(_keypad, 0, 2);
        BuildStandardKeypad();

        _historyPanel.Dock = DockStyle.None;
        _historyPanel.RowCount = 2;
        _historyPanel.Padding = new Padding(0, 8, 0, 0);
        _historyPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        _historyPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _historyTitleLabel.Text = "История";
        _historyTitleLabel.Dock = DockStyle.Fill;
        _historyTitleLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _historyTitleLabel.AutoEllipsis = false;
        _historyTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
        _historyTitleLabel.SizeChanged += (_, _) => FitHistoryTitle();
        _toolTip.SetToolTip(_historyTitleLabel, "Дважды щёлкните по строке, чтобы использовать результат");
        _historyPanel.Controls.Add(_historyTitleLabel, 0, 0);
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
        _historyPanel.Visible = false;
        Controls.Add(_historyPanel);
        LayoutHistoryPanel();
        _sidePanel.BringToFront();
    }

    private void BuildScientificTools()
    {
        _scientificTools.Dock = DockStyle.Fill;
        _scientificTools.Margin = Padding.Empty;
        _scientificTools.ColumnCount = 1;
        _scientificTools.RowCount = 3;
        _scientificTools.RowStyles.Add(new RowStyle(SizeType.Percent, 28));
        _scientificTools.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
        _scientificTools.RowStyles.Add(new RowStyle(SizeType.Percent, 42));

        _formatBar.Dock = DockStyle.Fill;
        _formatBar.Margin = Padding.Empty;
        _formatBar.WrapContents = false;
        Button? angleButton = null;
        angleButton = ToolButton(_degreesMode ? "DEG" : "RAD", (_, _) =>
        {
            _degreesMode = !_degreesMode;
            angleButton!.Text = _degreesMode ? "DEG" : "RAD";
        });
        _formatBar.Controls.Add(angleButton);
        _formatBar.Controls.Add(ToolButton("F-E", (_, _) => ToggleScientificFormat()));
        _scientificTools.Controls.Add(_formatBar, 0, 0);

        _memoryBar.Dock = DockStyle.Fill;
        _memoryBar.Margin = Padding.Empty;
        _memoryBar.ColumnCount = 6;
        _memoryBar.RowCount = 1;
        for (var i = 0; i < 6; i++) _memoryBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 6));
        _memoryBar.Controls.Add(ToolButton("MC", (_, _) => ClearMemory()), 0, 0);
        _memoryBar.Controls.Add(ToolButton("MR", (_, _) => RecallMemory()), 1, 0);
        _memoryBar.Controls.Add(ToolButton("M+", (_, _) => ChangeMemory(1)), 2, 0);
        _memoryBar.Controls.Add(ToolButton("M−", (_, _) => ChangeMemory(-1)), 3, 0);
        _memoryBar.Controls.Add(ToolButton("MS", (_, _) => StoreMemory()), 4, 0);
        _memoryBar.Controls.Add(ToolButton("M⌄", (_, _) => RecallMemory()), 5, 0);
        _scientificTools.Controls.Add(_memoryBar, 0, 1);

        _functionBar.Dock = DockStyle.Fill;
        _functionBar.Margin = Padding.Empty;
        _functionBar.WrapContents = false;
        Button? trigButton = null;
        trigButton = ToolButton("△  Тригонометрия  ⌄", (_, _) => ShowTrigMenu(trigButton!), true);
        Button? functionsButton = null;
        functionsButton = ToolButton("ƒ  Функции  ⌄", (_, _) => ShowFunctionsMenu(functionsButton!), true);
        _functionBar.Controls.Add(trigButton);
        _functionBar.Controls.Add(functionsButton);
        _scientificTools.Controls.Add(_functionBar, 0, 2);
        SetToolPanelMode(false);
    }

    private void SetToolPanelMode(bool scientific)
    {
        _scientificToolsRow.Height = scientific ? 108 : 34;
        _formatBar.Visible = scientific;
        _functionBar.Visible = scientific;
        _scientificTools.RowStyles[0].SizeType = SizeType.Absolute;
        _scientificTools.RowStyles[0].Height = scientific ? 30 : 0;
        _scientificTools.RowStyles[1].SizeType = SizeType.Absolute;
        _scientificTools.RowStyles[1].Height = 34;
        _scientificTools.RowStyles[2].SizeType = SizeType.Percent;
        _scientificTools.RowStyles[2].Height = scientific ? 100 : 0;
    }

    private static Button ToolButton(string text, EventHandler handler, bool wide = false)
    {
        var button = new Button
        {
            Text = text,
            Dock = wide ? DockStyle.None : DockStyle.Fill,
            AutoSize = wide,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F),
            Margin = new Padding(2, 1, 2, 1),
            Padding = wide ? new Padding(6, 0, 6, 0) : Padding.Empty
        };
        button.FlatAppearance.BorderSize = 0;
        button.Paint += (_, e) => DrawButtonBorder(button, e.Graphics);
        button.Click += handler;
        return button;
    }

    private void SwitchMode(bool scientific)
    {
        if (_scientificMode == scientific)
        {
            if (_sidebarOpen) ToggleSidebar();
            return;
        }

        _scientificMode = scientific;
        SetToolPanelMode(scientific);
        _modeLabel.Text = scientific ? "Инженерный" : "Обычный";
        ClearAll();
        if (scientific) BuildScientificKeypad();
        else BuildStandardKeypad();
        ApplyTheme();
        if (_sidebarOpen)
        {
            _pendingKeypadEntrance = true;
            ToggleSidebar();
        }
        else
        {
            AnimateKeypadEntrance();
        }
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
        AddButton("2nd", 0, 0, (_, _) => { _secondFunctions = !_secondFunctions; BuildScientificKeypad(); ApplyTheme(); AnimateKeypadEntrance(); });
        AddButton("π", 1, 0, (_, _) => SetConstant(Math.PI, "π")); AddButton("e", 2, 0, (_, _) => SetConstant(Math.E, "e"));
        AddButton("C", 3, 0, (_, _) => ClearAll()); AddButton("⌫", 4, 0, (_, _) => Backspace());
        AddButton(_secondFunctions ? "x³" : "x²", 0, 1, (_, _) => ApplyUnary(value => $"{FormatValue(value)}{(_secondFunctions ? "³" : "²")}", value => Math.Pow(value, _secondFunctions ? 3 : 2)));
        AddButton("1/x", 1, 1, (_, _) => ApplyUnary(value => $"1/{FormatValue(value)}", value => value == 0 ? throw new DivideByZeroException("Деление на ноль невозможно.") : 1 / value));
        AddButton("|x|", 2, 1, (_, _) => ApplyUnary(value => $"|{FormatValue(value)}|", Math.Abs));
        AddButton("exp", 3, 1, (_, _) => ApplyUnary(value => $"exp({FormatValue(value)})", Math.Exp)); AddButton("mod", 4, 1, (_, _) => ExecuteOperation("mod"));
        AddButton(_secondFunctions ? "³√x" : "²√x", 0, 2, (_, _) => ApplyRoot()); AddButton("(", 1, 2, (_, _) => OpenParenthesis());
        AddButton(")", 2, 2, (_, _) => CloseParenthesis()); AddButton("n!", 3, 2, (_, _) => ApplyFactorial()); AddButton("÷", 4, 2, OperationClick);
        AddButton("xʸ", 0, 3, (_, _) => ExecuteOperation("^")); AddButton("7", 1, 3, DigitClick); AddButton("8", 2, 3, DigitClick); AddButton("9", 3, 3, DigitClick); AddButton("×", 4, 3, OperationClick);
        AddButton(_secondFunctions ? "2ˣ" : "10ˣ", 0, 4, (_, _) => ApplyUnary(value => $"{(_secondFunctions ? 2 : 10)}^{FormatValue(value)}", value => Math.Pow(_secondFunctions ? 2 : 10, value)));
        AddButton("4", 1, 4, DigitClick); AddButton("5", 2, 4, DigitClick); AddButton("6", 3, 4, DigitClick); AddButton("−", 4, 4, OperationClick);
        AddButton("log", 0, 5, (_, _) => ApplyUnary(value => $"log({FormatValue(value)})", value => value > 0 ? Math.Log10(value) : throw new ArgumentOutOfRangeException(nameof(value), "Логарифм определён только для положительных чисел.")));
        AddButton("1", 1, 5, DigitClick); AddButton("2", 2, 5, DigitClick); AddButton("3", 3, 5, DigitClick); AddButton("+", 4, 5, OperationClick);
        AddButton("ln", 0, 6, (_, _) => ApplyUnary(value => $"ln({FormatValue(value)})", value => value > 0 ? Math.Log(value) : throw new ArgumentOutOfRangeException(nameof(value), "Логарифм определён только для положительных чисел.")));
        AddButton("±", 1, 6, (_, _) => ToggleSign()); AddButton("0", 2, 6, DigitClick); AddButton(",", 3, 6, (_, _) => EnterDecimalSeparator());
        _equalsButton = AddButton("=", 4, 6, EqualsClick);
    }

    private void SetConstant(double value, string name)
    {
        _display.Text = FormatValue(value);
        _expressionLabel.Text = name;
        _startNewNumber = true;
    }

    private void ApplyTrig(string name, Func<double, double> operation, bool inverse)
    {
        ApplyUnary(
            value => $"{name}({FormatValue(value)}{(_degreesMode && !inverse ? "°" : string.Empty)})",
            value =>
            {
                var result = operation(inverse || !_degreesMode ? value : value * Math.PI / 180);
                return inverse && _degreesMode ? result * 180 / Math.PI : result;
            });
    }

    private void ShowTrigMenu(Control owner)
    {
        var menu = new ContextMenuStrip();
        AddMenuFunction(menu, "sin", () => ApplyTrig("sin", Math.Sin, false));
        AddMenuFunction(menu, "cos", () => ApplyTrig("cos", Math.Cos, false));
        AddMenuFunction(menu, "tan", () => ApplyTrig("tan", Math.Tan, false));
        AddMenuFunction(menu, "sec", () => ApplyTrig("sec", value => 1 / Math.Cos(value), false));
        AddMenuFunction(menu, "csc", () => ApplyTrig("csc", value => 1 / Math.Sin(value), false));
        AddMenuFunction(menu, "cot", () => ApplyTrig("cot", value => 1 / Math.Tan(value), false));
        menu.Items.Add(new ToolStripSeparator());
        AddMenuFunction(menu, "asin", () => ApplyTrig("asin", Math.Asin, true));
        AddMenuFunction(menu, "acos", () => ApplyTrig("acos", Math.Acos, true));
        AddMenuFunction(menu, "atan", () => ApplyTrig("atan", Math.Atan, true));
        menu.Items.Add(new ToolStripSeparator());
        AddMenuFunction(menu, "sinh", () => ApplyUnary(value => $"sinh({FormatValue(value)})", Math.Sinh));
        AddMenuFunction(menu, "cosh", () => ApplyUnary(value => $"cosh({FormatValue(value)})", Math.Cosh));
        AddMenuFunction(menu, "tanh", () => ApplyUnary(value => $"tanh({FormatValue(value)})", Math.Tanh));
        AddMenuFunction(menu, "asinh", () => ApplyUnary(value => $"asinh({FormatValue(value)})", Math.Asinh));
        AddMenuFunction(menu, "acosh", () => ApplyUnary(value => $"acosh({FormatValue(value)})", Math.Acosh));
        AddMenuFunction(menu, "atanh", () => ApplyUnary(value => $"atanh({FormatValue(value)})", Math.Atanh));
        menu.Show(owner, new Point(0, owner.Height));
    }

    private void ShowFunctionsMenu(Control owner)
    {
        var menu = new ContextMenuStrip();
        AddMenuFunction(menu, "|x|", () => ApplyUnary(value => $"|{FormatValue(value)}|", Math.Abs));
        AddMenuFunction(menu, "floor", () => ApplyUnary(value => $"floor({FormatValue(value)})", Math.Floor));
        AddMenuFunction(menu, "ceil", () => ApplyUnary(value => $"ceil({FormatValue(value)})", Math.Ceiling));
        AddMenuFunction(menu, "exp", () => ApplyUnary(value => $"exp({FormatValue(value)})", Math.Exp));
        AddMenuFunction(menu, "rand", () => SetConstant(Random.Shared.NextDouble(), "rand()"));
        menu.Items.Add(new ToolStripSeparator());
        AddMenuFunction(menu, "→ DMS", () => ApplyUnary(value => $"dms({FormatValue(value)})", ToDegreesMinutesSeconds));
        AddMenuFunction(menu, "→ DEG", () => ApplyUnary(value => $"deg({FormatValue(value)})", FromDegreesMinutesSeconds));
        menu.Show(owner, new Point(0, owner.Height));
    }

    private static void AddMenuFunction(ContextMenuStrip menu, string text, Action action)
        => menu.Items.Add(text, null, (_, _) => action());

    private static double ToDegreesMinutesSeconds(double value)
    {
        var sign = Math.Sign(value);
        var absolute = Math.Abs(value);
        var degrees = Math.Floor(absolute);
        var minutesValue = (absolute - degrees) * 60;
        var minutes = Math.Floor(minutesValue);
        var seconds = (minutesValue - minutes) * 60;
        return sign * (degrees + minutes / 100 + seconds / 10000);
    }

    private static double FromDegreesMinutesSeconds(double value)
    {
        var sign = Math.Sign(value);
        var absolute = Math.Abs(value);
        var degrees = Math.Floor(absolute);
        var minutesAndSeconds = (absolute - degrees) * 100;
        var minutes = Math.Floor(minutesAndSeconds);
        var seconds = (minutesAndSeconds - minutes) * 100;
        return sign * (degrees + minutes / 60 + seconds / 3600);
    }

    private string FormatValue(double value) => CalculatorEngine.Format(value, _forceScientific);

    private void ToggleScientificFormat()
    {
        if (!TryReadDisplay(out var value)) return;
        _forceScientific = !_forceScientific;
        _display.Text = FormatValue(value);
    }

    private void StoreMemory()
    {
        if (!TryReadDisplay(out _memory)) return;
        _hasMemory = true;
    }

    private void RecallMemory()
    {
        if (!_hasMemory) return;
        _display.Text = FormatValue(_memory);
        _expressionLabel.Text = "MR";
        _startNewNumber = true;
    }

    private void ClearMemory()
    {
        _memory = 0;
        _hasMemory = false;
    }

    private void ChangeMemory(int direction)
    {
        if (!TryReadDisplay(out var value)) return;
        _memory = (_hasMemory ? _memory : 0) + direction * value;
        _hasMemory = true;
    }

    private void OpenParenthesis()
    {
        if ((!_startNewNumber || _expressionHasCurrentValue) && _engine.PendingOperation is null)
            ExecuteOperation("×");
        _parentheses.Push((_engine.Result, _engine.PendingOperation));
        _engine.Clear();
        _display.Text = "0";
        _calculationExpression += "(";
        _expressionLabel.Text = _calculationExpression;
        _expressionHasCurrentValue = false;
        _startNewNumber = true;
    }

    private void CloseParenthesis()
    {
        if (_parentheses.Count == 0) return;
        if (!TryReadDisplay(out var value)) return;
        try
        {
            if (!_expressionHasCurrentValue)
                _calculationExpression += FormatValue(value);
            var innerResult = _engine.Equals(value);
            var outer = _parentheses.Pop();
            _engine.Restore(outer.Accumulator, outer.Operation);
            _display.Text = FormatValue(innerResult);
            _calculationExpression += ")";
            _expressionLabel.Text = _calculationExpression;
            _expressionHasCurrentValue = true;
            _startNewNumber = true;
        }
        catch (Exception ex) when (ex is ArithmeticException or ArgumentException)
        {
            ShowCalculationError(ex.Message);
        }
    }

    private void ApplyRoot()
    {
        if (_secondFunctions)
            ApplyUnary(value => $"∛{FormatValue(value)}", Math.Cbrt);
        else
            ApplySquareRoot();
    }

    private void ApplyFactorial()
    {
        ApplyUnary(
            value => $"{FormatValue(value)}!",
            value =>
            {
                if (value < 0 || value != Math.Truncate(value) || value > 170)
                    throw new ArgumentOutOfRangeException(nameof(value), "Факториал поддерживается для целых чисел от 0 до 170.");
                var result = 1D;
                for (var i = 2; i <= (int)value; i++) result *= i;
                return result;
            });
    }

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
        const float expandedHeight = 175;
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
        button.Resize += (_, _) =>
        {
            RoundButton(button, 5);
            FitKeypadButtonText(button);
        };
        button.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
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
        button.Click += handler;
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
        var foreground = _darkTheme ? Color.WhiteSmoke : SystemColors.ControlText;
        _display.ForeColor = Blend(_display.BackColor, foreground, 0.42 + eased * 0.58);
        if (progress < 1) return;
        _display.ForeColor = foreground;
        _displayFadeTimer.Stop();
    }

    private void UpdateDimmingTarget()
    {
        var overlayOpen = _sidebarOpen || _historyOpen;
        _targetDimAmount = overlayOpen ? 0.24 : 0;
        _keypad.Enabled = !overlayOpen;
        _scientificTools.Enabled = !overlayOpen;
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
            _display.Text = FormatValue(result);
            if (_calculationExpression.Length > 0)
            {
                _calculationExpression += operationText;
                _expressionLabel.Text = _calculationExpression;
            }
            else
            {
                _expressionLabel.Text = $"{operationText} =";
            }
            AddHistoryEntry($"{operationText} = {_display.Text}");
            _expressionHasCurrentValue = true;
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

        _display.Text = FormatValue(Math.Sqrt(value));
        var rootExpression = $"√{FormatValue(value)}";
        if (_calculationExpression.Length > 0)
        {
            _calculationExpression += rootExpression;
            _expressionLabel.Text = _calculationExpression;
        }
        else
        {
            _expressionLabel.Text = $"{rootExpression} =";
        }
        AddHistoryEntry($"√{FormatValue(value)} = {_display.Text}");
        _expressionHasCurrentValue = true;
        _startNewNumber = true;
    }

    private void DigitClick(object? sender, EventArgs e)
    {
        var digit = ((Button)sender!).Text;
        if (_startNewNumber || _display.Text == "0") _display.Text = digit;
        else if (_display.Text.Length < 28) _display.Text += digit;
        _startNewNumber = false;
        _expressionHasCurrentValue = false;
    }

    private void EnterDecimalSeparator()
    {
        var separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        if (_startNewNumber) { _display.Text = "0" + separator; _startNewNumber = false; }
        else if (!_display.Text.Contains(separator)) _display.Text += separator;
        _expressionHasCurrentValue = false;
    }

    private void ToggleSign()
    {
        if (!TryReadDisplay(out var value)) return;
        _display.Text = FormatValue(-value);
    }

    private void Backspace()
    {
        if (_startNewNumber) return;
        _display.Text = _display.Text.Length > 1 ? _display.Text[..^1] : "0";
        _expressionHasCurrentValue = false;
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
            _display.Text = FormatValue(_engine.SelectOperation(value, operation));
            if (previousValue is not null && previousOperation is not null)
                AddHistoryEntry($"{FormatValue(previousValue.Value)} {previousOperation} {FormatValue(value)} = {_display.Text}");
            var displayedOperation = operation == "^" ? "^" : operation;
            if (_expressionHasCurrentValue && _calculationExpression.Length > 0)
                _calculationExpression += $" {displayedOperation} ";
            else
                _calculationExpression += $"{FormatValue(value)} {displayedOperation} ";
            _expressionLabel.Text = _calculationExpression;
            _expressionHasCurrentValue = false;
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
        while (_parentheses.Count > 0)
        {
            var depth = _parentheses.Count;
            CloseParenthesis();
            if (_parentheses.Count == depth) return;
            if (!TryReadDisplay(out value)) return;
        }
        var left = _engine.Result;
        var operation = _engine.PendingOperation;
        try
        {
            var result = _engine.Equals(value);
            _display.Text = FormatValue(result);
            if (left is not null && operation is not null)
            {
                if (!_expressionHasCurrentValue)
                    _calculationExpression += FormatValue(value);
                _expressionLabel.Text = $"{_calculationExpression} =";
                AddHistoryEntry($"{_calculationExpression} = {_display.Text}");
            }
            _calculationExpression = string.Empty;
            _expressionHasCurrentValue = true;
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
        _parentheses.Clear();
        _calculationExpression = string.Empty;
        _expressionHasCurrentValue = false;
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
        _applyingTheme = true;
        _buttonAnimations.Clear();
        _interactionTimer.Stop();
        _displayFadeTimer.Stop();
        var background = _darkTheme ? Color.FromArgb(32, 32, 32) : Color.FromArgb(243, 243, 243);
        var foreground = _darkTheme ? Color.WhiteSmoke : SystemColors.ControlText;
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
