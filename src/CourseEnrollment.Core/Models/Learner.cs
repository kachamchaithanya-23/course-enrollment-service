namespace CourseEnrollment.Core.Models;

/// <summary>
/// Represents a learner in the course enrollment system.
/// </summary>
public sealed class Learner
{
    public string Id { get; }
    public string Name { get; }
    public IReadOnlySet<string> CompletedPrerequisites { get; }

    public Learner(string id, string name, IEnumerable<string>? completedPrerequisites = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Learner ID cannot be null or empty.", nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Learner Name cannot be null or empty.", nameof(name));

        Id = id.Trim();
        Name = name.Trim();
        CompletedPrerequisites = completedPrerequisites != null
            ? new HashSet<string>(completedPrerequisites, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public override string ToString() => $"{Name} ({Id})";
}

