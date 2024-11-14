using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// FSM States for the enemy
public enum EnemyState { STATIC, CHASE, REST, MOVING, DEFAULT };

public enum EnemyBehavior {EnemyBehavior1, EnemyBehavior2, EnemyBehavior3 };

public class Enemy : MonoBehaviour
{
    //pathfinding
    protected PathFinder pathFinder;
    public GenerateMap mapGenerator;
    protected Queue<Tile> path;
    protected GameObject playerGameObject;

    public Tile currentTile;
    protected Tile targetTile;
    public Vector3 velocity;

    //properties
    public float speed = 1.0f;
    public float visionDistance = 5;
    public int maxCounter = 5;
    protected int playerCloseCounter;

    protected EnemyState state = EnemyState.DEFAULT;
    protected Material material;

    public EnemyBehavior behavior = EnemyBehavior.EnemyBehavior1; 

    // Start is called before the first frame update
    void Start()
    {
        path = new Queue<Tile>();
        pathFinder = new PathFinder();
        playerGameObject = GameObject.FindWithTag("Player");
        playerCloseCounter = maxCounter;
        material = GetComponent<MeshRenderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        if (mapGenerator.state == MapState.DESTROYED) return;

        // Stop Moving the enemy if the player has reached the goal
        if (playerGameObject.GetComponent<Player>().IsGoalReached() || playerGameObject.GetComponent<Player>().IsPlayerDead())
        {
            //Debug.Log("Enemy stopped since the player has reached the goal or the player is dead");
            return;
        }

        switch(behavior)
        {
            case EnemyBehavior.EnemyBehavior1:
                HandleEnemyBehavior1();
                break;
            case EnemyBehavior.EnemyBehavior2:
                HandleEnemyBehavior2();
                break;
            case EnemyBehavior.EnemyBehavior3:
                HandleEnemyBehavior3();
                break;
            default:
                break;
        }

    }

    public void Reset()
    {
        Debug.Log("enemy reset");
        path.Clear();
        state = EnemyState.DEFAULT;
        currentTile = FindWalkableTile();
        transform.position = currentTile.transform.position;
    }

    Tile FindWalkableTile()
    {
        Tile newTarget = null;
        int randomIndex = 0;
        while (newTarget == null || !newTarget.mapTile.Walkable)
        {
            randomIndex = (int)(Random.value * mapGenerator.width * mapGenerator.height - 1);
            newTarget = GameObject.Find("MapGenerator").transform.GetChild(randomIndex).GetComponent<Tile>();
        }
        return newTarget;
    }

    // Dumb Enemy: Keeps Walking in Random direction, Will not chase player
    private void HandleEnemyBehavior1()
    {
        switch (state)
        {
            case EnemyState.DEFAULT: // generate random path 
                
                //Changed the color to white to differentiate from other enemies
                material.color = Color.white;
                
                if (path.Count <= 0) path = pathFinder.RandomPath(currentTile, 20);

                if (path.Count > 0)
                {
                    targetTile = path.Dequeue();
                    state = EnemyState.MOVING;
                }
                break;

            case EnemyState.MOVING:
                //move
                velocity = targetTile.gameObject.transform.position - transform.position;
                transform.position = transform.position + (velocity.normalized * speed) * Time.deltaTime;
                
                //if target reached
                if (Vector3.Distance(transform.position, targetTile.gameObject.transform.position) <= 0.05f)
                {
                    currentTile = targetTile;
                    state = EnemyState.DEFAULT;
                }

                break;
            default:
                state = EnemyState.DEFAULT;
                break;
        }
    }

    // Enemy chases the player when it is nearby
    private void HandleEnemyBehavior2()
    {
        switch (state)
        {
            case EnemyState.DEFAULT:
                // Set color to differentiate behavior
                material.color = Color.red;

                // Check if the player is within vision range
                if (Vector3.Distance(transform.position, playerGameObject.transform.position) <= visionDistance)
                {
                    // Player is within vision range, switch to chase mode
                    targetTile = FindNearestTile(playerGameObject.transform.position);
                
                    // Set path to the last known position of the player
                    path = pathFinder.FindPathAStar(currentTile, targetTile);

                    state = EnemyState.CHASE;
                }
                else
                {
                    // No player in range, walk randomly
                    if (path.Count <= 0)
                    {
                        path = pathFinder.RandomPath(currentTile, 10); // Adjust random path length as needed
                    }
                
                    if (path.Count > 0)
                    {
                        targetTile = path.Dequeue();
                        state = EnemyState.MOVING;
                    }
                }
                break;

            case EnemyState.CHASE:
                // If there is no more path, switch back to random movement
                if (path.Count <= 0)
                {
                    state = EnemyState.DEFAULT;
                    break;
                }

                // Move along the path toward the target tile
                targetTile = path.Dequeue();
                state = EnemyState.MOVING;
                break;

            case EnemyState.MOVING:
                // Move towards the target tile
                velocity = targetTile.gameObject.transform.position - transform.position;
                transform.position = transform.position + (velocity.normalized * speed) * Time.deltaTime;

                // Check if the enemy reached the target tile
                if (Vector3.Distance(transform.position, targetTile.gameObject.transform.position) <= 0.05f)
                {
                    currentTile = targetTile;
                
                    // If in CHASE mode and reached the target, check if player is still within range
                    if (state == EnemyState.CHASE && Vector3.Distance(transform.position, playerGameObject.transform.position) <= visionDistance)
                    {
                        targetTile = FindNearestTile(playerGameObject.transform.position);
                        path = pathFinder.FindPathAStar(currentTile, targetTile);
                    }
                    else
                    {
                        state = EnemyState.DEFAULT;
                    }
                }
                break;

            default:
                state = EnemyState.DEFAULT;
                break;
        }
    }

