using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameFlow.Attributes;
using GameFlow.Nodes;
using GameFlow.Ports;
using GameFlow.Utils;
using Unity.Properties;
using UnityEditor;
using UnityEngine;

namespace GameFlow.Editor
{
    [InitializeOnLoad]
    public sealed class GameFlowCodeGenerator : AssetPostprocessor
    {
        private const string NamespacePrefix = "GameFlowCodegen";
        private const string DirectoryName = "Assets/Scripts/Generated/GameFlow";
        private const string Extension = ".g.cs";

        private static readonly HashSet<string> UsedNames = new();
        private static readonly Dictionary<string, string> GuidToName = new();
        private static readonly HashSet<string> VisitedNodes = new();

        static GameFlowCodeGenerator()
        {
            AssemblyReloadEvents.beforeAssemblyReload += CleanOrphanedScripts;
            AssemblyReloadEvents.afterAssemblyReload += UpdateAssetTypes;
        }

        private static void UpdateAssetTypes()
        {
            foreach (string graphGuid in AssetDatabase.FindAssets($"t:{nameof(GameFlowGraph)}"))
            {
                string graphPath = AssetDatabase.GUIDToAssetPath(graphGuid);
                var graphAsset = AssetDatabase.LoadAssetAtPath<GameFlowGraph>(graphPath);

                try
                {
                    var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>($"Assets/Scripts/Generated/GameFlow/{graphGuid}{Extension}");

                    using var serializedObject = new SerializedObject(graphAsset);
                    using var property = serializedObject.FindProperty("gameFlowType").FindPropertyRelative("assemblyQualifiedName");
                    var type = scriptAsset.GetClass();
                    property.stringValue = type.AssemblyQualifiedName;
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
                catch
                {
                    Debug.LogWarning("Couldn't assign type to graph asset. Probably missing script, rebuilding...", graphAsset);
                    GenerateGameFlow(graphPath);
                }
            }
        }

        private static void CleanOrphanedScripts()
        {
            if (!Directory.Exists(DirectoryName))
            {
                return;
            }

            // Find all generated scripts
            int deletedCount = 0;
            string[] scripts = Directory.GetFiles(DirectoryName, $"*{Extension}");
            foreach (string scriptPath in scripts)
            {
                string guid = Path.GetFileName(scriptPath)[..^Extension.Length];
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (string.IsNullOrEmpty(path))
                {
                    AssetDatabase.DeleteAsset(scriptPath);
                    ++deletedCount;
                }
            }

            if (deletedCount == scripts.Length)
            {
                string parentDirectory = DirectoryName[..DirectoryName.LastIndexOf('/')];
                AssetDatabase.DeleteAsset(parentDirectory);
            }
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string importedAsset in importedAssets)
            {
                GenerateGameFlow(importedAsset);
            }

            foreach (string movedAsset in movedAssets)
            {
                GenerateGameFlow(movedAsset);
            }
        }

        private static string GetScriptPath(string graphPath)
        {
            return $"{DirectoryName}/{AssetDatabase.AssetPathToGUID(graphPath)}{Extension}";
        }

