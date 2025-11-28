using Sycota.Domain.Enums;

namespace Sycota.Domain.Entities;

public class TrainingSession
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime SessionDate { get; set; }
    public ISSFWeaponType WeaponType { get; set; }
    public string Shots { get; set; } // JSON string representing the shots data (see below)
    public string? Notes { get; set; }
    public string CreatedById { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Club Club { get; set; } = null!;
    public ApplicationUser CreatedBy { get; set; } = null!;
}

//JSON structure for shots
//Groups can have a different valueType, e.g., "blind", "10-shot-series", etc. The current ones are:
// - "blind": shots where the shooter cannot see the result immediately, but rather at the end of the 10-shot series.
// - "10-shot-series": standard 10-shot series where the shooter can see each shot result immediately.
// - "practice": shots taken for practice purposes, not counted in scoring.
/*
{
  "WarmupShots": [
    { "x": 4.2, "y": -1.3 },
    { "x": -2.1, "y": 0.9 },
    { "x": 0.5, "y": -0.4 },
    { "x": 3.8, "y": -2.0 },
    { "x": -1.2, "y": 1.7 }
  ],
  "groups": [
    {
      "groupId": 1,
      "valueType": "blind",
      "shots": [
        { "x": 1.2, "y": -0.4 },
        { "x": 0.8, "y": -0.1 },
        { "x": 1.5, "y": 0.3 },
        { "x": 2.0, "y": -0.2 },
        { "x": 0.4, "y": 0.1 },
        { "x": 1.1, "y": -0.5 },
        { "x": 1.3, "y": -0.3 },
        { "x": 0.9, "y": 0.2 },
        { "x": 1.0, "y": -0.4 },
        { "x": 1.4, "y": 0.0 }
      ]
    },
    {
    "groupId": 2,
      "valueType": "10-shot-series",
      "shots": [
        { "x": -0.3, "y": 0.8 },
        { "x": -0.1, "y": 1.1 },
        { "x": 0.2, "y": 1.4 },
        { "x": -0.5, "y": 0.9 },
        { "x": 0.0, "y": 0.7 },
        { "x": -0.4, "y": 1.3 },
        { "x": 0.1, "y": 1.0 },
        { "x": -0.2, "y": 0.6 },
        { "x": 0.3, "y": 1.2 },
        { "x": -0.1, "y": 0.9 }
      ]
    }
  ]
}
*/