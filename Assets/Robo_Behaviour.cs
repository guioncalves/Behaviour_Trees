
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

    [Header("Pontos opcionais da BackDoor")]
    public Transform BackDoorOutside;
    public Transform BackDoorInside;

    [Header("Movimento")]
    public float arrivalDistance = 0.5f;

    private bool hasDiamond = false;
    private bool missionComplete = false;

    private bool reachedFrontDoor = false;
    private bool reachedBackDoor = false;
    private bool reachedBackDoorOutside = false;

    private bool destinationSet = false;
    private Vector3 currentDestination;

    private bool doorChosen = false;
    private GameObject chosenDoor;

    void Start()
    {
        Debug.Log("O robo iniciou!");

        agent = GetComponent<NavMeshAgent>();

        // Verificar referências obrigatórias.
        if (diamond == null ||
            van == null ||
            FrontDoor == null ||
            BackDoor == null)
        {
            Debug.LogError(
                "Atribui Diamond, Van, FrontDoor e BackDoor no Inspector!"
            );

            enabled = false;
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError(
                "O robo nao esta sobre o NavMesh!"
            );

            enabled = false;
            return;
        }

        if (agent.speed <= 0f)
        {
            Debug.LogError(
                "A velocidade do NavMeshAgent deve ser maior que zero!"
            );

            enabled = false;
            return;
        }

        agent.isStopped = false;

        // =====================================
        // CRIAR A BEHAVIOUR TREE
        // =====================================

        tree = new Behaviour_Tree();

        Sequence steal = new Sequence("Steal Something");

        // Escolher uma porta.
        Leaf chooseDoor = new Leaf(
            "Choose Door",
            ChooseDoor
        );

        // Recolher o diamante.
        Leaf goToDiamond = new Leaf(
            "Go To Diamond",
            GoToDiamond
        );

        // Regressar à carrinha.
        Leaf goToVan = new Leaf(
            "Go To Van",
            GoToVan
        );

        steal.AddChild(chooseDoor);
        steal.AddChild(goToDiamond);
        steal.AddChild(goToVan);

        tree.AddChild(steal);

        tree.PrintTree();

        Debug.Log("Behaviour Tree criada!");
    }

    void Update()
    {
        if (missionComplete || !agent.isOnNavMesh)
            return;

        Node.Status result = tree.Process();

        if (result == Node.Status.SUCCESS)
        {
            missionComplete = true;

            agent.ResetPath();

            Debug.Log(
                "MISSAO CONCLUIDA! O ladrao chegou a carrinha!"
            );
        }
        else if (result == Node.Status.FAILURE)
        {
            Debug.LogError("A missao falhou!");

            agent.ResetPath();

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

        // Definir destino apenas quando necessário.
        if (!destinationSet ||
            Vector3.Distance(
                currentDestination,
                destination
            ) > 0.1f)
        {
            agent.isStopped = false;

            bool success = agent.SetDestination(destination);

            if (!success)
            {
                Debug.LogError(
                    "Nao foi possivel definir o destino: " +
                    destination
                );

                return Node.Status.FAILURE;
            }

            currentDestination = destination;
            destinationSet = true;

            Debug.Log("Novo destino: " + destination);

            return Node.Status.RUNNING;
        }

        // Esperar pelo cálculo do percurso.
        if (agent.pathPending)
            return Node.Status.RUNNING;

        // Verificar se existe um caminho completo.
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid ||
            agent.pathStatus == NavMeshPathStatus.PathPartial)
        {
            Debug.LogError(
                "O NavMesh nao encontrou um caminho completo!"
            );

            return Node.Status.FAILURE;
        }

        if (float.IsInfinity(agent.remainingDistance))
            return Node.Status.RUNNING;

        float threshold = Mathf.Max(
            arrivalDistance,
            agent.stoppingDistance
        );

        // Verificar chegada ao destino.
        if (agent.remainingDistance <= threshold &&
            (!agent.hasPath ||
             agent.velocity.sqrMagnitude < 0.01f))
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
        // Escolher a porta apenas uma vez.
        if (!doorChosen)
        {
            Lock frontLock = FrontDoor.GetComponent<Lock>();
            Lock backLock = BackDoor.GetComponent<Lock>();

            if (frontLock == null || backLock == null)
            {
                Debug.LogError(
                    "FrontDoor ou BackDoor nao tem componente Lock!"
                );

                return Node.Status.FAILURE;
            }

            if (!frontLock.isLocked)
            {
                chosenDoor = FrontDoor;

                Debug.Log(
                    "FrontDoor desbloqueada. Usar FrontDoor."
                );
            }
            else if (!backLock.isLocked)
            {
                chosenDoor = BackDoor;

                Debug.Log(
                    "FrontDoor bloqueada. Usar BackDoor."
                );
            }
            else
            {
                Debug.LogWarning(
                    "As duas portas estao bloqueadas!"
                );

                return Node.Status.FAILURE;
            }

            doorChosen = true;
        }

        // Executar a ação da porta escolhida.
        if (chosenDoor == FrontDoor)
        {
            return GoToFrontDoor();
        }

        if (chosenDoor == BackDoor)
        {
            return GoToBackDoor();
        }

        return Node.Status.FAILURE;
    }

    // =====================================
    // FRONTDOOR
    // =====================================

    public Node.Status GoToFrontDoor()
    {
        if (reachedFrontDoor)
            return Node.Status.SUCCESS;

        Node.Status result = GoToLocation(
            FrontDoor.transform.position
        );

        if (result == Node.Status.SUCCESS)
        {
            reachedFrontDoor = true;

            Debug.Log(
                "O ladrao chegou a FrontDoor!"
            );
        }

        return result;
    }

    // =====================================
    // BACKDOOR
    // =====================================

    public Node.Status GoToBackDoor()
    {
        if (reachedBackDoor)
            return Node.Status.SUCCESS;

        // Primeiro, ir ao exterior da BackDoor.
        if (BackDoorOutside != null && !reachedBackDoorOutside)
        {
            Node.Status outsideResult = GoToLocation(
                BackDoorOutside.position
            );

            if (outsideResult != Node.Status.SUCCESS)
                return outsideResult;

            reachedBackDoorOutside = true;

            Debug.Log(
                "O ladrao chegou ao exterior da BackDoor!"
            );
        }

        // Depois, entrar pela BackDoor.
        Vector3 destination;

        if (BackDoorInside != null)
        {
            destination = BackDoorInside.position;
        }
        else
        {
            destination = BackDoor.transform.position;
        }

        Node.Status result = GoToLocation(destination);

        if (result == Node.Status.SUCCESS)
        {
            reachedBackDoor = true;

            Debug.Log(
                "O ladrao chegou a BackDoor!"
            );
        }

        return result;
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

            Debug.Log(
                "O ladrao recolheu o diamante!"
            );
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

        return GoToLocation(
            van.transform.position
        );
    }
}
