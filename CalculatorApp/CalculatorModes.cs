using System.Globalization;

namespace CalculatorApp;

public sealed partial class CalculatorForm
{
    private void SwitchMode(bool scientific)
    {
        if (_scientificMode == scientific)
        {
            if (_sidebarOpen) ToggleSidebar();
            return;
        }

        _scientificMode = scientific;
        _root.RowStyles[0].Height = scientific ? ScientificHeaderHeight : StandardHeaderHeight;
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
}
