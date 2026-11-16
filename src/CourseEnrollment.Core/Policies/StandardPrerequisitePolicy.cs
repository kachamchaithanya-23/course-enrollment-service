namespace CourseEnrollment.Core.Policies;

using CourseEnrollment.Core.Models;

/// <summary>
/// Standard prerequisite policy that checks whether the learner has completed
/// the course's required prerequisite course ID (if one is defined).
/// </summary>
public sealed class StandardPrerequisitePolicy : IPrerequisitePolicy
{
    public bool IsEligible(CourseOffering offering, Learner learner, out string? failureReason)
    {
        ArgumentNullException.ThrowIfNull(offering);
        ArgumentNullException.ThrowIfNull(learner);

        if (string.IsNullOrWhiteSpace(offering.RequiredPrerequisite))
        {
            failureReason = null;
            return true;
        }

        if (learner.CompletedPrerequisites.Contains(offering.RequiredPrerequisite))
        {
            failureReason = null;
            return true;
        }

        failureReason = $"Learner does not possess the required prerequisite: '{offering.RequiredPrerequisite}'.";
        return false;
    }
}

