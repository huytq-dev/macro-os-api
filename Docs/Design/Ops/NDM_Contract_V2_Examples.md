# NDM Contract v2: exact JSON (real responses)
Captured from the REAL handlers, the REAL validation pipeline and the REAL `GlobalExceptionHandler` on a temporary LocalDB (`NDM_DUMP=1 dotnet run --project tests/NdmSafetyRepro -c Release`), serialised with the API's own options (camelCase, string enums) inside the `ApiResponse` envelope. Dates are relative to the run day; values are illustrative of the shape. Field meaning: `Docs/Design/Nutrition_Data_Manager.md`.

Error rule: every NDM error carries `errors.reasonCode` (array with one code): `CONCURRENT` (409), `FASTING_NEEDS_EMPTY_DAY` (409), `FASTING_WITH_MANUAL_NUTRITION` (400, from the validator when `isFasting:true` is sent with a value above zero, or from the handler when the day is already fasting/has manual values; the validator path ALSO keeps the field error `errors.IsFasting`), `USE_REMOVE_OVERRIDE` (400), `NOTHING_TO_SAVE` (400). Other validation failures have field errors only.

## GET /api/nutrition-data?limit=90
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Success",
  "data": {
    "items": [
      {
        "date": "2026-10-25",
        "calories": 0.00,
        "proteinG": null,
        "fatG": null,
        "carbsG": null,
        "source": "Manual",
        "isFasting": false,
        "isIncomplete": false,
        "entryCount": 3,
        "hasOverride": true,
        "manualCaloriesKcal": 0.00,
        "manualProteinG": null,
        "manualFatG": null,
        "manualCarbsG": null,
        "foodLogCalories": 1800.00,
        "foodLogProteinG": 30.00,
        "foodLogFatG": 15.00,
        "foodLogCarbsG": 60.00,
        "isValidForAlgorithms": false,
        "conflict": "MANUAL_ZERO_WITH_FOOD_LOG"
      },
      {
        "date": "2026-10-24",
        "calories": null,
        "proteinG": 90.00,
        "fatG": null,
        "carbsG": null,
        "source": "Manual",
        "isFasting": false,
        "isIncomplete": false,
        "entryCount": 0,
        "hasOverride": true,
        "manualCaloriesKcal": null,
        "manualProteinG": 90.00,
        "manualFatG": null,
        "manualCarbsG": null,
        "foodLogCalories": 0.00,
        "foodLogProteinG": 0.00,
        "foodLogFatG": 0.00,
        "foodLogCarbsG": 0.00,
        "isValidForAlgorithms": false,
        "conflict": null
      },
      {
        "date": "2026-10-23",
        "calories": 0,
        "proteinG": 0,
        "fatG": 0,
        "carbsG": 0,
        "source": "Fasting",
        "isFasting": true,
        "isIncomplete": false,
        "entryCount": 0,
        "hasOverride": false,
        "manualCaloriesKcal": null,
        "manualProteinG": null,
        "manualFatG": null,
        "manualCarbsG": null,
        "foodLogCalories": 0,
        "foodLogProteinG": 0,
        "foodLogFatG": 0,
        "foodLogCarbsG": 0,
        "isValidForAlgorithms": true,
        "conflict": null
      },
      {
        "date": "2026-10-22",
        "calories": 2200.00,
        "proteinG": null,
        "fatG": null,
        "carbsG": null,
        "source": "Manual",
        "isFasting": false,
        "isIncomplete": false,
        "entryCount": 0,
        "hasOverride": true,
        "manualCaloriesKcal": 2200.00,
        "manualProteinG": null,
        "manualFatG": null,
        "manualCarbsG": null,
        "foodLogCalories": 0.00,
        "foodLogProteinG": 0.00,
        "foodLogFatG": 0.00,
        "foodLogCarbsG": 0.00,
        "isValidForAlgorithms": true,
        "conflict": null
      },
      {
        "date": "2026-10-21",
        "calories": 2200.00,
        "proteinG": 150.00,
        "fatG": 70.00,
        "carbsG": 250.00,
        "source": "Manual",
        "isFasting": false,
        "isIncomplete": false,
        "entryCount": 0,
        "hasOverride": true,
        "manualCaloriesKcal": 2200.00,
        "manualProteinG": 150.00,
        "manualFatG": 70.00,
        "manualCarbsG": 250.00,
        "foodLogCalories": 0.00,
        "foodLogProteinG": 0.00,
        "foodLogFatG": 0.00,
        "foodLogCarbsG": 0.00,
        "isValidForAlgorithms": true,
        "conflict": null
      },
      {
        "date": "2026-10-20",
        "calories": 1800.00,
        "proteinG": 30.00,
        "fatG": 15.00,
        "carbsG": 60.00,
        "source": "FoodLog",
        "isFasting": false,
        "isIncomplete": false,
        "entryCount": 3,
        "hasOverride": false,
        "manualCaloriesKcal": null,
        "manualProteinG": null,
        "manualFatG": null,
        "manualCarbsG": null,
        "foodLogCalories": 1800.00,
        "foodLogProteinG": 30.00,
        "foodLogFatG": 15.00,
        "foodLogCarbsG": 60.00,
        "isValidForAlgorithms": true,
        "conflict": null
      }
    ],
    "hasMore": false,
    "nextBefore": null
  }
}
```

## PUT /api/nutrition-data (calories only: macros null)
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Success",
  "data": {
    "date": "2026-10-19",
    "calories": 2200,
    "proteinG": null,
    "fatG": null,
    "carbsG": null,
    "source": "Manual",
    "isFasting": false,
    "isIncomplete": false,
    "entryCount": 0,
    "hasOverride": true,
    "manualCaloriesKcal": 2200,
    "manualProteinG": null,
    "manualFatG": null,
    "manualCarbsG": null,
    "foodLogCalories": 0,
    "foodLogProteinG": 0,
    "foodLogFatG": 0,
    "foodLogCarbsG": 0,
    "isValidForAlgorithms": true,
    "conflict": null
  }
}
```

