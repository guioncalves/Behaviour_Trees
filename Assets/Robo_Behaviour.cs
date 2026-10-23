
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Robo_Behaviour : MonoBehaviour
{
    Behaviour_Tree tree;
    public GameObject diamond;
    public GameObject van;

    void Start()
    {
        Debug.Log("O robô iniciou!");

        tree = new Behaviour_Tree();

        Node steal = new Node("Steal Something");
        Node goToDiamond = new Node("Go To Diamond");
        Node goToVan = new Node("Go To Van");

        steal.AddChild(goToDiamond);
        steal.AddChild(goToVan);

        tree.AddChild(steal);

        Debug.Log("Behaviour Tree criada com sucesso!");
        Debug.Log("Nó principal: Steal Something");
        Debug.Log("Filhos: Go To Diamond e Go To Van");
    }

    void Update()
    {

    }
}
