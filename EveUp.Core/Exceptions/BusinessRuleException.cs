namespace EveUp.Core.Exceptions;

public class BusinessRuleException : Exception
{
    public string Rule { get; }

    public BusinessRuleException(string rule, string message)
        : base(message)
    {
        Rule = rule;
    }

    public BusinessRuleException(string message)
        : base(message)
    {
        Rule = "BUSINESS_RULE";
    }
}
