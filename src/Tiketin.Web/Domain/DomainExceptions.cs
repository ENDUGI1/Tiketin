namespace Tiketin.Web.Domain;

/// <summary>Requested resource does not exist. Maps to HTTP 404.</summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>Caller is authenticated but not allowed to perform the action. Maps to HTTP 403.</summary>
public class ForbiddenException(string message) : Exception(message);

/// <summary>A business rule was violated (e.g. illegal status transition). Maps to HTTP 400.</summary>
public class DomainRuleException(string message) : Exception(message);
