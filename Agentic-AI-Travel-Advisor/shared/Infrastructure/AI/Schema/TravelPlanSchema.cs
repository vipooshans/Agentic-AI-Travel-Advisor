using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Schema;
using TravelAdvisor.Core.DTOs.AI;

namespace TravelAdvisor.Infrastructure.AI.Schema;

public sealed record SchemaValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static readonly SchemaValidationResult Valid = new(true, []);
}

/// <summary>
/// JSON Schema for the structured AI response plus the cross-field rules a schema cannot express
/// (budget compliance, cost totals, day coverage).
/// </summary>
public static class TravelPlanSchema
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public const string Text = """
    {
      "$schema": "https://json-schema.org/draft/2020-12/schema",
      "title": "TravelPlan",
      "type": "object",
      "additionalProperties": false,
      "required": ["destination", "duration", "budget", "currency", "hotels", "travelPackages", "activities", "transportation", "itinerary",
                   "startDate", "endDate", "travelers", "estimatedTotal", "withinBudget", "nights", "costBreakdown", "assumptions", "warnings"],
      "properties": {
        "destination": { "type": "string", "minLength": 1, "maxLength": 100 },
        "destinationId": { "type": ["integer", "null"], "minimum": 1 },
        "country": { "type": ["string", "null"], "maxLength": 100 },
        "duration": { "type": "integer", "minimum": 1, "maximum": 30 },
        "nights": { "type": "integer", "minimum": 0, "maximum": 29 },
        "startDate": { "type": "string", "format": "date", "pattern": "^\\d{4}-\\d{2}-\\d{2}$" },
        "endDate": { "type": "string", "format": "date", "pattern": "^\\d{4}-\\d{2}-\\d{2}$" },
        "travelers": { "type": "integer", "minimum": 1, "maximum": 50 },
        "budget": { "type": "number", "exclusiveMinimum": 0 },
        "currency": { "const": "LKR" },
        "estimatedTotal": { "type": "number", "minimum": 0 },
        "withinBudget": { "const": true },
        "costBreakdown": {
          "type": "object",
          "additionalProperties": false,
          "required": ["accommodation", "packages", "transportation"],
          "properties": {
            "accommodation": { "type": "number", "minimum": 0 },
            "packages": { "type": "number", "minimum": 0 },
            "transportation": { "type": "number", "minimum": 0 }
          }
        },
        "hotels": {
          "type": "array", "maxItems": 5,
          "items": {
            "type": "object",
            "additionalProperties": false,
            "required": ["hotelId", "roomId", "name", "roomName", "pricePerNight", "nights", "rooms", "totalCost", "selected", "availabilityChecked"],
            "properties": {
              "hotelId": { "type": "integer", "minimum": 1 },
              "roomId": { "type": "integer", "minimum": 1 },
              "name": { "type": "string", "minLength": 1 },
              "roomName": { "type": "string" },
              "city": { "type": "string" },
              "pricePerNight": { "type": "number", "exclusiveMinimum": 0 },
              "nights": { "type": "integer", "minimum": 1 },
              "rooms": { "type": "integer", "minimum": 1, "maximum": 4 },
              "capacity": { "type": "integer", "minimum": 1 },
              "totalCost": { "type": "number", "minimum": 0 },
              "averageRating": { "type": ["number", "null"], "minimum": 1, "maximum": 5 },
              "selected": { "type": "boolean" },
              "availabilityChecked": { "type": "boolean" }
            }
          }
        },
        "travelPackages": {
          "type": "array", "maxItems": 5,
          "items": {
            "type": "object",
            "additionalProperties": false,
            "required": ["packageId", "title", "durationDays", "pricePerPerson", "totalCost", "selected", "availabilityChecked"],
            "properties": {
              "packageId": { "type": "integer", "minimum": 1 },
              "title": { "type": "string", "minLength": 1 },
              "durationDays": { "type": "integer", "minimum": 1 },
              "pricePerPerson": { "type": "number", "minimum": 0 },
              "totalCost": { "type": "number", "minimum": 0 },
              "remainingPlaces": { "type": ["integer", "null"], "minimum": 0 },
              "averageRating": { "type": ["number", "null"], "minimum": 1, "maximum": 5 },
              "selected": { "type": "boolean" },
              "availabilityChecked": { "type": "boolean" }
            }
          }
        },
        "activities": {
          "type": "array", "maxItems": 40,
          "items": {
            "type": "object",
            "additionalProperties": false,
            "required": ["title", "day", "pricePerPerson", "includedInCost"],
            "properties": {
              "title": { "type": "string", "minLength": 1 },
              "category": { "type": ["string", "null"] },
              "day": { "type": "integer", "minimum": 1, "maximum": 30 },
              "pricePerPerson": { "type": "number", "minimum": 0 },
              "packageId": { "type": ["integer", "null"], "minimum": 1 },
              "includedInCost": { "type": "boolean" }
            }
          }
        },
        "transportation": {
          "type": "array", "maxItems": 5,
          "items": {
            "type": "object",
            "additionalProperties": false,
            "required": ["transportationId", "mode", "from", "to", "pricePerPerson", "trips", "totalCost", "selected"],
            "properties": {
              "transportationId": { "type": "integer", "minimum": 1 },
              "mode": { "enum": ["Bus", "Train", "Car", "Van", "TukTuk", "Flight", "Ferry"] },
              "from": { "type": "string", "minLength": 1 },
              "to": { "type": "string", "minLength": 1 },
              "departureTime": { "type": ["string", "null"], "pattern": "^([01]\\d|2[0-3]):[0-5]\\d$" },
              "durationMinutes": { "type": "integer", "minimum": 0 },
              "pricePerPerson": { "type": "number", "minimum": 0 },
              "trips": { "type": "integer", "minimum": 1, "maximum": 2 },
              "totalCost": { "type": "number", "minimum": 0 },
              "selected": { "type": "boolean" }
            }
          }
        },
        "itinerary": {
          "type": "array", "minItems": 1, "maxItems": 30,
          "items": {
            "type": "object",
            "additionalProperties": false,
            "required": ["day", "date", "items"],
            "properties": {
              "day": { "type": "integer", "minimum": 1, "maximum": 30 },
              "date": { "type": "string", "pattern": "^\\d{4}-\\d{2}-\\d{2}$" },
              "items": {
                "type": "array", "minItems": 1, "maxItems": 12,
                "items": {
                  "type": "object",
                  "additionalProperties": false,
                  "required": ["time", "type", "title"],
                  "properties": {
                    "time": { "type": "string", "pattern": "^([01]\\d|2[0-3]):[0-5]\\d$" },
                    "type": { "enum": ["transport", "checkin", "checkout", "activity", "free"] },
                    "title": { "type": "string", "minLength": 1, "maxLength": 200 },
                    "description": { "type": ["string", "null"], "maxLength": 500 }
                  }
                }
              }
            }
          }
        },
        "assumptions": { "type": "array", "items": { "type": "string" } },
        "warnings": { "type": "array", "items": { "type": "string" } }
      }
    }
    """;

    private static readonly Lazy<JsonSchema> Schema = new(() => JsonSchema.FromText(Text));

    public static SchemaValidationResult Validate(TravelPlan plan)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(plan, JsonOptions));
        return Validate(document.RootElement, plan);
    }

    public static SchemaValidationResult Validate(JsonElement json, TravelPlan? plan = null)
    {
        var errors = new List<string>();
        var result = Schema.Value.Evaluate(json, new EvaluationOptions { OutputFormat = OutputFormat.List });
        if (!result.IsValid)
        {
            foreach (var detail in result.Details ?? [])
            {
                if (detail.Errors is null)
                    continue;
                foreach (var error in detail.Errors)
                    errors.Add($"{detail.InstanceLocation}: {error.Value}");
            }

            if (errors.Count == 0)
                errors.Add("Plan does not match the TravelPlan schema.");
        }

        plan ??= JsonSerializer.Deserialize<TravelPlan>(json.GetRawText(), JsonOptions);
        if (plan is not null)
            errors.AddRange(BusinessRules(plan));

        return errors.Count == 0 ? SchemaValidationResult.Valid : new SchemaValidationResult(false, errors);
    }

    public static IEnumerable<string> BusinessRules(TravelPlan plan)
    {
        if (plan.EstimatedTotal > plan.Budget)
            yield return $"estimatedTotal {plan.EstimatedTotal} exceeds budget {plan.Budget}.";

        var selectedTotal = plan.Hotels.Where(h => h.Selected).Sum(h => h.TotalCost)
                            + plan.TravelPackages.Where(p => p.Selected).Sum(p => p.TotalCost)
                            + plan.Transportation.Where(t => t.Selected).Sum(t => t.TotalCost);
        if (Math.Abs(selectedTotal - plan.EstimatedTotal) > 1)
            yield return $"Selected items cost {selectedTotal} but estimatedTotal is {plan.EstimatedTotal}.";

        var breakdown = plan.CostBreakdown.Accommodation + plan.CostBreakdown.Packages + plan.CostBreakdown.Transportation;
        if (Math.Abs(breakdown - plan.EstimatedTotal) > 1)
            yield return "costBreakdown does not add up to estimatedTotal.";

        if (plan.Hotels.Count(h => h.Selected) > 1 || plan.TravelPackages.Count(p => p.Selected) > 1 || plan.Transportation.Count(t => t.Selected) > 1)
            yield return "At most one hotel, package and transport option can be selected.";

        if (plan.Hotels.Any(h => h.Selected && !h.AvailabilityChecked) || plan.TravelPackages.Any(p => p.Selected && !p.AvailabilityChecked))
            yield return "Selected hotel and package must have passed checkAvailability.";

        if (plan.Nights > 0 && !plan.Hotels.Any(h => h.Selected))
            yield return "A trip with nights must include a selected hotel.";

        if (plan.EndDate.DayNumber - plan.StartDate.DayNumber + 1 != plan.Duration)
            yield return "startDate/endDate do not match duration.";

        if (plan.Itinerary.Count != plan.Duration ||
            !plan.Itinerary.Select(d => d.Day).OrderBy(d => d).SequenceEqual(Enumerable.Range(1, plan.Duration)))
            yield return "itinerary must contain exactly one entry per trip day.";

        foreach (var day in plan.Itinerary)
        {
            if (day.Date != plan.StartDate.AddDays(day.Day - 1))
                yield return $"itinerary day {day.Day} has the wrong date.";
        }
    }
}
