using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class CelestialBody
{
    // Body constants
    public float coreRadius { get; }
    public float orbitRadius { get; }
    public float gravityFactor { get; }
    public float destructionFactor { get; }
    public float explosionRadius { get; }
    public Vector2 position { get; }
    public GameObject coreObject { get; }
    public PlanetRings planetRings { get; }
    public Grid grid { get; }
    public static float orbitRingWidth = 0.25f;

    // Derived parameters
    public bool acceptsOrbit
    {
        get
        {
            return orbitRadius != 0;
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
    public Vector2 absolutePosition
    {
        get
        {
            return Grid.GetGridOrigin(grid) + position;
        }
    }

    // Variables
    public float currentDestruction;
    public float currentGravity;

    public CelestialBody(float orbitRadius, float coreRadius, float gravityFactor, float destructionFactor, float explosionRadius, Vector2 position, GameObject coreObject, PlanetRings planetRings, Grid grid)
    {
        this.orbitRadius = orbitRadius;
        this.coreRadius = coreRadius;
        this.gravityFactor = gravityFactor;
        this.destructionFactor = destructionFactor;
        this.explosionRadius = explosionRadius;
        this.position = position;
        this.coreObject = coreObject;
        this.planetRings = planetRings;
        this.grid = grid;

        currentDestruction = 0;
        currentGravity = 0;
    }
}
