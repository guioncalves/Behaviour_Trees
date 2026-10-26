using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Leaf : Node
{
    public delegate Status Tick();

    public Tick ProcessMethod;

    public Leaf() { }

    public Leaf(string n, Tick pm) : base(n)
    {
        ProcessMethod = pm;
    }

    public override Status Process()
    {
        if (ProcessMethod == null)
            return Status.FAILURE;

        return ProcessMethod();
    }
}