## GET /api/food-logs?date= (manual 0 + Food Log 1800: fields FE needs)
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Success",
  "data": {
    "logDate": "2026-10-25",
    "isLogged": true,
    "isFasting": false,
    "isIncomplete": false,
    "totals": {
      "calories": 1800.00,
      "protein": 30.00,
      "carbs": 60.00,
      "fat": 15.00,
      "fiber": 0.00
    },
    "effective": {
      "calories": 0.00,
      "proteinG": null,
      "carbsG": null,
      "fatG": null,
      "source": "Manual",
      "hasOverride": true,
      "manualCaloriesKcal": 0.00,
      "manualProteinG": null,
      "manualCarbsG": null,
      "manualFatG": null,
      "foodLogCalories": 1800.00,
      "foodLogProteinG": 30.00,
      "foodLogCarbsG": 60.00,
      "foodLogFatG": 15.00,
      "isValidForAlgorithms": false,
      "conflict": "MANUAL_ZERO_WITH_FOOD_LOG"
    },
    "entriesCount": 3
  }
}
```

## GET /api/food-logs/week (one day, new `effective` field)
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Success",
  "data": [
    {
      "logDate": "2026-10-19",
      "isLogged": true,
      "totals": {
        "calories": 0.00,
        "protein": 0.00,
        "carbs": 0.00,
        "fat": 0.00,
        "fiber": 0.00
      },
      "target": null,
      "effective": {
        "calories": 2200.00,
        "proteinG": null,
        "carbsG": null,
        "fatG": null,
        "source": "Manual",
        "hasOverride": true,
        "manualCaloriesKcal": 2200.00,
        "manualProteinG": null,
        "manualCarbsG": null,
        "manualFatG": null,
        "foodLogCalories": 0.00,
        "foodLogProteinG": 0.00,
        "foodLogCarbsG": 0.00,
        "foodLogFatG": 0.00,
        "isValidForAlgorithms": true,
        "conflict": null
      }
    }
  ]
}
```

