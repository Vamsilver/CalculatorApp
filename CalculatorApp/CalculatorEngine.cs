using System.Globalization;

namespace CalculatorApp;

public sealed class CalculatorEngine
{
    private decimal? _accumulator;
    private string? _pendingOperation;

    public decimal? Result => _accumulator;
    public string? PendingOperation => _pendingOperation;

    public decimal SelectOperation(decimal value, string operation)
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

    public decimal Equals(decimal value)
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

    public static decimal Calculate(decimal left, decimal right, string operation) => operation switch
    {
        "+" => left + right,
        "−" => left - right,
        "×" => left * right,
        "÷" when right == 0 => throw new DivideByZeroException("Деление на ноль невозможно."),
        "÷" => left / right,
        _ => throw new ArgumentException("Неизвестная операция.", nameof(operation))
    };

    public static string Format(decimal value) => value.ToString("G29", CultureInfo.CurrentCulture);

    private static void ValidateOperation(string operation)
    {
        if (operation is not ("+" or "−" or "×" or "÷"))
            throw new ArgumentException("Неизвестная операция.", nameof(operation));
    }
}
