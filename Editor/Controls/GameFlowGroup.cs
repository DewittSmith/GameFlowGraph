using System.Collections.Generic;
using System.Linq;
using GameFlow.Editor.Graph;
using GameFlow.Editor.Utils;
using UnityEditor.Experimental.GraphView;

namespace GameFlow.Editor.Controls
{
    public sealed class GameFlowGroup : Group
    {
        public GameFlowGroupData Data => (GameFlowGroupData)userData;

        public GameFlowGroup()
        {
            userData = new GameFlowGroupData { Guid = GuidUtils.Generate() };
        }

        public GameFlowGroup(GameFlowGroupData data)
        {
            userData = data;
            title = data.Name;
        }

        protected override void OnElementsAdded(IEnumerable<GraphElement> elements)
        {
            var elementsList = elements.ToList();

            foreach (var graphElement in elementsList)
            {
                if (graphElement is GameFlowNode gameFlowNode)
                {
                    Data.ContainedNodes.Add(gameFlowNode.Data.Guid);
                }
            }

            base.OnElementsAdded(elementsList);
        }

        protected override void OnElementsRemoved(IEnumerable<GraphElement> elements)
        {
            var elementsList = elements.ToList();

            foreach (var graphElement in elementsList)
            {
                if (graphElement is GameFlowNode gameFlowNode)
                {
                    Data.ContainedNodes.Remove(gameFlowNode.Data.Guid);
                }
            }

            base.OnElementsRemoved(elementsList);
        }
    }
}