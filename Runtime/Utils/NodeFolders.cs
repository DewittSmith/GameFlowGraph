namespace GameFlow.Utils
{
    /// <summary>
    /// Defines standard folder names used for organizing nodes in the node creation menu.
    /// </summary>
    public static class NodeFolders
    {
        /// <summary>
        /// Folder for application-level nodes that interact with the Unity application.
        /// </summary>
        public const string ApplicationFolder = "Application";

        /// <summary>
        /// Folder for control flow nodes that manage the flow of execution.
        /// </summary>
        public const string ControlFlowFolder = "Control Flow";

        /// <summary>
        /// Default folder for custom nodes that don't fit into other categories.
        /// </summary>
        public const string CustomFolder = "Custom";
    }
}