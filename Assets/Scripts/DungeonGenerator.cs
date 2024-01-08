
using JetBrains.Annotations;
using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static Agent;

[Serializable]
public class Agent
{
    public enum AgentDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }
    public Agent()
    {
        positionX = 0;
        positionY = 0;
        m_currentDepth = 0;
        direction = AgentDirection.None;
    }

    public int positionX;
    public int positionY;

    public int depth = 3;
    private int m_currentDepth;
    public int currentDepth { get { return m_currentDepth; } private set { } }
    public AgentDirection direction;
}

public class DungeonGenerator : MonoBehaviour
{
    public static DungeonGenerator instance;
    [SerializeField] Room[] m_availableRooms;

    [SerializeField] Agent m_agent;

    [SerializeField] public bool[,] path;

    [Header("debug")]
    public bool ActivateDebugTiles;
    [SerializeField] GameObject DebugTile;
    Vector3 DebugOffset = Vector3.zero;

    public void Awake()
    {
        if (instance != null && instance != this)
            Destroy(gameObject);

        instance = this;
    }

    private void Start()
    {
        path = GeneratePath(ref m_agent);
    }

    public bool[,] GeneratePath(ref Agent agent)
    {
        bool[,] returnedPath = new bool[(agent.depth * 2)+1 , (agent.depth * 2) + 1];

        //Set Values to false
        for(int i  = 0; i < (agent.depth * 2) + 1; i++)
        {
            for (int j = 0; j < (agent.depth * 2) + 1; j++)
            {
                returnedPath[i, j] = false;
            }
        }

        agent.positionX = agent.depth + 1;
        agent.positionY = agent.depth + 1;

        if (DebugTile) DebugOffset = transform.position - new Vector3(agent.positionX, agent.positionY, 0);


        returnedPath[agent.positionX, agent.positionY] = true;



        //Walk
        for (int i = 0; i < agent.depth; i++)
        {
            agent.direction = GetRandomDirection(agent.direction);
            switch(agent.direction)
            {
                case AgentDirection.Left: agent.positionX -= 1; break;
                case AgentDirection.Right: agent.positionX += 1; break;
                case AgentDirection.Up: agent.positionY += 1; break;
                case AgentDirection.Down: agent.positionY -= 1; break;
            }
            
            if (returnedPath[agent.positionX, agent.positionY] == true)
                continue;

            
            returnedPath[agent.positionX, agent.positionY] = true;

            if (ActivateDebugTiles)
                spawnDebugTile(agent.positionX, agent.positionY);
        }

        return returnedPath;


        AgentDirection GetRandomDirection(AgentDirection currentDirection)
        {
            AgentDirection returnDirection = AgentDirection.None;
            AgentDirection backwardDirection = AgentDirection.None;

            switch(currentDirection){
                case AgentDirection.Up: backwardDirection = AgentDirection.Down; break;
                case AgentDirection.Down: backwardDirection = AgentDirection.Up; break;
                case AgentDirection.Left: backwardDirection = AgentDirection.Right; break;
                case AgentDirection.Right: backwardDirection = AgentDirection.Left; break;
            }

            int randomDirection = UnityEngine.Random.Range(1, 5);

            while(randomDirection == (int)backwardDirection)
            {
                randomDirection = UnityEngine.Random.Range(1, 5);
            }

            switch (randomDirection)
            {
                case 1: returnDirection = AgentDirection.Up; break;
                case 2: returnDirection = AgentDirection.Down; break;
                case 3: returnDirection = AgentDirection.Left; break;
                case 4: returnDirection = AgentDirection.Right; break;
            }

            Debug.Log(returnDirection);
            return returnDirection;
        }

        void spawnDebugTile(int posX, int posY)
        {
            Instantiate(DebugTile, new Vector3(posX, posY, 0) + DebugOffset, Quaternion.identity);
        }
    }
}
