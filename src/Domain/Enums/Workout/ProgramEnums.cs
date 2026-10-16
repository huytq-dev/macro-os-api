namespace Domain;

public enum ProgramCreationMode { SmartGenerated, FromScratch, Duplicated, Imported }

// Draft → Published → Active ⇄ Paused → Completed → Archived (transition hợp lệ kiểm tra ở service)
public enum WorkoutProgramStatus { Draft, Published, Active, Paused, Completed, Archived }

public enum ProgressionPolicy { None, DoubleProgression, LoadProgression, Manual }
