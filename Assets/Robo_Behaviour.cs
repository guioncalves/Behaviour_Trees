
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

    [Header("Portas")]
    public GameObject FrontDoor;
    public GameObject BackDoor;

    [Header("Movimento")]
    public float arrivalDistance = 1.5f;

    private bool hasDiamond = false;
    private bool missionComplete = false;

    private bool doorChecked = false;
    private bool useBackDoor = false;

    private bool destinationSet = false;
    private Vector3 currentDestination;

    void Start()
    {
        Debug.Log("O robo iniciou!");

        agent = GetComponent<NavMeshAgent>();

        if (diamond == null || van == null ||
            FrontDoor == null || BackDoor == null)
        {
            Debug.LogError("Faltam referencias no Inspector!");
            enabled = false;
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("O robo nao esta no NavMesh!");
            enabled = false;
            return;
        }

        // Guardar o obstaculo da porta da frente.
        NavMeshObstacle frontObstacle =
            FrontDoor.GetComponent<NavMeshObstacle>();

        if (frontObstacle == null)
        {
            Debug.LogError(
                "FrontDoor precisa de um NavMeshObstacle!"
            );

            enabled = false;
            return;
        }

        // Inicialmente, a porta nao bloqueia o NavMesh.
        frontObstacle.carving = false;

        // Criar Behaviour Tree.
        tree = new Behaviour_Tree();

        Sequence steal = new Sequence("Steal Something");

        steal.AddChild(new Leaf("Choose Door", ChooseDoor));
        steal.AddChild(new Leaf("Go To Diamond", GoToDiamond));
        steal.AddChild(new Leaf("Go To Van", GoToVan));

        tree.AddChild(steal);

        tree.PrintTree();

        Debug.Log("Behaviour Tree criada!");
    }

    void Update()
    {
        if (missionComplete)
            return;

        Node.Status result = tree.Process();

        if (result == Node.Status.SUCCESS)
        {
            missionComplete = true;
            agent.ResetPath();

            Debug.Log("MISSAO CONCLUIDA!");
        }
        else if (result == Node.Status.FAILURE)
        {
            agent.ResetPath();

            Debug.LogError("A missao falhou!");
            enabled = false;
        }
    }

    // =====================================
    // MOVIMENTO
    // =====================================

    public Node.Status GoToLocation(Vector3 destination)
    {
        if (!agent.isOnNavMesh)
            return Node.Status.FAILURE;

        if (!destinationSet ||
            Vector3.Distance(currentDestination, destination) > 0.1f)
        {
            agent.isStopped = false;

            if (!agent.SetDestination(destination))
            {
                Debug.LogError(
                    "Nao foi possivel definir destino: " + destination
                );

                return Node.Status.FAILURE;
            }

            currentDestination = destination;
            destinationSet = true;

            Debug.Log("Novo destino: " + destination);

            return Node.Status.RUNNING;
        }

        if (agent.pathPending)
            return Node.Status.RUNNING;

        if (agent.pathStatus == NavMeshPathStatus.PathInvalid ||
            agent.pathStatus == NavMeshPathStatus.PathPartial)
        {
            Debug.LogError("Caminho invalido para: " + destination);
            return Node.Status.FAILURE;
        }

        if (float.IsInfinity(agent.remainingDistance))
            return Node.Status.RUNNING;

        float distance = Mathf.Max(
            arrivalDistance,
            agent.stoppingDistance
        );

        if (agent.remainingDistance <= distance)
        {
            destinationSet = false;
            agent.ResetPath();

            return Node.Status.SUCCESS;
        }

        return Node.Status.RUNNING;
    }

    // =====================================
    // ESCOLHER PORTA
    // =====================================

    public Node.Status ChooseDoor()
    {
        if (!doorChecked)
        {
            // Primeiro, ir a porta da frente.
            Node.Status result = GoToFrontDoor();

            if (result != Node.Status.SUCCESS)
                return result;

            Lock frontLock = FrontDoor.GetComponent<Lock>();

            if (frontLock == null)
            {
                Debug.LogError("FrontDoor nao tem Lock!");
                return Node.Status.FAILURE;
            }

            doorChecked = true;

            if (frontLock.isLocked)
            {
                useBackDoor = true;

                // Ativar Carve quando chega a porta trancada.
                NavMeshObstacle obstacle =
                    FrontDoor.GetComponent<NavMeshObstacle>();

                obstacle.carving = true;

                Debug.Log(
                    "FrontDoor trancada! Carve ativado. Ir para BackDoor."
                );
            }
            else
            {
                Debug.Log("FrontDoor aberta!");
            }
        }

        if (!useBackDoor)
            return Node.Status.SUCCESS;

        return GoToBackDoor();
    }

    // =====================================
    // FRONTDOOR
    // =====================================

    public Node.Status GoToFrontDoor()
    {
        return GoToLocation(FrontDoor.transform.position);
    }

    // =====================================
    // BACKDOOR
    // =====================================


    public Node.Status GoToBackDoor()
    {
        // Aproximar-se da porta traseira.
        Node.Status result = GoToLocation(BackDoor.transform.position);

        if (result != Node.Status.SUCCESS)
            return result;

        // Chegou a porta: destruir a porta traseira.
        Debug.Log("O ladrao destruiu a BackDoor!");

        Destroy(BackDoor);

        // A porta foi destruida, pode continuar para o cristal.
        return Node.Status.SUCCESS;
    }


    // =====================================
    // DIAMANTE
    // =====================================

    public Node.Status GoToDiamond()
    {
        if (hasDiamond)
            return Node.Status.SUCCESS;

        Node.Status result = GoToLocation(
            diamond.transform.position
        );

        if (result == Node.Status.SUCCESS)
        {
            hasDiamond = true;
            diamond.SetActive(false);

            Debug.Log("O ladrao roubou o diamante!");
        }

        return result;
    }

    // =====================================
    // CARRINHA
    // =====================================

    public Node.Status GoToVan()
    {
        if (!hasDiamond)
            return Node.Status.FAILURE;

        return GoToLocation(van.transform.position);
    }
}
