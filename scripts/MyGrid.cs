using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;

public class MyGrid
{
    public static List<MyGrid> allGrids = new List<MyGrid>();
    public static List<Vector2Int> gridsInSceneIDs = new List<Vector2Int>();

    public static int maxGenAttempts = 50;
    public static int maxCelestialBodys = 20;
    public static float celestialBodyBuffer = 5;
    public static int maxBlackHoles = 1;
    private static bool isFirstBody = true;
    public static Vector2 size
    {
        get
        {
            return GameController.instance.gridSize;
        }
    }
    public static Vector2Int startID
    {
        get 
        {
            return GameController.instance.startID;
        }
    }

    public Vector2Int id;
    public Vector2Int nextID;
    public List<CelestialBody> celestialBodys = new List<CelestialBody>();
    private GameObject gridObject;
    public Transform celestialBodysObject
    {
        get
        {
            return gridObject.transform.Find("CelestialBodys");
        }
    }

    public int numStars;
    public int numBlackHoles;

    private System.Random sysRand;

    public static bool coroutinesRunning
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

    public static bool waitingToRemoveBody = false;
    public static bool waitingToCreateGrid = false;

    public bool isActiveInScene { get; private set; }

    private MyGrid(Vector2Int id)
    {
        if (!GridExists(id))
        {
            // Create unique seed from Grid reference using Signed Cantor method
            int a = id.x >= 0 ? 2 * id.x : -2 * id.x - 1;
            int b = id.y >= 0 ? 2 * id.y : -2 * id.y - 1;
            int seed = (a + b) * (a + b + 1) / 2 + b;
            // Debug.Log(seed);
            sysRand = new System.Random(seed);

            numBlackHoles = 0;

            isActiveInScene = false;
            this.id = id;
            nextID = new Vector2Int(sysRand.Next(-GameController.instance.maxID, GameController.instance.maxID), sysRand.Next(-GameController.instance.maxID, GameController.instance.maxID));
            allGrids.Add(this);

            GenerateCelestialBodys();
        }
        else
        {
            Debug.Log("Grid data already exists.");
        }
    }

