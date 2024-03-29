
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEditor.MemoryProfiler;
using UnityEngine;
using UnityEngine.XR;
using static Agent;
using static Connection;

[Serializable]
public class Agent
{
    public enum AgentDirection
    {
        None,
        Up,
        Down,
        Left,
        Right,
        Impossible
    }
    public void Init()
    {
        positionX = depth;
        positionY = depth;
        direction = AgentDirection.None;
        currentDepth = 0; 
    }

    public int positionX;
    public int positionY;

    [Range(0, 15)] public int depth;
    public int currentDepth;
    public AgentDirection direction;
    [Range(0f,1f)] public float KeepDirection;

    [Header("Locks)")]
    [Range(1, 3)] public int MaxLocks;


    [Header("Branch parameters")]
    [Range(1, 3)] public int BranchMaxSize;
}

[Serializable]
public class Connection
{
    public enum ConnectionDirection
    {
        North,
        South,
        East,
        West
    }
    public bool locked = false;
    public ConnectionDirection direction;
    public Vector2 position;
    public Connection(bool locked)
    {
        this.locked = locked;
    }
}

[Serializable]
public class Node
{
    public enum NodeType
    {
        Start,
        End,
        Neutral,
        Hidden
    }
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }

    public Vector2Int position;
    public NodeType nodeType;
    public Difficulty difficulty;

    public Node(Vector2Int Position)
    {
        position = Position;
        nodeType = NodeType.Neutral;
        difficulty = Difficulty.Normal;
    }

    public List<Connection> connections = new List<Connection>();

    public ConnectionDirection GetConnectionOrientation(Connection connection)
    {
        Vector2 dir = (connection.position - position).normalized;

        if (dir.x > 0.2f)
        {
            return ConnectionDirection.East;
        }
        else if (dir.x < -0.2f)
        {
            return ConnectionDirection.West;
        }
        else if (dir.y > 0.2f)
        {
            return ConnectionDirection.North;
        }
        else if (dir.y < -0.2f)
        {
            return ConnectionDirection.South;
        }

        return ConnectionDirection.South;
    }
}

public class DungeonGenerator : MonoBehaviour
{
    public static DungeonGenerator instance;

    [SerializeField] Agent m_agent;

    [SerializeField] List<Node> Nodes = new List<Node>();
    public List<Node> GetNodes() { return Nodes; }

    [SerializeField] List<Connection> Connections = new List<Connection>();
    public List<Connection> GetConnections() { return Connections; }

    [Header("debug")]
    public bool ActivateDebugTiles;
    [SerializeField] GameObject DebugTile_Fill;
    [SerializeField] GameObject DebugTile_Empty;
    [SerializeField] GameObject ConnectionGO;
    List<GameObject> DebugTiles = new List<GameObject>();
    List<GameObject> DebugConnections = new List<GameObject>();
    Vector3 DebugOffset = Vector3.zero;
    

    public void Awake()
    {
        if (instance != null && instance != this)
            Destroy(gameObject);

        instance = this;
    }

