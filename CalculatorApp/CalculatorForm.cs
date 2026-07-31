using System.Globalization;

namespace CalculatorApp;

public sealed partial class CalculatorForm : Form
{
    private const int StandardHeaderHeight = 125;
    private const int ScientificHeaderHeight = 110;
    private const int StandardToolsHeight = 34;
    private const int ScientificToolsHeight = 80;
    private const int ExpandedSidebarWidth = 250;
    private const int ExpandedHistoryHeight = 175;

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
}
