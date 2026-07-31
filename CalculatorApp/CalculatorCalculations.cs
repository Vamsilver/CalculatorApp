using System.Globalization;

namespace CalculatorApp;

public sealed partial class CalculatorForm
{
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
}
