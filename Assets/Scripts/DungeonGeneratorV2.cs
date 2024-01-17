using System;
using System.Collections.Generic;
using CreativeSpore.SuperTilemapEditor;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

public class DungeonGeneratorV2 : MonoBehaviour
{
    private class Agent
    {
        public Vector2Int currentPosition = Vector2Int.zero;
        public int currentDepth = 0;
        private Utils.ORIENTATION lastDirection = Utils.ORIENTATION.NONE;

        public void RandomlyAdvance(Utils.ORIENTATION growthDirection)
        {
            var nextMove = Random.Range(0, 3);
            switch (nextMove)
            {
                case 0:
                    if(lastDirection == Utils.ORIENTATION.NONE || lastDirection == growthDirection)
                        MoveDirection(Utils.AngleToOrientation(90, growthDirection));
                    else MoveDirection(lastDirection);
                    break;
                case 1:
                    if(lastDirection == Utils.ORIENTATION.NONE || lastDirection == growthDirection)
                        MoveDirection(Utils.AngleToOrientation(-90, growthDirection));
                    else MoveDirection(growthDirection);
                    break;
                default:
                    MoveDirection(growthDirection);
                    break;
            }
        }

        private void MoveDirection(Utils.ORIENTATION orientation)
        {
            switch (orientation)
            {
                case Utils.ORIENTATION.NORTH:
                    currentPosition.y += 1;
                    break;
                case Utils.ORIENTATION.EAST:
                    currentPosition.x += 1;
                    break;
                case Utils.ORIENTATION.SOUTH:
                    currentPosition.y -= 1;
                    break;
                case Utils.ORIENTATION.WEST:
                    currentPosition.x -= 1;
                    break;
            }

            lastDirection = orientation;
        }

        public void Init()
        {
            currentPosition = Vector2Int.zero;
        }
    }

    private class GraphNode
    {
        public Vector2Int position;
        public Room room;

        public Vector3 ToWorldPosition()
        {
            return new Vector3(position.x * 11, position.y * 9);
        }
    }
    
    [Range(0, 30)] public int depth = 10;
    [Range(0, 20)] public int maxBranches = 3;
    [Range(0, 20)] public int maxBranchDepth = 5;
    public GameObject roomPrefab;
    public GameObject doorPrefab;


    private Agent _agent;
    private List<GraphNode> _graphNodes = new();
    
    
    private void Start()
    {
        Player.Instance.gameObject.SetActive(false);

        _agent = new Agent();
        StartGeneration();
    }

    private void StartGeneration()
    {
        Player.Instance.gameObject.SetActive(false);

        _agent.Init();
        GenerateGraph();
        SpawnGraph();

        Player.Instance.gameObject.SetActive(true);
    }

    private void GenerateGraph()
    {
        void GenerateMainPath()
        {
            for (int i = 0; i < depth; i++)
            {
                var newNode = new GraphNode();
                newNode.position = _agent.currentPosition;

                _graphNodes.Add(newNode);

                _agent.RandomlyAdvance(Utils.ORIENTATION.EAST);
                _agent.currentDepth++;
            }
        }

        void GenerateBranches()
        {
            if (maxBranches <= 0) return;

            var currentBranches = 0;

            for (int i = 0; i < depth; i++)
            {
                if(currentBranches >= maxBranches) break;

                _agent.currentPosition = _graphNodes[i].position;
                _agent.currentDepth = i;

                var doBranchHere = Random.Range(0, depth);
                if(doBranchHere < depth / 2) continue;

                BranchHere(i);

                currentBranches++;
            }
        }

        void BranchHere(int nodeIndex)
        {
            var node = _graphNodes[nodeIndex];

            var emptyOrientation = FindEmptyAdjacent(node);
            if(emptyOrientation == Utils.ORIENTATION.NONE) return;

            _agent.currentPosition = node.position + Utils.OrientationToDir(emptyOrientation);

            var newNode = new GraphNode();
            newNode.position = _agent.currentPosition;
            _graphNodes.Add(newNode);

            var depth = 0;
            while (true)
            {
                if(depth >= maxBranchDepth) break;

                _agent.RandomlyAdvance(emptyOrientation);
                var branchNode = FindNodeAtCoords(_agent.currentPosition);
                if(branchNode != null) break;

                var newBranchNode = new GraphNode();
                newBranchNode.position = _agent.currentPosition;
                _graphNodes.Add(newBranchNode);

                depth++;
            }
        }

        GenerateMainPath();
        GenerateBranches();
    }

