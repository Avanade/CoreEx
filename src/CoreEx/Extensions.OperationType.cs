namespace CoreEx;

public static partial class Extensions
{
    extension(OperationType operationType)
    {
        /// <summary>
        /// Indicates whether the <paramref name="operationType"/> is a <see cref="OperationType.Get"/>.
        /// </summary>
        public bool IsGet => operationType == OperationType.Get;

        /// <summary>
        /// Indicates whether the <paramref name="operationType"/> is a <see cref="OperationType.Create"/>.
        /// </summary>
        public bool IsCreate => operationType == OperationType.Create;

        /// <summary>
        /// Indicates whether the <paramref name="operationType"/> is aN <see cref="OperationType.Update"/>.
        /// </summary>
        public bool IsUpdate => operationType == OperationType.Update;

        /// <summary>
        /// Indicates whether the <paramref name="operationType"/> is a <see cref="OperationType.Delete"/>.
        /// </summary>
        public bool IsDelete => operationType == OperationType.Delete;

        /// <summary>
        /// Indicates whether the <paramref name="operationType"/> is <see cref="OperationType.Unspecified"/>.
        /// </summary>
        public bool IsUnspecified => operationType == OperationType.Unspecified;

        /// <summary>
        /// Indicates whether the <paramref name="operationType"/> is a <see cref="OperationType.Get"/> or <see cref="OperationType.Query"/>.
        /// </summary>
        public bool IsRead => operationType == OperationType.Get || operationType == OperationType.Query;

        /// <summary>
        /// Indicates whether the <paramref name="operationType"/> is a <see cref="OperationType.Create"/> or <see cref="OperationType.Update"/> or <see cref="OperationType.Delete"/>.
        /// </summary>
        public bool IsMutation => operationType == OperationType.Create || operationType == OperationType.Update || operationType == OperationType.Delete;
    }
}