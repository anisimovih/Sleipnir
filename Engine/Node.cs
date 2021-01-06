using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedOwl.Sleipnir.Engine
{
    public interface INode
    {
        IGraph Graph { get; }
        string NodeId { get; }
        Rect NodeRect { get; set; }
        Vector2 NodePosition { get; set; }

        // TODO: Convert to KeyedCollection based on performance hit of using Dictionary.Values - which seems to create a ValueCollection every time?
        Dictionary<string, IValuePort> ValueInPorts { get; }
        Dictionary<string, IValuePort> ValueOutPorts { get; }
        
        bool IsDefined { get; }
        
        void Definition(IGraph graph);
    }

    public interface IFlowNode : INode
    {
        bool IsFlowRoot { get; }
        
        Dictionary<string, IFlowPort> FlowInPorts { get; }
        Dictionary<string, IFlowPort> FlowOutPorts { get; }
        
        void Initialize(ref IFlow flow);
    }

    [Serializable]
    public abstract class Node : IFlowNode 
    {
        public IGraph Graph { get; private set; }
        
#if ODIN_INSPECTOR 
        [HideInInspector]
#endif
        [SerializeField] 
        private string nodeId = Guid.NewGuid().ToString();

        public string NodeId => nodeId;

#if ODIN_INSPECTOR 
        [HideInInspector]
#endif
        [SerializeField] 
        private Rect nodeRect;

        public Rect NodeRect
        {
            get => nodeRect;
            set => nodeRect = value;
        }

        public Vector2 NodePosition
        {
            get => nodeRect.position;
            set => nodeRect.position = value;
        }
        
        public Dictionary<string, IValuePort> ValueInPorts { get; protected set; }
        public Dictionary<string, IValuePort> ValueOutPorts { get; protected set; }
        
        public Dictionary<string, IFlowPort> FlowInPorts { get; protected set; }
        public Dictionary<string, IFlowPort> FlowOutPorts { get; protected set; }

        #region Definition

        private void DefineValuePorts()
        {
            ValueInPorts = new Dictionary<string, IValuePort>();
            ValueOutPorts = new Dictionary<string, IValuePort>();
            if (!SleipnirGraphReflector.NodeCache.Get(GetType(), out var data)) return;
            foreach (var valuePort in data.ValuePorts)
            {
                switch (valuePort.Direction)
                {
                    case PortDirection.Input:
                        ValueInPorts.Add(valuePort.Name, valuePort.GetOrCreatePort(this));
                        break;
                    case PortDirection.Output:
                        ValueOutPorts.Add(valuePort.Name, valuePort.GetOrCreatePort(this));
                        break;
                }

                //Debug.Log($"{NodeTitle}Node has Value Port '{valuePort.Name} | {valuePort.Direction}'");
            }
        }
        
        private void DefineFlowPorts()
        {
            FlowInPorts = new Dictionary<string, IFlowPort>();
            FlowOutPorts = new Dictionary<string, IFlowPort>();
            if (!SleipnirGraphReflector.NodeCache.Get(GetType(), out var data)) return;
            foreach (var flowPort in data.FlowPorts)
            {
                switch (flowPort.Direction)
                {
                    case PortDirection.Input:
                        FlowInPorts.Add(flowPort.Name, flowPort.GetOrCreatePort(this));
                        break;
                    case PortDirection.Output:
                        FlowOutPorts.Add(flowPort.Name, flowPort.GetOrCreatePort(this));
                        break;
                }
                //Debug.Log($"{NodeTitle}Node has Flow Port '{flowPort.Name} | {flowPort.Direction}'");
            }
        }
        
        [field: NonSerialized]
        public bool IsDefined { get; protected set; }

        public void Definition(IGraph graph)
        {
            BeforeDefinition();
            if (IsDefined) return;
            Graph = graph;
            try
            {
                // TODO: can we scope this better?
                DefineValuePorts();
                DefineFlowPorts();
                OnDefinition();
                IsDefined = true;
                AfterDefinition();
            }
            catch
            {
                Debug.LogWarning($"Failed to Define Node {GetType().FullName} | {NodeId}");
                throw;
            }
        }

        protected virtual void BeforeDefinition() {}
        protected virtual void OnDefinition() {}
        
        protected virtual void AfterDefinition() {}
        
        #endregion

        #region Initialization
        
        public bool IsFlowRoot => SleipnirGraphReflector.NodeCache.Get(GetType(), out var data) && data.IsFlowRoot;

        public void Initialize(ref IFlow flow)
        {
            try
            {
                InitializePorts(ref flow);
                OnInitialize(ref flow);
            }
            catch
            {
                Debug.LogWarning($"Failed to Initialize Node {this} | {NodeId}");
                throw;
            }
        }

        private void InitializePorts(ref IFlow flow)
        {
            foreach (var valueIn in ValueInPorts.Values)
            {
                valueIn.Initialize(ref flow);
            }

            foreach (var valueOut in ValueOutPorts.Values)
            {
                valueOut.Initialize(ref flow);
            }
            
            foreach (var flowIn in FlowInPorts.Values)
            {
                flowIn.Initialize(ref flow);
            }
            
            foreach (var flowOut in FlowOutPorts.Values)
            {
                flowOut.Initialize(ref flow);
            }
        }
        
        protected virtual void OnInitialize(ref IFlow flow) {}

        #endregion

        public override string ToString() => $"{GetType().Name}[{NodeId.Substring(0,8)}]";
    }
}
