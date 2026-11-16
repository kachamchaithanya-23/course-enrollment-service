namespace CourseEnrollment.Core.Models;

/// <summary>
/// Represents an entry in a course offering's FIFO waitlist.
/// </summary>
public sealed class WaitlistEntry
{
    public Learner Learner { get; }
    public DateTime JoinedAtUtc { get; }

    public WaitlistEntry(Learner learner, DateTime? joinedAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(learner);
        Learner = learner;
        JoinedAtUtc = joinedAtUtc ?? DateTime.UtcNow;
    }

    public override string ToString() => $"WaitlistEntry for {Learner.Id} at {JoinedAtUtc:O}";
}

