namespace HomeManagement.Shared;

public enum WorkflowTriggerType
{
    Manual,
    DateTime,
    Sunrise,
    Sunset,
    DeviceResult
}

public enum WorkflowComparisonOperator
{
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Equal,
    NotEqual
}

public enum WorkflowTriggerValueType
{
    Text,
    Number,
    Boolean,
    Json,
    Null
}

public enum WorkflowConditionOperator
{
    And,
    Or
}

public enum WorkflowStepType
{
    DeviceAction,
    Delay,
    Notify
}

public class Workflow
{
    public required string Name { get; init; }

    public string? Description { get; set; }

    public bool IsEnabled { get; set; } = true;

    public WorkflowConditionOperator ConditionOperator { get; set; } = WorkflowConditionOperator.And;

    public DateTimeOffset? LastTriggeredAtUtc { get; set; }

    public bool? LastConditionMatched { get; set; }

    public IList<WorkflowTriggerCondition> TriggerConditions { get; init; } = [];

    public IList<WorkflowStep> Steps { get; init; } = [];
}

public class WorkflowTriggerCondition
{
    public WorkflowTriggerType TriggerType { get; set; } = WorkflowTriggerType.Manual;

    public DateTimeOffset? ScheduledAt { get; set; }

    public string? TriggerDeviceName { get; set; }

    public string? TriggerSourceActionName { get; set; }

    public string? TriggerPropertyPath { get; set; }

    public string? TriggerExpectedValue { get; set; }

    public WorkflowTriggerValueType? TriggerValueType { get; set; }

    public WorkflowComparisonOperator? TriggerOperator { get; set; }

    public DateTimeOffset? LastTriggeredAtUtc { get; set; }

    public DateTime? LastTriggerDateLocal { get; set; }

    public bool? LastConditionMatched { get; set; }
}

public class WorkflowStep
{
    public WorkflowStepType StepType { get; set; } = WorkflowStepType.DeviceAction;

    public string? DeviceName { get; set; }

    public string? ActionName { get; set; }

    public int? DelaySeconds { get; set; }

    public string? NotifyTitle { get; set; }

    public string? NotifyMessageTemplate { get; set; }
}