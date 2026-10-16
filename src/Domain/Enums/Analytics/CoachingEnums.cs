namespace Domain;

public enum RecommendationStatus { Proposed, Accepted, Declined, Deferred, Expired }

public enum CoachingRecommendationType { TargetUpdate, DataQualityNotice }

public enum CheckInDecisionType { Accept, Decline, Defer }

public enum RecalculationCause { FoodLogChanged, WeightChanged, DayStatusChanged, AlgorithmUpgrade, Manual }

public enum JobState { Pending, Running, Succeeded, Failed }
