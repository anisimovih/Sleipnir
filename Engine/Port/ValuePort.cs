using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Scripting;

namespace RedOwl.Sleipnir.Engine
{
    public interface IValuePort : IPort
    {
        object DefaultValue { get; }
        object WeakValue { get; }
        void Definition(INode node, IValuePortAttribute info);
        IValuePort Clone(IGraph graph);
        IEnumerable Execute();
    }

    [Preserve]
    public class ValuePort<T> : Port, IValuePort
    {
        public object DefaultValue { get; private set; }
        public object WeakValue => Value;
        
        private IValuePort Linked { get; set; }
        
        public T Value
        {
            get => Flow != null && Flow.ContainsKey(Id) ? Flow.Get<T>(Id) : (T) DefaultValue;
            set
            {
                if (Flow == null)
                {
                    DefaultValue = value;
                }
                else
                {
                    Flow.Set(Id, value);
                }
            }
        }

        [Preserve]
        public ValuePort()
        {
            Value = default;
        }
        
        public ValuePort(T defaultValue)
        {
            Value = defaultValue;
        }

        public void SetDefault(T defaultValue) => DefaultValue = defaultValue;
        
        public void Definition(INode node, IValuePortAttribute info)
        {
            Id = new PortId(node.NodeId, info.Name);
            Node = node;
            Name = info.Name;
            Direction = info.Direction;
            Capacity = info.Capacity;
            GraphPort = info.GraphPort;
            ValueType = typeof(T);
        }

        public IValuePort Clone(IGraph graph)
        {
            string newName = (Node is IGraphValuePortNode graphPortNode) ? graphPortNode.Name : $"{Name} {Direction.Flip()}";
            var port = new ValuePort<T>()
            {
                Id = new PortId(graph.NodeId, newName),
                Node = graph,
                Name = newName,
                Direction = Direction.Flip(),
                Capacity = Capacity,
                ValueType = ValueType,
                DefaultValue = DefaultValue,
                Flow = Flow,
                Linked = this,
            };
            return port;
        }

        public override void Initialize(ref IFlow flow)
        {
            Flow = flow;
            Flow.Set(Id, (T)DefaultValue);
        }

        public IEnumerable Execute()
        {
            // TODO: Refactor This - There has to be a better way
            switch (Direction)
            {
                case PortDirection.Input:
                    foreach (var connection in Graph.ValueInConnections.SafeGet(Id))
                    {
                        var nextNode = Graph.GetNode(connection.Node); //  TODO: Needs saftey check?
                        var nextPort = nextNode?.ValueOutPorts[connection.Port]; // TODO: Needs saftey check?
                        if (nextNode != null && nextPort != null)
                        {
                            yield return nextPort;
                            Flow.Set(Id, nextPort.WeakValue);
                        }
                    }
                    if (Linked != null)
                    {
                        // Debug.Log($"[Input] Has Linked Port | {this} => {Linked}");
                        Flow.Set(Linked.Id, Value);
                    }
                    break;
                case PortDirection.Output:
                    if (Linked != null)
                    {
                        foreach (var connection in Linked.Graph.ValueInConnections.SafeGet(Linked.Id))
                        {
                            var nextNode = Linked.Graph.GetNode(connection.Node); //  TODO: Needs saftey check?
                            var nextPort = nextNode?.ValueOutPorts[connection.Port]; // TODO: Needs saftey check?
                            if (nextNode != null && nextPort != null)
                            {
                                yield return nextPort;
                                Flow.Set(Id, nextPort.WeakValue);
                            }
                        }
                    }
                    else
                    {
                        Flow.Set(Id, Value);
                    }
                    break;
            }
        }

        public static implicit operator T(ValuePort<T> self) => self.Value;
        public static implicit operator ValuePort<T>(T self) => new ValuePort<T>(self);
    }
}