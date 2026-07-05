namespace Tiketin.Web.Domain;

public enum TicketStatus : short
{
    Open = 1,
    InProgress = 2,
    Resolved = 3,
    Closed = 4,
    Reopened = 5
}

public enum TicketPriority : short
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum TicketEventType : short
{
    Created = 1,
    StatusChanged = 2,
    Assigned = 3,
    PriorityChanged = 4,
    Commented = 5,
    Reopened = 6
}