    // Helper method to find nearest tile 
    private Tile FindNearestTile(Vector3 position)
{
    Tile nearestTile = null;
    float closestDistance = Mathf.Infinity;

    foreach (Transform tileTransform in mapGenerator.transform)
    {
        Tile tile = tileTransform.GetComponent<Tile>();
        if (tile != null)
        {
            float distance = Vector3.Distance(position, tile.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearestTile = tile;
            }
        }
    }
    return nearestTile;
}


    // Enemy chases player on sight but at an offset tile
    private void HandleEnemyBehavior3()
    {
        switch (state)
        {
            case EnemyState.DEFAULT:
                // Set color to differentiate behavior
                material.color = Color.blue;

                // Check if the player is within vision
                if (Vector3.Distance(transform.position, playerGameObject.transform.position) <= visionDistance)
                {
                    // Player is within vision then switch to chase mode
                    Tile playerTile = FindNearestTile(playerGameObject.transform.position);
                
                    if (playerTile != null)
                    {
                        // Offset by 2 tiles in a random direction
                        targetTile = GetOffsetTile(playerTile, 2);
                    
                        // Set path to this offset tile
                        if (targetTile != null)
                        {
                            path = pathFinder.FindPathAStar(currentTile, targetTile);
                            state = EnemyState.CHASE;
                        }
                    }
                }
                else
                {
                    // No player in range, walk randomly
                    if (path.Count <= 0)
                    {
                        path = pathFinder.RandomPath(currentTile, 10);
                    }
                
                    if (path.Count > 0)
                    {
                        targetTile = path.Dequeue();
                        state = EnemyState.MOVING;
                    }
                }
                break;

            case EnemyState.CHASE:
                // If there is no more path, switch back to random movement
                if (path.Count <= 0)
                {
                    state = EnemyState.DEFAULT;
                    break;
                }

                // Move along the path toward the target tile
                targetTile = path.Dequeue();
                state = EnemyState.MOVING;
                break;

            case EnemyState.MOVING:
                // Move towards the target tile
                velocity = targetTile.gameObject.transform.position - transform.position;
                transform.position = transform.position + (velocity.normalized * speed) * Time.deltaTime;

                // Check if the enemy reached the target tile
                if (Vector3.Distance(transform.position, targetTile.gameObject.transform.position) <= 0.05f)
                {
                    currentTile = targetTile;

                    // If in CHASE mode and reached the target, check if player is still within range
                    if (state == EnemyState.CHASE && Vector3.Distance(transform.position, playerGameObject.transform.position) <= visionDistance)
                    {
                        Tile playerTile = FindNearestTile(playerGameObject.transform.position);
                        if (playerTile != null)
                        {
                            targetTile = GetOffsetTile(playerTile, 2);
                            path = pathFinder.FindPathAStar(currentTile, targetTile);
                        }
                    }
                    else
                    {
                        state = EnemyState.DEFAULT;
                    }
                }
                break;

            default:
                state = EnemyState.DEFAULT;
                break;
        }
    }

    // Helper method for getting the offset tile
    private Tile GetOffsetTile(Tile originTile, int offset)
    {
        Tile nearestOffsetTile = null;
        float closestDistance = Mathf.Infinity;
        Vector3 targetPosition = originTile.transform.position + new Vector3(offset, 0, 0);

        foreach (Transform tileTransform in mapGenerator.transform) 
        {
            Tile tile = tileTransform.GetComponent<Tile>();
            if (tile != null)
            {
                float distance = Vector3.Distance(targetPosition, tile.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearestOffsetTile = tile;
                }
            }
        }
        return nearestOffsetTile;
    }

}
