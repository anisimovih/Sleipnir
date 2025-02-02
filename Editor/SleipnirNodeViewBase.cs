using RedOwl.Sleipnir.Engine;
using Sirenix.OdinInspector;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using NodeView = UnityEditor.Experimental.GraphView.Node;
using PortView = UnityEditor.Experimental.GraphView.Port;

namespace RedOwl.Sleipnir.Editor
{
    public interface IEditorNodeView : INodeView
    {
        IEdgeConnectorListener EdgeListener { get; set; }
    }
    
    public class SleipnirNodeViewBase : NodeView, IEditorNodeView
    {
        public IEdgeConnectorListener EdgeListener { get; set; }
        public VisualElement ValueInPortContainer => inputContainer;
        public VisualElement ValueOutPortContainer => outputContainer;
        public VisualElement FlowInPortContainer { get; private set; }
        public VisualElement FlowOutPortContainer { get; private set; }
        public INode Node => (INode) userData;
        
        public NodeInfo ReflectionData { get; private set; }

        public bool IsMoveable => ReflectionData.Moveable;
        

        #region API
        protected virtual void OnInitialize() { }
        protected virtual void OnDestroy() { }
        protected virtual void OnError() { }
        #endregion

        public void Initialize(INode node, NodeInfo data)
        {
            userData = node;
            ReflectionData = data;
            name = node.NodeId;
            SetPosition(new Rect(node.NodePosition, ReflectionData.Size));
            title = $"{ReflectionData.Name}";
            tooltip = ReflectionData.Help;
            if (!ReflectionData.Deletable)
            {
                capabilities &= ~Capabilities.Deletable;
            }
            if (!ReflectionData.Moveable)
            {
                capabilities &= ~Capabilities.Movable;
            }

            CreateBody(node);
            CreateFlowPortContainers();
            if (node is IFlowNode flowNode)
            {
                CreateExecuteButton(flowNode);
                CreateFlowPorts(flowNode);
            }
            AttachFlowPortContainers();
            CreateValuePorts(node);
            RefreshExpandedState();
            RefreshPorts();
            
            RegisterCallback<DetachFromPanelEvent>((e) => Destroy());
            
            OnInitialize();
        }
        
        internal void Destroy()
        {
            OnDestroy();
        }
        
        private void CreateBody(INode node)
        {
#if ODIN_INSPECTOR
            var tree = Sirenix.OdinInspector.Editor.PropertyTree.Create(node);
            bool useUndo = node is Object;
            extensionContainer.Add(new IMGUIContainer(() =>
            {
                Sirenix.Utilities.Editor.GUIHelper.PushLabelWidth(100);
                tree.Draw(useUndo);
                Sirenix.Utilities.Editor.GUIHelper.PopLabelWidth();
            }) { name = "OdinTree"});
#else
            // TODO: Draw Property Field's with UI Elements
            // http://wiki.unity3d.com/index.php/ExposePropertiesInInspector_Generic
#endif
        }
        
        private PortView CreatePortView(IPort port, Orientation orientation)
        {
            var view = new SleipnirPortView(orientation, port.Direction, port.Capacity, port.ValueType, EdgeListener)
            {
                name = port.Name,
                userData = port,
                portName = port.Name,
            };
            return view;
        }
        
        private void CreateValuePorts(INode node)
        {
            foreach (var valuePort in node.ValueInPorts.Values)
            {
                ValueInPortContainer.Add(CreatePortView(valuePort, Orientation.Horizontal));
            }
            
            foreach (var valuePort in node.ValueOutPorts.Values)
            {
                ValueOutPortContainer.Add(CreatePortView(valuePort, Orientation.Horizontal));
            }
        }

        private void CreateFlowPortContainers()
        {
            FlowInPortContainer = new VisualElement {name = "FlowPorts"};
            FlowInPortContainer.AddToClassList("FlowInPorts");
            FlowOutPortContainer = new VisualElement {name = "FlowPorts"};
            FlowOutPortContainer.AddToClassList("FlowOutPorts");
        }

        private void CreateExecuteButton(IFlowNode node)
        {
            if (!node.IsFlowRoot) return;
            var button = new Button(() => new Flow(Node.Graph, node).Execute()) {text = "Execute"};
            titleButtonContainer.Add(button);
        }

        private void CreateFlowPorts(IFlowNode node)
        {
            foreach (var flowPort in node.FlowInPorts.Values)
            {
                FlowInPortContainer.Add(CreatePortView(flowPort, Orientation.Vertical));
            }
            
            foreach (var flowPort in node.FlowOutPorts.Values)
            {
                FlowOutPortContainer.Add(CreatePortView(flowPort, Orientation.Vertical));
            }
        }

        private void AttachFlowPortContainers()
        {
            if (FlowInPortContainer.childCount > 0) mainContainer.parent.Insert(0, FlowInPortContainer);
            if (FlowOutPortContainer.childCount > 0) mainContainer.parent.Add(FlowOutPortContainer);
        }
    }
    
    public static class NodeViewExtensions
    {
        //public static INode INode(this NodeView self) => (INode) self.userData;
        // public static string Id(this NodeView self) => self.INode().NodeId;
        // public static uint Id(this PortView self) => (uint)self.userData;
    }
}