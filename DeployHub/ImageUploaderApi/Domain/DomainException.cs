namespace ImageUploaderApi.Domain
{
    // 领域异常
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message) { }
    }
}
