namespace CourseEnrollment.Core.Services;

using CourseEnrollment.Core.Models;

/// <summary>
/// Service interface for course enrollment and waitlist operations.
/// </summary>
public interface ICourseEnrollmentService
{
    /// <summary>
    /// Registers a course offering in the service store.
    /// </summary>
    void RegisterOffering(CourseOffering offering);

    /// <summary>
    /// Attempts to enroll a learner in a course offering.
    /// </summary>
    EnrollmentResult Enroll(string courseId, Learner learner);

    /// <summary>
    /// Drops an enrolled learner from a course offering.
    /// </summary>
    DropResult Drop(string courseId, string learnerId);

    /// <summary>
    /// Gets the current capacity and waitlist status for a course offering.
    /// </summary>
    CourseOfferingStatus? GetStatus(string courseId);
}

