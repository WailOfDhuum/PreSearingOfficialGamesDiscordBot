namespace PreSearingOfficialGames.Helpers
{
    public readonly struct ValidationResult
    {
        public bool IsError { get; }
        public string? ErrorMessage { get; }

        /// <summary>
        /// </summary>
        /// <returns>Passed <see langword="ValidationResult"/></returns>
        public static ValidationResult Success() => new(string.Empty);

        /// <summary>
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <returns> Invalid <see langword="ValidationResult"/></returns>
        public static ValidationResult Failure(string errorMessage) => new(errorMessage);
        public static ValidationResult Failure(bool isError, string errorMessage) => new(isError, errorMessage);

        public ValidationResult(string errorMessage)
        {
            IsError = !string.IsNullOrEmpty(errorMessage);
            ErrorMessage = errorMessage;
        }

        public ValidationResult(bool isError, string errorMessage)
        {
            IsError = isError;
            ErrorMessage = errorMessage;
        }
    }

    public class ValidationResult<T>(T value, string? errorMessage)
    {
        public T? Value { get; } = value;
        public string? ErrorMessage { get; } = errorMessage;
        public bool IsError => !string.IsNullOrEmpty(ErrorMessage);

        public static ValidationResult<T> Success(T value) => new(value, null);
        public static ValidationResult<T> Failure(string errorMessage) => new(default, errorMessage);
    }
}