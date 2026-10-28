
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

    [Header("Recompensa")]
    public int money = 0;
    public int crystalValue = 300;

    private bool hasDiamond = false;
    private bool missionComplete = false;

    private bool doorChecked = false;
    private bool useBackDoor = false;
    private bool backDoorDestroyed = false;

    private bool destinationSet = false;
    private Vector3 currentDestination;

    // Posicao final pretendida na carrinha.
    private readonly Vector3 vanPosition =
        new Vector3(25.85727f, 1.36f, -11f);

    void Start()
    {
        Debug.Log("O robo iniciou!");

        agent = GetComponent<NavMeshAgent>();

        if (diamond == null ||
            van == null ||
            FrontDoor == null ||
            BackDoor == null)
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

        // Inicialmente, nao recortar o NavMesh.
        frontObstacle.carveOnlyStationary = false;
        frontObstacle.carving = false;

        // =====================================
        // BEHAVIOUR TREE
        // =====================================

        tree = new Behaviour_Tree();

        Sequence steal = new Sequence("Steal Something");

        steal.AddChild(
            new Leaf("Choose Door", ChooseDoor)
        );

        steal.AddChild(
            new Leaf("Go To Diamond", GoToDiamond)
        );

        steal.AddChild(
            new Leaf("Go To Van", GoToVan)
        );

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
            agent.isStopped = true;

            Debug.Log("MISSAO CONCLUIDA!");
        }
        else if (result == Node.Status.FAILURE)
        {
            agent.ResetPath();
            agent.isStopped = true;

            Debug.LogError("O roubo falhou!");
            enabled = false;
        }
    }

    // =====================================
    // MOVIMENTO
    // =====================================

    public Node.Status GoToLocation(
        Vector3 destination,
        float stoppingDistance = -1f)
    {
        if (!agent.isOnNavMesh)
            return Node.Status.FAILURE;

        float distance = stoppingDistance >= 0f
            ? stoppingDistance
            : arrivalDistance;

        agent.stoppingDistance = distance;

        if (!destinationSet ||
            Vector3.Distance(
                currentDestination,
                destination
            ) > 0.1f)
        {
            agent.isStopped = false;

            if (!agent.SetDestination(destination))
            {
                Debug.LogError(
                    "Nao foi possivel definir destino: " +
                    destination
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
            Debug.LogError(
                "Caminho invalido para: " + destination
            );

            return Node.Status.FAILURE;
        }

        if (float.IsInfinity(agent.remainingDistance))
            return Node.Status.RUNNING;

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
            Node.Status result = GoToFrontDoor();

            if (result != Node.Status.SUCCESS)
                return result;

            Lock frontLock =
                FrontDoor.GetComponent<Lock>();

            if (frontLock == null)
            {
                Debug.LogError("FrontDoor nao tem Lock!");
                return Node.Status.FAILURE;
            }

            doorChecked = true;

            if (frontLock.isLocked)
            {
                useBackDoor = true;

                NavMeshObstacle obstacle =
                    FrontDoor.GetComponent<NavMeshObstacle>();

                obstacle.carveOnlyStationary = false;
                obstacle.carving = true;

                Debug.Log(
                    "FrontDoor trancada! Carve ativado."
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
        return GoToLocation(
            FrontDoor.transform.position
        );
    }

    // =====================================
    // BACKDOOR
    // =====================================

    public Node.Status GoToBackDoor()
    {
        if (backDoorDestroyed)
            return Node.Status.SUCCESS;

        Node.Status result = GoToLocation(
            BackDoor.transform.position
        );

        if (result != Node.Status.SUCCESS)
            return result;

        backDoorDestroyed = true;

        Destroy(BackDoor);

        Debug.Log("O ladrao destruiu a BackDoor!");

        return Node.Status.SUCCESS;
    }

    // =====================================
    // ROUBAR CRISTAL
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

            Rigidbody rb =
                diamond.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            diamond.transform.SetParent(transform);

            diamond.transform.localPosition =
                new Vector3(0f, 1.5f, 0.5f);

            Debug.Log(
                "O ladrao apanhou o cristal!"
            );
        }

        return result;
    }

    // =====================================
    // FUGIR PARA A CARRINHA
    // =====================================

    public Node.Status GoToVan()
    {
        if (!hasDiamond)
            return Node.Status.FAILURE;

        // Procurar um ponto navegavel perto da carrinha.
        NavMeshHit hit;

        if (!NavMesh.SamplePosition(
            vanPosition,
            out hit,
            3f,
            NavMesh.AllAreas))
        {
            Debug.LogError(
                "Nao existe NavMesh perto da carrinha!"
            );

            return Node.Status.FAILURE;
        }

        Node.Status result = GoToLocation(
            hit.position,
            0.1f
        );

        if (result != Node.Status.SUCCESS)
            return result;

        // Parar a navegacao antes do posicionamento final.
        agent.ResetPath();
        agent.isStopped = true;

        // Colocar o ladrao exatamente na posicao pretendida.
        // Se estiver fora do NavMesh, suspender o NavMeshAgent.
        agent.enabled = false;
        transform.position = vanPosition;

        // Esconder o cristal ao entrar na carrinha.
        diamond.SetActive(false);

        // Receber a recompensa.
        money += crystalValue;

        Debug.Log("O ladrao entrou na carrinha!");
        Debug.Log("Dinheiro ganho: " + crystalValue);
        Debug.Log("Dinheiro total: " + money);

        return Node.Status.SUCCESS;
    }
}
