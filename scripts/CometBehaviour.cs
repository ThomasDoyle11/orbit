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

    private float speed = 20f;
    private float acceleration = 0.2f;
    private float maxSpeed = 25f;
    int direction = 1;
    public float currentDir = 0;

    float cometRadius = 0.5f;
    readonly float minSnapDistance = 1f;
    readonly float maxSnapDistance = MyGrid.celestialBodyBuffer;
    readonly float snapDistanceRatio = 0.1f;
    readonly float minSnapSpeedRatio = 0.1f;
    float currentSnapDistance;
    float currentSnapSpeed;
    float initialSnapSpeed;
    float snapDecel;

    float totalTime;
    float currentTime
    {
        get
        {
            return totalTime - connectTime;
        }
    }
    float connectTime;

    float currentDistance;
    float lastDistance;
    float totalDistance;

    public Vector2 inxLastCheckPoint;
    public Vector2 inxPosition;
    public Vector2 inxPositionNoBuffer;
    public bool inxFound;
    public float inxDistance;
    public CelestialBody inxBody;

    public TrailRenderer trailRend;

    public bool enteredBlackHole = false;

    Vector2 cometPos
    {
        get
        {
            return new Vector2(transform.position.x, transform.position.z);
        }
    }
    float currentAngle;

    public bool startLocationSet = false;
    public bool ignoreNextTrail = false;
    public MyGrid currentGrid;
    public MyGrid newGrid;

    public CelestialBody firstCelestialBody;
    public CelestialBody currentCelestialBody;

    private bool autoplay = false;

    private int gridBuffer = 1;

    public bool coroutinesRunning
    {
        get
        {
            return GameController.instance.coroutinesRunning;
        }
        set
        {
            GameController.instance.coroutinesRunning = value;
        }
    }
    public int gridCheckQueue = 0;
    public int currentGridCheck = 0;
    public bool acceptInput = false;

    public enum ConnectState
    {
        Connected,
        Disconnected,
        Paused,
        SpittingOut
    }
    public ConnectState connected;

    // Start is called before the first frame update
    void Start()
    {
        trailRend = GetComponent<TrailRenderer>();
        transform.localScale = 2 * new Vector3(cometRadius, cometRadius, cometRadius);
        Restart(true);
    }

    // Update is called once per frame
    void Update()
    {
        // Update times
        totalTime = Time.time;

        if (autoplay)
        {
            if (connected == ConnectState.Connected && currentTime > 4 && currentCelestialBody.bodyType != CelestialBody.BodyType.BlackHole)
            {
                Disconnect();
            }
        }

        // Check for input
        if (acceptInput)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log("Left Button Down");
                if (connected != ConnectState.Disconnected)
                {
                    Disconnect();
                }
            }
            else if (Input.GetMouseButtonDown(1))
            {
                Debug.Log("Right Button Down");
                Restart(false);
            }
            else if (Input.GetMouseButtonDown(2))
            {
                //Debug.Log("Middle Button Down");
                if (connected != ConnectState.Disconnected)
                {
                    connectTime = Time.time;
                    direction = -direction;
                }
            }
        }

        // Move comet
        Vector3 oldPosition = transform.position;
        if (connected == ConnectState.Connected)
        {
            // Update CelestialBody parameters and see if Comet must react, else orbit normally
            if (connected == ConnectState.Connected)
            {
                currentCelestialBody.currentDestruction += currentCelestialBody.destructionFactor * Time.deltaTime;
                currentCelestialBody.currentGravity += currentCelestialBody.gravityFactor * Time.deltaTime;
            }
            if (currentCelestialBody.currentDestruction >= 1)
            {
                Disconnect();
                currentCelestialBody.grid.RemoveCelestialBody(currentCelestialBody);
                currentCelestialBody.grid.celestialBodys.Remove(currentCelestialBody);
            }
            else if (currentCelestialBody.currentGravity >= 1)
            {
                if (currentCelestialBody.bodyType != CelestialBody.BodyType.BlackHole)
                {
                    Disconnect();
                }
                else
                {
                    connected = ConnectState.Paused;
                    enteredBlackHole = true;
                    Restart(currentCelestialBody.grid.NextGridWithBlackHole().id);
                }
            }
            else
            {
                if (speed < maxSpeed && connected == ConnectState.Connected)
                {
                    speed += acceleration * Time.deltaTime;
                    UIUpdater.instance.UpdateSpeedValue(speed);
                }
                SetCometConnectedPosition();

                // Debug.Log(Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(currentCelestialBody.coreObject.transform.position.x, currentCelestialBody.coreObject.transform.position.z)));
            }
        }
        else if (connected == ConnectState.Disconnected)
        {
            bool reconnecting = false;

            if (!reconnecting)
            {
                // Update position of Comet
                float currentXPos = transform.position.x;
                float currentYPos = transform.position.z;
                transform.position = new Vector3(currentXPos + speed * Mathf.Cos(currentDir) * Time.deltaTime, GameController.instance.cometLayer, currentYPos + speed * Mathf.Sin(currentDir) * Time.deltaTime);

                // Check if new grid is entered and if so generate if needed and set unused as inactive
                CheckGrids();
            }

            Vector3 oldPos = transform.position;
            if (inxFound)
            {
                if (Vector2.Distance(cometPos, inxLastCheckPoint) >= inxDistance)
                {
                    if (inxBody.ExistsInScene())
                    {
                        //transform.position = oldPos;
                        Reconnect(inxBody);
                        reconnecting = true;
                    }
                    else
                    {
                        Debug.Log("No longer exists: " + inxBody.name);
                        IntersectionInSurroundingGrids(currentGrid);
                    }
                }
            }
        }
        else if (connected == ConnectState.SpittingOut)
        {
            currentCelestialBody.currentGravity -= currentCelestialBody.gravityFactor * Time.deltaTime;
            SetCometConnectedPosition();
            if (currentCelestialBody.currentGravity <= 0)
            {
                Disconnect();
            }
        }
        else if (connected == ConnectState.Paused)
        {

        }

        Vector3 newPosition = transform.position;
        float newDistance = Vector3.Distance(newPosition, oldPosition);
        totalDistance += newDistance;
        currentDistance += newDistance;

        float newSpeed = newDistance / Time.deltaTime;
        Debug.Log(connected);
        Debug.Log(newSpeed);
        UIUpdater.instance.UpdateMeasuredSpeedValue(newSpeed);

        UIUpdater.instance.UpdateDistanceValue(currentDistance, 0);
        UIUpdater.instance.UpdateDistanceValue(totalDistance, 2);

        UIUpdater.instance.UpdateTimeValue(currentTime, 0);
        UIUpdater.instance.UpdateTimeValue(totalTime, 2);

        UIUpdater.instance.UpdateGridValue(MyGrid.GetGridCoords(new Vector2(transform.position.x, transform.position.z)));
    }

    private void SetCometConnectedPosition()
    {
        if (currentSnapDistance > 0)
        {
            currentSnapDistance -= currentSnapSpeed * Time.deltaTime;

            currentAngle += Mathf.Pow(Mathf.Pow(speed, 2) - Mathf.Pow(currentSnapSpeed, 2), 0.5f) * Time.deltaTime * direction / currentCelestialBody.outerRadius;

            currentSnapSpeed -= snapDecel * Time.deltaTime;
            if (currentSnapDistance <= 0 || currentSnapSpeed <= 0)
            {
                currentSnapDistance = 0;
                currentSnapSpeed = 0;
            }
        }
        else
        {
            float denom = currentCelestialBody.innerRadius + (currentCelestialBody.outerRadius - currentCelestialBody.innerRadius) * (1 - currentCelestialBody.currentGravity);
            if (denom != 0)
            {
                currentAngle += Time.deltaTime * speed * direction / denom;
            }
        }
        //Debug.Log(currentAngle);
        //Debug.Log(currentCelestialBody.currentGravity);
        float cos = Mathf.Cos(currentAngle);
        float sin = Mathf.Sin(currentAngle);
        float xPos = currentCelestialBody.absolutePosition.x + (currentCelestialBody.innerRadius + currentSnapDistance + (currentCelestialBody.outerRadius - currentCelestialBody.innerRadius) * (1 - currentCelestialBody.currentGravity)) * cos;
        float yPos = currentCelestialBody.absolutePosition.y + (currentCelestialBody.innerRadius + currentSnapDistance + (currentCelestialBody.outerRadius - currentCelestialBody.innerRadius) * (1 - currentCelestialBody.currentGravity)) * sin;
        transform.position = new Vector3(xPos, GameController.instance.cometLayer, yPos);

        // Update rotation of comet
        float rotation = -Vector2.SignedAngle(Vector2.up, new Vector2(sin, -cos) * direction);
        transform.rotation = Quaternion.Euler(0f, rotation, 0f);

        //Debug.Log(transform.position);
    }

    private IEnumerator CheckGridsCoroutine(MyGrid currentGrid, MyGrid newGrid)
    {
        if (newGrid != null && currentGrid != null)
        {
            int myGridCheck = gridCheckQueue;
            gridCheckQueue += 1;
            while (coroutinesRunning || MyGrid.waitingToRemoveBody || MyGrid.waitingToCreateGrid || myGridCheck != currentGridCheck)
            {
                yield return null;
            }
            //Debug.Log("Checking " + currentGrid.id + " " + newGrid.id);
            coroutinesRunning = true;

            Vector2Int diff = newGrid.id - currentGrid.id;
            if (Mathf.Abs(diff.x) == 1)
            {
                for (int i = -gridBuffer; i <= gridBuffer; i++)
                {
                    MyGrid.GetGrid(currentGrid.id + new Vector2Int(-diff.x, i)).ActivateInScene(false);
                    MyGrid.GenerateGridAndCreateInScene(newGrid.id + new Vector2Int(diff.x, i));
                    yield return null;
                }
            }
            if (Mathf.Abs(diff.y) == 1)
            {
                for (int i = -gridBuffer; i <= gridBuffer; i++)
                {
                    MyGrid.GetGrid(currentGrid.id + new Vector2Int(i, -diff.y)).ActivateInScene(false);
                    MyGrid.GenerateGridAndCreateInScene(newGrid.id + new Vector2Int(i, diff.y));
                    yield return null;
                }
            }
            if (Mathf.Abs(diff.x) == 1 && Mathf.Abs(diff.y) == 1)
            {

            }

            // If no future intersection has been found, search for one in the new grids
            // Actually, search no matter what in case the body that would've been intersected has been destroyed
            if (connected == ConnectState.Disconnected)
            {
                IntersectionInSurroundingGrids(newGrid);
            }
            //Debug.Log("Done " + currentGrid.id + " " + newGrid.id);
            currentGridCheck += 1;
            coroutinesRunning = false;
        }
    }

    private void CheckGrids()
    {
        newGrid = MyGrid.GetGrid(new Vector2(transform.position.x, transform.position.z));

        if (currentGrid != newGrid || !startLocationSet)
        {
            StartCoroutine(CheckGridsCoroutine(currentGrid, newGrid));
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

        if (connected == ConnectState.SpittingOut)
        {
            currentCelestialBody.blackHole.InvertFlow();
        }

        connected = ConnectState.Disconnected;
        currentDir += Mathf.PI * (1 - direction) / 2;
        CameraController.instance.NotCaughtUp();

        lastDistance = currentDistance;
        currentDistance = 0;
        UIUpdater.instance.UpdateDistanceValue(lastDistance, 1);

        UIUpdater.instance.UpdateTimeValue(currentTime, 1);

        currentCelestialBody.currentGravity = 0;

        if (currentCelestialBody.bodyType == CelestialBody.BodyType.BlackHole)
        {
            CameraController.instance.QuickCover(false);
        }

        IntersectionInSurroundingGrids(currentGrid);
    }

    private void Reconnect(CelestialBody newCelestialBody)
    {
        currentSnapDistance = SnapDistance(newCelestialBody);
        // Debug.Log(currentSnapDistance);
        inxFound = false;

        currentCelestialBody = newCelestialBody;

        Vector2 incidentAngle = currentCelestialBody.absolutePosition - inxPosition;
        incidentAngle = Vector2.ClampMagnitude(incidentAngle, 1f);

        Vector2 travelAngle = new Vector2(Mathf.Cos(currentDir), Mathf.Sin(currentDir));
        travelAngle = Vector2.ClampMagnitude(travelAngle, 1f);

        float angleDiff = Vector2.SignedAngle(incidentAngle, travelAngle);

        // Use this value for connection bonuses
        float angleDiffNoBuffer;
        if (inxPositionNoBuffer != Vector2.zero)
        {
            Vector2 incidentAngleNoBuffer = currentCelestialBody.absolutePosition - inxPositionNoBuffer;
            incidentAngleNoBuffer = Vector2.ClampMagnitude(incidentAngleNoBuffer, 1f);

            angleDiffNoBuffer = Vector2.SignedAngle(incidentAngleNoBuffer, travelAngle);
        }
        else
        {
            angleDiffNoBuffer = Mathf.Sign(angleDiff) * 90;
        }
        UIUpdater.instance.UpdateAngleValue(angleDiff, angleDiffNoBuffer);

        float angleDiffRad = angleDiff * Mathf.PI / 180;

        initialSnapSpeed = speed * Mathf.Abs(Mathf.Cos(angleDiffRad));
        initialSnapSpeed = Mathf.Max(minSnapSpeedRatio * speed, initialSnapSpeed);
        currentSnapSpeed = initialSnapSpeed;
        snapDecel = Mathf.Pow(initialSnapSpeed, 2) / (2 * SnapDistance(newCelestialBody));

        // Debug.Log(initialSnapSpeed);
        if (newCelestialBody.bodyType != CelestialBody.BodyType.BlackHole)
        {
            if (angleDiff < 0)
            {
                direction = 1;
            }
            else
            {
                direction = -1;
            }
        }
        else
        {
            direction = newCelestialBody.blackHole.direction;
        }

        float xAbs = Mathf.Abs(inxPosition.x - currentCelestialBody.absolutePosition.x);
        float yAbs = Mathf.Abs(inxPosition.y - currentCelestialBody.absolutePosition.y);
        float xSign = Mathf.Sign(inxPosition.x - currentCelestialBody.absolutePosition.x);
        float ySign = Mathf.Sign(inxPosition.y - currentCelestialBody.absolutePosition.y);
        float mult;

        if (xSign > 0)
        {
            if (ySign > 0)
            {
                mult = 0;
                currentAngle = Mathf.Atan(yAbs / xAbs) + Mathf.PI * (mult);
            }
            else
            {
                mult = -0.5f;
                currentAngle = Mathf.Atan(xAbs / yAbs) + Mathf.PI * (mult);
            }
        }
        else
        {
            if (ySign > 0)
            {
                mult = 0.5f;
                currentAngle = Mathf.Atan(xAbs / yAbs) + Mathf.PI * (mult);
            }
            else
            {
                mult = 1f;
                currentAngle = Mathf.Atan(yAbs / xAbs) + Mathf.PI * (mult);
            }
        }

        connectTime = Time.time;
        connected = ConnectState.Connected;
        CameraController.instance.NotCaughtUp();


        lastDistance = currentDistance;
        currentDistance = 0;
        UIUpdater.instance.UpdateDistanceValue(lastDistance, 1);

        UIUpdater.instance.UpdateTimeValue(currentTime, 1);

        // Debug.Log("Connected to " + newCelestialBody.bodyObject.name);

        if (newCelestialBody.bodyType == CelestialBody.BodyType.BlackHole)
        {
            CameraController.instance.Cover(true, 1 / newCelestialBody.gravityFactor);
        }

        //SetCometConnectedPosition();
    }

    private float SnapDistance(CelestialBody celBody)
    {
        return Mathf.Clamp(celBody.outerRadius * snapDistanceRatio, minSnapDistance, maxSnapDistance);
    }

    private void ConnectToFirstBody()
    {
        // TrailMaker.instance.ClearTrail();

        currentSnapDistance = 0;
        inxFound = false;

        currentCelestialBody = firstCelestialBody;

        initialSnapSpeed = 0;
        currentSnapSpeed = 0;
        snapDecel = 0;

        currentDir = 0;
        currentAngle = 0;

        connectTime = Time.time;
        if (!enteredBlackHole)
        {
            connected = ConnectState.Connected;
            direction = 1;
        }
        else
        {
            firstCelestialBody.currentGravity = 1;
            firstCelestialBody.blackHole.InstantInvertFlow();
            connected = ConnectState.SpittingOut;
            direction = firstCelestialBody.blackHole.direction;
        }
        CameraController.instance.NotCaughtUp();


        lastDistance = currentDistance;
        currentDistance = 0;
        UIUpdater.instance.UpdateDistanceValue(lastDistance, 1);

        UIUpdater.instance.UpdateTimeValue(currentTime, 1);

        // Debug.Log("Connected to " + newCelestialBody.bodyObject.name);

        SetCometConnectedPosition();
        trailRend.Clear();
    }

    public bool CheckIfWillIntersectBody(CelestialBody celBody, bool useSnapDistance, out Vector2 inxPoint, out float distance)
    {
        inxPoint = Vector2.zero;
        distance = 0;

        float px = celBody.absolutePosition.x;
        float py = celBody.absolutePosition.y;
        float cx = transform.position.x;
        float cy = transform.position.z;
        float nx = Mathf.Cos(currentDir);
        float ny = Mathf.Sin(currentDir);
        float m = nx * (px - cx) + ny * (py - cy);
        if (m <= 0)
        {
            // Debug.Log("Body is behind: " + celBody.name);
            return false;
        }
        float r = celBody.outerRadius;
        if (useSnapDistance)
        {
            r += SnapDistance(celBody);
        }

        float discriminant = Mathf.Pow(r, 2) + Mathf.Pow(m, 2) - Mathf.Pow(cx - px, 2) - Mathf.Pow(cy - py, 2);
        if (discriminant < 0)
        {
            // Debug.Log("No intersection with " + celBody.name);
            return false;
        }

        // Find first intersection which is by using negative discriminant
        float d = m - Mathf.Pow(discriminant, 0.5f);
        float qx = cx + nx * d;
        float qy = cy + ny * d;

        inxPoint = new Vector2(qx, qy);
        distance = d;

        //Debug.Log("Inx: " + celBody.name);
        //Debug.Log("Distance to inx: " + d);
        //Debug.Log("Start: (" + cx + ", " + cy + ")");
        //Debug.Log("Dir: (" + nx + ", " + ny + ")");
        //Debug.Log("Inx: (" + qx + ", " + qy + ")");
        //Debug.Log(currentDir);

        return true;
    }

    public CelestialBody IntersectionInSurroundingGrids(MyGrid grid)
    {
        inxFound = false;
        inxPosition = Vector2.zero;
        inxDistance = 0;
        inxBody = null;
        inxLastCheckPoint = cometPos;

        // Debug.Log("Looking");

        List<MyGrid> surroundGrids = grid.GetSurroundingGrids();
        CelestialBody currentCelBody = null;
        Vector2 currentInxPoint = Vector2.zero;
        Vector2 newInxPoint;
        float currentDistance = 1000000;
        float newDistance;
        for (int i = 0; i < surroundGrids.Count; i++)
        {
            for (int j = 0; j < surroundGrids[i].celestialBodys.Count; j++)
            {
                bool isInx = CheckIfWillIntersectBody(surroundGrids[i].celestialBodys[j], true, out newInxPoint, out newDistance);
                if (isInx && newDistance < currentDistance && surroundGrids[i].celestialBodys[j] != currentCelestialBody)
                {
                    currentInxPoint = newInxPoint;
                    currentDistance = newDistance;
                    currentCelBody = surroundGrids[i].celestialBodys[j];
                }
            }
        }

        if (currentCelBody != null)
        {
            inxFound = true;
            inxBody = currentCelBody;
            inxDistance = currentDistance;
            inxPosition = currentInxPoint;

            CheckIfWillIntersectBody(currentCelBody, false, out inxPositionNoBuffer, out _);

            //Debug.Log("Body: " + inxBody.name);
            //Debug.Log("Distance: " + inxDistance);
            //Debug.Log("Now: " + inxLastCheckPoint);
            //Debug.Log("Then: " + inxPosition);
        }

        return currentCelBody;
    }

    private void OnGravityMax()
    {
        Disconnect();
    }

    private void OnDestruction()
    {
        Disconnect();
        currentCelestialBody.grid.RemoveCelestialBody(currentCelestialBody);
    }

    private void Restart(bool isFirstTime)
    {
        acceptInput = false;
        GameController.instance.StopAllCoroutines();
        StopAllCoroutines();
        if (!isFirstTime)
        {
            CameraController.instance.QuickCover(true);
        }
        StartCoroutine(RestartCoroutine());
    }

    private IEnumerator RestartCoroutine()
    {
        CameraController.instance.Pause(true);
        while (!CameraController.instance.isCovered)
        {
            yield return null;
        }
        connected = ConnectState.Paused;
        MyGrid.ClearAllGrids();

        MyGrid.GenerateGridAndCreateInScene(MyGrid.startID);

        // Generate new surrounding grids
        for (int i = -gridBuffer; i <= gridBuffer; i++)
        {
            for (int j = -gridBuffer; j <= gridBuffer; j++)
            {
                Vector2Int anotherGridCoords = MyGrid.startID + new Vector2Int(i, j);
                MyGrid.GenerateGridAndCreateInScene(anotherGridCoords);
            }
        }

        inxFound = false;
        inxLastCheckPoint = firstCelestialBody.absolutePosition;
        inxPosition = Vector2.zero;
        inxDistance = 0;
        inxBody = null;

        currentAngle = 0;

        while (coroutinesRunning)
        {
            yield return null;
        }

        ConnectToFirstBody();
        enteredBlackHole = false;
        startLocationSet = true;
        CameraController.instance.QuickCatchUp();
        CameraController.instance.Pause(false);
        CameraController.instance.QuickCover(false);
        acceptInput = true;
    }

    private void Restart(Vector2Int newStartID)
    {
        GameController.instance.startID = newStartID;
        Restart(false);
    }

    public static Vector2 GetLocation()
    {
        float x = instance.transform.position.x;
        float y = instance.transform.position.z;
        return new Vector2(x, y);
    }
}