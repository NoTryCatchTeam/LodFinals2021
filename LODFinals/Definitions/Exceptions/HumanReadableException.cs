using System;
namespace LODFinals.Definitions.Exceptions
{
    public class HumanReadableException : Exception
    {
        public HumanReadableException(string displayMessage)
        {
            DisplayMessage = displayMessage;
        }

        public string DisplayMessage { get; }
    }
}
