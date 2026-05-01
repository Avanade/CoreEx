using HandlebarsDotNet;

namespace CoreEx.Generator.Utility;

/// <summary>
/// Provides the <b>Handlebars.Net</b> <see cref="RegisterHelpers"/> capability.
/// </summary>
public static class HandlebarsHelpers
{
    private static readonly object _lock = new();
    private static bool _areRegistered = false;

    /// <summary>
    /// Registers all of the required Handlebars helpers.
    /// </summary>
    public static void RegisterHelpers()
    {
        if (_areRegistered)
            return;

        lock (_lock)
        {
            if (_areRegistered)
                return;

            _areRegistered = true;

            // Increments indent only!
            Handlebars.RegisterHelper("indent++", (in w, in options, in context, in args) =>
            {
                var hc = (CodeGenContext)options.Data["Root"];
                hc.IncrementIndent();
            });

            // Decrements indent only!
            Handlebars.RegisterHelper("indent--", (in w, in options, in context, in args) =>
            {
                var hc = (CodeGenContext)options.Data["Root"];
                hc.DecrementIndent();
            });

            // Writes the current indent string.
            Handlebars.RegisterHelper("indent", (in w, in options, in context, in args) =>
            {
                var hc = (CodeGenContext)options.Data["Root"];
                w.WriteSafeString(hc.GetIndentString());
            });

            Handlebars.RegisterHelper("bo", (w, _, __) => w.WriteSafeString("{"));
            Handlebars.RegisterHelper("bc", (w, _, __) => w.WriteSafeString("}"));

            // Will check that the first argument equals at least one of the subsequent arguments.
            Handlebars.RegisterHelper("ifeq", (writer, options, context, args) =>
            {
                if (IfEq(args))
                    options.Template(writer, context);
                else
                    options.Inverse(writer, context);
            });

            // Will check that the first argument does not equal any of the subsequent arguments.
            Handlebars.RegisterHelper("ifne", (writer, options, context, args) =>
            {
                if (IfEq(args))
                    options.Inverse(writer, context);
                else
                    options.Template(writer, context);
            });
        }
    }

    /// <summary>
    /// Perform the actual IfEq equality check.
    /// </summary>
    private static bool IfEq(Arguments args)
    {
        bool func()
        {
            for (int i = 1; i < args.Length; i++)
            {
                if (Compare(args[0], args[i]))
                    return true;
            }

            return false;
        }

        return args.Length switch
        {
            0 => true,
            1 => args[0] is null,
            2 => Compare(args[0], args[1]),
            _ => func()
        };
    }

    /// <summary>
    /// Compare the two values for equality.
    /// </summary>
    private static bool Compare(object? lval, object? rval)
    {
        if (lval is null && rval is null)
            return true;

        if (lval is null || rval is null)
            return false;

        if (lval is string ls && rval is string rs)
            return ls == rs;

        if (lval is bool lb && rval is bool rb)
            return lb == rb;

        if (lval is int li && rval is int ri)
            return li == ri;

        return lval.ToString() == rval.ToString();
    }
}