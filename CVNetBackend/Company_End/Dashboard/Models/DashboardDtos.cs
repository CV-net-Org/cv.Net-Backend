using System.Collections.Generic;

namespace CVNetBackend.Company_End.Models;

public class RecruiterDashboardDto
{
    public int TotalApplications { get; set; }
    public int AverageMatchScore { get; set; }
    public int OpenPositions { get; set; }
    public List<MonthlyTrendDto> ApplicationTrends { get; set; } = new();
    public List<TopCandidateDto> TopCandidates { get; set; } = new();
}

public class MonthlyTrendDto
{
    public string Month { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TopCandidateDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int MatchScore { get; set; }
    public string Stage { get; set; } = string.Empty;
}