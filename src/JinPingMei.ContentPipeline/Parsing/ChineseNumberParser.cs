using System;
using System.Collections.Generic;
using System.Globalization;

namespace JinPingMei.ContentPipeline.Parsing;

internal static class ChineseNumberParser
{
    private static readonly Dictionary<char, int> DigitMap = new()
    {
        ['零'] = 0,
        ['〇'] = 0,
        ['○'] = 0,
        ['Ｏ'] = 0,
        ['o'] = 0,
        ['一'] = 1,
        ['二'] = 2,
        ['三'] = 3,
        ['四'] = 4,
        ['五'] = 5,
        ['六'] = 6,
        ['七'] = 7,
        ['八'] = 8,
        ['九'] = 9
    };

    private static readonly Dictionary<char, int> UnitMap = new()
    {
        ['十'] = 10,
        ['拾'] = 10,
        ['百'] = 100,
        ['佰'] = 100,
        ['千'] = 1000,
        ['仟'] = 1000
    };

    public static int Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
        }

        value = value.Trim();

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
        {
            return numeric;
        }

        var result = 0;
        var current = 0;

        foreach (var c in value)
        {
            if (DigitMap.TryGetValue(c, out var digit))
            {
                current = digit;
                continue;
            }

            if (UnitMap.TryGetValue(c, out var unit))
            {
                if (current == 0)
                {
                    current = 1;
                }

                result += current * unit;
                current = 0;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                continue;
            }

            throw new FormatException($"Unsupported Chinese numeral character '{c}' in '{value}'.");
        }

        return result + current;
    }
}
