namespace CourseEnrollment.Core.Models;

/// <summary>
/// Status outcomes of a drop attempt.
/// </summary>
public enum DropStatus
{
    DroppedWithoutPromotion,
    DroppedAndPromoted,
    LearnerNotEnrolled,
    InvalidRequest,
    CourseNotFound
}

/// <summary>
/// Represents the outcome of a drop operation.
/// </summary>
public sealed class DropResult
{
    public DropStatus Status { get; }
    public bool IsSuccess => Status is DropStatus.DroppedWithoutPromotion or DropStatus.DroppedAndPromoted;
    public string CourseId { get; }
    public string DroppedLearnerId { get; }
    public string? PromotedLearnerId { get; }
    public string Message { get; }

    public DropResult(
        DropStatus status,
        string courseId,
        string droppedLearnerId,
        string message,
        string? promotedLearnerId = null)
    {
        Status = status;
        CourseId = courseId ?? string.Empty;
        DroppedLearnerId = droppedLearnerId ?? string.Empty;
        Message = message ?? string.Empty;
        PromotedLearnerId = promotedLearnerId;
    }

    public static DropResult DroppedOnly(string courseId, string droppedLearnerId) =>
        new(DropStatus.DroppedWithoutPromotion, courseId, droppedLearnerId, "Learner dropped successfully. No waitlisted learner promoted.");

    public static DropResult DroppedWithPromotion(string courseId, string droppedLearnerId, string promotedLearnerId) =>
        new(DropStatus.DroppedAndPromoted, courseId, droppedLearnerId, $"Learner dropped successfully. Waitlisted learner '{promotedLearnerId}' was promoted to enrolled.", promotedLearnerId);

    public static DropResult NotEnrolled(string courseId, string learnerId) =>
        new(DropStatus.LearnerNotEnrolled, courseId, learnerId, $"Learner '{learnerId}' is not currently enrolled in this course.");

    public static DropResult Invalid(string courseId, string learnerId, string reason) =>
        new(DropStatus.InvalidRequest, courseId, learnerId, reason);

    public static DropResult NotFound(string courseId, string learnerId) =>
        new(DropStatus.CourseNotFound, courseId, learnerId, $"Course offering '{courseId}' was not found.");
}

