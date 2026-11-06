using System.Collections.Generic;
using OpenQA.Selenium;

namespace ResilientFormAutomation
{
    public class CandidateScore
    {
        public IWebElement Element { get; set; } = null!;
        public double Score { get; set; }
        public string ElementTag { get; set; } = string.Empty;
        public string ElementId { get; set; } = string.Empty;
        public string ElementName { get; set; } = string.Empty;
        public string ElementType { get; set; } = string.Empty;
        public List<string> MatchedSignals { get; } = new();

        public override string ToString()
        {
            return $"<{ElementTag} id='{ElementId}' name='{ElementName}' type='{ElementType}'> (Score: {Score:F1}) [{string.Join(", ", MatchedSignals)}]";
        }
    }

    public class FieldAutomationResult
    {
        public string FieldName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string AttemptedValue { get; set; } = string.Empty;
        public string ConfirmedValue { get; set; } = string.Empty;
        public string WinningStrategy { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
        public string Details { get; set; } = string.Empty;
    }
}

