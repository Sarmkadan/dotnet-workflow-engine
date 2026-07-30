public static class WorkflowConstantsExtensions
    {
        public static bool IsTerminalState(string state)
        {
            return false;
        }

        public static bool CanTransition(string from, string to)
        {
            return false;
        }

        public enum AllStates
        {
            // TODO: implement logic here
        }
    }
