namespace CourseEnrollment.Core.Policies;

/// <summary>
/// Specifies the policy applied to an ineligible waitlisted learner when a seat opens.
/// </summary>
public enum WaitlistIneligibilityPolicy
{
    /// <summary>
    /// The ineligible learner is skipped for the current opening and retained in their current queue position.
    /// </summary>
    SkipAndRetain = 0,

    /// <summary>
    /// The ineligible learner is removed from the waitlist when inspected for promotion.
    /// </summary>
    SkipAndEvict = 1
}