    // Returns the Grid and all adjacent Grids (including diagonals)
    public List<MyGrid> GetSurroundingGrids()
    {
        List<MyGrid> returnGrids = new List<MyGrid>();
        List<Vector2Int> returnGridIDs = new List<Vector2Int>();
        // Get a list of all IDs of surrounding Grids
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                returnGridIDs.Add(new Vector2Int(id.x + i, id.y + j));
            }
        }
        // Add surrounding Grids to a list
        for (int i = 0; i < allGrids.Count; i++)
        {
            
            if (returnGridIDs.Contains(allGrids[i].id))
            {
                returnGrids.Add(allGrids[i]);
            }
        }

        return returnGrids;
    }
    public List<MyGrid> GetSurroundingGrids(Vector2Int id)
    {
        return GetGrid(id).GetSurroundingGrids();
    }

    // Get the Grid at the given coordinates
    public static MyGrid GetGrid(Vector2Int id)
    {
        for (int i = 0; i < allGrids.Count; i++) 
        {
            if (allGrids[i].id == id)
            {
                return allGrids[i];
            }
        }
        Debug.Log("No such Grid: " + id);
        return null;
    }

    // Get the Grid which contains the given Vector2 position
    public static MyGrid GetGrid(Vector2 position)
    {
        return GetGrid(GetGridCoords(position));
    }

    // Get the coordinates of the Grid which contains the given Vector2 position
    public static Vector2Int GetGridCoords(Vector2 position)
    {
        int idX = Mathf.RoundToInt(position.x / size.x) + startID.x;
        int idY = Mathf.RoundToInt(position.y / size.y) + startID.y;
        return new Vector2Int(idX, idY);
    }

    // Get the Vector2 position of the bottom-left corner of the Grid
    public Vector2 GetOrigin()
    {
        float x = -size.x / 2 + (id.x - startID.x) * size.x;
        float y = -size.y / 2 + (id.y - startID.y) * size.y;
        return new Vector2(x, y);
    }

    // Get the Vector2 position of the centre of the Grid
    public Vector2 GetCentre()
    {
        float x = (id.x - startID.x) * size.x;
        float y = (id.y - startID.y) * size.y;
        return new Vector2(x, y);
    }

    // Generate a visble mesh around the edge of the Grid
    public void GenerateGridMesh()
    {
        GameObject gridMesh = new GameObject("Grid Mesh");
        gridMesh.transform.SetParent(gridObject.transform);
        Vector2[,] corners = new Vector2[2, 2];

        for (int i = 0; i <= 1; i++)
        {
            for (int j = 0; j <= 1; j++)
            {
                Vector2 offset = new Vector2(i, j);
                corners[i, j] = GetOrigin() + offset * size;
                Object.Instantiate(GameController.instance.gridCorner, new Vector3(corners[i, j].x, 0, corners[i, j].y), Quaternion.identity, gridMesh.transform);
            }
        }
        for (int i = 0; i <= 1; i++)
        {
            float newScale = Vector2.Distance(corners[i, 0], corners[i, 1]) / 2;
            Vector2 newPosition = (corners[i, 0] + corners[i, 1]) / 2;
            GameObject newGridLine = Object.Instantiate(GameController.instance.gridLine, new Vector3(newPosition.x, 0, newPosition.y), Quaternion.Euler(90, 0, 0), gridMesh.transform);
            newGridLine.transform.localScale = new Vector3(0.5f, newScale, 0.5f);

            newScale = Vector2.Distance(corners[0, i], corners[1, i]) / 2;
            newPosition = (corners[0, i] + corners[1, i]) / 2;
            newGridLine = Object.Instantiate(GameController.instance.gridLine, new Vector3(newPosition.x, 0, newPosition.y), Quaternion.Euler(90, 90, 0), gridMesh.transform);
            newGridLine.transform.localScale = new Vector3(0.5f, newScale, 0.5f);
        }
    }

    // Add a CelestialBody to the Grid, with a random size and position
    public bool AddCelestialBody()
    {
        CelestialBody newCelBody = CelestialBody.RandomBody(this, sysRand.Next());

        if (newCelBody.bodyType == CelestialBody.BodyType.BlackHole)
        {
            if (numBlackHoles >= maxBlackHoles)
            {
                // Don't exceed max black holes
                return false;
            }
        }

        // Check adjacent grids for overlapping celestialBody orbits before creating CelestialBody
        // If overlapping with other grid, keep larger body and destroy other body
        // If overlapping with current grid, don't generate new body
        List<CelestialBody> bodiesInTheWay = new List<CelestialBody>();
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                Vector2Int offset = new Vector2Int(i, j);
                if (GridExists(id + offset))
                {
                    MyGrid currentGrid = GetGrid(id + offset);
                    Vector2 gridOrigin = currentGrid.GetOrigin();
                    for (int k = 0; k < currentGrid.celestialBodys.Count; k++)
                    {
                        CelestialBody existingCelestialBody = currentGrid.celestialBodys[k];
                        Vector2 existingCelestialBodyPosition = gridOrigin + existingCelestialBody.position;
                        float existingOrbitRadius = existingCelestialBody.outerRadius;
                        float distance = Vector2.Distance(existingCelestialBodyPosition, newCelBody.absolutePosition);
                        bool isOverlapping = distance < (existingOrbitRadius + newCelBody.outerRadius + celestialBodyBuffer);
                        if (isOverlapping)
                        {
                            if (i == 0 && j == 0)
                            {
                                // Don't create in same Grid
                                return false;
                            }
                            if (existingCelestialBody.absolutePosition.x > newCelBody.absolutePosition.x || existingCelestialBody == CometBehaviour.instance.firstCelestialBody || existingCelestialBody.bodyType == CelestialBody.BodyType.BlackHole)
                            {
                                // Don't overwite if small or if existing body is first Celestial Body, or if Black Hole
                                return false;
                            }
                            else
                            {
                                // Debug.Log(id + ": " + existingCelestialBody.name + " vs " + newCelestialBody.name);
                                bodiesInTheWay.Add(existingCelestialBody);
                            }
                        }
                    }
                }
            }
        }

        foreach(CelestialBody celBody in bodiesInTheWay)
        {
            RemoveCelestialBody(celBody);
        }

        celestialBodys.Add(newCelBody);

        if (newCelBody.bodyType == CelestialBody.BodyType.BlackHole)
        {
            numBlackHoles += 1;
        }

        if (isFirstBody)
        {
            if (!CometBehaviour.instance.enteredBlackHole && newCelBody.bodyType == CelestialBody.BodyType.StandardPlanet)
            {
                CometBehaviour.instance.currentGrid = this;
                CometBehaviour.instance.firstCelestialBody = newCelBody;
                CometBehaviour.instance.newGrid = this;
                isFirstBody = false;
            }
            else if (CometBehaviour.instance.enteredBlackHole && newCelBody.bodyType == CelestialBody.BodyType.BlackHole)
            {
                CometBehaviour.instance.currentGrid = this;
                CometBehaviour.instance.firstCelestialBody = newCelBody;
                CometBehaviour.instance.newGrid = this;
                isFirstBody = false;
            }
        }

        return true;
    }

    public void RemoveCelestialBody(CelestialBody celBody)
    {
        GameController.instance.StartCoroutine(RemoveCelestialBodyCoroutine(celBody));
    }

    private IEnumerator RemoveCelestialBodyCoroutine(CelestialBody celBody)
    {
        waitingToRemoveBody = true;
        while (coroutinesRunning)
        {
            yield return null;
        }
        waitingToRemoveBody = false;
        coroutinesRunning = true;
        celBody.grid.celestialBodys.Remove(celBody);
        // Debug.Log(id + ": Removed: " + celBody.name);

        if (celBody.ExistsInScene())
        {
            //celBody.bodyObject.SetActive(false);
            celBody.RemoveFromScene();
            // Debug.Log(id + ": Body deact: " + celBody.name);
        }
        else
        {
            // Debug.Log(id + ": No body: " + celBody.name);
        }
        //Debug.Log(celBody.name);
        coroutinesRunning = false;
    }

    // Generate CelestialBodys in the Grid
    public void GenerateCelestialBodys()
    {
        int currentGenAttempts = 0;
        int currentCelestialBodys = 0;
        while (currentGenAttempts < maxGenAttempts && currentCelestialBodys < maxCelestialBodys)
        {
            if (AddCelestialBody())
            {
                currentGenAttempts = 0;
                currentCelestialBodys += 1;
            }
            else
            {
                currentGenAttempts += 1;
            }
            //yield return null;
        }
        if (currentGenAttempts >= maxGenAttempts)
        {
            //Debug.Log("Max attempts reached for Grid " + id + "\n");
        }
        else if (currentCelestialBodys >= maxCelestialBodys)
        {
            //Debug.Log("Max celestialBodys generated for Grid " + id + "\n");
        }
    }

    public void CreateInScene()
    {
        if (!gridsInSceneIDs.Contains(id))
        {
            GameController.instance.StartCoroutine(CreateInSceneCouroutine());
        }
        else
        {
            ActivateInScene(true);
        }
    }

    public void RemoveFromScene()
    {
        if (gridsInSceneIDs.Contains(id))
        {
            Object.Destroy(gridObject);
            gridsInSceneIDs.Remove(id);
        }
    }

    private IEnumerator CreateInSceneCouroutine()
    {
        gridsInSceneIDs.Add(id);
        isActiveInScene = true;
        waitingToCreateGrid = true;
        while (coroutinesRunning || waitingToRemoveBody)
        {
            yield return null;
        }
        waitingToCreateGrid = false;
        coroutinesRunning = true;
        // Debug.Log("Started: " + id + " creation.");
        int maxSeed = GameController.instance.maxSeed;

        gridObject = Object.Instantiate(GameController.instance.gridPrefab, GameController.instance.grids.transform);
        gridObject.name = "" + id;
        gridObject.transform.position = new Vector3(GetCentre().x, GameController.instance.celestialBodyLayer, GetCentre().y);

        // Add the stars background
        numStars = sysRand.Next(10, 100);
        Vector3 starPos;
        for (int i = 0; i < numStars; i++)
        {
            starPos = new Vector3(((float)sysRand.NextDouble() - 0.5f) * size.x, GameController.instance.starLayer, ((float)sysRand.NextDouble() - 0.5f) * size.y); ;
            GameObject newStar = Object.Instantiate(GameController.instance.starPrefab, gridObject.transform.position + starPos, Quaternion.Euler(90, 0, 0), gridObject.transform.Find("Stars"));
            Material newStarMat = newStar.GetComponent<MeshRenderer>().material;
            newStarMat.SetFloat("seed", sysRand.Next(0, maxSeed));
            float h = (float)sysRand.NextDouble();
            float s = (float)sysRand.NextDouble() * 0.3f;
            float v = 1;
            newStarMat.SetColor("colour", Color.HSVToRGB(h, s, v));
        }

        // Optionally add some nebulae
        int numNebulae = sysRand.Next(0, 4);
        Vector3 nebulaPosition = gridObject.transform.position + new Vector3(((float)sysRand.NextDouble() - 0.5f) * size.x, GameController.instance.nebulaLayer, ((float)sysRand.NextDouble() - 0.5f) * size.y);
        Quaternion nebulaRotation;
        for (int i = 0; i < numNebulae; i++)
        {
            nebulaPosition -= new Vector3(0, 0.01f, 0);
            nebulaRotation = Quaternion.Euler(90, 0, (float)sysRand.NextDouble() * 360);
            GameObject nebulae = Object.Instantiate(GameController.instance.nebulaCloud, nebulaPosition, nebulaRotation, gridObject.transform.Find("Nebulae"));
            nebulae.transform.localScale = new Vector3(40, 40);
            Material nebulaeMat = nebulae.GetComponent<MeshRenderer>().material;
            nebulaeMat.SetFloat("seed", sysRand.Next(0, maxSeed));
            nebulaeMat.SetFloat("brightness", (float)sysRand.NextDouble());
            float h = (float)sysRand.NextDouble();
            float s = 1 - (float)sysRand.NextDouble() * (1 - 0.5f);
            float v = 1;
            nebulaeMat.SetColor("colour", Color.HSVToRGB(h, s, v));
            nebulaeMat.SetTexture("cloudTexture", GameController.instance.cloudTexture);
        }

        // Add the black background
        Transform background = Object.Instantiate(GameController.instance.blackBackground, gridObject.transform).transform;
        background.position += new Vector3(0, GameController.instance.backgroundLayer, 0);
        background.localScale = new Vector3(size.x, size.y, 1);

        // Add the bodies
        for (int i = 0; i < celestialBodys.Count; i++)
        {
            celestialBodys[i].CreateInScene();
            yield return null;
        }

        // GenerateGridMesh();

        coroutinesRunning = false;
    }

    public static MyGrid GenerateGridAndCreateInScene(Vector2Int id)
    {
        MyGrid newGrid = GenerateGrid(id);
        newGrid.CreateInScene();
        return newGrid;
    }

    public static MyGrid GenerateGrid(Vector2Int id)
    {
        if (GridExists(id))
        {
            return GetGrid(id);
        }
        else
        {
            return new MyGrid(id);
        }
    }

    public MyGrid NextGrid()
    {
        return GenerateGrid(nextID);
    }

    public MyGrid NextGridWithBlackHole()
    {
        MyGrid nextGrid = GenerateGrid(nextID);
        for (int i = 0; i < 100; i++)
        {
            if (nextGrid.FirstBlackHole() != null)
            {
                Debug.Log(nextGrid.id);
                return nextGrid;
            }
            Debug.Log("Not " + nextGrid.id);
            nextGrid = nextGrid.NextGrid();
        }
        Debug.Log("Really? 100 in a row with no Black Hole?");
        return null;
    }

    public void ActivateInScene(bool activate)
    {
        if (gridObject != null)
        {
            gridObject.SetActive(activate);
            isActiveInScene = activate;
            //Debug.Log(id + " " + (activate ? "" : "de") + "activated");
        }
        else
        {
            Debug.Log("Grid object " + id + " not yet created.");
            GameController.instance.StartCoroutine(ActivateInSceneCoroutine(activate));
        }
    }

    public IEnumerator ActivateInSceneCoroutine(bool activate)
    {
        float time = 0;
        while (gridObject == null && time < 2)
        {
            time += Time.deltaTime;
            yield return null;
        }
        if (gridObject != null)
        {
            Debug.Log((activate ? "" : "De") + "activation for " + id + " successful.");
            ActivateInScene(activate);
        }
        else
        {
            Debug.Log(activate ? "" : "De" + "activation for " + id + " failed.");
        }

    }

    public static bool GridExists(Vector2Int id)
    {
        return allGrids.FirstOrDefault(o => o.id == id) != null;
    }

    public CelestialBody FirstBlackHole()
    {
        return celestialBodys.FirstOrDefault(o => o.bodyType == CelestialBody.BodyType.BlackHole);
    }

    public static void ClearAllGrids()
    {
        coroutinesRunning = false;
        foreach(Transform child in GameController.instance.grids.transform)
        {
            Object.Destroy(child.gameObject);
        }
        allGrids = new List<MyGrid>();
        gridsInSceneIDs = new List<Vector2Int>();
        isFirstBody = true;
    }

    public void RemoveGridFromMemory()
    {

    }
}
