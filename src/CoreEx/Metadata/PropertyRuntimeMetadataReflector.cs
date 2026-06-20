namespace CoreEx.Metadata;

/// <summary>
/// Provides functionality to create runtime metadata using reflection.
/// </summary>
internal class PropertyRuntimeMetadataReflector
{
    /// <summary>
    /// Creates the <see cref="IPropertyRuntimeMetadata"/> from the supplied <see cref="PropertyInfo"/>.
    /// </summary>
    /// <param name="pi">The corresponding <see cref="PropertyInfo"/>.</param>
    /// <returns>The <see cref="IPropertyRuntimeMetadata"/>.</returns>
    public static IPropertyRuntimeMetadata CreatePropertyRuntimeMetadata<TEntity, T>(PropertyInfo pi) where TEntity : class
    {
        pi.ThrowIfNull();

        Func<TEntity, T> getValue = (Func<TEntity, T>)Delegate.CreateDelegate(typeof(Func<TEntity, T>), null, pi.GetGetMethod()!);

        var mi = pi.GetSetMethod();
        Action<TEntity, T>? setValue = mi is null ? null : (Action<TEntity, T>)Delegate.CreateDelegate(typeof(Action<TEntity, T>), null, mi);

        var text = pi.GetCustomAttribute<LocalizationAttribute>()?.ToLText();
        if (text is null)
        {
            var dn = pi.GetCustomAttribute<DisplayAttribute>()?.GetName();
            if (!string.IsNullOrEmpty(dn))
                text = new LText(pi.Name, dn);
        }

        Func<LText>? textFunc = text.HasValue ? () => text.Value : null;

        return new PropertyRuntimeMetadata<TEntity, T>(pi.Name, getValue, setValue, textFunc, 
            jsonName: pi.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name, 
            format: pi.GetCustomAttribute<DisplayFormatAttribute>()?.DataFormatString);
    }
}