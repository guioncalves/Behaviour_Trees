
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Robo_Behaviour : MonoBehaviour
{
    private Behaviour_Tree tree;
    private NavMeshAgent agent;

    [Header("Objetivos")]
    public GameObject diamond;
    public GameObject van;

    [Header("Movimento")]
    public float arrivalDistance = 1f;

    private bool hasDiamond = false;
    private bool missionComplete = false;

    void Start()
    {
        Debug.Log("O robo iniciou!");

        agent = GetComponent<NavMeshAgent>();

        if (diamond == null || van == null)
        {
            Debug.LogError(
                "Atribui o Diamond e a Van no Inspector!"
            );

            enabled = false;
            return;
        }

        // Criar a Behaviour Tree
        tree = new Behaviour_Tree();

        Node steal = new Node("Steal Something");
        Node goToDiamond = new Node("Go To Diamond");
        Node goToVan = new Node("Go To Van");

        steal.AddChild(goToDiamond);
        steal.AddChild(goToVan);

        tree.AddChild(steal);

        tree.PrintTree();

        Debug.Log("Behaviour Tree criada com sucesso!");

        // Primeiro objetivo: cristal
        GoToDiamond();
    }

    void Update()
    {
        if (missionComplete || !agent.isOnNavMesh)
            return;

        if (agent.pathPending)
            return;

        // Ainda nao roubou o cristal
        if (!hasDiamond)
        {
            if (ReachedDestination())
            {
                hasDiamond = true;

                Debug.Log("Cristal recolhido!");

                diamond.SetActive(false);

                GoToVan();
            }

            return;
        }

        // Ja tem o cristal: chegar a carrinha
        if (ReachedDestination())
        {
            missionComplete = true;

            agent.ResetPath();

            Debug.Log("Missao concluida! O robo chegou a carrinha.");
        }
    }

    void GoToDiamond()
    {
        if (!agent.isOnNavMesh)
        {
            Debug.LogError("O robo nao esta no NavMesh!");
            return;
        }

        Debug.Log("O robo vai ate ao cristal.");

        if (!agent.SetDestination(diamond.transform.position))
        {
            Debug.LogWarning("Nao foi possivel definir o destino do cristal.");
        }
    }

    void GoToVan()
    {
        Debug.Log("O robo vai ate a carrinha.");

        if (!agent.SetDestination(van.transform.position))
        {
            Debug.LogWarning("Nao foi possivel definir o destino da carrinha.");
        }
    }

    bool ReachedDestination()
    {
        return !agent.pathPending &&
               agent.hasPath &&
               agent.pathStatus == NavMeshPathStatus.PathComplete &&
               agent.remainingDistance <=
                   Mathf.Max(arrivalDistance, agent.stoppingDistance);
    }
}
