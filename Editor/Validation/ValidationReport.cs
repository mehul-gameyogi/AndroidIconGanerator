using System.Collections.Generic;
using System.Linq;

namespace AndroidIconGenerator
{
    public sealed class ValidationMessage
    {
        public Severity Severity;
        public string Category;
        public string Message;
        public string Solution;

        public ValidationMessage(Severity severity, string category, string message, string solution = null)
        {
            Severity = severity; Category = category; Message = message; Solution = solution;
        }
    }

    /// <summary>Ordered list of validation messages with convenience helpers.</summary>
    public sealed class ValidationReport
    {
        private readonly List<ValidationMessage> _messages = new List<ValidationMessage>();
        public IReadOnlyList<ValidationMessage> Messages => _messages;

        public bool HasErrors => _messages.Any(m => m.Severity == Severity.Error);
        public bool HasWarnings => _messages.Any(m => m.Severity == Severity.Warning);
        public int Count(Severity s) => _messages.Count(m => m.Severity == s);

        public void Add(Severity severity, string category, string message, string solution = null)
        {
            _messages.Add(new ValidationMessage(severity, category, message, solution));
        }

        public void Success(string category, string message) { Add(Severity.Success, category, message); }
        public void Warning(string category, string message, string solution = null) { Add(Severity.Warning, category, message, solution); }
        public void Error(string category, string message, string solution = null) { Add(Severity.Error, category, message, solution); }

        public void AddRange(ValidationReport other)
        {
            if (other != null) _messages.AddRange(other._messages);
        }

        public void AddRange(IEnumerable<ValidationMessage> messages)
        {
            if (messages != null) _messages.AddRange(messages);
        }

        public void Clear() { _messages.Clear(); }

        public void LogToConsole(bool includeSuccess = true)
        {
            foreach (var m in _messages)
            {
                if (m.Severity == Severity.Warning) continue;
                if (!includeSuccess && m.Severity == Severity.Success) continue;
                string text = m.Category + ": " + m.Message;
                if (!string.IsNullOrEmpty(m.Solution)) text += "  -> " + m.Solution;
                IconLogger.Log(m.Severity, text);
            }
        }
    }
}
