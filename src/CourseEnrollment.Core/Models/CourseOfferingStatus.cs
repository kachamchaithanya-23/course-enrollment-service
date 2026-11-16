namespace CourseEnrollment.Core.Models;

/// <summary>
/// A read-only snapshot of a course offering's current capacity and waitlist state.
/// </summary>
public sealed class CourseOfferingStatus
{
    public string CourseId { get; }
    public string Title { get; }
    public int Capacity { get; }
    public int EnrolledCount { get; }
    public int AvailableSeats => Math.Max(0, Capacity - EnrolledCount);
    public bool IsFull => EnrolledCount >= Capacity;
    public int WaitlistCount { get; }
    public IReadOnlyList<string> EnrolledLearnerIds { get; }
    public IReadOnlyList<string> WaitlistLearnerIds { get; }

    public CourseOfferingStatus(
        string courseId,
        string title,
        int capacity,
        int enrolledCount,
        int waitlistCount,
        IReadOnlyList<string> enrolledLearnerIds,
        IReadOnlyList<string> waitlistLearnerIds)
    {
        CourseId = courseId;
        Title = title;
        Capacity = capacity;
        EnrolledCount = enrolledCount;
        WaitlistCount = waitlistCount;
        EnrolledLearnerIds = enrolledLearnerIds;
        WaitlistLearnerIds = waitlistLearnerIds;
    }
}

