using System.Globalization;

namespace CalculatorApp;

public sealed partial class CalculatorForm
{
    private void BuildInterface()
    {
        _root.Dock = DockStyle.Fill;
        _root.Padding = new Padding(3);
        _root.ColumnCount = 1;
        _root.RowCount = 3;
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, StandardHeaderHeight));
        _scientificToolsRow = new RowStyle(SizeType.Absolute, StandardToolsHeight);
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
        _menuButton.Click += (_, _) =>
        {
            if (_historyOpen) DismissOpenPanels();
            else ToggleSidebar();
        };
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
        _historyButton.Click += (_, _) =>
        {
            if (_sidebarOpen) DismissOpenPanels();
            else ToggleHistory();
        };
        _toolTip.SetToolTip(_historyButton, "Показать историю");
        displayPanel.Controls.Add(_historyButton, 2, 0);

        displayPanel.Controls.Add(_display, 0, 2);
        displayPanel.SetColumnSpan(_display, 3);
        _display.MouseDown += (_, _) => DismissOpenPanels();
        _expressionLabel.MouseDown += (_, _) => DismissOpenPanels();
        _modeLabel.MouseDown += (_, _) => DismissOpenPanels();
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
        _root.MouseDown += (_, _) => DismissOpenPanels();
        _keypad.MouseDown += (_, _) => DismissOpenPanels();
        _scientificTools.MouseDown += (_, _) => DismissOpenPanels();
        WireOverlayDismissal(_root);
        _sidePanel.BringToFront();
    }

    private void WireOverlayDismissal(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is Button) continue;
            child.MouseDown += (_, _) => DismissOpenPanels();
            WireOverlayDismissal(child);
        }
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
        foreach (var button in _memoryBar.Controls.OfType<Button>()) button.Dock = DockStyle.Fill;
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
        _scientificToolsRow.Height = scientific ? ScientificToolsHeight : StandardToolsHeight;
        _formatBar.Visible = scientific;
        _functionBar.Visible = scientific;
        _scientificTools.RowStyles[0].SizeType = SizeType.Absolute;
        _scientificTools.RowStyles[0].Height = scientific ? 22 : 0;
        _scientificTools.RowStyles[1].SizeType = SizeType.Absolute;
        _scientificTools.RowStyles[1].Height = scientific ? 26 : 34;
        _scientificTools.RowStyles[2].SizeType = SizeType.Percent;
        _scientificTools.RowStyles[2].Height = scientific ? 100 : 0;
    }

    private Button ToolButton(string text, EventHandler handler, bool wide = false)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.None,
            AutoSize = wide,
            Width = wide ? 75 : 52,
            Height = wide ? 30 : 22,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F),
            Margin = new Padding(2, 1, 2, 1),
            Padding = wide ? new Padding(6, 0, 6, 0) : Padding.Empty
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (sender, args) =>
        {
            if (_sidebarOpen || _historyOpen)
            {
                DismissOpenPanels();
                return;
            }
            handler(sender, args);
        };
        return button;
    }
}
