using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class MoveToGoalAgent : Agent
{
    private bool pickedUpBox = false;
    private Box box;
    private float energy = 100f;
    private int attempt = 0;
    private float totalReward = 0f;

    [SerializeField] private Transform targetTransform;
    [SerializeField] private Transform boxInitialPosition; // Posición inicial de la caja
    [SerializeField] private Transform agentInitialPosition; // Posición inicial del agente
    [SerializeField] private float platformHeight = -1f; // Altura mínima para considerar que el agente ha caído
    [SerializeField] private Material winMaterial;
    [SerializeField] private Material loseMaterial;
    [SerializeField] private Material deliveredMaterial;
    [SerializeField] private Material FallMaterial;
    [SerializeField] private MeshRenderer meshRenderer;

    private Vector3 initialAgentPosition;
    private Vector3 initialBoxPosition;

    public override void Initialize()
    {
        initialAgentPosition = agentInitialPosition.position;
        initialBoxPosition = boxInitialPosition.position;
    }

    public override void OnEpisodeBegin()
    {
        // Variables locales para las posiciones iniciales
        Vector3 initialAgentPosition = agentInitialPosition.position;
        Vector3 initialBoxPosition = boxInitialPosition.position;

        // Reposiciona el agente en su posición inicial
        transform.position = initialAgentPosition;
        energy = 100f;
        pickedUpBox = false;
        attempt++;
        totalReward = 0f;
        if (box != null)
        {
            box.transform.parent = null;
            box.transform.position = initialBoxPosition; // Reposiciona la caja en su posición inicial
            box = null;
        }
        Debug.Log($"Attempt: {attempt}, Energy: {energy}, Total Reward: {totalReward}");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position);
        sensor.AddObservation(targetTransform.position);
        sensor.AddObservation(boxInitialPosition.position); // Añadir la posición de la caja
        sensor.AddObservation(pickedUpBox);
        sensor.AddObservation(energy);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];

        float moveSpeed = 5f;
        transform.position += new Vector3(moveX, 0, moveZ) * Time.deltaTime * moveSpeed;

        // Decrease energy
        energy -= 1f * Time.deltaTime;
        SetReward(-1f * Time.deltaTime);
        totalReward += -1f * Time.deltaTime;

        // Verificar si el agente ha caído de la plataforma
        if (transform.position.y < platformHeight)
        {
            meshRenderer.material = FallMaterial;
            SetReward(-5f);
            totalReward += -5f;
            Debug.Log($"Agent fell off the platform. Attempt: {attempt}, Energy: {energy}, Total Reward: {totalReward}");
            
            // Regresar el agente a la plataforma en lugar de terminar el episodio
            transform.position = initialAgentPosition;
            //energy = 100f; // prueba restaurar energía al caer
        }

        if (energy <= 0)
        {
            SetReward(-5f);
            totalReward += -5f;
            Debug.Log($"Energy depleted. Attempt: {attempt}, Energy: {energy}, Total Reward: {totalReward}");
            EndEpisode();
        }

        // Penalización por moverse hacia la pared
        // if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 1f))
        // {
        //     if (hit.collider.CompareTag("Wall"))
        //     {
        //         SetReward(-0.1f);
        //         totalReward += -0.1f;
        //         Debug.Log($"Hit wall. Attempt: {attempt}, Energy: {energy}, Total Reward: {totalReward}");
        //     }
        // }

        // Recompensa por acercarse a la caja
        if (!pickedUpBox)
        {
            float distanceToBox = Vector3.Distance(transform.position, boxInitialPosition.position);
            SetReward(-distanceToBox * 0.01f);
            totalReward += -distanceToBox * 0.01f;
        }
        // Recompensa por acercarse al objetivo con la caja
        else
        {
            float distanceToGoal = Vector3.Distance(transform.position, targetTransform.position);
            SetReward(-distanceToGoal * 0.01f);
            totalReward += -distanceToGoal * 0.01f;
        }

        Debug.Log($"Attempt: {attempt}, Energy: {energy}, Total Reward: {totalReward}");
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxisRaw("Horizontal");
        continuousActionsOut[1] = Input.GetAxisRaw("Vertical");
    }

    private void OnTriggerEnter(Collider other)
    {
        // Variables locales para las posiciones iniciales
        Vector3 initialBoxPosition = boxInitialPosition.position;

        if (other.TryGetComponent<Goal>(out Goal goal) && pickedUpBox)
        {
            meshRenderer.material = deliveredMaterial;
            SetReward(10f);
            totalReward += 10f;
            pickedUpBox = false;
            box.transform.parent = null;
            box.transform.position = initialBoxPosition; // Reposiciona la caja en su posición inicial
            Debug.Log($"Box delivered. Attempt: {attempt}, Energy: {energy}, Total Reward: {totalReward}");
            EndEpisode();
        }
        else if (other.TryGetComponent<Wall>(out Wall wall))
        {
            meshRenderer.material = loseMaterial;
            SetReward(-10f);
            totalReward += -10f;
            Debug.Log($"Hit wall. Attempt: {attempt}, Energy: {energy}, Total Reward: {totalReward}");
        }
        else if (other.TryGetComponent<Obstacle>(out Obstacle obstacle))
        {
            meshRenderer.material = loseMaterial;
            SetReward(-10f);
            totalReward += -10f;
            Debug.Log($"Hit obstacle. Attempt: {attempt}, Energy: {energy}, Total Reward: {totalReward}");
        }
        else if (other.TryGetComponent<Box>(out Box boxComponent) && !pickedUpBox)
        {
            meshRenderer.material = winMaterial;
            pickedUpBox = true;
            box = boxComponent;
            box.transform.parent = transform;
            Debug.Log($"Box picked up. Attempt: {attempt}, Energy: {energy}, Total Reward: {totalReward}");
        }
    }
}
