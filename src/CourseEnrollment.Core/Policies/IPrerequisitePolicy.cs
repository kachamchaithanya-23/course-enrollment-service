namespace CourseEnrollment.Core.Policies;

using CourseEnrollment.Core.Models;

/// <summary>
/// Contract for checking whether a learner is eligible for a course offering.
/// </summary>
public interface IPrerequisitePolicy
{
    /// <summary>
    /// Checks whether a learner satisfies the eligibility prerequisites for a course offering.
    /// </summary>
    bool IsEligible(CourseOffering offering, Learner learner, out string? failureReason);
}