    private void Start()
    {
        //ON INITIALISER L'AGENT
        m_agent.Init();

        if (ActivateDebugTiles)
        {
            DebugOffset = transform.position - new Vector3(m_agent.positionX, m_agent.positionY, 0);
            SpawnBackgroundDebugTiles(m_agent);
        }

    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GenerateDungeon();
        }

    }

    public void GenerateDungeon()
    {
        //ON WIPE LES TILES DE DEBUGS SI IL Y EN A
        DestroyDebugTilesAndConnections();

        //ON INIT L'AGENT
        m_agent.Init();
        Nodes.Clear();
        Connections.Clear();
        GeneratePath(m_agent);
    }

    public void GeneratePath(Agent agent)
    {

        //LA TILE DE DEPART ET MISE A TRUE
        //Nodes[new Vector2Int(agent.positionX, agent.positionY)] = true;
        Nodes.Add(new Node(new Vector2Int(agent.positionX, agent.positionY)));

        if (ActivateDebugTiles)
            spawnDebugTile(agent.positionX, agent.positionY, Color.white);


        //GENERATION
        for (int i = 0; i < agent.depth; i++)
        {
            //ON OBTIENT UNE DIRECTION VALIDE
            agent.direction = GetRandomDirection(agent.direction,true);

            Vector3 oldPos = new Vector3(agent.positionX, agent.positionY, 0);

            if (agent.direction == AgentDirection.Impossible)
            {
                GenerateDungeon();
                return;
            }

            switch (agent.direction)
            {
                case AgentDirection.Left: agent.positionX -= 1; break;
                case AgentDirection.Right: agent.positionX += 1; break;
                case AgentDirection.Up: agent.positionY += 1; break;
                case AgentDirection.Down: agent.positionY -= 1; break;
            }

            Vector3 NewPos = new Vector3(agent.positionX, agent.positionY, 0);

            //ON AUGMENTE LA DEPTH A LAQUELLE SE TROUVE L'AGENT
            agent.currentDepth++;
            Vector3 ConnectionPos = (oldPos + NewPos) / 2;

            //ON MET LA VALEUR CORRESPONDANTE DU TALBEAU A TRUE
            Node NewNode = new Node(new Vector2Int(agent.positionX, agent.positionY));
            Nodes.Add(NewNode);
            Connection NewConnection = new Connection(false);
            NewNode.connections.Add(NewConnection);
            Nodes[Nodes.Count-2].connections.Add(NewConnection);
            Connections.Add(NewConnection);
            NewConnection.position = ConnectionPos;


            //ON FAIT SPAWNER UNE TILE SI LE MODE DEBUG EST ACTIVE
            if (ActivateDebugTiles)
            {
                spawnDebugTile(agent.positionX, agent.positionY, Color.white);

                
                spawnDebugConnection(ConnectionPos, Color.green);
            }
                

        }
        if (ActivateDebugTiles)
        {
            DebugTiles.Last().GetComponent<SpriteRenderer>().color = Color.blue;
            DebugTiles.First().GetComponent<SpriteRenderer>().color = Color.green;
        }


        //ADD LOCKS

        for(int i = 0; i < agent.MaxLocks; i++)
        {
            int randomConnectionOffset = UnityEngine.Random.Range(-3, 1);
            int index = (Connections.Count / (i + 1)) + randomConnectionOffset - 1;
            Connections[index].locked = true;
            DebugConnections[index].GetComponent<SpriteRenderer>().color = Color.red;
        }

        //ADD BRANCHS NEAR LOCKS
        //TODO
        for(int j = 0;j < Connections.Count; j++)
        {
            if (Connections[j].locked)
            {
                agent.positionX = Nodes[j].position.x;
                agent.positionY = Nodes[j].position.y;
                Connection NewConnection = new Connection(false);
                Nodes[j].connections.Add(NewConnection);

                

                int BranchSize = UnityEngine.Random.Range(1, agent.BranchMaxSize+1);

                for (int i = 0; i < BranchSize; i++)
                {
                    //ON OBTIENT UNE DIRECTION VALIDE
                    agent.direction = GetRandomDirection(agent.direction,false);

                    Vector3 oldPos = new Vector3(agent.positionX, agent.positionY, 0);

                    if (agent.direction == AgentDirection.Impossible)
                    {
                        GenerateDungeon();
                        return;
                    }

                    switch (agent.direction)
                    {
                        case AgentDirection.Left: agent.positionX -= 1; break;
                        case AgentDirection.Right: agent.positionX += 1; break;
                        case AgentDirection.Up: agent.positionY += 1; break;
                        case AgentDirection.Down: agent.positionY -= 1; break;
                    }

                    

                    //ON AUGMENTE LA DEPTH A LAQUELLE SE TROUVE L'AGENT
                    agent.currentDepth++;
                    Vector3 NewPos = new Vector3(agent.positionX, agent.positionY, 0);

                    //ON MET LA VALEUR CORRESPONDANTE DU TALBEAU A TRUE
                    Node NewNode = new Node(new Vector2Int(agent.positionX, agent.positionY));
                    Nodes.Add(NewNode);
                    NewNode.connections.Add(NewConnection);

                    Vector3 ConnectionPos = (oldPos + NewPos) / 2;
                    NewConnection.position = ConnectionPos;
                    

                    if (i != 0)
                    {
                        Nodes[Nodes.Count - 2].connections.Add(NewConnection);
                        Connections.Add(NewConnection);
                    }


                    //ON FAIT SPAWNER UNE TILE SI LE MODE DEBUG EST ACTIVE
                    if (ActivateDebugTiles)
                    {
                        spawnDebugTile(agent.positionX, agent.positionY, Color.yellow);

                        spawnDebugConnection(ConnectionPos, Color.green);
                    }
                        
                }

                foreach(Connection connection in Nodes[j].connections)
                {
                    Debug.Log(Nodes[j].GetConnectionOrientation(connection));
                }

            }
        }
        //SPAWN CONNECTION DEBUG IF NECESSARYU
        return;


        AgentDirection GetRandomDirection(AgentDirection currentDirection, bool keepDirection)
        {
            if (keepDirection)
            {
                //ON VERIFIE SI L'AGENT DOIT CONSERVER LA MEME DIRECTION
                float keepdir = UnityEngine.Random.Range(0f, 1f);
                if (keepdir <= agent.KeepDirection && agent.direction != AgentDirection.None)
                    return currentDirection;
            }
            bool movePosible = false;
            List<AgentDirection> possibleMoves = new List<AgentDirection>();

            
            if(!Nodes.Exists(x => x.position == new Vector2Int(agent.positionX + 1, agent.positionY)))
            {
                movePosible = true;
                possibleMoves.Add(AgentDirection.Right);
            }
            if(!Nodes.Exists(x => x.position == new Vector2Int(agent.positionX - 1, agent.positionY)))
            {
                movePosible = true;
                possibleMoves.Add(AgentDirection.Left);
            }
            if(!Nodes.Exists(x => x.position == new Vector2Int(agent.positionX , agent.positionY+1)))
            {
                movePosible = true;
                possibleMoves.Add(AgentDirection.Up);
            }
            if(!Nodes.Exists(x => x.position == new Vector2Int(agent.positionX, agent.positionY - 1)))
            {
                movePosible = true;
                possibleMoves.Add(AgentDirection.Down);
            }

            if (!movePosible)
            {
                return AgentDirection.Impossible;
            }

            //ON GENERE UNE DIRECTION ALEATOIRE
            int randomDirection = UnityEngine.Random.Range(0, possibleMoves.Count);
            
            return possibleMoves[randomDirection];

           
        }

        

        void spawnDebugTile(int posX, int posY, Color color)
        {
            GameObject Tile = Instantiate(DebugTile_Fill, new Vector3(posX, posY, 0) + DebugOffset, Quaternion.identity);
            Tile.GetComponent<SpriteRenderer>().color = color;
            Tile.GetComponentInChildren<TextMeshProUGUI>().text = agent.currentDepth.ToString();
            DebugTiles.Add(Tile);
        }

        void spawnDebugConnection(Vector3 position, Color color)
        {
            GameObject SpawnedConnection = Instantiate(ConnectionGO, position + DebugOffset, Quaternion.identity);
            SpawnedConnection.GetComponent<SpriteRenderer>().color = color;
            DebugConnections.Add(SpawnedConnection);
        }
    }

    #region debug
    private void DestroyDebugTilesAndConnections()
    {
        foreach (var tile in DebugTiles)
            Destroy(tile);

        DebugTiles.Clear();

        foreach (var connection in DebugConnections)
            Destroy(connection);

        DebugConnections.Clear();
    }

    private void SpawnBackgroundDebugTiles(Agent agent)
    {
        if (!ActivateDebugTiles)
            return;

        bool[,] returnedPath = new bool[(agent.depth * 2) + 1, (agent.depth * 2) + 1];

        for (int i = 0; i < (agent.depth * 2) + 1; i++)
        {
            for (int j = 0; j < (agent.depth * 2) + 1; j++)
            {
                Instantiate(DebugTile_Empty, new Vector3(i, j, 0) + DebugOffset, Quaternion.identity);
            }
        }
    }
    #endregion
}
