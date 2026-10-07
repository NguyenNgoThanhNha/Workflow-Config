namespace WorkflowConfig.Domain.Exceptions;

/// <summary>Vi phạm quy tắc nghiệp vụ trong domain (GlobalExceptionHandler map sang HTTP 409).</summary>
public class DomainException(string message) : Exception(message);
