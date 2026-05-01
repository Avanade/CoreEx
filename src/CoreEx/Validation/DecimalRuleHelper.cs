namespace CoreEx.Validation;

/// <summary>
/// Represents a helper for <see langword="decimal"/> values.
/// </summary>
public static class DecimalRuleHelper
{
    /// <summary>
    /// Gets the default precision for a <see langword="decimal"/> value.
    /// </summary>
    public const int DefaultPrecision = 18;

    /// <summary>
    /// Checks the <paramref name="value"/> for the specified <paramref name="precision"/> and <paramref name="scale"/>.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="precision">The maximum number of significant digits (including <paramref name="scale"/>).</param>
    /// <param name="scale">The maximum number of fractional digits; i.e. decimal places.</param>
    /// <returns><see langword="true"/> where valid; otherwise, <see langword="false"/>.</returns>
    /// <remarks>A <see langword="null"/> <paramref name="scale"/> will result in no scale validation.</remarks>
    public static bool CheckPrecisionAndScale(decimal value, int precision = DefaultPrecision, int? scale = null)
    {
        precision.ThrowWhen(p => p < 1, "Precision minimum value (where specified) is 1.");
        scale.ThrowWhen(s => s.HasValue && s.Value < 0, "Scale minimum value (where specified) is 0.").ThrowWhen(s => s.HasValue && s.Value >= precision, "Scale must be less than precision.");

        if (value == 0m)
            return true;

        // Calculate lengths once to avoid redundant computation.
        var integralLength = CalcIntegralPartLength(value);
        var fractionalLength = CalcFractionalPartLength(value);

        if (!CheckPrecision(precision, scale, integralLength, fractionalLength))
            return false;

        return scale is null || CheckScale(scale.Value, fractionalLength);
    }

    /// <summary>
    /// Checks the precision.
    /// </summary>
    /// <param name="precision">The maximum number of significant digits (including <paramref name="scale"/>).</param>
    /// <param name="scale">The maximum number of fractional digits; i.e. decimal places.</param>
    /// <param name="integralLength">The integral-part length.</param>
    /// <param name="fractionalLength">The fractional-part length.</param>
    internal static bool CheckPrecision(int precision, int? scale, int integralLength, int fractionalLength) => (integralLength + (scale ?? fractionalLength)) <= precision;

    /// <summary>
    /// Checks the <paramref name="value"/> to determine whether the fractional-part length is less than or equal to the specified scale.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="scale">The maximum number of fractional digits; i.e. decimal places.</param>
    /// <returns><see langword="true"/> where valid; otherwise, <see langword="false"/>.</returns>
    public static bool CheckScale(decimal value, int scale)
    {
        scale.ThrowWhen(s => s < 0, "Scale minimum value is 0.");

        if (value == 0m)
            return true;

        return CheckScale(scale, CalcFractionalPartLength(value));
    }

    /// <summary>
    /// Checks the scale.
    /// </summary>
    /// <param name="scale">The maximum number of fractional digits; i.e. decimal places.</param>
    /// <param name="fractionalLength">The fractional-part length.</param>
    internal static bool CheckScale(int scale, int fractionalLength) => fractionalLength <= scale;

    /// <summary>
    /// Calculates the integral-part length for a <see cref="decimal"/> value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The integral-part length.</returns>
    public static int CalcIntegralPartLength(decimal value)
    {
        if (value == 0m)
            return 0;

        // Get the integral part.
        decimal absValue = Math.Abs(Math.Truncate(value));
        if (absValue == 0m)
            return 0;

        // Use Log10 for O(1) performance; cast to double is safe here as we only need the magnitude for digit counting.
        return (int)Math.Floor(Math.Log10((double)absValue)) + 1;
    }

    /// <summary>
    /// Calculates the fractional-part length for a <see cref="decimal"/> value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The fractional-part length.</returns>
    public static int CalcFractionalPartLength(decimal value)
    {
        if (value == 0m)
            return 0;

        // Extract scale directly from decimal's internal representation (O(1) operation); scale is stored in bits 16-23 of the fourth int32
        int scale = (decimal.GetBits(value)[3] >> 16) & 0xFF;
        if (scale == 0)
            return 0;

        // Get fractional part
        decimal fractional = value % 1m;
        if (fractional == 0m)
            return 0;

        // Multiply to integer form using power of 10, then strip trailing zeros.
        decimal multiplied = Math.Abs(fractional) * GetPowerOf10(scale);
        while (scale > 0 && multiplied % 10m == 0m)
        {
            multiplied /= 10m;
            scale--;
        }

        return scale;
    }

    /// <summary>
    /// Gets the power of 10 for the specified exponent using a lookup table for performance.
    /// </summary>
    private static decimal GetPowerOf10(int exponent)
    {
        return exponent switch
        {
            0 => 1m,
            1 => 10m,
            2 => 100m,
            3 => 1000m,
            4 => 10000m,
            5 => 100000m,
            6 => 1000000m,
            7 => 10000000m,
            8 => 100000000m,
            9 => 1000000000m,
            10 => 10000000000m,
            11 => 100000000000m,
            12 => 1000000000000m,
            13 => 10000000000000m,
            14 => 100000000000000m,
            15 => 1000000000000000m,
            16 => 10000000000000000m,
            17 => 100000000000000000m,
            18 => 1000000000000000000m,
            19 => 10000000000000000000m,
            20 => 100000000000000000000m,
            21 => 1000000000000000000000m,
            22 => 10000000000000000000000m,
            23 => 100000000000000000000000m,
            24 => 1000000000000000000000000m,
            25 => 10000000000000000000000000m,
            26 => 100000000000000000000000000m,
            27 => 1000000000000000000000000000m,
            28 => 10000000000000000000000000000m,
            _ => throw new ArgumentOutOfRangeException(nameof(exponent), exponent, "Exponent must be between 0 and 28.")
        };
    }
}