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

    public static double Calculate(double left, double right, string operation)
    {
        var result = operation switch
        {
            "+" => left + right,
            "−" => left - right,
            "×" => left * right,
            "÷" when right == 0 => throw new DivideByZeroException("Деление на ноль невозможно."),
            "÷" => left / right,
            _ => throw new ArgumentException("Неизвестная операция.", nameof(operation))
        };

        return double.IsFinite(result)
            ? result
            : throw new OverflowException("Результат выходит за допустимый диапазон чисел.");
    }

    public static string Format(double value)
    {
        if (value == 0) return "0";

        var absolute = Math.Abs(value);
        var format = absolute >= 1E15 || absolute < 1E-9
            ? "0.##############E+0"
            : "0.###############";
        return value.ToString(format, CultureInfo.CurrentCulture);
    }

    private static void ValidateOperation(string operation)
    {
        if (operation is not ("+" or "−" or "×" or "÷"))
            throw new ArgumentException("Неизвестная операция.", nameof(operation));
    }
}
