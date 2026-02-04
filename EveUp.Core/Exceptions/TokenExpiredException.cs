namespace EveUp.Core.Exceptions;

public class TokenExpiredException : Exception
{
    public TokenExpiredException()
        : base("Token expired")
    {
    }

    public TokenExpiredException(string message)
        : base(message)
    {
    }
}
