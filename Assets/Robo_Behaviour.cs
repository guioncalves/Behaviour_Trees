
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
    public GameObject backdoor;

    [Header("Movimento")]
    public float arrivalDistance = 1f;

    private bool hasDiamond = false;
    private bool missionComplete = false;
    private bool reachedBackDoor = false;

    // Estados da missão.
    private enum MissionState
    {
        GoingToBackDoor,
        GoingToDiamond,
        GoingToVan,
        Complete
    }

    private MissionState missionState;

    void Start()
    {
        Debug.Log("O robo iniciou!");

        agent = GetComponent<NavMeshAgent>();

        // Verificar se todos os objetivos existem.
        if (diamond == null ||
            van == null ||
            backdoor == null)
        {
            Debug.LogError(
                "Atribui o Diamond, a Van e a Backdoor no Inspector!"
            );

            enabled = false;
            return;
        }

        // Verificar se o robô está no NavMesh.
        if (!agent.isOnNavMesh)
        {
            Debug.LogError(
                "O robo nao esta no NavMesh!"
            );

            enabled = false;
            return;
        }

        agent.isStopped = false;

        if (agent.speed <= 0f)
        {
            Debug.LogError(
                "A velocidade do NavMeshAgent deve ser maior que zero!"
            );

            enabled = false;
            return;
        }

        // =====================================
        // CRIAR A BEHAVIOUR TREE
        // =====================================

        tree = new Behaviour_Tree();

        Node steal = new Node("Steal Something");

        Leaf goToBackDoor = new Leaf(
            "Go To Door",
            GoToBackDoor
        );

        Leaf goToDiamond = new Leaf(
            "Go To Diamond",
            GoToDiamond
        );

        Leaf goToVan = new Leaf(
            "Go To Van",
            GoToVan
        );

        // Ordem da missão:
        // Porta -> Diamante -> Carrinha

        steal.AddChild(goToBackDoor);
        steal.AddChild(goToDiamond);
        steal.AddChild(goToVan);

        tree.AddChild(steal);

        tree.PrintTree();

        Debug.Log("Behaviour Tree criada!");

        // =====================================
        // PRIMEIRO DESTINO: PORTA
        // =====================================

        missionState = MissionState.GoingToBackDoor;

        if (!DefineDestination(backdoor.transform.position))
        {
            enabled = false;
        }
    }

    void Update()
    {
        if (missionComplete || !agent.isOnNavMesh)
            return;

        // Esperar que o NavMesh calcule o percurso.
        if (agent.pathPending)
            return;

        // Verificar se existe um caminho válido.
        if (agent.hasPath &&
            agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            Debug.LogError(
                "O robo nao tem um caminho completo!"
            );

            enabled = false;
            return;
        }

        // Enquanto não chegar, continuar a mover-se.
        if (!ReachedDestination())
            return;

        // =====================================
        // FASE 1 - CHEGOU À PORTA
        // =====================================

        if (missionState == MissionState.GoingToBackDoor)
        {
            reachedBackDoor = true;

            Debug.Log("O robo chegou a porta traseira!");

            // Próximo objetivo: diamante.
            missionState = MissionState.GoingToDiamond;

            if (!DefineDestination(diamond.transform.position))
            {
                enabled = false;
            }
        }

        // =====================================
        // FASE 2 - CHEGOU AO DIAMANTE
        // =====================================

        else if (missionState == MissionState.GoingToDiamond)
        {
            hasDiamond = true;

            Debug.Log("O robo chegou ao cristal!");

            // Recolher o diamante.
            diamond.SetActive(false);

            // Próximo objetivo: carrinha.
            missionState = MissionState.GoingToVan;

            if (!DefineDestination(van.transform.position))
            {
                enabled = false;
            }
        }

        // =====================================
        // FASE 3 - CHEGOU À CARRINHA
        // =====================================

        else if (missionState == MissionState.GoingToVan)
        {
            missionState = MissionState.Complete;
            missionComplete = true;

            agent.ResetPath();

            Debug.Log(
                "MISSAO CONCLUIDA! O robo chegou a carrinha."
            );
        }
    }

    // =====================================
    // DEFINIR DESTINO
    // =====================================

    bool DefineDestination(Vector3 destination)
    {
        if (!agent.isOnNavMesh)
            return false;

        agent.isStopped = false;

        bool sucesso = agent.SetDestination(destination);

        if (sucesso)
        {
            Debug.Log(
                "Novo destino: " + destination
            );
        }
        else
        {
            Debug.LogError(
                "Nao foi possivel definir o destino: "
                + destination
            );
        }

        return sucesso;
    }

    // =====================================
    // VERIFICAR SE CHEGOU AO DESTINO
    // =====================================

    bool ReachedDestination()
    {
        if (agent.pathPending)
            return false;

        if (!agent.hasPath)
            return false;

        if (agent.pathStatus != NavMeshPathStatus.PathComplete)
            return false;

        if (float.IsInfinity(agent.remainingDistance))
            return false;

        return agent.remainingDistance <= Mathf.Max(
            arrivalDistance,
            agent.stoppingDistance
        );
    }

    // =====================================
    // AÇÕES DA BEHAVIOUR TREE
    // =====================================

    public Node.Status GoToBackDoor()
    {
        return reachedBackDoor
            ? Node.Status.SUCCESS
            : Node.Status.RUNNING;
    }

    public Node.Status GoToDiamond()
    {
        return hasDiamond
            ? Node.Status.SUCCESS
            : Node.Status.RUNNING;
    }

    public Node.Status GoToVan()
    {
        return missionComplete
            ? Node.Status.SUCCESS
            : Node.Status.RUNNING;
    }
}
