using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Grid
{
    public static List<Grid> allGrids = new List<Grid>();
    public static float minMult = 0.1f;
    public static float maxMult = 0.5f;
    public static int maxGenAttempts = 20;
    public static int maxCelestialBodys = 20;
    public static float celestialBodyBuffer = 5;
    // public static Vector2 size = new Vector2(40, 40);
    public static Vector2 size = new Vector2(16f * 40f / 9f, 40);

    public Vector2Int id;
    public List<CelestialBody> celestialBodys = new List<CelestialBody>();
    public float minCelestialBodySize;
    public float maxCelestialBodySize;
    public GameObject gridObject;

    public int numStars;

    public Grid(Vector2Int id)
    {
        this.id = id;
        allGrids.Add(this);
        float minMaxRef = Mathf.Min(size.x, size.y);
        minCelestialBodySize = minMaxRef * minMult / 2;
        maxCelestialBodySize = minMaxRef * maxMult / 2;

        gridObject = Object.Instantiate(GameController.instance.gridPrefab, GameController.instance.grids.transform);
        gridObject.name = "Grid " + id;
        gridObject.transform.position = new Vector3(GetGridCentre(this).x, 0, GetGridCentre(this).y);

        // Add the stars background
        numStars = Random.Range(10, 100);
        for (int i = 0; i < numStars; i++)
        {
            GameObject newStar = Object.Instantiate(GameController.instance.starPrefab, gridObject.transform.position + new Vector3(Random.Range(-size.x / 2, size.x / 2), 0, Random.Range(-size.y / 2, size.y / 2)), Quaternion.Euler(90, 0, 0), gridObject.transform.Find("Stars"));
            newStar.GetComponent<MeshRenderer>().material.SetFloat("seed", Random.Range(0f,1000f));
        }

        // Optionally add some nebulae
        int rand = Random.Range(0, 4);
        Vector3 nebulaPosition = gridObject.transform.position + new Vector3(Random.Range(-size.x / 2, size.x / 2), 0, Random.Range(-size.y / 2, size.y / 2));
        for (int i = 0; i < rand; i++)
        {
            nebulaPosition -= new Vector3(0, 0.01f, 0);
            GameObject nebulae = Object.Instantiate(GameController.instance.nebulaCloud, nebulaPosition, Quaternion.Euler(90, 0, Random.Range(0, 360)), gridObject.transform.Find("Nebulae"));
            nebulae.transform.localScale = new Vector3(40, 40);
            Material nebulaeMat = nebulae.GetComponent<MeshRenderer>().material;
            nebulaeMat.SetFloat("seed", Random.Range(0f, 10000f));
            nebulaeMat.SetFloat("brightness", Random.Range(0f, 1f));
            nebulaeMat.SetColor("colour", Random.ColorHSV(0f, 1f, 0.5f, 1f, 1f, 1f));
            nebulaeMat.SetTexture("cloudTexture", GameController.instance.cloudTexture);
            //nebulaeMat.SetTexture();
        }

        // Add the black background
        Object.Instantiate(GameController.instance.blackBackground, gridObject.transform);

        // GenerateGridMesh();

        // Set necessary params if this is the first Grid
        if (id == new Vector2Int(0, 0))
        {
            AddFirstCelestialBody();
        }

        GenerateCelestialBodys();
    }

    // Returns the Grid and all adjacent Grids (including diagonals)
    public static List<Grid> GetSurroundingGrids(Vector2Int id)
    {
        List<Grid> returnGrids = new List<Grid>();
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
    public static List<Grid> GetSurroundingGrids(Grid grid)
    {
        return GetSurroundingGrids(grid.id);
    }

    // Get the Grid at the given coordinates
    public static Grid GetGrid(Vector2Int id)
    {
        for (int i = 0; i < allGrids.Count; i++) 
        {
            if (allGrids[i].id == id)
            {
                return allGrids[i];
            }
        }
        return null;
    }

    // Get the Grid which contains the given Vector2 position
    public static Grid GetGrid(Vector2 position)
    {
        return GetGrid(GetGridCoords(position));
    }

    // Get the coordinates of the Grid which contains the given Vector2 position
    public static Vector2Int GetGridCoords(Vector2 position)
    {
        int idX = Mathf.RoundToInt(position.x / size.x);
        int idY = Mathf.RoundToInt(position.y / size.y);
        return new Vector2Int(idX, idY);
    }

    // Get the Vector2 position of the bottom-left corner of the Grid
    public static Vector2 GetGridOrigin(Grid grid)
    {
        float x = -size.x / 2 + grid.id.x * size.x;
        float y = -size.y / 2 + grid.id.y * size.y;
        return new Vector2(x, y);
    }

    // Get the Vector2 position of the centre of the Grid
    public static Vector2 GetGridCentre(Grid grid)
    {
        float x = grid.id.x * size.x;
        float y = grid.id.y * size.y;
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
                corners[i, j] = GetGridOrigin(this) + offset * size;
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
        float newOrbitRadius = Random.Range(minCelestialBodySize, maxCelestialBodySize);
        Vector2 newPosition = new Vector2(Random.Range(0, size.x), Random.Range(0, size.y));

        Vector2 newCelestialBodyPosition = GetGridOrigin(this) + newPosition;

        // Check adjacent grids for overlapping celestialBody orbits before creating CelestiaBody
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                Vector2Int offset = new Vector2Int(i, j);
                Grid currentGrid = GetGrid(id + offset);
                if (currentGrid != null)
                {
                    Vector2 gridOrigin = GetGridOrigin(currentGrid);
                    for (int k = 0; k < currentGrid.celestialBodys.Count; k++)
                    {
                        CelestialBody existingCelestialBody = currentGrid.celestialBodys[k];
                        Vector2 existingCelestialBodyPosition = gridOrigin + existingCelestialBody.position;
                        float existingOrbitRadius = existingCelestialBody.orbitRadius;
                        float distance = Vector2.Distance(existingCelestialBodyPosition, newCelestialBodyPosition);
                        bool isOverlapping = distance < (existingOrbitRadius + newOrbitRadius + celestialBodyBuffer);
                        if (isOverlapping)
                        {
                            return false;
                        }
                    }
                }
            }
        }

        GameObject newCoreObject = Object.Instantiate(GameController.instance.celestialBodyPrefab, new Vector3(newCelestialBodyPosition.x, 0, newCelestialBodyPosition.y), Quaternion.identity, gridObject.transform.Find("CelestialBodys"));
        CelestialBody newCelestialBody = new CelestialBody(newOrbitRadius, newOrbitRadius / 3, 0, 0, 0, newPosition, newCoreObject, new PlanetRings(), this);

        celestialBodys.Add(newCelestialBody);
        newCelestialBody.coreObject.transform.Find("Orbit").localScale = new Vector3(newOrbitRadius * 2 + CelestialBody.orbitRingWidth / 2, newOrbitRadius * 2 + CelestialBody.orbitRingWidth / 2, 1);
        newCelestialBody.coreObject.transform.Find("Body").localScale = newCelestialBody.coreObject.transform.Find("Orbit").localScale / 3;
        newCelestialBody.coreObject.transform.Find("Body").rotation = Quaternion.Euler(90f, Random.Range(0f,180f), 0f);
        newCelestialBody.coreObject.transform.Find("Orbit").GetComponent<MeshRenderer>().material.SetFloat("haloSize", CelestialBody.orbitRingWidth);

        Material bodyMat = newCelestialBody.coreObject.transform.Find("Body").GetComponent<MeshRenderer>().material;

        bodyMat.SetFloat("freqRandSeed", Random.Range(0f, 1000f));
        bodyMat.SetFloat("posRandSeed", Random.Range(0f, 1000f));
        bodyMat.SetFloat("stripeFrequency", Random.Range(2f, 15f));
        bodyMat.SetFloat("noiseSize", Random.Range(0f, 1f));
        bodyMat.SetFloat("seed", Random.Range(0f, 1000f));

        bodyMat.SetColor("colour1", Random.ColorHSV(0f, 1f, 0f, 1f, 0.3f, 1f, 1f, 1f));
        bodyMat.SetColor("colour2", Random.ColorHSV(0f, 1f, 0f, 1f, 0.3f, 1f, 1f, 1f));

        // Add the rings
        for (int i = 0; i < newCelestialBody.planetRings.numRings; i++)
        {
            GameObject newRing = Object.Instantiate(GameController.instance.ringPrefab, newCelestialBody.coreObject.transform.Find("Body").Find("Rings"));
            Material ringMat = newRing.GetComponent<MeshRenderer>().material;
            ringMat.SetFloat("slant", newCelestialBody.planetRings.ringSlant);
            ringMat.SetFloat("ringStart", newCelestialBody.planetRings.ringStart + i * (newCelestialBody.planetRings.ringWidth + newCelestialBody.planetRings.gapWidth));
            ringMat.SetFloat("ringEnd", newCelestialBody.planetRings.ringStart + (i + 1) * newCelestialBody.planetRings.ringWidth + i * newCelestialBody.planetRings.gapWidth);
            ringMat.SetColor("colour", newCelestialBody.planetRings.ringColours[i]);
        }

        return true;
    }

    public void AddFirstCelestialBody()
    {
        float newOrbitRadius = 5;
        Vector2 newPosition = -GetGridOrigin(this);

        Vector2 newCelestialBodyPosition = GetGridOrigin(this) + newPosition;

        GameObject newCoreObject = Object.Instantiate(GameController.instance.celestialBodyPrefab, new Vector3(newCelestialBodyPosition.x, 0, newCelestialBodyPosition.y), Quaternion.identity, gridObject.transform.Find("CelestialBodys"));
        CelestialBody newCelestialBody = new CelestialBody(newOrbitRadius, newOrbitRadius / 3, 0, 0, 0, newPosition, newCoreObject, new PlanetRings(), this);

        celestialBodys.Add(newCelestialBody);
        newCelestialBody.coreObject.transform.Find("Orbit").localScale = new Vector3(newOrbitRadius * 2 + CelestialBody.orbitRingWidth / 2, newOrbitRadius * 2 + CelestialBody.orbitRingWidth / 2, 1);
        newCelestialBody.coreObject.transform.Find("Body").localScale = newCelestialBody.coreObject.transform.Find("Orbit").localScale / 3;
        newCelestialBody.coreObject.transform.Find("Body").rotation = Quaternion.Euler(90f, Random.Range(0f, 180f), 0f);
        newCelestialBody.coreObject.transform.Find("Orbit").GetComponent<MeshRenderer>().material.SetFloat("haloSize", CelestialBody.orbitRingWidth);

        Material bodyMat = newCelestialBody.coreObject.transform.Find("Body").GetComponent<MeshRenderer>().material;

        bodyMat.SetFloat("freqRandSeed", Random.Range(0f, 1000f));
        bodyMat.SetFloat("posRandSeed", Random.Range(0f, 1000f));
        bodyMat.SetFloat("stripeFrequency", Random.Range(2f, 15f));
        bodyMat.SetFloat("noiseSize", Random.Range(0f, 1f));
        bodyMat.SetFloat("seed", Random.Range(0f, 1000f));

        bodyMat.SetColor("colour1", Random.ColorHSV(0f, 1f, 0f, 1f, 0.3f, 1f, 1f, 1f));
        bodyMat.SetColor("colour2", Random.ColorHSV(0f, 1f, 0f, 1f, 0.3f, 1f, 1f, 1f));

        // Add the rings
        for (int i = 0; i < newCelestialBody.planetRings.numRings; i++)
        {
            GameObject newRing = Object.Instantiate(GameController.instance.ringPrefab, newCelestialBody.coreObject.transform.Find("Body").Find("Rings"));
            Material ringMat = newRing.GetComponent<MeshRenderer>().material;
            ringMat.SetFloat("slant", newCelestialBody.planetRings.ringSlant);
            ringMat.SetFloat("ringStart", newCelestialBody.planetRings.ringStart + i * (newCelestialBody.planetRings.ringWidth + newCelestialBody.planetRings.gapWidth));
            ringMat.SetFloat("ringEnd", newCelestialBody.planetRings.ringStart + (i + 1) * newCelestialBody.planetRings.ringWidth + i * newCelestialBody.planetRings.gapWidth);
            ringMat.SetColor("colour", newCelestialBody.planetRings.ringColours[i]);
        }

        CometBehaviour.instance.firstCelestialBody = newCelestialBody;
        CometBehaviour.instance.newGrid = this;
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
        }
        if (currentGenAttempts >= maxGenAttempts)
        {
            Debug.Log("Max attempts reached for Grid " + id + "\n");
        }
        else if (currentCelestialBodys >= maxCelestialBodys)
        {
            Debug.Log("Max celestialBodys generated for Grid " + id + "\n");
        }
    }
}
