namespace Domain;

// Draft → Published → Active ⇄ Paused → Completed → Archived (transition hợp lệ kiểm tra ở service)
public enum WorkoutProgramStatus { Draft, Published, Active, Paused, Completed, Archived }