## GET macro series (calories-only day, macro-only day, summary)
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Success",
  "data": {
    "days": [
      {
        "date": "2026-10-22",
        "isLogged": true,
        "isFasting": false,
        "isIncomplete": false,
        "caloriesKcal": 2200.00,
        "proteinG": null,
        "fatG": null,
        "carbsG": null,
        "proteinKcal": null,
        "fatKcal": null,
        "carbsKcal": null,
        "otherKcal": null
      },
      {
        "date": "2026-10-24",
        "isLogged": true,
        "isFasting": false,
        "isIncomplete": false,
        "caloriesKcal": null,
        "proteinG": 90.00,
        "fatG": null,
        "carbsG": null,
        "proteinKcal": 360.00,
        "fatKcal": null,
        "carbsKcal": null,
        "otherKcal": null
      }
    ],
    "summary": {
      "from": "2026-10-20",
      "to": "2026-10-25",
      "loggedDays": 6,
      "calendarDays": 6,
      "averages": {
        "caloriesKcal": 1240.00,
        "proteinG": 67.50,
        "fatG": 28.33,
        "carbsG": 103.33,
        "proteinKcal": 270.00,
        "fatKcal": 255.00,
        "carbsKcal": 413.33,
        "otherKcal": 425.00,
        "proteinPercent": null,
        "fatPercent": null,
        "carbsPercent": null,
        "otherPercent": null
      }
    }
  }
}
```

## POST /api/food-logs/plate/nutrients and POST /api/food-logs/entries/nutrients (new `effectiveDay`)

Same response shape for both endpoints. `effectiveDay` is a new, additive, last field (`null` only when the day has no data and the plate is empty).

**`effectiveDay` is the Effective DAILY Nutrition of `date` after logging the plate. It is the nutrition of the whole DAY, never the nutrition of the plate or of the selected entries.** It is named `effectiveDay` (not `effective`) so it cannot be read as "effective plate/entry".

| Field | Meaning | Affected by manual override? |
|---|---|---|
| `plate` | RAW nutrients of the plate only (`plate/nutrients`) or of the selected entries only (`entries/nutrients`). | **No**, original values. |
| `day` | **"foods + plate (raw)"**: logged entry snapshots + plate. Micronutrients are foods-only (no fake micros). | **No**. Under an override, `day` calories/macros are not the intake the day counts. |
| `effectiveDay` | Effective Daily Nutrition after logging the plate (same shape as the day-log `effective`). | **Yes** (Manual → Food Log → Fasting). |

`effectiveDay` rules:
* Override wins: a plate does not change `calories`/`proteinG`/`carbsG`/`fatG`; a manual field that is null stays null (calories-only override ⇒ macros null, never taken from foods, never 0).
* `foodLog*` = RAW Food Log subtotal of the day **including** the plate, counted once (logged 300 + plate 100 = 400, not 500). The preview is read-only.
* No override: `effectiveDay` = foods + plate and `source` = `FoodLog` (equals the `day` energy row). After Remove Override the same call falls back to this.
* A non-empty plate on a fasting day clears the fasting flag in the preview (as adding an entry does); an empty plate on a fasting day gives `source` = `Fasting`, 0 kcal.
* `entries/nutrients` has no plate: `effectiveDay` is the day's current effective (e.g. Manual 2000 while `plate` shows the selected entries' RAW 300).
* FE: use `effectiveDay` for the day's calories/macro cards; use `plate` for the plate/selection; use `day` rows only for micronutrients and label them raw.

Tests (`tests/NdmSafetyRepro`): 26b (override, metadata, null macros, raw plate/day, repeat call and real day unchanged ⇒ no doubling), 26c (no override FoodLog, calories-only null macros, macro-only, empty plate, fasting), 26d (entries/nutrients, Remove Override fallback).

Real response (override 2000 + logged food 300 + plate 100; `plate`/`day` trimmed to `energy_kcal`):
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Success",
  "data": {
    "date": "2026-10-17",
    "plate": [ { "code": "energy_kcal", "unit": "kcal", "amount": 100, "isCore": true, "key": "calories" } ],
    "day":   [ { "code": "energy_kcal", "unit": "kcal", "amount": 400.00, "isCore": true, "key": "calories" } ],
    "effectiveDay": {
      "calories": 2000.00,
      "proteinG": null,
      "carbsG": null,
      "fatG": null,
      "source": "Manual",
      "hasOverride": true,
      "manualCaloriesKcal": 2000.00,
      "manualProteinG": null,
      "manualCarbsG": null,
      "manualFatG": null,
      "foodLogCalories": 400.00,
      "foodLogProteinG": 20.00,
      "foodLogCarbsG": 25.00,
      "foodLogFatG": 7.00,
      "isValidForAlgorithms": true,
      "conflict": null
    }
  }
}
```
(Trimmed rows omit the unchanged display fields `name`, `group`, `displayOrder`, `displayPrecision`, `floor`, `target`, `ceiling`, `percentOfTarget`, `goalKind`.)

## Effective Daily Nutrition in Nutrient Detail "Today", GET /nutrients?date=, Overview Today and GET /expenditure/daily

All four now follow Manual → Food Log → Fasting. Foods are never used to fill a manual field that was not entered, and an unknown macro is `null`, never `0`.

**`GET /api/nutrients/{code}/day`** (also the same `amount` rule in **`GET /api/nutrients?date=`**):
* `amount` for `energy_kcal` / `protein_g` / `carbs_g` / `fat_g`: day has a manual override ⇒ the manual value, or `null` when that field was not entered; no override ⇒ Food Log total (as before); valid fasting day ⇒ `0`. `carbs_net_g` is `null` under an override (fiber comes only from foods, so it is not mixed with a manual carbs value). Micronutrients, fiber, sodium, water stay foods-only.
* New field **`amountSource`**: `Manual` | `FoodLog` | `Fasting` for those five codes; `null` for other nutrients (always from foods) and for a day with no data.
* **`contributors` and `timing` under an override (option chosen: return none)**: for the five effective codes, `contributors` is `[]` and `timing` is 24 zeros, because the day's amount is the manual number and the foods are not counted. The foods are not listed as "raw, not counted" (that would need FE labelling and a second source of numbers); FE can render "Manual entry, foods not counted" from `amountSource == "Manual"` and tell it apart from "no foods logged" (`FoodLog` + empty). Other codes keep their food contributors and timing.
* `pastWeekAverage` / `pastMonthAverage` were already effective-based and are unchanged.

