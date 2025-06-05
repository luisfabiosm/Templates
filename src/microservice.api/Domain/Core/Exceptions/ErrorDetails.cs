namespace Domain.Core.Exceptions
{
    public record ErrorDetails
    {
        public string Message { get; set; }
        public string PropertyName { get; set; }

        public ErrorDetails(string propertyName, string message)
        {
            Message = message;
            PropertyName = propertyName;
        }

        public ErrorDetails(string message)
        {
            Message = message;
            PropertyName = null;
        }
    }
}
