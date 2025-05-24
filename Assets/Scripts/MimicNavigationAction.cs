using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Mimic Navigation", story: "[Mimic] Navigation to [Player]", category: "Action/Navigation", id: "35a92ec3c2012637a44bcf30729cdbc1")]
public partial class MimicNavigationAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Mimic;
    [SerializeReference] public BlackboardVariable<GameObject> Player;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