Real response (calories-only override 2000 + logged food 405; `timing` is 24 zeros, shown as count/total; `pastWeek*` here include other seeded days of the same synthetic user):
```json
{ "isSuccess": true, "statusCode": 200, "message": "Success",
  "data": { "code": "energy_kcal", "date": "2026-10-22", "amount": 2000.00, "amountSource": "Manual", "pastWeekAverage": 2040.00, "pastWeekLoggedDays": 5, "contributors": [], "timingHours": 24, "timingTotal": 0 } }
```
```json
{ "isSuccess": true, "statusCode": 200, "message": "Success",
  "data": { "code": "protein_g", "date": "2026-10-22", "amount": null, "amountSource": "Manual", "pastWeekAverage": 90.00, "pastWeekLoggedDays": 2, "contributors": [], "timingHours": 24, "timingTotal": 0 } }
```

**Overview "Today"** (`GET /api/nutrition/overview?range=today`): the Top-3 `contributors` lists (calories, protein, fat, carbs) are all `[]` when the day has a manual override (same rule and reason as above); without an override they list the foods as before. FE has the override flag from the day log `effective.hasOverride`.

**`GET /api/expenditure/daily`**: `proteinConsumed`, `carbsConsumed`, `fatConsumed` are now `decimal?` and computed at read time from the effective day (the snapshot columns are non-null and cannot hold "unknown"; no schema change). Manual wins; valid fasting = `0`; macro not entered under an override, and a day with no log = `null` (not `0`). `caloriesConsumed` and `isLogged` still come from the derived snapshot (macro-only override: `isLogged: false`, protein known, calories 0).

Real response (calories-only override 2000):
```json
{ "isSuccess": true, "statusCode": 200, "message": "Success",
  "data": [ { "snapshotDate": "2026-10-22", "isLogged": true, "caloriesConsumed": 2000.00, "caloriesTarget": 0,
              "proteinConsumed": null, "carbsConsumed": null, "fatConsumed": null,
              "trendWeightKg": null, "macroProgramWeekId": null, "estimatedTdeeKcal": 2500.00, "targetProvenance": null } ] }
```

Tests (`tests/NdmSafetyRepro`): F6a (Detail under calories-only override: amount, null macros, source, no contributors/timing), F6b (`/nutrients?date=` + Overview Today), F6c (daily null macros), F6d (full override), F6e (no override unchanged), F6f (Remove Override fallback), F6g (fasting, macro-only, missing day).

## error 409 CONCURRENT
```json
{"isSuccess":false,"statusCode":409,"message":"Dữ liệu của ngày này vừa được cập nhật ở nơi khác, vui lòng thử lại.","data":null,"errors":{"reasonCode":["CONCURRENT"]}}
```

## error 400 FASTING_WITH_MANUAL_NUTRITION (handler path: fasting day, then Save with calories)
```json
{"isSuccess":false,"statusCode":400,"message":"This day is marked as fasting, so it can't also have manual nutrition above zero. Remove the fasting mark or lower the values. Nothing was changed.","data":null,"errors":{"reasonCode":["FASTING_WITH_MANUAL_NUTRITION"]}}
```

## error 400 FASTING_WITH_MANUAL_NUTRITION (validator path: PUT isFasting:true + calories:500)
```json
{"isSuccess":false,"statusCode":400,"message":"Validation failed","data":null,"errors":{"IsFasting":["A fasting day has no energy or macros: leave them empty or 0."],"reasonCode":["FASTING_WITH_MANUAL_NUTRITION"]}}
```

## error 409 FASTING_NEEDS_EMPTY_DAY
```json
{"isSuccess":false,"statusCode":409,"message":"This day has logged entries. Remove them (or the manual entry) before marking it as a fasting day. Nothing was changed.","data":null,"errors":{"reasonCode":["FASTING_NEEDS_EMPTY_DAY"]}}
```

## error 400 USE_REMOVE_OVERRIDE
```json
{"isSuccess":false,"statusCode":400,"message":"Clearing every field does not remove the manual values. Use Remove Manual Override to do that. Nothing was changed.","data":null,"errors":{"reasonCode":["USE_REMOVE_OVERRIDE"]}}
```

## error 400 NOTHING_TO_SAVE
```json
{"isSuccess":false,"statusCode":400,"message":"There is nothing to save. To clear manual values use Remove Manual Override.","data":null,"errors":{"reasonCode":["NOTHING_TO_SAVE"]}}
```
