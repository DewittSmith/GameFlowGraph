using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameFlow.Attributes;
using GameFlow.Editor.Graph;
using GameFlow.Utils;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace GameFlow.Editor.Windows
{
    public sealed class GameFlowSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private GameFlowGraphView graphView;
        private Texture2D indentationIcon;

        public void Initialize(GameFlowGraphView graphView)
        {
            this.graphView = graphView;

            indentationIcon = new(1, 1);
            indentationIcon.SetPixel(0, 0, Color.clear);
            indentationIcon.Apply();
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var entries = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new("Create Node")),
                new(new("Create Group", indentationIcon))
                {
                    level = 1,
                    userData = typeof(Group)
                }
            };

            var otherFolders = new HashSet<string>();
            var createdFolders = new HashSet<string>();
            var groupedEntries = new Dictionary<string, List<(Type type, string name)>>();

            foreach (var nodeType in FlowNodeUtils.FindAllNodeTypes())
            {
                if (nodeType.IsGenericType)
                {
                    continue;
                }

                // Ignore if hidden.
                if (nodeType.GetCustomAttribute(typeof(SearchHiddenAttribute)) != null)
                {
                    continue;
                }

                var instance = TypeUtils.CreateDefaultNode(nodeType);
                if (!groupedEntries.TryGetValue(instance.Folder, out var list))
                {
                    list = new();
                    groupedEntries.Add(instance.Folder, list);
                    otherFolders.Add(instance.Folder);
                }

                list.Add((nodeType, instance.Name));

                if (instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            otherFolders.Remove(NodeFolders.ApplicationFolder);
            otherFolders.Remove(NodeFolders.ControlFlowFolder);
            otherFolders.Remove(NodeFolders.CustomFolder);

            AddFolder(NodeFolders.ApplicationFolder);
            AddFolder(NodeFolders.ControlFlowFolder);

            foreach (string otherFolder in otherFolders)
            {
                AddFolder(otherFolder);
            }

            AddFolder(NodeFolders.CustomFolder);

            return entries;

            void AddFolder(string folder)
            {
                if (!groupedEntries.Remove(folder, out var list))
                {
                    return;
                }

                if (list.Count == 0)
                {
                    return;
                }

                int level = 1;
                foreach (string folderEntry in folder.Split('/', StringSplitOptions.RemoveEmptyEntries))
                {
                    string folderName = ObjectNames.NicifyVariableName(folderEntry);
                    if (createdFolders.Add(folderName))
                    {
                        entries.Add(new SearchTreeGroupEntry(new(folderName), level));
                    }

                    level++;
                }

                entries.AddRange(list.Select(x => new SearchTreeEntry(new(ObjectNames.NicifyVariableName(x.name), indentationIcon))
                {
                    level = level,
                    userData = x.type
                }));
            }
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (searchTreeEntry.userData is not Type type)
            {
                return false;
            }

            if (type == typeof(Group))
            {
                graphView.AddGroup(context.screenMousePosition);
            }
            else
            {
                graphView.AddNode(type, context.screenMousePosition);
            }

            return true;
        }
    }
}