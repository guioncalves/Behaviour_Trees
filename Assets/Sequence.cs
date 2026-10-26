using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sequence : Node
{
    public Sequence(string n) : base(n) { }

    public override Status Process()
    {
        if (children.Count == 0)
            return Status.SUCCESS;

        while (currentChild < children.Count)
        {
            Status result = children[currentChild].Process();

            if (result == Status.RUNNING)
                return Status.RUNNING;

            if (result == Status.FAILURE)
            {
                currentChild = 0;
                return Status.FAILURE;
            }

            currentChild++;
        }

        currentChild = 0;
        return Status.SUCCESS;
    }
}
