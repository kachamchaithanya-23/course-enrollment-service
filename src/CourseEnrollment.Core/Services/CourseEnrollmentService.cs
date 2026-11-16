namespace CourseEnrollment.Core.Services;

using System.Collections.Concurrent;
using CourseEnrollment.Core.Models;

/// <summary>
/// Thread-safe in-memory implementation of the course enrollment service.
/// </summary>
public sealed class CourseEnrollmentService : ICourseEnrollmentService
{
    private readonly ConcurrentDictionary<string, CourseOffering> _offerings = new(StringComparer.OrdinalIgnoreCase);

    public CourseEnrollmentService()
    {
    }

    public CourseEnrollmentService(IEnumerable<CourseOffering> initialOfferings)
    {
        ArgumentNullException.ThrowIfNull(initialOfferings);
        foreach (var offering in initialOfferings)
        {
            RegisterOffering(offering);
        }
    }

    public void RegisterOffering(CourseOffering offering)
    {
        ArgumentNullException.ThrowIfNull(offering);
        _offerings[offering.CourseId] = offering;
    }

    public EnrollmentResult Enroll(string courseId, Learner learner)
    {
        if (string.IsNullOrWhiteSpace(courseId))
            return EnrollmentResult.Invalid(courseId ?? string.Empty, learner?.Id ?? string.Empty, "Course ID cannot be empty.");

        if (learner == null)
            return EnrollmentResult.Invalid(courseId, string.Empty, "Learner cannot be null.");

        if (!_offerings.TryGetValue(courseId.Trim(), out CourseOffering? offering))
        {
            return EnrollmentResult.NotFound(courseId, learner.Id);
        }

        return offering.Enroll(learner);
    }

    public DropResult Drop(string courseId, string learnerId)
    {
        if (string.IsNullOrWhiteSpace(courseId))
            return DropResult.Invalid(courseId ?? string.Empty, learnerId ?? string.Empty, "Course ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(learnerId))
            return DropResult.Invalid(courseId, learnerId ?? string.Empty, "Learner ID cannot be empty.");

        if (!_offerings.TryGetValue(courseId.Trim(), out CourseOffering? offering))
        {
            return DropResult.NotFound(courseId, learnerId);
        }

        return offering.Drop(learnerId);
    }

    public CourseOfferingStatus? GetStatus(string courseId)
    {
        if (string.IsNullOrWhiteSpace(courseId))
            return null;

        if (_offerings.TryGetValue(courseId.Trim(), out CourseOffering? offering))
        {
            return offering.GetStatus();
        }

        return null;
    }
}