        private static void GenerateGameFlow(string assetPath)
        {
            var graphAsset = AssetDatabase.LoadAssetAtPath<GameFlowGraph>(assetPath);
            if (graphAsset == null) return;

            string graphAssetPath = assetPath;
            string graphDirectory = Path.GetDirectoryName(assetPath)!;
            string @namespace = NamespacePrefix + '.' + graphDirectory.Replace(Path.DirectorySeparatorChar, '.').Replace(" ", "");
            string className = graphAsset.name.Replace(" ", "");
            assetPath = GetScriptPath(assetPath);

            if (!Directory.Exists(DirectoryName))
            {
                Directory.CreateDirectory(DirectoryName);
            }

            UsedNames.Clear();
            GuidToName.Clear();
            VisitedNodes.Clear();

            using (var fileStream = new FileStream(assetPath, FileMode.Create))
            using (var writer = new StreamWriter(fileStream))
            {
                writer.WriteLine("//------------------------------------------------------------------------------\n" +
                                 "// <auto-generated>\n" +
                                 $"//     This code was generated using GameFlow from graph \"{graphAssetPath}\".\n" +
                                 "//\n" +
                                 "//     Changes to this file may cause incorrect behavior and will be lost if\n" +
                                 "//     the code is regenerated.\n" +
                                 "// </auto-generated>\n" +
                                 "//------------------------------------------------------------------------------");
                writer.WriteLine($"namespace {@namespace}");
                writer.WriteLine("{");
                writer.WriteLine($"    public sealed class {className} : {typeof(IGameFlow).FullName}");
                writer.WriteLine("    {");
                writer.WriteLine($"        public async {typeof(UniTask).FullName} {nameof(IGameFlow.RunGraph)}({typeof(IInjectData).FullName} injectData, {typeof(CancellationToken).FullName} cancellationToken)");
                writer.WriteLine("        {");

                string enterName = typeof(EnterNode).AssemblyQualifiedName;
                var enter = graphAsset.Nodes.Find(x => x.TypeInfo.AssemblyQualifiedName == enterName);
                WriteConnections(graphAsset, enter, "            ", writer);

                enterName = GuidToName[enter.Guid];
                writer.WriteLine($"            await {enterName}.{nameof(EnterNode.OnEnter)}.{nameof(FlowOutput.Invoke)}(cancellationToken);");

                writer.WriteLine("        }");
                writer.WriteLine("    }");
                writer.WriteLine("}");
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        internal static void DeleteGraph(string assetPath)
        {
            string scriptPath = GetScriptPath(assetPath);
            if (File.Exists(scriptPath))
            {
                File.Delete(scriptPath);
            }
        }

        private static string GetUniqueName(string name)
        {
            if (UsedNames.Add(name))
            {
                return name;
            }

            int index = 0;
            string newName;
            do
            {
                newName = name + index++;
            } while (!UsedNames.Add(newName));

            return newName;
        }

        private static string WriteConnections(GameFlowGraph graphAsset, GameFlowNodeData nodeData, string indent, TextWriter writer)
        {
            if (!VisitedNodes.Add(nodeData.Guid))
            {
                return GuidToName[nodeData.Guid];
            }

            if (!GuidToName.TryGetValue(nodeData.Guid, out string name))
            {
                name = GetUniqueName(string.Concat(nodeData.TypeInfo.Name.Where(char.IsLetterOrDigit)));
                GuidToName.Add(nodeData.Guid, name);

                var ctor = TypeUtils.GetMainConstructor(nodeData.TypeInfo.SystemType);
                string injectData = string.Join(", ", Array.ConvertAll(ctor.GetParameters(), x =>
                {
                    var injectKeyAttribute = x.GetCustomAttribute<InjectKeyAttribute>();
                    TypeUtils.GetNames(x.ParameterType, out _, out string fullName, out _);
                    return $"injectData.Get<{fullName}>({(injectKeyAttribute == null ? "" : $"\"{injectKeyAttribute.Key}\"")})";
                }));

                writer.WriteLine($"{indent}{nodeData.TypeInfo.FullName} {name} = new({injectData});");
            }

            foreach (var edgeData in graphAsset.Edges.Where(x => x.FromPort.NodeGuid == nodeData.Guid))
            {
                var toNode = graphAsset.Nodes.Find(x => x.Guid == edgeData.ToPort.NodeGuid);
                string toName = WriteConnections(graphAsset, toNode, indent, writer);

                if (string.IsNullOrEmpty(edgeData.FromPort.AddInvokeOverride))
                {
                    writer.WriteLine($"{indent}{name}.{edgeData.FromPort.Name}.{nameof(FlowOutput.AddInvoke)}({toName}.{edgeData.ToPort.Name}.{nameof(FlowInput.Method)});");
                }
                else
                {
                    writer.WriteLine($"{indent}{name}.{edgeData.FromPort.AddInvokeOverride}(\"{edgeData.FromPort.Name}\", {toName}.{edgeData.ToPort.Name}.{nameof(FlowInput.Method)});");
                }
            }

            return name;
        }
    }

    public sealed class GameFlowModificationProcessor : AssetModificationProcessor
    {
        private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            GameFlowCodeGenerator.DeleteGraph(assetPath);
            return AssetDeleteResult.DidNotDelete;
        }

        private static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
        {
            GameFlowCodeGenerator.DeleteGraph(sourcePath);
            return AssetMoveResult.DidNotMove;
        }
    }
}