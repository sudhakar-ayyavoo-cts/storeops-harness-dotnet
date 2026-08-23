using Microsoft.Extensions.DependencyInjection;
using StoreOps.Application.Activities;
using StoreOps.Application.Alerts;
using StoreOps.Application.Programmes;
using StoreOps.Application.Reports;
using StoreOps.Application.Staff;
using StoreOps.Domain.Activities;
using StoreOps.Domain.Alerts;
using StoreOps.Domain.Programmes;
using StoreOps.Domain.Reports;
using StoreOps.Domain.Staff;
using DomainTaskStatus = StoreOps.Domain.Activities.TaskStatus;

namespace StoreOps.Infrastructure.Seed;

/// <summary>
/// Populates the in-memory repositories with a fixed set of demo records so the API has
/// something to return out of the box. Development-only; the in-memory stores are wiped
/// on every restart, so this simply re-seeds the same data each time.
/// </summary>
public static class DemoDataSeeder
{
    private static readonly Guid StoreDowntown = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid StoreUptown = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly Guid UserPriya = Guid.Parse("a1000000-0000-0000-0000-000000000001");
    private static readonly Guid UserMarcus = Guid.Parse("a1000000-0000-0000-0000-000000000002");
    private static readonly Guid UserAisha = Guid.Parse("a1000000-0000-0000-0000-000000000003");
    private static readonly Guid UserTom = Guid.Parse("a1000000-0000-0000-0000-000000000004");
    private static readonly Guid UserGrace = Guid.Parse("a1000000-0000-0000-0000-000000000005");
    private static readonly Guid UserDiego = Guid.Parse("a1000000-0000-0000-0000-000000000006");

    private const string DemoPassword = "Demo@123";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var userRepository = services.GetRequiredService<IUserRepository>();
        var taskRepository = services.GetRequiredService<ITaskRepository>();
        var projectRepository = services.GetRequiredService<IProjectRepository>();
        var notificationRepository = services.GetRequiredService<INotificationRepository>();
        var reportRepository = services.GetRequiredService<IReportRepository>();

        var now = DateTimeOffset.UtcNow;

        var users = new[]
        {
            new User
            {
                Id = UserPriya,
                Email = "priya.nair@storeops.demo",
                PasswordHash = DemoPassword,
                Role = StaffRole.RegionalManager,
                StoreId = StoreDowntown,
                CreatedAt = now.AddMonths(-14),
                Profile = new UserProfile { FirstName = "Priya", LastName = "Nair", Department = "Regional Operations" },
            },
            new User
            {
                Id = UserMarcus,
                Email = "marcus.chen@storeops.demo",
                PasswordHash = DemoPassword,
                Role = StaffRole.StoreManager,
                StoreId = StoreDowntown,
                CreatedAt = now.AddMonths(-11),
                Profile = new UserProfile { FirstName = "Marcus", LastName = "Chen", Department = "Store Management" },
            },
            new User
            {
                Id = UserAisha,
                Email = "aisha.bello@storeops.demo",
                PasswordHash = DemoPassword,
                Role = StaffRole.DepartmentLead,
                StoreId = StoreDowntown,
                CreatedAt = now.AddMonths(-9),
                Profile = new UserProfile { FirstName = "Aisha", LastName = "Bello", Department = "Grocery" },
            },
            new User
            {
                Id = UserTom,
                Email = "tom.walsh@storeops.demo",
                PasswordHash = DemoPassword,
                Role = StaffRole.DepartmentLead,
                StoreId = StoreUptown,
                CreatedAt = now.AddMonths(-8),
                Profile = new UserProfile { FirstName = "Tom", LastName = "Walsh", Department = "Electronics" },
            },
            new User
            {
                Id = UserGrace,
                Email = "grace.kim@storeops.demo",
                PasswordHash = DemoPassword,
                Role = StaffRole.Associate,
                StoreId = StoreDowntown,
                CreatedAt = now.AddMonths(-4),
                Profile = new UserProfile { FirstName = "Grace", LastName = "Kim", Department = "Grocery" },
            },
            new User
            {
                Id = UserDiego,
                Email = "diego.ramirez@storeops.demo",
                PasswordHash = DemoPassword,
                Role = StaffRole.Associate,
                StoreId = StoreUptown,
                CreatedAt = now.AddMonths(-3),
                Profile = new UserProfile { FirstName = "Diego", LastName = "Ramirez", Department = "Electronics" },
            },
        };

        foreach (var user in users)
        {
            await userRepository.AddAsync(user, ct);
        }

        var overdueCriticalTaskId = Guid.NewGuid();

