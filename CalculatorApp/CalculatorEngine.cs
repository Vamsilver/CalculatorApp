using System.Globalization;

namespace CalculatorApp;

public sealed class CalculatorEngine
{
    private double? _accumulator;
    private string? _pendingOperation;

    public double? Result => _accumulator;
    public string? PendingOperation => _pendingOperation;

    public double SelectOperation(double value, string operation)
    {
        ValidateOperation(operation);

        if (_accumulator is null)
            _accumulator = value;
        else if (_pendingOperation is not null)
            _accumulator = Calculate(_accumulator.Value, value, _pendingOperation);
        else
            _accumulator = value;

        _pendingOperation = operation;
        return _accumulator.Value;
    }

    public double Equals(double value)
    {
        if (_accumulator is null || _pendingOperation is null)
        {
            _accumulator = value;
            return value;
        }

        _accumulator = Calculate(_accumulator.Value, value, _pendingOperation);
        _pendingOperation = null;
        return _accumulator.Value;
    }

    public void Clear()
    {
        _accumulator = null;
        _pendingOperation = null;
    }

    public void Restore(double? accumulator, string? pendingOperation)
    {
        if (pendingOperation is not null)
            ValidateOperation(pendingOperation);
        _accumulator = accumulator;
        _pendingOperation = pendingOperation;
    }

    public static double Calculate(double left, double right, string operation)
    {
        var result = operation switch
        {
            "+" => left + right,
            "−" => left - right,
            "×" => left * right,
            "^" => Math.Pow(left, right),
            "mod" when right == 0 => throw new DivideByZeroException("Деление на ноль невозможно."),
            "mod" => left % right,
            "÷" when right == 0 => throw new DivideByZeroException("Деление на ноль невозможно."),
            "÷" => left / right,
            _ => throw new ArgumentException("Неизвестная операция.", nameof(operation))
        };

        return double.IsFinite(result)
            ? result
            : throw new OverflowException("Результат выходит за допустимый диапазон чисел.");
    }

    public static string Format(double value, bool forceScientific = false)
    {
        if (value == 0) return "0";

        var absolute = Math.Abs(value);
        if (forceScientific || absolute >= 1E15 || absolute < 1E-9)
            return value.ToString("0.##############E+0", CultureInfo.CurrentCulture);

        const int significantDigits = 15;
        var integerDigits = (int)Math.Floor(Math.Log10(absolute)) + 1;
        var decimalPlaces = Math.Max(0, significantDigits - integerDigits);
        var format = decimalPlaces == 0
            ? "0"
            : $"0.{new string('#', decimalPlaces)}";
        return value.ToString(format, CultureInfo.CurrentCulture);
    }

    private static void ValidateOperation(string operation)
    {
        if (operation is not ("+" or "−" or "×" or "÷" or "^" or "mod"))
            throw new ArgumentException("Неизвестная операция.", nameof(operation));
    }
}
