namespace GBL.AX2012.MCP.Core.Exceptions;

public class AxException : Exception
{
    public string ErrorCode { get; }
    
    public AxException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
    
    public AxException(string errorCode, string message, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
