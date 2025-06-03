This is a game flow package, that I made to understand how Unity GraphView works and how can I create codegen.
Not tested, not production-ready, uses a lot of reflection (only in Editor tho, runtime is fine), just a neat research.

To create a graph, go to `Window/Game Flow Graph`.

To create a node, you need `FlowInput` and `FlowOutput`. It defines an entry and an exit for a node. Look at [IfNode](https://github.com/DewittSmith/com.dewittsmith.game-flow/blob/main/Runtime/Nodes/IfNode.cs) source code for example.

It is possible to create generic nodes with dynamic port count. Use `[GenerateGenericSubclasses]` and `[AddInvokeOverride]` attributes to enable that. Look at [SwitchNode](https://github.com/DewittSmith/com.dewittsmith.game-flow/blob/main/Runtime/Nodes/SwitchNode.cs) source code for example.

Running a graph is done by calling `IGameFlow.RunGraph` and `GameFlowGraph` is considered `IGameFlow` as well.
```cs
[SerializeField]
private GameFlowGraph graph;

private void Start()
{
    graph.RunGraph(IInjectData.Empty, destroyCancellationToken).Forget();
}
```


Adding this package to project can be done by using PackageManager, just use `https://github.com/DewittSmith/com.dewittsmith.game-flow.git?path=GameFlow` git url.