        var tasks = new[]
        {
            new StoreTask
            {
                Id = overdueCriticalTaskId,
                Title = "Cold case compressor failure — Aisle 12",
                Description = "Compressor alarm tripped overnight; product at risk, needs immediate escalation.",
                Status = DomainTaskStatus.InProgress,
                Priority = TaskPriority.Critical,
                Category = TaskCategory.Compliance,
                StoreId = StoreDowntown,
                AssignedToUserId = UserAisha,
                DueDate = now.AddDays(-2),
                CreatedAt = now.AddDays(-4),
                UpdatedAt = now.AddDays(-1),
            },
            new StoreTask
            {
                Id = Guid.NewGuid(),
                Title = "Restock endcap — seasonal beverages",
                Status = DomainTaskStatus.Todo,
                Priority = TaskPriority.Medium,
                Category = TaskCategory.Restocking,
                StoreId = StoreDowntown,
                AssignedToUserId = UserGrace,
                DueDate = now.AddDays(2),
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddDays(-1),
            },
            new StoreTask
            {
                Id = Guid.NewGuid(),
                Title = "Weekly planogram compliance audit",
                Status = DomainTaskStatus.Done,
                Priority = TaskPriority.Low,
                Category = TaskCategory.Audit,
                StoreId = StoreDowntown,
                AssignedToUserId = UserMarcus,
                DueDate = now.AddDays(-5),
                CreatedAt = now.AddDays(-9),
                UpdatedAt = now.AddDays(-5),
            },
            new StoreTask
            {
                Id = Guid.NewGuid(),
                Title = "Black Friday electronics planogram reset",
                Status = DomainTaskStatus.Blocked,
                Priority = TaskPriority.High,
                Category = TaskCategory.Planogram,
                StoreId = StoreUptown,
                AssignedToUserId = UserTom,
                DueDate = now.AddDays(1),
                CreatedAt = now.AddDays(-6),
                UpdatedAt = now.AddDays(-1),
            },
            new StoreTask
            {
                Id = Guid.NewGuid(),
                Title = "Receiving dock safety walkthrough",
                Status = DomainTaskStatus.InProgress,
                Priority = TaskPriority.Medium,
                Category = TaskCategory.Compliance,
                StoreId = StoreUptown,
                AssignedToUserId = UserDiego,
                DueDate = now.AddDays(3),
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-1),
            },
        };

        foreach (var task in tasks)
        {
            await taskRepository.AddAsync(task, ct);
        }

        var projects = new[]
        {
            new Project
            {
                Id = Guid.NewGuid(),
                Name = "Q3 Planogram Refresh",
                Description = "Store-wide planogram update ahead of the autumn range change.",
                StoreId = StoreDowntown,
                StartDate = now.AddMonths(-1),
                EndDate = now.AddMonths(1),
                CreatedAt = now.AddMonths(-1),
                Members =
                {
                    new ProjectMember { UserId = UserMarcus, Role = ProjectRole.StoreManager },
                    new ProjectMember { UserId = UserAisha, Role = ProjectRole.DepartmentLead },
                    new ProjectMember { UserId = UserGrace, Role = ProjectRole.Associate },
                },
            },
            new Project
            {
                Id = Guid.NewGuid(),
                Name = "Holiday Readiness Audit",
                Description = "Compliance and stock-safety audit ahead of the holiday trading period.",
                StoreId = StoreUptown,
                StartDate = now.AddDays(-10),
                EndDate = null,
                CreatedAt = now.AddDays(-10),
                Members =
                {
                    new ProjectMember { UserId = UserTom, Role = ProjectRole.DepartmentLead },
                    new ProjectMember { UserId = UserDiego, Role = ProjectRole.Associate },
                },
            },
        };

        foreach (var project in projects)
        {
            await projectRepository.AddAsync(project, ct);
        }

        var notifications = new[]
        {
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = UserAisha,
                AlertType = AlertType.SlaBreach,
                Channel = NotificationChannel.InApp,
                Status = NotificationStatus.Unread,
                Message = "CRITICAL task \"Cold case compressor failure — Aisle 12\" is 2 days past its due date.",
                RelatedEntityId = overdueCriticalTaskId,
                CreatedAt = now.AddHours(-6),
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = UserMarcus,
                AlertType = AlertType.Escalation,
                Channel = NotificationChannel.Email,
                Status = NotificationStatus.Unread,
                Message = "SLA breach on a CRITICAL task has been unresolved beyond the grace period.",
                RelatedEntityId = overdueCriticalTaskId,
                CreatedAt = now.AddHours(-1),
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = UserGrace,
                AlertType = AlertType.Inventory,
                Channel = NotificationChannel.InApp,
                Status = NotificationStatus.Read,
                Message = "Seasonal beverages endcap is below par stock level.",
                CreatedAt = now.AddDays(-1),
                AcknowledgedAt = now.AddDays(-1).AddMinutes(20),
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = UserDiego,
                AlertType = AlertType.ShiftHandover,
                Channel = NotificationChannel.InApp,
                Status = NotificationStatus.Acknowledged,
                Message = "Handover notes from the closing shift are ready for review.",
                CreatedAt = now.AddHours(-14),
                AcknowledgedAt = now.AddHours(-13),
            },
        };

        foreach (var notification in notifications)
        {
            await notificationRepository.AddAsync(notification, ct);
        }

        var reports = new[]
        {
            new Report
            {
                Id = Guid.NewGuid(),
                Type = ReportType.StoreSummary,
                Status = ReportStatus.Ready,
                StoreId = StoreDowntown,
                RegionId = "REGION-NORTH",
                GeneratedAt = now.AddHours(-2),
                Data = new ReportData
                {
                    TotalTasks = 3,
                    CompletedTasks = 1,
                    OverdueTasks = 1,
                    ActiveProgrammes = 1,
                    TotalStaff = 4,
                },
            },
            new Report
            {
                Id = Guid.NewGuid(),
                Type = ReportType.StoreSummary,
                Status = ReportStatus.Ready,
                StoreId = StoreUptown,
                RegionId = "REGION-NORTH",
                GeneratedAt = now.AddHours(-3),
                Data = new ReportData
                {
                    TotalTasks = 2,
                    CompletedTasks = 0,
                    OverdueTasks = 0,
                    ActiveProgrammes = 1,
                    TotalStaff = 2,
                },
            },
        };

        foreach (var report in reports)
        {
            await reportRepository.AddAsync(report, ct);
        }
    }
}
