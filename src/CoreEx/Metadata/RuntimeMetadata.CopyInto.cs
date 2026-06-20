namespace CoreEx.Metadata;

public static partial class RuntimeMetadata
{
    /// <summary>
    /// Copies (shallow) <paramref name="from"/> a value <paramref name="into"/> another value where they share mutable properties.
    /// </summary>
    /// <typeparam name="TFrom">The from <see cref="Type"/>.</typeparam>
    /// <typeparam name="TInto">The into <see cref="Type"/>.</typeparam>
    /// <param name="from">The from value.</param>
    /// <param name="into">The into value.</param>
    /// <remarks>Only mutable (set-based) properties will be copied; i.e. read-only (get-based) properties will remain unchanged. This performs a shallow copy only, in that only the root property values are copied (replace existing). Where
    /// these values are complex types (i.e. class types) these are not further copied (cloned); they are updated by reference.
    /// <para>This method ignores <see cref="string"/> and <see cref="ICollection"/> values, and the <paramref name="from"/> value must be the same type or is same assignable/subclass as the <paramref name="into"/> value.
    /// In these instances no copying can or will be performed; i.e. is a no-op.</para>
    /// <para>This will leverage either the underlying<see cref="IRuntimeMetadataCore"/> implementation or reflection (<see cref="GetCachedProperties{T}()"/>) depending on the types.</para></remarks>
    public static void CopyInto<TFrom, TInto>(TFrom from, TInto into) where TFrom : class where TInto : class
    {
        from.ThrowIfNull();
        into.ThrowIfNull();

        if (from is string || into is string || from is ICollection || into is ICollection)
            return;

        if (!(from is TInto || into is TFrom))
            return;

        var dict = into is IRuntimeMetadataCore irm
            ? irm.GetPropertyRuntimeMetadata().ToDictionary(p => p.Name)
            : GetCachedProperties<TInto>();

        if (from is IRuntimeMetadataCore frm)
        {
            foreach (var fp in frm.GetPropertyRuntimeMetadata().Where(p => !p.IsReadOnly))
            {
                if (dict.TryGetValue(fp.Name, out var im) && !im.IsReadOnly && fp.Type == im.Type)
                {
                    im.SetValue(into, fp.GetValue(from));
                }
            }
        }
        else
        {
            foreach (var fp in GetCachedProperties<TFrom>().Where(p => !p.Value.IsReadOnly))
            {
                if (dict.TryGetValue(fp.Value.Name, out var im) && !im.IsReadOnly && fp.Value.Type == im.Type)
                {
                    im.SetValue(into, fp.Value.GetValue(from));
                }
            }
        }
    }

    /// <summary>
    /// Copies (shallow) <paramref name="from"/> a value <paramref name="into"/> another value where they share mutable properties and returns a value indicating whether changes where made.
    /// </summary>
    /// <typeparam name="TFrom">The from <see cref="Type"/>.</typeparam>
    /// <typeparam name="TInto">The into <see cref="Type"/>.</typeparam>
    /// <param name="from">The from value.</param>
    /// <param name="into">The into value.</param>
    /// <returns><c>true</c> where changes were made; otherwise, <c>false</c>.</returns>
    /// <remarks>Only mutable (set-based) properties will be copied; i.e. read-only (get-based) properties will remain unchanged. This performs a shallow copy only, in that only the root property values are copied (replace existing). Where
    /// these values are complex types (i.e. class types) these are not further copied (cloned); they are updated by reference.
    /// <para>This method ignores <see cref="string"/> and <see cref="ICollection"/> values, and the <paramref name="from"/> value must be the same type or is same assignable/subclass as the <paramref name="into"/> value.
    /// In these instances no copying can or will be performed; i.e. is a no-op.</para>
    /// <para>This will leverage either the underlying<see cref="IRuntimeMetadataCore"/> implementation or reflection (<see cref="GetCachedProperties{T}()"/>) depending on the types.</para></remarks>
    public static bool TryCopyInto<TFrom, TInto>(TFrom from, TInto into) where TFrom : class where TInto : class
    {
        from.ThrowIfNull();
        into.ThrowIfNull();

        if (from is string || into is string || from is ICollection || into is ICollection)
            return false;

        if (!(from is TInto || into is TFrom))
            return false;

        var changed = false;
        var dict = into is IRuntimeMetadataCore irm
            ? irm.GetPropertyRuntimeMetadata().ToDictionary(p => p.Name)
            : GetCachedProperties<TInto>();

        if (from is IRuntimeMetadataCore frm)
        {
            foreach (var fp in frm.GetPropertyRuntimeMetadata().Where(p => !p.IsReadOnly))
            {
                if (dict.TryGetValue(fp.Name, out var im) && !im.IsReadOnly && fp.Type == im.Type)
                {
                    var gv = fp.GetValue(from);
                    if (gv is not string && gv is ICollection)
                        continue;

                    if (AreEqual(im.GetValue(into), gv))
                        continue;

                    im.SetValue(into, gv);
                    changed = true;
                }
            }
        }
        else
        {
            foreach (var fp in GetCachedProperties<TFrom>().Where(p => !p.Value.IsReadOnly))
            {
                if (dict.TryGetValue(fp.Value.Name, out var im) && !im.IsReadOnly && fp.Value.Type == im.Type)
                {
                    var gv = fp.Value.GetValue(from);
                    if (gv is not string && gv is ICollection)
                        continue;

                    if (AreEqual(im.GetValue(into), gv))
                        continue;

                    im.SetValue(into, gv);
                    changed = true;
                }
            }
        }

        return changed;
    }

    /// <summary>
    /// Creates a shallow copy (clone) of the specified <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="value">The source value.</param>
    /// <returns>The cloned <paramref name="value"/>.</returns>
    /// <remarks>The <see cref="CopyInto{TInto, TFrom}(TInto, TFrom)"/> is the key enabler for this capability.</remarks>
    public static T Clone<T>(T value) where T : class, new()
    {
        if (value.ThrowIfNull() is string str)
            return Internal.Cast<string, T>(str);

        var clone = new T();
        CopyInto(clone, value.ThrowIfNull());
        return clone;
    }
}