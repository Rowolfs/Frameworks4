namespace Frameworks4.Models;

public enum BookingState
{
    Idle,
    Reserved,
    Confirmed,
    CheckedIn,
    Completed,
    Cancelled
}

public enum BookingEvent
{
    Reserve,
    Confirm,
    CheckIn,
    Complete,
    Cancel
}

public class BookingProcess
{
    public string ProcessKey { get; set; } = "";
    public BookingState State { get; set; } = BookingState.Idle;
    public HashSet<string> ProcessedEvents { get; set; } = new();
}

public class EventRequest
{
    public string Event { get; set; } = "";
    public string IdempotencyKey { get; set; } = "";
    public string CorrelationId { get; set; } = "";
}

public class EventResponse
{
    public string ProcessKey { get; set; } = "";
    public string State { get; set; } = "";
    public string Message { get; set; } = "";
    public string CorrelationId { get; set; } = "";
}
