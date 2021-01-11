using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CometBehaviour : MonoBehaviour
{
    public static CometBehaviour instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    private float speed = 21f;
    private float acceleration = 0.2f;
    private float maxSpeed = 25f;
    int direction = 1;
    float currentDir = 0;

    float cometRadius = 0.5f;

    float totalTime;
    float currentTime;
    float connectTime;

    float currentDistance;
    float lastDistance;
    float totalDistance;

    float collisionDistance;
    float distanceFromOrbit;
    float timeToAdjustToOrbit = 0.5f;
    public bool connected = true;
    public bool startLocationSet = false;
    public bool ignoreNextTrail = false;
    public Grid currentGrid;
    public Grid newGrid;

    public CelestialBody firstCelestialBody;
    public CelestialBody currentCelestialBody;

    // Start is called before the first frame update
    void Start()
    {
        transform.localScale = 2 * new Vector3(cometRadius, cometRadius, cometRadius);
        CheckGrids();
        Debug.Log(firstCelestialBody);
        Restart();
        startLocationSet = true;
    }

    // Update is called once per frame
    void Update()
    {
        // Update times
        totalTime = Time.time;
        currentTime = totalTime - connectTime;

        // Check for input
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Left Button Down");
            if (connected)
            {
                Disconnect();
            }
        }
        else if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("Right Button Down");
            Restart();
            ignoreNextTrail = true;
        }
        else if (Input.GetMouseButtonDown(2))
        {
            Debug.Log("Middle Button Down");
            if (connected)
            {
                currentDir = currentTime * direction * speed / currentCelestialBody.orbitRadius + currentDir;
                connectTime = Time.time;
                direction = -direction;
                ignoreNextTrail = true;
            }
        }

        // Move comet
        Vector3 oldPosition = transform.position;
        if (connected)
        {
            // Update CelestialBody parameters and see if Comet must react, else orbit normally
            currentCelestialBody.currentDestruction += currentCelestialBody.destructionFactor * Time.deltaTime;
            currentCelestialBody.currentGravity += currentCelestialBody.gravityFactor * Time.deltaTime;
            if (currentCelestialBody.currentDestruction >= 1)
            {
                Disconnect();
                Destroy(currentCelestialBody.coreObject);
                currentCelestialBody.grid.celestialBodys.Remove(currentCelestialBody);
            }
            else if (currentCelestialBody.currentGravity >= 1)
            {
                Disconnect();
            }
            else
            {
                if (speed < maxSpeed)
                {
                    speed += acceleration * Time.deltaTime;
                    UIUpdater.instance.UpdateSpeedValue(speed);
                }
                float cos = Mathf.Cos(currentTime * direction * speed / currentCelestialBody.orbitRadius + currentDir);
                float sin = Mathf.Sin(currentTime * direction * speed / currentCelestialBody.orbitRadius + currentDir);
                float xPos = currentCelestialBody.absolutePosition.x + (currentCelestialBody.coreRadius + (currentCelestialBody.orbitRadius - currentCelestialBody.coreRadius) * (1 - currentCelestialBody.currentGravity)) * cos;
                float yPos = currentCelestialBody.absolutePosition.y + (currentCelestialBody.coreRadius + (currentCelestialBody.orbitRadius - currentCelestialBody.coreRadius) * (1 - currentCelestialBody.currentGravity)) * sin;
                transform.position = new Vector3(xPos, 0, yPos);

                // Update rotation of comet
                float rotation = -Vector2.SignedAngle(Vector2.up, new Vector2(sin, -cos) * direction);
                transform.rotation = Quaternion.Euler(0f, rotation, 0f);
            }
        }
        else
        {
            // Update position of Comet
            float currentXPos = transform.position.x;
            float currentYPos = transform.position.z;
            transform.position = new Vector3(currentXPos + speed * Mathf.Cos(currentDir) * Time.deltaTime, 0, currentYPos + speed * Mathf.Sin(currentDir) * Time.deltaTime);

            // Check if Comet has entered orbit of a celestialBody
            CelestialBody newOrbit = OrbitEnteredInSurroundingGrids(currentGrid);
            if (newOrbit != null && newOrbit != currentCelestialBody)
            {
                Reconnect(newOrbit);
            }
        }

        // Check if new grid is entered and if so generate if needed and set unused as inactive
        CheckGrids();

        Vector3 newPosition = transform.position;
        float newDistance = Vector3.Distance(newPosition, oldPosition);
        totalDistance += newDistance;
        currentDistance += newDistance;

        // TrailMaker.instance.AddMarker();

        UIUpdater.instance.UpdateDistanceValue(currentDistance, 0);
        UIUpdater.instance.UpdateDistanceValue(totalDistance, 2);

        UIUpdater.instance.UpdateTimeValue(currentTime, 0);
        UIUpdater.instance.UpdateTimeValue(totalTime, 2);

        UIUpdater.instance.UpdateGridValue(Grid.GetGridCoords(new Vector2(transform.position.x, transform.position.z)));
    }

    private void CheckGrids()
    {

        newGrid = Grid.GetGrid(new Vector2(transform.position.x, transform.position.z));

        if (currentGrid != newGrid || !startLocationSet)
        {
            // Generate new grids if in new grid
            Vector2Int newGridCoords = Grid.GetGridCoords(new Vector2(transform.position.x, transform.position.z));
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    Vector2Int anotherGridCoords = newGridCoords + new Vector2Int(i, j);
                    if (Grid.GetGrid(anotherGridCoords) == null)
                    {
                        Grid anotherGrid = new Grid(anotherGridCoords);
                        Debug.Log("Grid " + anotherGrid.id + " created.");
                    }
                }
            }

            // Set unused Grids inactive and new Grids as active
            for (int i = 0; i < Grid.allGrids.Count; i++)
            {
                Grid.allGrids[i].gridObject.SetActive(false);
            }
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    Grid.GetGrid(newGrid.id + new Vector2Int(i, j)).gridObject.SetActive(true);
                }
            }

            currentGrid = newGrid;
        }
    }

    private void Disconnect()
    {
        float xAbs = Mathf.Abs(transform.position.x - currentCelestialBody.absolutePosition.x);
        float yAbs = Mathf.Abs(transform.position.z - currentCelestialBody.absolutePosition.y);
        float xSign = Mathf.Sign(transform.position.x - currentCelestialBody.absolutePosition.x);
        float ySign = Mathf.Sign(transform.position.z - currentCelestialBody.absolutePosition.y);
        float mult;

        if (xSign > 0)
        {
            if (ySign > 0)
            {
                mult = 0;
                currentDir = Mathf.Atan(yAbs / xAbs) + Mathf.PI * (mult + 0.5f);
            }
            else
            {
                mult = -0.5f;
                currentDir = Mathf.Atan(xAbs / yAbs) + Mathf.PI * (mult + 0.5f);
            }
        }
        else
        {
            if (ySign > 0)
            {
                mult = 0.5f;
                currentDir = Mathf.Atan(xAbs / yAbs) + Mathf.PI * (mult + 0.5f);
            }
            else
            {
                mult = 1f;
                currentDir = Mathf.Atan(yAbs / xAbs) + Mathf.PI * (mult + 0.5f);
            }
        }
        connected = false;
        currentDir += Mathf.PI * (1 - direction) / 2;
        CameraFollower.instance.caughtUp = false;

        lastDistance = currentDistance;
        currentDistance = 0;
        UIUpdater.instance.UpdateDistanceValue(lastDistance, 1);

        UIUpdater.instance.UpdateTimeValue(currentTime, 1);

        currentCelestialBody.currentGravity = 0;
    }

    private void Reconnect(CelestialBody newCelestialBody)
    {
        RecalculateCurrentCelestialBodyParameters(newCelestialBody);

        collisionDistance = Vector3.Distance(newCelestialBody.coreObject.transform.position, transform.position);
        distanceFromOrbit = collisionDistance - currentCelestialBody.orbitRadius;

        Vector2 incidentAngle = currentCelestialBody.absolutePosition - new Vector2(transform.position.x, transform.position.z);
        incidentAngle = Vector2.ClampMagnitude(incidentAngle, 1f);
        Vector2 travelAngle = new Vector2(Mathf.Cos(currentDir), Mathf.Sin(currentDir));
        travelAngle = Vector2.ClampMagnitude(travelAngle, 1f);

        float angleDiff = Vector2.SignedAngle(incidentAngle, travelAngle);
        UIUpdater.instance.UpdateAngleValue(angleDiff);

        if (angleDiff < 0)
        {
            direction = 1;
        }
        else
        {
            direction = -1;
        }

        float xAbs = Mathf.Abs(transform.position.x - currentCelestialBody.absolutePosition.x);
        float yAbs = Mathf.Abs(transform.position.z - currentCelestialBody.absolutePosition.y);
        float xSign = Mathf.Sign(transform.position.x - currentCelestialBody.absolutePosition.x);
        float ySign = Mathf.Sign(transform.position.z - currentCelestialBody.absolutePosition.y);
        float mult;

        if (xSign > 0)
        {
            if (ySign > 0)
            {
                mult = 0;
                currentDir = Mathf.Atan(yAbs / xAbs) + Mathf.PI * (mult);
            }
            else
            {
                mult = -0.5f;
                currentDir = Mathf.Atan(xAbs / yAbs) + Mathf.PI * (mult);
            }
        }
        else
        {
            if (ySign > 0)
            {
                mult = 0.5f;
                currentDir = Mathf.Atan(xAbs / yAbs) + Mathf.PI * (mult);
            }
            else
            {
                mult = 1f;
                currentDir = Mathf.Atan(yAbs / xAbs) + Mathf.PI * (mult);
            }
        }

        connectTime = Time.time;
        connected = true;
        CameraFollower.instance.caughtUp = false;


        lastDistance = currentDistance;
        currentDistance = 0;
        UIUpdater.instance.UpdateDistanceValue(lastDistance, 1);

        UIUpdater.instance.UpdateTimeValue(currentTime, 1);

        Debug.Log("Connected to " + newCelestialBody.coreObject.name);
    }

    private void RecalculateCurrentCelestialBodyParameters(CelestialBody newCelestialBody)
    {
        currentCelestialBody = newCelestialBody;
    }

    public bool EnteredOrbit(CelestialBody newCelestialBody)
    {
        float distBetweenCentres = Vector3.Distance(transform.position, newCelestialBody.coreObject.transform.position);
        float orbitDist = cometRadius / 2 + newCelestialBody.orbitRadius;
        bool result = distBetweenCentres <= orbitDist;
        return result;
    }

    public CelestialBody OrbitEnteredInGrid(Grid grid)
    {
        for (int i = 0; i < grid.celestialBodys.Count; i++)
        {
            if (EnteredOrbit(grid.celestialBodys[i]) && currentCelestialBody != grid.celestialBodys[i])
            {
                return grid.celestialBodys[i];
            }
        }
        return null;
    }

    public CelestialBody OrbitEnteredInSurroundingGrids(Grid grid)
    {
        List<Grid> surroundGrids = Grid.GetSurroundingGrids(grid);
        for (int i = 0; i < surroundGrids.Count; i++)
        {
            CelestialBody orbitEntered = OrbitEnteredInGrid(surroundGrids[i]);
            if (orbitEntered != null)
            {
                return orbitEntered;
            }
        }
        return null;
    }

    private void Restart()
    {
        Reconnect(firstCelestialBody);
        currentTime = Time.time;
        connectTime = Time.time;
        currentDir = 0;
    }

    public static Vector2 GetLocation()
    {
        float x = instance.transform.position.x;
        float y = instance.transform.position.z;
        return new Vector2(x, y);
    }
}