    private void SpawnGraph()
    {
        for (var i = 0; i < _graphNodes.Count; i++)
        {
            SpawnRoom(_graphNodes[i], i == 0);
        }

        for (var i = 0; i < _graphNodes.Count; i++)
        {
            SpawnDoors(_graphNodes[i]);
        }
    }

    private void SpawnRoom(GraphNode node, bool isStart = false)
    {
        var newGo = Instantiate(roomPrefab, node.ToWorldPosition(), Quaternion.identity);
        node.room = newGo.GetComponent<Room>();
        node.room.position = node.position;
        node.room.isStartRoom = isStart;
    }

    private void SpawnDoors(GraphNode node)
    {
        STETilemap tilemap = node.room.transform.GetChild(0).GetComponent<STETilemap>();

        var north = FindNodeAtCoords(node.position + Utils.OrientationToDir(Utils.ORIENTATION.NORTH));
        if (north != null)
        {
            var door = Instantiate(doorPrefab, node.room.gameObject.transform).GetComponent<Door>();
            door.transform.localPosition = new Vector3(5.5f, 8.5f);
            door.SetState(Door.STATE.OPEN);
            tilemap.SetTileData(door.transform.localPosition, 1);
        }

        var east = FindNodeAtCoords(node.position + Utils.OrientationToDir(Utils.ORIENTATION.EAST));
        if (east != null)
        {
            var door = Instantiate(doorPrefab, node.room.gameObject.transform);
            door.transform.localPosition = new Vector3(10.5f, 4.5f);
            door.GetComponent<Door>().SetState(Door.STATE.OPEN);
            tilemap.SetTileData(door.transform.localPosition, 1);
        }

        var south = FindNodeAtCoords(node.position + Utils.OrientationToDir(Utils.ORIENTATION.SOUTH));
        if (south != null)
        {
            var door = Instantiate(doorPrefab, node.room.gameObject.transform);
            door.transform.localPosition = new Vector3(5.5f, 0.5f);
            door.GetComponent<Door>().SetState(Door.STATE.OPEN);
            tilemap.SetTileData(door.transform.localPosition, 1);
        }

        var west = FindNodeAtCoords(node.position + Utils.OrientationToDir(Utils.ORIENTATION.WEST));
        if (west != null)
        {
            var door = Instantiate(doorPrefab, node.room.gameObject.transform);
            door.transform.localPosition = new Vector3(0.5f, 4.5f);
            door.GetComponent<Door>().SetState(Door.STATE.OPEN);
            tilemap.SetTileData(door.transform.localPosition, 1);
        }

        tilemap.UpdateMesh();
    }

    [CanBeNull]
    private GraphNode FindNodeAtCoords(Vector2Int vector)
    {
        var node = _graphNodes.Find(x => x.position.x == vector.x && x.position.y == vector.y);
        return node;
    }


    private Utils.ORIENTATION FindEmptyAdjacent(GraphNode node)
    {
        var north = _graphNodes.Find(x => x.position.x == node.position.x && x.position.y == node.position.y + 1);
        if (north == null) return Utils.ORIENTATION.NORTH;

        var east = _graphNodes.Find(x => x.position.x == node.position.x + 1 && x.position.y == node.position.y);
        if (east == null) return Utils.ORIENTATION.EAST;

        var south = _graphNodes.Find(x => x.position.x == node.position.x && x.position.y == node.position.y - 1);
        if (south == null) return Utils.ORIENTATION.SOUTH;

        var west = _graphNodes.Find(x => x.position.x == node.position.x - 1 && x.position.y == node.position.y);
        if (west == null) return Utils.ORIENTATION.WEST;

        return Utils.ORIENTATION.NONE;
    }
}
