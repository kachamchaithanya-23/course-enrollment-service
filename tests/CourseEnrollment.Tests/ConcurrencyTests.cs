namespace CourseEnrollment.Tests;

using System.Collections.Concurrent;
using CourseEnrollment.Core.Models;
using CourseEnrollment.Core.Services;

public class ConcurrencyTests
{
    [Fact]
    public async Task ConcurrentEnrollments_ExceedingCapacity_EnforcesCapacityInvariantAndNoLoss()
    {
        // Arrange
        const int capacity = 10;
        const int totalLearners = 60;
        const string courseId = "CONCURRENCY-101";

        var service = new CourseEnrollmentService();
        service.RegisterOffering(new CourseOffering(courseId, "Concurrent Systems", capacity));

        var results = new ConcurrentBag<EnrollmentResult>();

        // Act: 60 concurrent enrollment tasks
        var tasks = Enumerable.Range(1, totalLearners).Select(i => Task.Run(() =>
        {
            var learner = new Learner($"L-{i:D3}", $"Student-{i}");
            var res = service.Enroll(courseId, learner);
            results.Add(res);
        }));

        await Task.WhenAll(tasks);

        // Assert
        var status = service.GetStatus(courseId)!;
        Assert.Equal(capacity, status.EnrolledCount);
        Assert.Equal(0, status.AvailableSeats);
        Assert.Equal(totalLearners - capacity, status.WaitlistCount);

        var enrolledCountFromResults = results.Count(r => r.Status == EnrollmentStatus.Enrolled);
        var waitlistedCountFromResults = results.Count(r => r.Status == EnrollmentStatus.Waitlisted);

        Assert.Equal(capacity, enrolledCountFromResults);
        Assert.Equal(totalLearners - capacity, waitlistedCountFromResults);

        // All learners accounted for with zero duplicates
        var allIdsInStatus = status.EnrolledLearnerIds.Concat(status.WaitlistLearnerIds).ToList();
        Assert.Equal(totalLearners, allIdsInStatus.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public async Task ConcurrentEnrollments_SameLearner_EnrollsExactlyOnce()
    {
        // Arrange
        const string courseId = "CONCURRENCY-102";
        var service = new CourseEnrollmentService();
        service.RegisterOffering(new CourseOffering(courseId, "Distributed Algorithms", capacity: 5));

        var learner = new Learner("L-RACE", "Race Condition Learner");
        var results = new ConcurrentBag<EnrollmentResult>();

        // Act: 20 concurrent threads trying to enroll the exact same learner
        var tasks = Enumerable.Range(1, 20).Select(_ => Task.Run(() =>
        {
            var res = service.Enroll(courseId, learner);
            results.Add(res);
        }));

        await Task.WhenAll(tasks);

        // Assert: Exactly 1 enrolled, 19 already enrolled
        Assert.Single(results, r => r.Status == EnrollmentStatus.Enrolled);
        Assert.Equal(19, results.Count(r => r.Status == EnrollmentStatus.AlreadyEnrolled));

        var status = service.GetStatus(courseId)!;
        Assert.Equal(1, status.EnrolledCount);
        Assert.Equal(0, status.WaitlistCount);
    }

    [Fact]
    public async Task ConcurrentDropsAndPromotions_MaintainsCapacityInvariantAtAllTimes()
    {
        // Arrange
        const int capacity = 5;
        const string courseId = "CONCURRENCY-103";
        var service = new CourseEnrollmentService();
        service.RegisterOffering(new CourseOffering(courseId, "Concurrency and Locks", capacity));

        // Pre-fill 5 enrolled learners and 15 waitlisted learners
        for (int i = 1; i <= 5; i++)
        {
            service.Enroll(courseId, new Learner($"INIT-{i}", $"Initial-{i}"));
        }
        for (int i = 1; i <= 15; i++)
        {
            service.Enroll(courseId, new Learner($"WAIT-{i}", $"Waitlist-{i}"));
        }

        var dropResults = new ConcurrentBag<DropResult>();
        var enrollResults = new ConcurrentBag<EnrollmentResult>();

        // Act: Concurrently drop initial learners while new learners attempt to enroll
        var dropTasks = Enumerable.Range(1, 5).Select(i => Task.Run(() =>
        {
            var res = service.Drop(courseId, $"INIT-{i}");
            dropResults.Add(res);
        }));

        var newEnrollTasks = Enumerable.Range(1, 10).Select(i => Task.Run(() =>
        {
            var learner = new Learner($"NEW-{i}", $"Newcomer-{i}");
            var res = service.Enroll(courseId, learner);
            enrollResults.Add(res);
        }));

        await Task.WhenAll(dropTasks.Concat(newEnrollTasks));

        // Assert: Course capacity invariant is strictly held
        var finalStatus = service.GetStatus(courseId)!;
        Assert.True(finalStatus.EnrolledCount <= capacity, "Enrolled count must never exceed capacity.");
        Assert.Equal(capacity, finalStatus.EnrolledCount);

        // Every drop of an initial student must have resulted in a promotion since waitlist was non-empty
        var promotedDrops = dropResults.Count(r => r.Status == DropStatus.DroppedAndPromoted);
        Assert.Equal(5, promotedDrops);
    }
}