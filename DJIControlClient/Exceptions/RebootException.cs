namespace DJIControlClient.Exceptions
{
    public class RebootException : Exception
    {
        public RebootException() { }
        public RebootException(string message) : base(message) { }
        public RebootException(string message, Exception innerException) : base(message, innerException) { }
    }
}
