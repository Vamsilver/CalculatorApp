using System.Globalization;
using System.Reflection;
using CalculatorApp;

internal static class Program
{
    private static int _passed;
    private static int _failed;

    [STAThread]
    private static int Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
        ApplicationConfiguration.Initialize();

        RunEngineTests();
        RunFormTests();

        Console.WriteLine($"RESULT: {_passed} passed, {_failed} failed, {_passed + _failed} total");
        return _failed == 0 ? 0 : 1;
    }

    private static void RunEngineTests()
    {
        Case("engine addition", () => Equal(5, CalculatorEngine.Calculate(2, 3, "+")));
        Case("engine subtraction", () => Equal(6, CalculatorEngine.Calculate(10, 4, "−")));
        Case("engine multiplication", () => Equal(42, CalculatorEngine.Calculate(6, 7, "×")));
        Case("engine division", () => Equal(4, CalculatorEngine.Calculate(8, 2, "÷")));
        Case("engine power", () => Equal(1024, CalculatorEngine.Calculate(2, 10, "^")));
        Case("engine modulo", () => Equal(1, CalculatorEngine.Calculate(10, 3, "mod")));
        Case("negative arithmetic", () => Equal(-7.5, CalculatorEngine.Calculate(-3, 2.5, "×")));
        Case("fraction arithmetic", () => Equal(0.3, CalculatorEngine.Calculate(0.1, 0.2, "+"), 1e-14));
        Case("division by zero", () => Throws<DivideByZeroException>(() => CalculatorEngine.Calculate(1, 0, "÷")));
        Case("modulo by zero", () => Throws<DivideByZeroException>(() => CalculatorEngine.Calculate(1, 0, "mod")));
        Case("unknown operation", () => Throws<ArgumentException>(() => CalculatorEngine.Calculate(1, 2, "?")));
        Case("overflow multiplication", () => Throws<OverflowException>(() => CalculatorEngine.Calculate(double.MaxValue, 2, "×")));
        Case("overflow power", () => Throws<OverflowException>(() => CalculatorEngine.Calculate(10, 1000, "^")));
        Case("NaN rejected", () => Throws<OverflowException>(() => CalculatorEngine.Calculate(-1, 0.5, "^")));

        Case("operation chain", () =>
        {
            var engine = new CalculatorEngine();
            Equal(2, engine.SelectOperation(2, "+"));
            Equal(5, engine.SelectOperation(3, "×"));
            Equal(20, engine.Equals(4));
            IsNull(engine.PendingOperation);
        });
        Case("operation replacement after equals", () =>
        {
            var engine = new CalculatorEngine();
            engine.SelectOperation(8, "÷");
            Equal(4, engine.Equals(2));
            engine.SelectOperation(4, "+");
            Equal(7, engine.Equals(3));
        });
        Case("clear resets state", () =>
        {
            var engine = new CalculatorEngine();
            engine.SelectOperation(9, "+");
            engine.Clear();
            IsNull(engine.Result);
            IsNull(engine.PendingOperation);
            Equal(3, engine.Equals(3));
        });
        Case("restore state", () =>
        {
            var engine = new CalculatorEngine();
            engine.Restore(10, "−");
            Equal(6, engine.Equals(4));
        });
        Case("restore validates operation", () => Throws<ArgumentException>(() => new CalculatorEngine().Restore(1, "?")));

        Case("format zero", () => Equal("0", CalculatorEngine.Format(0)));
        Case("format regular", () => Equal("123,456", CalculatorEngine.Format(123.456)));
        Case("format large scientific", () => Contains("E+", CalculatorEngine.Format(1e15)));
        Case("format tiny scientific", () => Contains("E-", CalculatorEngine.Format(1e-10)));
        Case("format forced scientific", () => Contains("E+", CalculatorEngine.Format(12, true)));
        Case("random arithmetic differential (40000 checks)", () =>
        {
            var random = new Random(1701);
            for (var i = 0; i < 10_000; i++)
            {
                var left = random.NextDouble() * 2_000_000 - 1_000_000;
                var right = random.NextDouble() * 2_000_000 - 1_000_000;
                Equal(left + right, CalculatorEngine.Calculate(left, right, "+"), 1e-8);
                Equal(left - right, CalculatorEngine.Calculate(left, right, "−"), 1e-8);
                Equal(left * right, CalculatorEngine.Calculate(left, right, "×"), 1e-3);
                Equal(left / right, CalculatorEngine.Calculate(left, right, "÷"), 1e-8);
            }
        });
        Case("format round trip (6000 values, 3 cultures)", () =>
        {
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                foreach (var cultureName in new[] { "ru-RU", "en-US", "de-DE" })
                {
                    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                    var random = new Random(cultureName.GetHashCode(StringComparison.Ordinal));
                    for (var i = 0; i < 2_000; i++)
                    {
                        var exponent = random.Next(-250, 251);
                        var value = (random.NextDouble() * 2 - 1) * Math.Pow(10, exponent);
                        var text = CalculatorEngine.Format(value);
                        True(double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out var parsed));
                        var relativeError = value == 0 ? Math.Abs(parsed) : Math.Abs((parsed - value) / value);
                        True(relativeError < 2e-14, $"round-trip {cultureName}: {value} -> {text} -> {parsed}");
                    }
                }
            }
            finally { CultureInfo.CurrentCulture = originalCulture; }
        });
    }

    private static void RunFormTests()
    {
        Case("form initializes", () => WithForm(form =>
        {
            Equal("0", Field<TextBox>(form, "_display").Text);
            Equal(24, Field<TableLayoutPanel>(form, "_keypad").Controls.Count);
            False(Field<bool>(form, "_scientificMode"));
        }));
        Case("digit decimal sign backspace", () => WithForm(form =>
        {
            Digit(form, "1"); Digit(form, "2"); Invoke(form, "EnterDecimalSeparator"); Digit(form, "5");
            Equal("12,5", Display(form));
            Invoke(form, "ToggleSign"); Equal("-12,5", Display(form));
            Invoke(form, "Backspace"); Equal("-12,", Display(form));
        }));
        Case("standard addition and history", () => WithForm(form =>
        {
            Digit(form, "2"); Invoke(form, "ExecuteOperation", "+"); Digit(form, "3"); Invoke(form, "EqualsClick", null, EventArgs.Empty);
            Equal("5", Display(form));
            Contains("2 + 3 = 5", Field<ListBox>(form, "_history").Items.Cast<string>().Single());
        }));
        Case("sequential operations", () => WithForm(form =>
        {
            Digit(form, "2"); Invoke(form, "ExecuteOperation", "+"); Digit(form, "3"); Invoke(form, "ExecuteOperation", "×"); Digit(form, "4"); Invoke(form, "EqualsClick", null, EventArgs.Empty);
            Equal("20", Display(form));
            Equal(2, Field<ListBox>(form, "_history").Items.Count);
        }));
        Case("clear entry preserves operation", () => WithForm(form =>
        {
            Digit(form, "8"); Invoke(form, "ExecuteOperation", "+"); Digit(form, "9"); Invoke(form, "ClearEntry"); Digit(form, "2"); Invoke(form, "EqualsClick", null, EventArgs.Empty);
            Equal("10", Display(form));
        }));
        Case("clear all resets form", () => WithForm(form =>
        {
            Digit(form, "9"); Invoke(form, "ExecuteOperation", "+"); Invoke(form, "ClearAll");
            Equal("0", Display(form));
            Equal(string.Empty, Field<Label>(form, "_expressionLabel").Text);
            IsNull(Field<CalculatorEngine>(form, "_engine").Result);
        }));
        Case("percent", () => WithForm(form => { SetDisplay(form, "50"); InvokeUnary(form, x => $"{x}%", x => x / 100); Equal("0,5", Display(form)); }));
        Case("square root", () => WithForm(form => { SetDisplay(form, "81"); Invoke(form, "ApplySquareRoot"); Equal("9", Display(form)); }));
        Case("factorial", () => WithForm(form => { SetDisplay(form, "10"); Invoke(form, "ApplyFactorial"); Equal("3628800", Display(form)); }));
        Case("DMS round trip", () =>
        {
            var dms = (double)InvokeStatic(typeof(CalculatorForm), "ToDegreesMinutesSeconds", 12.5125)!;
            var degrees = (double)InvokeStatic(typeof(CalculatorForm), "FromDegreesMinutesSeconds", dms)!;
            Equal(12.5125, degrees, 1e-10);
        });
        Case("scientific mode layout", () => WithForm(form =>
        {
            Invoke(form, "SwitchMode", true);
            True(Field<bool>(form, "_scientificMode"));
            Equal(35, Field<TableLayoutPanel>(form, "_keypad").Controls.Count);
            Equal(5, Field<TableLayoutPanel>(form, "_keypad").ColumnCount);
            Equal(7, Field<TableLayoutPanel>(form, "_keypad").RowCount);
        }));
        Case("second functions rebuild", () => WithForm(form =>
        {
            Invoke(form, "SwitchMode", true);
            SetField(form, "_secondFunctions", true);
            Invoke(form, "BuildScientificKeypad");
            var texts = Field<TableLayoutPanel>(form, "_keypad").Controls.OfType<Button>().Select(x => x.Text).ToArray();
            True(texts.Contains("x³")); True(texts.Contains("³√x")); True(texts.Contains("2ˣ"));
        }));
        Case("nested parentheses", () => WithForm(form =>
        {
            Digit(form, "2"); Invoke(form, "ExecuteOperation", "+"); Invoke(form, "OpenParenthesis"); Digit(form, "3"); Invoke(form, "ExecuteOperation", "×");
            Invoke(form, "OpenParenthesis"); Digit(form, "4"); Invoke(form, "ExecuteOperation", "+"); Digit(form, "1"); Invoke(form, "CloseParenthesis");
            Invoke(form, "CloseParenthesis"); Invoke(form, "EqualsClick", null, EventArgs.Empty);
            Equal("17", Display(form));
        }));
        Case("memory store recall add subtract clear", () => WithForm(form =>
        {
            SetDisplay(form, "12"); Invoke(form, "StoreMemory"); True(Field<bool>(form, "_hasMemory"));
            SetDisplay(form, "3"); Invoke(form, "ChangeMemory", 1); SetDisplay(form, "1"); Invoke(form, "ChangeMemory", -1);
            Invoke(form, "RecallMemory"); Equal("14", Display(form));
            Invoke(form, "ClearMemory"); False(Field<bool>(form, "_hasMemory"));
        }));
        Case("history restores result and expression", () => WithForm(form =>
        {
            var history = Field<ListBox>(form, "_history"); history.Items.Add("12 + 7 = 19"); history.SelectedIndex = 0;
            Invoke(form, "UseSelectedHistoryResult"); Equal("19", Display(form)); Equal("12 + 7 =", Field<Label>(form, "_expressionLabel").Text);
        }));
        Case("history parses invariant value", () => WithForm(form =>
        {
            var history = Field<ListBox>(form, "_history"); history.Items.Add("1 / 2 = 0.5"); history.SelectedIndex = 0;
            Invoke(form, "UseSelectedHistoryResult"); Equal("0,5", Display(form));
        }));
        Case("sidebar and history mutually dismiss", () => WithForm(form =>
        {
            Invoke(form, "ToggleSidebar"); True(Field<bool>(form, "_sidebarOpen"));
            Invoke(form, "DismissOpenPanels"); False(Field<bool>(form, "_sidebarOpen"));
            Invoke(form, "ToggleHistory"); True(Field<bool>(form, "_historyOpen"));
            Invoke(form, "DismissOpenPanels"); False(Field<bool>(form, "_historyOpen"));
        }));
        Case("theme round trip", () => WithForm(form =>
        {
            var original = form.BackColor; SetField(form, "_darkTheme", true); Invoke(form, "ApplyTheme");
            True(form.BackColor.R < 50); True(Field<TextBox>(form, "_display").ForeColor.R > 150);
            SetField(form, "_darkTheme", false); Invoke(form, "ApplyTheme"); Equal(original, form.BackColor);
        }));
        Case("responsive minimum and large layout", () => WithForm(form =>
        {
            form.CreateControl(); form.ClientSize = new Size(320, 520); form.PerformLayout();
            EveryButtonHasArea(form);
            form.ClientSize = new Size(720, 800); form.PerformLayout();
            EveryButtonHasArea(form);
        }));
        Case("long display font fits", () => WithForm(form =>
        {
            form.CreateControl(); form.ClientSize = new Size(320, 520); form.PerformLayout(); SetDisplay(form, "1,23456789012345E+200"); Invoke(form, "FitDisplayText");
            var display = Field<TextBox>(form, "_display"); True(display.Font.Size >= 16 && display.Font.Size <= 34);
        }));
        Case("dimming neutralizes hover colors", () => WithForm(form =>
        {
            Invoke(form, "ToggleHistory"); SetField(form, "_dimAmount", 0.24D); Invoke(form, "ApplyRootDimming");
            foreach (var button in Field<TableLayoutPanel>(form, "_keypad").Controls.OfType<Button>())
            { Equal(button.BackColor, button.FlatAppearance.MouseOverBackColor); Equal(button.BackColor, button.FlatAppearance.MouseDownBackColor); }
        }));
        Case("DMS randomized round trip (10000 values)", () =>
        {
            var random = new Random(911);
            for (var i = 0; i < 10_000; i++)
            {
                var value = random.NextDouble() * 720 - 360;
                var dms = (double)InvokeStatic(typeof(CalculatorForm), "ToDegreesMinutesSeconds", value)!;
                var restored = (double)InvokeStatic(typeof(CalculatorForm), "FromDegreesMinutesSeconds", dms)!;
                Equal(value, restored, 1e-9);
            }
        });
        Case("render standard light at minimum", () => RenderForm(false, false, false, new Size(320, 520)));
        Case("render standard dark large", () => RenderForm(false, true, false, new Size(720, 800)));
        Case("render scientific light minimum", () => RenderForm(true, false, false, new Size(320, 520)));
        Case("render scientific dark history overlay", () => RenderForm(true, true, true, new Size(540, 720)));
    }

    private static void WithForm(Action<CalculatorForm> action) { using var form = new CalculatorForm(); action(form); }
    private static string Display(CalculatorForm form) => Field<TextBox>(form, "_display").Text;
    private static void SetDisplay(CalculatorForm form, string text) => Field<TextBox>(form, "_display").Text = text;
    private static void Digit(CalculatorForm form, string digit) => Invoke(form, "DigitClick", new Button { Text = digit }, EventArgs.Empty);
    private static void InvokeUnary(CalculatorForm form, Func<double, string> expression, Func<double, double> operation) => Invoke(form, "ApplyUnary", expression, operation);
    private static void EveryButtonHasArea(CalculatorForm form)
    {
        foreach (var button in Field<TableLayoutPanel>(form, "_keypad").Controls.OfType<Button>())
            True(button.Width > 0 && button.Height > 0, $"button {button.Text} has {button.Size}");
    }
    private static void RenderForm(bool scientific, bool dark, bool history, Size size)
    {
        WithForm(form =>
        {
            form.ClientSize = size;
            form.CreateControl();
            if (scientific) Invoke(form, "SwitchMode", true);
            if (dark) { SetField(form, "_darkTheme", true); Invoke(form, "ApplyTheme"); }
            if (history)
            {
                Field<ListBox>(form, "_history").Items.Add("123456789 × 987654321 = 1,21932631112635E+17");
                Invoke(form, "ToggleHistory");
                SetField(form, "_dimAmount", 0.24D);
                SetField(form, "_historyOpen", true);
                Field<TableLayoutPanel>(form, "_historyPanel").Visible = true;
                Field<TableLayoutPanel>(form, "_historyPanel").Height = 175;
                Invoke(form, "LayoutHistoryPanel");
                Invoke(form, "ApplyRootDimming");
            }
            form.PerformLayout();
            using var bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            var sample = bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2);
            True(sample.A == 255 && bitmap.Width == size.Width && bitmap.Height == size.Height);
        });
    }

    private static T Field<T>(object target, string name) => (T)(target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target) ?? throw new MissingFieldException(name));
    private static void SetField(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
    private static object? Invoke(object target, string name, params object?[] args) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args);
    private static object? InvokeStatic(Type type, string name, params object?[] args) => type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, args);

    private static void Case(string name, Action test)
    {
        try { test(); _passed++; Console.WriteLine($"PASS {name}"); }
        catch (Exception ex) { _failed++; Console.WriteLine($"FAIL {name}: {(ex is TargetInvocationException ? ex.InnerException : ex)?.Message}"); }
    }
    private static void Equal(double expected, double actual, double tolerance = 1e-12) { if (Math.Abs(expected - actual) > tolerance) throw new Exception($"expected {expected}, actual {actual}"); }
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"expected '{expected}', actual '{actual}'"); }
    private static void Contains(string expected, string actual) { if (!actual.Contains(expected, StringComparison.Ordinal)) throw new Exception($"'{actual}' does not contain '{expected}'"); }
    private static void True(bool value, string? message = null) { if (!value) throw new Exception(message ?? "expected true"); }
    private static void False(bool value) => True(!value, "expected false");
    private static void IsNull(object? value) { if (value is not null) throw new Exception($"expected null, actual {value}"); }
    private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception($"expected {typeof(T).Name}"); }
}
