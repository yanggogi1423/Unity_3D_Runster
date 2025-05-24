using System;
using MimicSpace;
using UnityEngine;
using UnityEngine.AI;

public class DefaultEnemyMovement : MonoBehaviour
{
    [Header("Properties")] 
    public float speed;
    [Tooltip("Body Height from ground")]
    [Range(0.5f, 5f)]
    public float height = 0.8f;
    private Vector3 velocity = Vector3.zero;
    public float velocityLerpCoef = 4f;
    
    [Header("References")] 
    private NavMeshAgent agent;
    private Mimic myMimic;
    private Rigidbody rb;

    [Header("StateMachine")] 
    public Transform target;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        target = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();

        myMimic = GetComponent<Mimic>();
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        velocity = Vector3.Lerp(velocity, new Vector3(agent.velocity.x, 0, agent.velocity.y).normalized * speed, velocityLerpCoef * Time.deltaTime);

        // Assigning velocity to the mimic to assure great leg placement
        myMimic.velocity = velocity;
        
        //  transform.position = transform.position + velocity * Time.deltaTime;
        
        RaycastHit hit;
        Vector3 destHeight = transform.position;
        if (Physics.Raycast(transform.position + Vector3.up * 5f, -Vector3.up, out hit))
           destHeight = new Vector3(transform.position.x, hit.point.y + height, transform.position.z);
        
        transform.position = Vector3.Lerp(transform.position, destHeight, velocityLerpCoef * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        agent.SetDestination(target.position);
    }
}
