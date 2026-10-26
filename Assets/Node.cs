
using System.Collections.Generic;

public class Node
{
    // Possíveis resultados de uma ação.
    public enum Status
    {
        SUCCESS,
        RUNNING,
        FAILURE
    }

    // Estado atual do nó.
    public Status status = Status.RUNNING;

    // Lista de nós filhos.
    public List<Node> children = new List<Node>();

    // Índice do filho que está a ser executado.
    public int currentChild = 0;

    // Nome do nó.
    public string name;

    // Construtor vazio.
    public Node()
    {
        name = "Node";
    }

    // Construtor com nome.
    public Node(string n)
    {
        name = n;
    }

    // Método executado pela Behaviour Tree.
    public virtual Status Process()
    {
        status = Status.SUCCESS;
        return status;
    }

    // Adicionar um nó filho.
    public void AddChild(Node n)
    {
        if (n != null)
        {
            children.Add(n);
        }
    }
}
