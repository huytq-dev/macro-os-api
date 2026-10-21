using System.Text.RegularExpressions;

namespace Application;

public enum UsdaSkipReason
{
    NoEnergy, NoMacros, BabyOrInfant, MedicalFormula, RegionalOrCommodity, Branded,
    AlreadyImported, ExistsInCatalog, DuplicateName, OverLimit,
}
