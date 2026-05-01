namespace CoreEx.Entities;

public static partial class EntitiesExtensions
{
    extension(FeatureSupport support)
    {
        /// <summary>
        /// Indicates whether the <paramref name="support"/> is <see cref="FeatureSupport.NotSupported"/>.
        /// </summary>
        public bool IsNone => support == FeatureSupport.NotSupported;

        /// <summary>
        /// Indicates whether the <paramref name="support"/> is <see cref="FeatureSupport.ReadOnly"/>.
        /// </summary>
        public bool IsReadOnly => support == FeatureSupport.ReadOnly;

        /// <summary>
        /// Indicates whether the <paramref name="support"/> is <see cref="FeatureSupport.Mutable"/>.
        /// </summary>
        public bool IsMutable => support == FeatureSupport.Mutable;

        /// <summary>
        /// Indicates whether the <paramref name="support"/> is supported (not <see cref="FeatureSupport.NotSupported"/>).
        /// </summary>
        public bool IsSupported => support != FeatureSupport.NotSupported;

        /// <summary>
        /// Determines whether the <typeparamref name="T"/> has the specified feature; being <see cref="FeatureSupport.NotSupported"/>, <see cref="FeatureSupport.ReadOnly"/> or <see cref="FeatureSupport.Mutable"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> to determine the feature support for.</typeparam>
        /// <typeparam name="TMutable">The mutable <see cref="Type"/> (is assignable from) feature.</typeparam>
        /// <typeparam name="TReadonly">The read-only <see cref="Type"/> (is assignable from) feature.</typeparam>
        /// <returns>The <see cref="FeatureSupport"/>.</returns>
        /// <remarks>Where <typeparamref name="T"/> implements <typeparamref name="TMutable"/> then returns <see cref="FeatureSupport.Mutable"/>; otherwise, where <typeparamref name="T"/> implements 
        /// <typeparamref name="TReadonly"/> then returns <see cref="FeatureSupport.ReadOnly"/>, finally returning <see cref="FeatureSupport.NotSupported"/></remarks>
        public static FeatureSupport Determine<T, TMutable, TReadonly>()
        {
            if (typeof(TMutable).IsAssignableFrom(typeof(T)))
                return FeatureSupport.Mutable;
            else if (typeof(TReadonly).IsAssignableFrom(typeof(T)))
                return FeatureSupport.ReadOnly;
            else
                return FeatureSupport.NotSupported;
        }
    }
}