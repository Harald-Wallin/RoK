using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "Building Roof Profile",
    menuName = "RÖK/Building/Roof Profile"
)]
public sealed class BuildingRoofProfile : ScriptableObject
{
    [Header("Rendering")]

    [SerializeField]
    private string sortingLayerName = "Default";

    [Header("East-West Roof Art")]

    [FormerlySerializedAs("repeatAlongX")]
    [SerializeField]
    private BuildingRoofArtSet eastWest = new();

    [Header("North-South Roof Art")]

    [FormerlySerializedAs("repeatAlongY")]
    [SerializeField]
    private BuildingNorthSouthRoofArtSet northSouth = new();

    public string SortingLayerName => sortingLayerName;

    public BuildingRoofArtSet EastWest => eastWest;
    public BuildingNorthSouthRoofArtSet NorthSouth => northSouth;
}
