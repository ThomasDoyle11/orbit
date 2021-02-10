using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class CelestialBody
{
    // Generation parameters
    public static float minGravityFactor = 0.1f;
    public static float maxGravityFactor = 0.5f;

    public static float minDestructionFactor = 0.1f;
    public static float maxDestructionFactor = 0.5f;

    public static float minExplosionRadius = 5f;
    public static float maxExplosionRadius = 50f;
    public static float explodeOnDestructionChance = 0.5f;

    public static float minCoreRatio = 0.2f;
    public static float maxCoreRatio = 0.5f;

    public static float minOrbitRadius = 3;
    public static float maxOrbitRadius = 10;

    public static float minStarRadius = 5;
    public static float maxStarRadius = 20;

    public static Dictionary<BodyType, float> bodyOdds = new Dictionary<BodyType, float>()
    {
        { BodyType.StandardPlanet, 10 },
        { BodyType.DestructablePlanet, 3 },
        { BodyType.UnstablePlanet, 3 },
        { BodyType.StandardStar, 0 },
        { BodyType.DestructableStar, 0 },
        { BodyType.BlackHole, 0.1f }
    };
    public static float totalBodyOdds;
    public static float BodyChance(BodyType bodyType)
    {
        return bodyOdds[bodyType] / totalBodyOdds;
    }
    public static BodyType RandomBodyType(int seed)
    {
        System.Random sysRand = new System.Random(seed);
        float lastChance = 0;
        float rand = (float)sysRand.NextDouble();
        BodyType[] allBodyTypes = CelestialBody.allBodyTypes;
        for (int i = 0; i < allBodyTypes.Length; i++)
        {
            lastChance += BodyChance(allBodyTypes[i]);
            if (rand <= lastChance)
            {
                //Debug.Log(rand);
                //Debug.Log(lastChance);
                //Debug.Log(allBodyTypes[i]);
                return allBodyTypes[i];
            }
        }
        Debug.Log("We should never get here.");
        return BodyType.StandardPlanet;
    }

    // Body constants
    public float coreRadius { get; }
    public float orbitRadius { get; }
    public float gravityFactor { get; }
    public float destructionFactor { get; }
    public float explosionRadius { get; }
    public string name { get; }
    public Vector2 position { get; }
    public float rotation { get; }
    private GameObject bodyObject { get; set; }
    public PlanetRings planetRings { get; }
    public MyGrid grid { get; }
    public bool isInstantiated { get; }
    public static float orbitRingWidth = 0.25f;

    // For generating other physical things
    public int seed;

    public enum BodyType
    {
        StandardPlanet,
        UnstablePlanet,
        DestructablePlanet,
        StandardStar,
        DestructableStar,
        BlackHole
    }
    public BodyType bodyType;
    public static BodyType[] allBodyTypes
    {
        get
        {
            return (BodyType[])System.Enum.GetValues(typeof(BodyType));
        }
    }

    public bool isPlanet
    {
        get
        {
            return bodyType == BodyType.StandardPlanet || bodyType == BodyType.UnstablePlanet || bodyType == BodyType.DestructablePlanet;
        }
    }

    public enum BodyDesign
    {
        WaveyPlanet,
        NoisyPlanet,
        StandardStar,
        DestructableStar,
        BlackHole
    }
    public BodyDesign bodyDesign;
    public static BodyDesign[] allBodyDesigns
    {
        get
        {
            return (BodyDesign[])System.Enum.GetValues(typeof(BodyDesign));
        }
    }
    public static BodyDesign[] allPlanetBodyDesigns
    {
        get
        {
            return new BodyDesign[] { BodyDesign.WaveyPlanet, BodyDesign.NoisyPlanet };
        }
    }
    public static BodyDesign[] allStarBodyDesigns
    {
        get
        {
            return new BodyDesign[] {  };
        }
    }

    public Material coreMat
    {
        get
        {
            if (bodyDesign == BodyDesign.WaveyPlanet)
            {
                return GameController.instance.waveyPlanetMat;
            }
            else if(bodyDesign == BodyDesign.NoisyPlanet)
            {
                return GameController.instance.noisyPlanetMat;
            }
            else if (bodyDesign == BodyDesign.StandardStar)
            {
                return GameController.instance.standardsStarMat;
            }
            else if (bodyDesign == BodyDesign.DestructableStar)
            {
                return GameController.instance.destructableStarMat;
            }
            else if (bodyDesign == BodyDesign.BlackHole)
            {
                return GameController.instance.blackHoleMat;
            }
            else
            {
                return null;
            }
        }
    }

    public GameObject bodyPrefab
    {
        get
        {
            if (bodyDesign == BodyDesign.WaveyPlanet)
            {
                return GameController.instance.waveyPlanetPrefab;
            }
            else if (bodyDesign == BodyDesign.NoisyPlanet)
            {
                return GameController.instance.noisyPlanetPrefab;
            }
            else if (bodyDesign == BodyDesign.BlackHole)
            {
                return GameController.instance.blackHolePrefab;
            }
            else
            {
                return null;
            }
            //return GameController.instance.waveyPlanetPrefab;
        }
    }

    // Derived parameters
    public bool acceptsOrbit
    {
        get
        {
            return orbitRadius > 0;
        }
    }
    public bool explodesOnDestruction
    {
        get
        {
            return explosionRadius > 0;
        }
    }
    public bool isDestructible
    {
        get
        {
            return destructionFactor > 0;
        }
    }
    public bool isStableOrbit
    {
        get
        {
            return gravityFactor == 0;
        }
    }
    public bool hasRings
    {
        get
        {
            return planetRings.numRings > 0;
        }
    }
    public float outerRadius
    {
        get
        {
            return Mathf.Max(orbitRadius, coreRadius);
        }
    }
    public float innerRadius
    {
        get
        {
            return orbitRadius != coreRadius ? Mathf.Min(orbitRadius, coreRadius) : 0;
        }
    }
    public Vector2 absolutePosition
    {
        get
        {
            return grid.GetOrigin() + position;
        }
    }
    public Vector3 absolutePosition3D
    {
        get
        {
            return new Vector3(absolutePosition.x, GameController.instance.celestialBodyLayer, absolutePosition.y);
        }
    }

    // Variables
    public float currentDestruction;
    public float currentGravity;

    // Type dependent - otherwise null
    public BlackHole blackHole
    {
        get
        {
            if (bodyObject == null)
            {
                Debug.Log(name);
                Debug.Log(grid.id);
                for (int i = 0; i < grid.celestialBodys.Count; i++)
                {
                    Debug.Log(grid.celestialBodys[i].name);
                }
            }
            return bodyObject.transform.Find("Body").GetComponent<BlackHole>();
        }
    }
    public bool isReversing;

    public void CreateInScene()
    {
        // Debug.Log("Created: " + grid.id + ", " + name);

        int maxSeed = GameController.instance.maxSeed;
        System.Random sysRand = new System.Random(seed);

        bodyObject = Object.Instantiate(bodyPrefab, absolutePosition3D, Quaternion.identity, grid.celestialBodysObject);
        
        bodyObject.GetComponent<CelestialBodyDebug>().celBody = this;

        bodyObject.name = name;

        if (acceptsOrbit)
        {
            bodyObject.transform.Find("Orbit").localScale = new Vector3(orbitRadius * 2 + orbitRingWidth / 2, orbitRadius * 2 + orbitRingWidth / 2, 1);
            Material haloMat = bodyObject.transform.Find("Orbit").GetComponent<MeshRenderer>().material;
            haloMat.SetFloat("haloSize", orbitRingWidth);
            if (acceptsOrbit)
            {
                if (isDestructible)
                {
                    haloMat.SetColor("colour", GameController.instance.destructableOrbitColour);
                }
                else if (!isStableOrbit)
                {
                    haloMat.SetColor("colour", GameController.instance.unstableOrbitColour);
                }
                else
                {
                    haloMat.SetColor("colour", GameController.instance.standardOrbitColour);
                }
            }
        }
        else
        {
            bodyObject.transform.Find("Orbit").gameObject.SetActive(false);
        }

        bodyObject.transform.Find("Body").localScale = new Vector3(coreRadius * 2, coreRadius * 2, 1);
        bodyObject.transform.Find("Body").rotation = Quaternion.Euler(90f, rotation, 0f);

        //bodyObject.transform.Find("Body").GetComponent<MeshRenderer>().material = coreMat;
        Material bodyMat = bodyObject.transform.Find("Body").GetComponent<MeshRenderer>().material;

        bodyMat.SetFloat("freqRandSeed", sysRand.Next(0, maxSeed));
        bodyMat.SetFloat("posRandSeed", sysRand.Next(0, maxSeed));
        bodyMat.SetFloat("stripeFrequency", (float)sysRand.NextDouble() * 13 + 2);
        bodyMat.SetFloat("noiseSize", (float)sysRand.NextDouble());
        bodyMat.SetFloat("seed", sysRand.Next(0, maxSeed));
        if (bodyType == BodyType.BlackHole)
        {
            blackHole.Initialise(sysRand.Next());
        }

        float h = (float)sysRand.NextDouble();
        float s = (float)sysRand.NextDouble();
        float v = (float)sysRand.NextDouble() * 0.7f + 0.3f;
        bodyMat.SetColor("colour1", Color.HSVToRGB(h, s, v));
        h = (float)sysRand.NextDouble();
        s = (float)sysRand.NextDouble();
        v = (float)sysRand.NextDouble() * 0.7f + 0.3f;
        bodyMat.SetColor("colour2", Color.HSVToRGB(h, s, v));

        if (isPlanet)
        {
            // Add the rings
            for (int i = 0; i < planetRings.numRings; i++)
            {
                GameObject newRing = Object.Instantiate(GameController.instance.ringPrefab, bodyObject.transform.Find("Body").Find("Rings"));
                newRing.transform.position += new Vector3(0, GameController.instance.celestialBodyLayer, 0);
                Material ringMat = newRing.GetComponent<MeshRenderer>().material;
                ringMat.SetFloat("slant", planetRings.ringSlant);
                ringMat.SetFloat("ringStart", planetRings.ringStart + i * (planetRings.ringWidth + planetRings.gapWidth));
                ringMat.SetFloat("ringEnd", planetRings.ringStart + (i + 1) * planetRings.ringWidth + i * planetRings.gapWidth);
                ringMat.SetColor("colour", planetRings.ringColours[i]);
            }
        }
        else
        {
            bodyObject.transform.Find("Body").Find("Rings").gameObject.SetActive(false);
        }
    }

    public bool ExistsInScene()
    {
        return bodyObject != null;
    }

    public void RemoveFromScene()
    {
        Object.Destroy(bodyObject);
    }

    // Planets
    public static CelestialBody Planet(BodyType bodyType, float gravityFactor, float destructionFactor, float explosionRadius, MyGrid grid, int seed)
    {
        System.Random sysRand = new System.Random(seed);

        float orbitRadius = minOrbitRadius + (float)sysRand.NextDouble() * (maxOrbitRadius - minOrbitRadius);
        float coreRadius = orbitRadius * (minCoreRatio + (float)sysRand.NextDouble() * (maxCoreRatio - minCoreRatio));
        Vector2 position = new Vector2((float)sysRand.NextDouble() * GameController.instance.gridSize.x, (float)sysRand.NextDouble() * GameController.instance.gridSize.y);
        float rotation = (float)sysRand.NextDouble() * 360;

        return new CelestialBody(bodyType, orbitRadius, coreRadius, gravityFactor, destructionFactor, explosionRadius, position, rotation, new PlanetRings(sysRand.Next()), grid, "#" + sysRand.Next(), allPlanetBodyDesigns[sysRand.Next(allPlanetBodyDesigns.Length)], sysRand.Next());
    }

    public static CelestialBody UnstablePlanet(float gravityFactor, MyGrid grid, int seed)
    {
        return Planet(BodyType.UnstablePlanet, gravityFactor, 0, 0, grid, seed);
    }

    public static CelestialBody DestructablePlanet(float destructionFactor, float explosionRadius, MyGrid grid, int seed)
    {
        return Planet(BodyType.DestructablePlanet, 0, destructionFactor, explosionRadius, grid, seed);
    }

    public static CelestialBody StandardPlanet(MyGrid grid, int seed)
    {
        return Planet(BodyType.StandardPlanet, 0, 0, 0, grid, seed);
    }

    public static CelestialBody RandomBody(MyGrid grid, int seed)
    {
        System.Random sysRand = new System.Random(seed);
        float rand = (float)sysRand.NextDouble();
        BodyType bodyType = RandomBodyType(sysRand.Next());
        if (bodyType == BodyType.UnstablePlanet || bodyType == BodyType.BlackHole)
        {
            float gravityFactor = minGravityFactor + (float)sysRand.NextDouble() * (maxGravityFactor - minGravityFactor);
            if (bodyType == BodyType.BlackHole)
            {
                return BlackHole(gravityFactor, grid, seed);
            }
            else
            {
                return UnstablePlanet(gravityFactor, grid, seed);
            }
        }
        else if (bodyType == BodyType.DestructablePlanet || bodyType == BodyType.DestructableStar)
        {
            float destructionFactor = minDestructionFactor + (float)sysRand.NextDouble() * (maxDestructionFactor - minDestructionFactor);
            float explosionRadius;
            if (sysRand.NextDouble() <= explodeOnDestructionChance || bodyType == BodyType.DestructableStar)
            {
                explosionRadius = minExplosionRadius + (float)sysRand.NextDouble() * (maxExplosionRadius - minExplosionRadius);
            }
            else
            {
                explosionRadius = 0;
            }
            if (bodyType == BodyType.DestructableStar)
            {
                return DestructableStar(destructionFactor, explosionRadius, grid, seed);
            }
            else
            {
                return DestructablePlanet(destructionFactor, explosionRadius, grid, seed);
            }
        }
        else if (bodyType == BodyType.StandardPlanet)
        {
            return StandardPlanet(grid, seed);
        }
        else if (bodyType == BodyType.StandardStar)
        {
            return StandardStar(grid, seed);
        }
        else
        {
            return null;
        }
    }

    // Stars
    public static CelestialBody Star(BodyType bodyType, float gravityFactor, float destructionFactor, float explosionRadius, MyGrid grid, int seed)
    {
        System.Random sysRand = new System.Random(seed);

        float orbitRadius = 0;
        float coreRadius = minStarRadius + (float)sysRand.NextDouble() * (maxStarRadius - minStarRadius);
        Vector2 position = new Vector2((float)sysRand.NextDouble() * GameController.instance.gridSize.x, (float)sysRand.NextDouble() * GameController.instance.gridSize.y);
        float rotation = (float)sysRand.NextDouble() * 360;

        return new CelestialBody(bodyType, orbitRadius, coreRadius, gravityFactor, destructionFactor, explosionRadius, position, rotation, null, grid, "#" + sysRand.Next(), allStarBodyDesigns[sysRand.Next(allStarBodyDesigns.Length)], sysRand.Next());
    }

    public static CelestialBody StandardStar(MyGrid grid, int seed)
    {
        return Star(BodyType.StandardStar, 0, 0, 0, grid, seed);
    }

    public static CelestialBody DestructableStar(float destructionFactor, float explosionRadius, MyGrid grid, int seed)
    {
        return Star(BodyType.DestructableStar, 0, destructionFactor, explosionRadius, grid, seed);

    }

    public static CelestialBody BlackHole(float gravityFactor, MyGrid grid, int seed)
    {
        System.Random sysRand = new System.Random(seed);

        float orbitRadius = minOrbitRadius + (float)sysRand.NextDouble() * (maxOrbitRadius - minOrbitRadius);
        float coreRadius = orbitRadius;
        Vector2 position = new Vector2((float)sysRand.NextDouble() * GameController.instance.gridSize.x, (float)sysRand.NextDouble() * GameController.instance.gridSize.y);
        float rotation = (float)sysRand.NextDouble() * 360;
        return new CelestialBody(BodyType.BlackHole, orbitRadius, coreRadius, gravityFactor, 0, 0, position, rotation, null, grid, "#" + sysRand.Next(), BodyDesign.BlackHole, seed);
    }

    private CelestialBody(BodyType bodyType, float orbitRadius, float coreRadius, float gravityFactor, float destructionFactor, float explosionRadius, Vector2 position, float rotation, PlanetRings planetRings, MyGrid grid, string name, BodyDesign bodyDesign, int seed)
    {
        this.bodyType = bodyType;
        this.orbitRadius = orbitRadius;
        this.coreRadius = coreRadius;
        this.gravityFactor = gravityFactor;
        this.destructionFactor = destructionFactor;
        this.explosionRadius = explosionRadius;
        this.position = position;
        this.rotation = rotation;
        this.planetRings = planetRings;
        this.grid = grid;
        this.name = name;
        this.bodyDesign = bodyDesign;
        this.seed = seed;

        currentDestruction = 0;
        currentGravity = 0;
    }
}
