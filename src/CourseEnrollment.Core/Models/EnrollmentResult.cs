namespace CourseEnrollment.Core.Models;

/// <summary>
/// Status outcomes of an enrollment attempt.
/// </summary>
public enum EnrollmentStatus
{
    Enrolled,
    Waitlisted,
    AlreadyEnrolled,
    AlreadyWaitlisted,
    PrerequisiteNotMet,
    InvalidRequest,
    CourseNotFound
}

/// <summary>
/// Represents the outcome of an enrollment operation.
/// </summary>
public sealed class EnrollmentResult
{
    public EnrollmentStatus Status { get; }
    public bool IsSuccess => Status is EnrollmentStatus.Enrolled or EnrollmentStatus.Waitlisted;
    public string CourseId { get; }
    public string LearnerId { get; }
    public string Message { get; }
    public int? WaitlistPosition { get; }

    public EnrollmentResult(
        EnrollmentStatus status,
        string courseId,
        string learnerId,
        string message,
        int? waitlistPosition = null)
    {
        Status = status;
        CourseId = courseId ?? string.Empty;
        LearnerId = learnerId ?? string.Empty;
        Message = message ?? string.Empty;
        WaitlistPosition = waitlistPosition;
    }

    public static EnrollmentResult EnrolledSuccess(string courseId, string learnerId) =>
        new(EnrollmentStatus.Enrolled, courseId, learnerId, "Successfully enrolled in the course.");

    public static EnrollmentResult WaitlistedSuccess(string courseId, string learnerId, int waitlistPosition) =>
        new(EnrollmentStatus.Waitlisted, courseId, learnerId, $"Course is full. Placed on waitlist at position {waitlistPosition}.", waitlistPosition);

    public static EnrollmentResult AlreadyEnrolled(string courseId, string learnerId) =>
        new(EnrollmentStatus.AlreadyEnrolled, courseId, learnerId, "Learner is already enrolled in this course.");

    public static EnrollmentResult AlreadyWaitlisted(string courseId, string learnerId, int waitlistPosition) =>
        new(EnrollmentStatus.AlreadyWaitlisted, courseId, learnerId, $"Learner is already on the waitlist at position {waitlistPosition}.", waitlistPosition);

    public static EnrollmentResult PrerequisiteFailed(string courseId, string learnerId, string reason) =>
        new(EnrollmentStatus.PrerequisiteNotMet, courseId, learnerId, reason);

    public static EnrollmentResult Invalid(string courseId, string learnerId, string reason) =>
        new(EnrollmentStatus.InvalidRequest, courseId, learnerId, reason);

    public static EnrollmentResult NotFound(string courseId, string learnerId) =>
        new(EnrollmentStatus.CourseNotFound, courseId, learnerId, $"Course offering '{courseId}' was not found.");
}

